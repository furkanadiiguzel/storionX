using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EvStorionX.Domain.Entities;
using EvStorionX.Infrastructure.Persistence;

namespace EvStorionX.MockEv.Generator.Generation;

/// <summary>Inserts generated data into MySQL using EF Core with 1 000-row batches.</summary>
public sealed class Seeder(MigrationDbContext db, ILogger<Seeder> logger)
{
    private const int BatchSize = 1_000;

    public async Task SeedAsync(GeneratedData data, bool reset, CancellationToken ct = default)
    {
        if (reset)
        {
            logger.LogInformation("Resetting tables...");
            // Truncate without FK checks (our schema has no FK constraints, but be safe).
            await db.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0", ct);
            foreach (var table in new[] { "items", "sis_parts", "archives",
                                          "migration_records", "audit_events", "run_checkpoints" })
                await db.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE `{table}`", ct);
            await db.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1", ct);
        }

        logger.LogInformation("Inserting {N} archives...", data.Archives.Count);
        await InsertBatchedAsync(db.Archives, data.Archives, ct);

        logger.LogInformation("Inserting {N} SIS parts...", data.Parts.Count);
        await InsertBatchedAsync(db.SisParts, data.Parts.Select(p => p.Part).ToList(), ct);

        logger.LogInformation("Inserting {N} items...", data.Items.Count);
        await InsertBatchedAsync(db.Items, data.Items, ct);

        logger.LogInformation("Seeding complete.");
    }

    private async Task InsertBatchedAsync<T>(
        Microsoft.EntityFrameworkCore.DbSet<T> set,
        IReadOnlyList<T> entities,
        CancellationToken ct) where T : class
    {
        for (int i = 0; i < entities.Count; i += BatchSize)
        {
            var batch = entities
                .Skip(i)
                .Take(BatchSize)
                .ToList();

            await set.AddRangeAsync(batch, ct);
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();

            logger.LogDebug("  {Done}/{Total}", Math.Min(i + BatchSize, entities.Count), entities.Count);
        }
    }
}
