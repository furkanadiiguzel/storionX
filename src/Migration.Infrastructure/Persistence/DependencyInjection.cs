using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EvStorionX.Application.Abstractions;
using EvStorionX.Infrastructure.Persistence.Repositories;
using EvStorionX.Infrastructure.Reporting;
using EvStorionX.Infrastructure.StateStore;

namespace EvStorionX.Infrastructure.Persistence;

/// <summary>Registers all Infrastructure services into the DI container.</summary>
public static class DependencyInjection
{
    /// <param name="connectionString">MySQL connection string (never hard-coded; read from config/env).</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Pinned server version avoids a live-DB round-trip on every app start.
        var serverVersion = new MySqlServerVersion(new Version(8, 4, 0));

        // AddPooledDbContextFactory registers IDbContextFactory<T> only.
        // The scoped registration below bridges the gap for services that inject DbContext directly.
        services.AddPooledDbContextFactory<MigrationDbContext>(options =>
            options.UseMySql(connectionString, serverVersion));
        services.AddScoped(sp =>
            sp.GetRequiredService<IDbContextFactory<MigrationDbContext>>().CreateDbContext());

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IMigrationRecordRepository, EfMigrationRecordRepository>();

        // EfStateStore is Singleton: only depends on Singleton IDbContextFactory + TimeProvider.
        services.AddSingleton<IStateStore, EfStateStore>();

        // EfReporter is Scoped: captures IStorionXClient (Transient typed HttpClient) per scope.
        services.AddScoped<IReporter, EfReporter>();

        return services;
    }
}
