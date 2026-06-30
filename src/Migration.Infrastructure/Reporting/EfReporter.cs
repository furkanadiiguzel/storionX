using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using EvStorionX.Application.Abstractions;
using EvStorionX.Application.Dto;
using EvStorionX.Domain.Entities;
using EvStorionX.Infrastructure.Persistence;

namespace EvStorionX.Infrastructure.Reporting;

/// <summary>EF Core implementation of <see cref="IReporter"/>.</summary>
public sealed class EfReporter(
    IDbContextFactory<MigrationDbContext> dbFactory,
    IStorionXClient storionXClient,
    TimeProvider timeProvider) : IReporter
{
    // Thread-safe: each call creates an independent DbContext from the pool.
    public async Task RecordAsync(AuditEvent ev, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.AuditEvents.Add(ev);
        await db.SaveChangesAsync(ct);
    }

    public async Task<RunSummary> BuildSummaryAsync(Guid runId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var runEvents = await db.AuditEvents
            .AsNoTracking()
            .Where(e => e.RunId == runId
                        && (e.EventType == "RunStarted" || e.EventType == "RunCompleted"))
            .OrderBy(e => e.TimestampUtc)
            .ToListAsync(ct);

        var startedAt  = runEvents.FirstOrDefault(e => e.EventType == "RunStarted")?.TimestampUtc
                         ?? DateTime.UtcNow;
        var finishedAt = runEvents.FirstOrDefault(e => e.EventType == "RunCompleted")?.TimestampUtc;

        long totalArchives = 0, totalItems = 0, migrated = 0,
             alreadyPresent = 0, orphaned = 0, skipped = 0, failed = 0;

        var completedPayload = runEvents.FirstOrDefault(e => e.EventType == "RunCompleted")?.Payload;
        if (completedPayload is not null)
        {
            using var doc  = JsonDocument.Parse(completedPayload);
            var root       = doc.RootElement;
            totalArchives  = GetLong(root, "totalArchives");
            totalItems     = GetLong(root, "totalItems");
            migrated       = GetLong(root, "migrated");
            alreadyPresent = GetLong(root, "alreadyPresent");
            orphaned       = GetLong(root, "orphaned");
            skipped        = GetLong(root, "skipped");
            failed         = GetLong(root, "failed");
        }

        return new RunSummary(runId, startedAt, finishedAt, (int)totalArchives, totalItems,
            migrated, alreadyPresent, orphaned, skipped, failed,
            new Dictionary<string, ArchiveSummary>());
    }

    public async Task<ReconciliationReport> ReconcileAsync(Guid runId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var dbMigrated = await db.AuditEvents
            .Where(e => e.RunId == runId && e.EventType == "ItemMigrated")
            .LongCountAsync(ct);
        var dbAlreadyPresent = await db.AuditEvents
            .Where(e => e.RunId == runId && e.EventType == "ItemAlreadyPresent")
            .LongCountAsync(ct);
        var dbTotal = dbMigrated + dbAlreadyPresent;

        var stats         = await storionXClient.GetStatsAsync(ct);
        var storionXTotal = stats?.TotalIngested ?? -1L;

        var missing    = new List<string>();
        var unexpected = new List<string>();

        if (storionXTotal < 0)
            missing.Add("storionX stats unavailable — cannot reconcile");
        else if (dbTotal > storionXTotal)
            missing.Add($"{dbTotal - storionXTotal} item(s) recorded as migrated locally but not confirmed in storionX");
        else if (storionXTotal > dbTotal)
            unexpected.Add($"{storionXTotal - dbTotal} item(s) present in storionX but exceed local migration records for run {runId}");

        return new ReconciliationReport(
            RunId:              runId,
            GeneratedAtUtc:     timeProvider.GetUtcNow().UtcDateTime,
            MissingInTarget:    missing,
            MismatchedInTarget: [],
            UnexpectedInTarget: unexpected,
            IsClean:            missing.Count == 0 && unexpected.Count == 0);
    }

    private static long GetLong(JsonElement root, string property) =>
        root.TryGetProperty(property, out var el) ? el.GetInt64() : 0L;
}
