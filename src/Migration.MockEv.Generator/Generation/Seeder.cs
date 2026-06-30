using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EvStorionX.Domain.Entities;
using EvStorionX.Infrastructure.Persistence;

namespace EvStorionX.MockEv.Generator.Generation;

/// <summary>Inserts generated data into MySQL using EF Core with 1 000-row batches.</summary>
public sealed partial class Seeder(MigrationDbContext db, ILogger<Seeder> logger)
{
    private static readonly string[] Tables =
        ["items", "sis_parts", "archives", "migration_records", "audit_events", "run_checkpoints"];

    private const int BatchSize = 1_000;

    public async Task SeedAsync(GeneratedData data, bool reset, CancellationToken ct = default)
    {
        if (reset)
        {
            LogResetting(logger);
            await db.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0", ct);
            foreach (var table in Tables)
#pragma warning disable EF1002 // table names are internal constants, not user input
                await db.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE `{table}`", ct);
#pragma warning restore EF1002
            await db.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1", ct);
        }

        LogInserting(logger, "archives", data.Archives.Count);
        await InsertBatchedAsync(db.Archives, data.Archives, ct);

        LogInserting(logger, "SIS parts", data.Parts.Count);
        await InsertBatchedAsync(db.SisParts, data.Parts.Select(p => p.Part).ToList(), ct);

        LogInserting(logger, "items", data.Items.Count);
        await InsertBatchedAsync(db.Items, data.Items, ct);

        LogSeedingComplete(logger);
    }

    private async Task InsertBatchedAsync<T>(
        Microsoft.EntityFrameworkCore.DbSet<T> set,
        IReadOnlyList<T> entities,
        CancellationToken ct) where T : class
    {
        for (int i = 0; i < entities.Count; i += BatchSize)
        {
            var batch = entities.Skip(i).Take(BatchSize).ToList();
            await set.AddRangeAsync(batch, ct);
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
            var done = Math.Min(i + BatchSize, entities.Count);
            LogBatchProgress(logger, done, entities.Count);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Resetting tables...")]
    private static partial void LogResetting(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Inserting {N} {Entity}...")]
    private static partial void LogInserting(ILogger logger, string entity, int n);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeding complete.")]
    private static partial void LogSeedingComplete(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "  {Done}/{Total}")]
    private static partial void LogBatchProgress(ILogger logger, int done, int total);
}
