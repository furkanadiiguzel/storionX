using Microsoft.EntityFrameworkCore;
using EvStorionX.Infrastructure.Persistence;

namespace EvStorionX.Infrastructure.Persistence.Repositories;

/// <summary>Generic EF Core repository; specialised repositories can inherit from this.</summary>
public class EfRepository<T> : IRepository<T> where T : class
{
    protected MigrationDbContext Db { get; }

    public EfRepository(MigrationDbContext db) => Db = db;

    public IQueryable<T> Query => Db.Set<T>().AsNoTracking();

    public Task AddAsync(T entity, CancellationToken ct = default) =>
        Db.Set<T>().AddAsync(entity, ct).AsTask();

    public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default) =>
        Db.Set<T>().AddRangeAsync(entities, ct);

    public void Update(T entity) => Db.Set<T>().Update(entity);

    public void Remove(T entity) => Db.Set<T>().Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        Db.SaveChangesAsync(ct);
}
