using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Migration.Infrastructure.Persistence;

/// <summary>
/// Design-time factory — lets <c>dotnet ef</c> create a <see cref="MigrationDbContext"/>
/// without a running application host or startup project.
/// </summary>
internal sealed class MigrationDbContextFactory : IDesignTimeDbContextFactory<MigrationDbContext>
{
    public MigrationDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Server=localhost;Port=3307;Database=migration_db;User=root;Password=devpassword;";

        // Use a pinned version so dotnet-ef never needs a live DB connection to detect the version.
        var serverVersion = new MySqlServerVersion(new Version(8, 4, 0));

        var options = new DbContextOptionsBuilder<MigrationDbContext>()
            .UseMySql(connectionString, serverVersion)
            .Options;

        return new MigrationDbContext(options);
    }
}
