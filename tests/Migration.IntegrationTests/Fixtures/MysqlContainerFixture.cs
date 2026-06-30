extern alias MigrationApi;

using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using EvStorionX.Infrastructure.Persistence;

namespace EvStorionX.IntegrationTests.Fixtures;

/// <summary>
/// Starts a real MySQL container once per test collection and exposes a connection string
/// with the schema already migrated. Implements IAsyncLifetime so xUnit manages lifecycle.
/// </summary>
public sealed class MysqlContainerFixture : IAsyncLifetime
{
    private readonly MySqlContainer _container = new MySqlBuilder()
        .WithImage("mysql:8.4")
        .WithDatabase("storionx_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var opts = new DbContextOptionsBuilder<MigrationDbContext>()
            .UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString),
                o => o.MigrationsAssembly(typeof(MigrationDbContext).Assembly.GetName().Name))
            .Options;

        await using var db = new MigrationDbContext(opts);
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
