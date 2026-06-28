using Microsoft.EntityFrameworkCore;
using EvStorionX.Domain.Entities;
using EvStorionX.Domain.Enums;
using EvStorionX.Infrastructure.Persistence;

namespace EvStorionX.Infrastructure.Persistence.Repositories;

internal sealed class EfMigrationRecordRepository
    : EfRepository<MigrationRecord>, IMigrationRecordRepository
{
    public EfMigrationRecordRepository(MigrationDbContext db) : base(db) { }

    public Task<MigrationRecord?> FindByIdempotencyKeyAsync(string key, CancellationToken ct = default) =>
        Db.MigrationRecords.FirstOrDefaultAsync(r => r.IdempotencyKey == key, ct);

    public async Task<IReadOnlyList<MigrationRecord>> FindByStatusAsync(
        MigrationStatus status, CancellationToken ct = default) =>
        await Db.MigrationRecords
            .Where(r => r.Status == status)
            .AsNoTracking()
            .ToListAsync(ct);

    public Task<int> CountByStatusAsync(MigrationStatus status, CancellationToken ct = default) =>
        Db.MigrationRecords.CountAsync(r => r.Status == status, ct);
}
