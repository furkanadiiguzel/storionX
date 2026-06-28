namespace Migration.Infrastructure.Persistence.Repositories;

/// <summary>Generic read-write repository backed by EF Core.</summary>
public interface IRepository<T> where T : class
{
    /// <summary>Exposes the underlying <see cref="IQueryable{T}"/> for LINQ composition.</summary>
    IQueryable<T> Query { get; }

    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
