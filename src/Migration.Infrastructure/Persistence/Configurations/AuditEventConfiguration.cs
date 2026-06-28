using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EvStorionX.Domain.Entities;

namespace EvStorionX.Infrastructure.Persistence.Configurations;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).ValueGeneratedNever();

        b.Property(e => e.EventType).HasMaxLength(100).IsRequired();
        b.Property(e => e.ItemId).HasMaxLength(200);
        b.Property(e => e.Payload).HasColumnType("longtext").IsRequired();

        b.Property(e => e.TimestampUtc).HasColumnType("datetime(6)");

        b.HasIndex(e => e.RunId);
        b.HasIndex(e => new { e.RunId, e.EventType });
    }
}
