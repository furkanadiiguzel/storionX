using Microsoft.EntityFrameworkCore;
using EvStorionX.Domain.Entities;

namespace EvStorionX.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the migration pipeline.
/// Only Infrastructure references this type — Domain and Application remain persistence-agnostic.
/// </summary>
public sealed class MigrationDbContext : DbContext
{
    public MigrationDbContext(DbContextOptions<MigrationDbContext> options) : base(options) { }

    public DbSet<Archive> Archives => Set<Archive>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<SisPart> SisParts => Set<SisPart>();
    public DbSet<MigrationRecord> MigrationRecords => Set<MigrationRecord>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<RunCheckpoint> RunCheckpoints => Set<RunCheckpoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MigrationDbContext).Assembly);

        // Applied last so all fluent-API names are already set before conversion.
        modelBuilder.UseSnakeCaseNamingConvention();
    }
}
