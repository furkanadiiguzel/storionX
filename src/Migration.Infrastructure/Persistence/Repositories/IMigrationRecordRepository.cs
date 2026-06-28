using Migration.Domain.Entities;
using Migration.Domain.Enums;

namespace Migration.Infrastructure.Persistence.Repositories;

/// <summary>Domain-specific queries on top of the generic repository contract.</summary>
public interface IMigrationRecordRepository : IRepository<MigrationRecord>
{
    Task<MigrationRecord?> FindByIdempotencyKeyAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<MigrationRecord>> FindByStatusAsync(MigrationStatus status, CancellationToken ct = default);
    Task<int> CountByStatusAsync(MigrationStatus status, CancellationToken ct = default);
}
