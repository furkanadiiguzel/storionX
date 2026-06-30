using Microsoft.EntityFrameworkCore;
using EvStorionX.Application.Abstractions;
using EvStorionX.Domain.Entities;
using EvStorionX.Domain.Enums;
using EvStorionX.Domain.ValueObjects;
using EvStorionX.Infrastructure.Persistence;

namespace EvStorionX.Infrastructure.StateStore;

/// <summary>EF Core + MySQL implementation of <see cref="IStateStore"/>.</summary>
public sealed class EfStateStore(
    IDbContextFactory<MigrationDbContext> dbFactory,
    TimeProvider timeProvider) : IStateStore
{
    public async Task<bool> IsAlreadyMigratedAsync(IdempotencyKey key, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var keyStr = (string)key;
        return await db.MigrationRecords.AnyAsync(
            r => r.IdempotencyKey == keyStr
                 && (r.Status == MigrationStatus.Migrated || r.Status == MigrationStatus.AlreadyPresent),
            ct);
    }

    public async Task MarkMigratedAsync(IdempotencyKey key, string targetId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var keyStr = (string)key;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Key format: ev:<vaultStore>:<archiveId>:<itemId>
        var parts = keyStr.Split(':', 4);
        var archiveId = parts[2];
        var itemId = parts[3];

        // MySQL UPSERT: insert on first success; update counters on idempotent replay
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO migration_records
                 (idempotency_key, source_item_id, archive_id, target_archive, target_id,
                  status, extracted_at_utc, ingested_at_utc, attempt_count, last_error_code)
             VALUES
                 ({keyStr}, {itemId}, {archiveId}, '', {targetId},
                  'Migrated', {now}, {now}, 1, NULL)
             ON DUPLICATE KEY UPDATE
                 target_id       = VALUES(target_id),
                 status          = 'Migrated',
                 ingested_at_utc = VALUES(ingested_at_utc),
                 attempt_count   = attempt_count + 1,
                 last_error_code = NULL
             """, ct);
    }

    public async Task<RunCheckpoint?> LoadCheckpointAsync(Guid runId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.RunCheckpoints
            .AsNoTracking()
            .Where(c => c.RunId == runId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task SaveCheckpointAsync(RunCheckpoint checkpoint, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO run_checkpoints (run_id, phase, created_at_utc, metadata)
             VALUES ({checkpoint.RunId}, {checkpoint.Phase}, {checkpoint.CreatedAtUtc}, {checkpoint.Metadata})
             ON DUPLICATE KEY UPDATE
                 created_at_utc = VALUES(created_at_utc),
                 metadata       = VALUES(metadata)
             """, ct);
    }
}
