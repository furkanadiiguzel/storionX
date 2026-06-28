using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Migration.Infrastructure.Persistence.Repositories;

namespace Migration.Infrastructure.Persistence;

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

        services.AddDbContextPool<MigrationDbContext>(options =>
            options.UseMySql(connectionString, serverVersion));

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IMigrationRecordRepository, EfMigrationRecordRepository>();

        return services;
    }
}
