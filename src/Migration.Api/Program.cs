using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using EvStorionX.Application.Abstractions;
using EvStorionX.Application.Common;
using EvStorionX.Application.Mapping;
using EvStorionX.Application.Pipeline;
using EvStorionX.Application.Rehydration;
using EvStorionX.Application.Transform;
using EvStorionX.Infrastructure.MockEv;
using EvStorionX.Infrastructure.Persistence;
using EvStorionX.Infrastructure.StorionXClient;
using EvStorionX.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Options ───────────────────────────────────────────────────────────────────
builder.Services.Configure<OrchestratorOptions>(builder.Configuration.GetSection("Orchestrator"));
builder.Services.Configure<StorionXClientOptions>(builder.Configuration.GetSection("StorionXClient"));
builder.Services.Configure<FilePartReaderOptions>(builder.Configuration.GetSection("FilePartReader"));
builder.Services.Configure<JsonIdentityMapOptions>(builder.Configuration.GetSection("JsonIdentityMap"));
builder.Services.Configure<PolicyOptions>(builder.Configuration.GetSection("PolicyOptions"));
builder.Services.Configure<TransformerOptions>(builder.Configuration.GetSection("Transformer"));

// ── Infrastructure ────────────────────────────────────────────────────────────
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is required."));

builder.Services.AddStorionXClient();

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<RunTracker>();
builder.Services.AddScoped<ActiveRunContext>();

// Scoped IPostConfigureOptions reads ActiveRunContext to inject RunId before orchestrator is built.
builder.Services.AddScoped<IPostConfigureOptions<OrchestratorOptions>>(sp =>
{
    var ctx = sp.GetRequiredService<ActiveRunContext>();
    return new PostConfigureOptions<OrchestratorOptions>(Options.DefaultName, opts =>
    {
        if (ctx.RunId != Guid.Empty)
            opts.RunId = ctx.RunId;
    });
});

builder.Services.AddScoped<IDiscovery, EfDiscovery>();
builder.Services.AddSingleton<IIdentityMap, JsonIdentityMap>();
builder.Services.AddScoped<IPartReader, FilePartReader>();
builder.Services.AddTransient<IPolicyEngine, PolicyEngine>();
builder.Services.AddSingleton<ICachePolicy<string, byte[]>>(new LruCache<string, byte[]>(1000));
builder.Services.AddScoped<IRehydrator, Rehydrator>();
builder.Services.AddTransient<ITransformer, EvToStorionXTransformer>();
builder.Services.AddScoped<MigrationOrchestrator>();

// ── OpenAPI ───────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy        = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull;
    o.SerializerOptions.PropertyNameCaseInsensitive = true;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// ── POST /runs ────────────────────────────────────────────────────────────────
// Start a new migration run. Body: { "runId": "<guid>" } is optional;
// omit to get a freshly generated RunId. Supplying a previous RunId resumes from checkpoint.
app.MapPost("/runs", (StartRunRequest? req, RunTracker tracker, IServiceScopeFactory scopeFactory) =>
{
    var runId = req?.RunId is { } rid && rid != Guid.Empty ? rid : Guid.NewGuid();
    var cts   = new CancellationTokenSource();

    var scope = scopeFactory.CreateScope();

    async Task<EvStorionX.Application.Dto.RunSummary> RunAsync()
    {
        try
        {
            // Set RunId in scope context BEFORE resolving orchestrator (IOptionsSnapshot is lazy).
            var ctx          = scope.ServiceProvider.GetRequiredService<ActiveRunContext>();
            ctx.RunId        = runId;
            var orchestrator = scope.ServiceProvider.GetRequiredService<MigrationOrchestrator>();
            return await orchestrator.RunAsync(cts.Token);
        }
        finally
        {
            scope.Dispose();
        }
    }

    var entry = new RunEntry(runId, RunAsync(), cts);
    tracker.Add(entry);

    return Results.Accepted($"/runs/{runId}", new { runId });
})
.WithName("StartRun")
.WithSummary("Start a new migration run (or resume from checkpoint when RunId matches a prior run).");

// ── GET /runs/{id} ────────────────────────────────────────────────────────────
app.MapGet("/runs/{id:guid}", async (Guid id, RunTracker tracker) =>
{
    var entry = tracker.Get(id);
    if (entry is null) return Results.NotFound();

    EvStorionX.Application.Dto.RunSummary? summary = null;
    if (entry.Task.IsCompletedSuccessfully)
        summary = await entry.Task;

    return Results.Ok(new
    {
        runId     = id,
        status    = entry.Status.ToString(),
        startedAt = entry.StartedAt,
        summary,
    });
})
.WithName("GetRun")
.WithSummary("Poll the status of an in-progress or completed migration run.");

// ── POST /runs/{id}/resume ────────────────────────────────────────────────────
// Cancels the current task for this run, then restarts it using the same RunId
// so the orchestrator loads the existing checkpoint.
app.MapPost("/runs/{id:guid}/resume", async (Guid id, RunTracker tracker, IServiceScopeFactory scopeFactory) =>
{
    var entry = tracker.Get(id);
    if (entry is null) return Results.NotFound();

    // Graceful cancel + drain
    entry.Cts.Cancel();
    try { await entry.Task.ConfigureAwait(false); }
    catch { /* task may have faulted or been cancelled — expected */ }

    // Restart with the same RunId → orchestrator will LoadCheckpointAsync
    var cts   = new CancellationTokenSource();
    var scope = scopeFactory.CreateScope();

    async Task<EvStorionX.Application.Dto.RunSummary> ResumeAsync()
    {
        try
        {
            var ctx          = scope.ServiceProvider.GetRequiredService<ActiveRunContext>();
            ctx.RunId        = id;
            var orchestrator = scope.ServiceProvider.GetRequiredService<MigrationOrchestrator>();
            return await orchestrator.RunAsync(cts.Token);
        }
        finally
        {
            scope.Dispose();
        }
    }

    var newEntry = new RunEntry(id, ResumeAsync(), cts);
    tracker.Add(newEntry);

    return Results.Accepted($"/runs/{id}", new { runId = id });
})
.WithName("ResumeRun")
.WithSummary("Cancel the active task for a run and restart it from the last checkpoint.");

// ── GET /runs/{id}/audit?format=json|csv ──────────────────────────────────────
app.MapGet("/runs/{id:guid}/audit", async (
    Guid id,
    string? format,
    IDbContextFactory<MigrationDbContext> dbFactory,
    CancellationToken ct) =>
{
    await using var db = await dbFactory.CreateDbContextAsync(ct);
    var events = await db.AuditEvents
        .AsNoTracking()
        .Where(e => e.RunId == id)
        .OrderBy(e => e.TimestampUtc)
        .ToListAsync(ct);

    if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
    {
        var ms = new MemoryStream();
        await using (var writer = new StreamWriter(ms, leaveOpen: true))
        await using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            await csv.WriteRecordsAsync(events, ct);
        }
        ms.Position = 0;
        return Results.File(ms, "text/csv", $"audit-{id}.csv");
    }

    return Results.Ok(events);
})
.WithName("GetAudit")
.WithSummary("Stream audit events for a run. Use ?format=csv for CSV download.");

// ── GET /runs/{id}/reconciliation ─────────────────────────────────────────────
app.MapGet("/runs/{id:guid}/reconciliation", async (
    Guid id,
    IReporter reporter,
    CancellationToken ct) =>
{
    var report = await reporter.ReconcileAsync(id, ct);
    return Results.Ok(report);
})
.WithName("GetReconciliation")
.WithSummary("Compare local migration records against storionX /stats for discrepancies.");

// ── GET /runs ─────────────────────────────────────────────────────────────────
app.MapGet("/runs", (RunTracker tracker) =>
{
    var runs = tracker.All().Select(e => new
    {
        runId     = e.RunId,
        status    = e.Status.ToString(),
        startedAt = e.StartedAt,
    });
    return Results.Ok(runs);
})
.WithName("ListRuns")
.WithSummary("List all known migration runs (in-memory, resets on restart).");

// ── GET /archives ─────────────────────────────────────────────────────────────
app.MapGet("/archives", () => Results.Ok(Array.Empty<object>()))
.WithName("ListArchives")
.WithSummary("Placeholder: returns discovered EV archives.");

await app.RunAsync();

// ── Request body models ───────────────────────────────────────────────────────
internal sealed record StartRunRequest(Guid? RunId = null, bool? DryRun = null);

public partial class Program { }
