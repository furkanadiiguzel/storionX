using System.Text.Json;
using System.Text.Json.Serialization;
using EvStorionX.MockStorionX.Abstractions;
using EvStorionX.MockStorionX.Chaos;
using EvStorionX.MockStorionX.Handlers;
using EvStorionX.MockStorionX.Options;
using EvStorionX.MockStorionX.RateLimiting;
using EvStorionX.MockStorionX.Storage;
using Scalar.AspNetCore;
using Serilog;

// ── Bootstrap logger (used before DI is ready) ───────────────────────────────
#pragma warning disable CA1305 // Serilog Console sink does not expose a meaningful IFormatProvider
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();
#pragma warning restore CA1305

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, _, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .Enrich.FromLogContext());

    // ── OpenAPI ───────────────────────────────────────────────────────────────
    builder.Services.AddOpenApi();

    // ── Options ───────────────────────────────────────────────────────────────
    builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection("RateLimit"));
    builder.Services.Configure<ChaosOptions>(builder.Configuration.GetSection("Chaos"));

    // ── Application services ──────────────────────────────────────────────────
    builder.Services.AddSingleton<IStorionXStorage, InMemoryStorionXStorage>();
    builder.Services.AddSingleton<IChaosMonkey, RandomChaosMonkey>();
    builder.Services.AddSingleton<IRateLimiterFactory, TokenBucketRateLimiterFactory>();

    // ── JSON: camelCase property names ────────────────────────────────────────
    builder.Services.ConfigureHttpJsonOptions(o =>
    {
        o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        o.SerializerOptions.PropertyNameCaseInsensitive = true;
    });

    var app = builder.Build();

    // ── OpenAPI / Scalar UI (dev only) ────────────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseSerilogRequestLogging();

    // ── Endpoints ─────────────────────────────────────────────────────────────
    app.MapPost("/ingest", IngestHandler.HandleAsync)
       .WithName("Ingest")
       .WithSummary("Ingest an item into the mock storionX archive.")
       .WithDescription("Idempotent: replaying the same idempotencyKey always returns 200 without side effects.");

    app.MapGet("/health", () => Results.Ok("OK"))
       .WithName("Health")
       .WithSummary("Docker healthcheck endpoint.");

    app.MapGet("/stats", StatsHandler.HandleAsync)
       .WithName("Stats")
       .WithSummary("Return current ingest counters and dedup statistics.");

    app.MapPost("/admin/reset", AdminHandler.ResetAsync)
       .WithName("AdminReset")
       .WithSummary("Wipe all in-memory state (for integration test teardown).");

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "MockStorionX terminated unexpectedly.");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program { }
