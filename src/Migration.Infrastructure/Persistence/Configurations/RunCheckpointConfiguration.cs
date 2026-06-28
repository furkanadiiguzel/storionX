using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EvStorionX.Domain.Entities;

namespace EvStorionX.Infrastructure.Persistence.Configurations;

internal sealed class RunCheckpointConfiguration : IEntityTypeConfiguration<RunCheckpoint>
{
    public void Configure(EntityTypeBuilder<RunCheckpoint> b)
    {
        b.HasKey(c => new { c.RunId, c.Phase });
        b.Property(c => c.Phase).HasMaxLength(100);
        b.Property(c => c.Metadata).HasColumnType("longtext");
        b.Property(c => c.CreatedAtUtc).HasColumnType("datetime(6)");
    }
}
