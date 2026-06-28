using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EvStorionX.Domain.Entities;
using EvStorionX.Domain.Enums;

namespace EvStorionX.Infrastructure.Persistence.Configurations;

internal sealed class MigrationRecordConfiguration : IEntityTypeConfiguration<MigrationRecord>
{
    public void Configure(EntityTypeBuilder<MigrationRecord> b)
    {
        b.HasKey(r => r.IdempotencyKey);
        b.Property(r => r.IdempotencyKey).HasMaxLength(200).ValueGeneratedNever();

        b.HasIndex(r => r.IdempotencyKey).IsUnique();

        b.Property(r => r.SourceItemId).HasMaxLength(200).IsRequired();
        b.Property(r => r.ArchiveId).HasMaxLength(200).IsRequired();
        b.Property(r => r.TargetArchive).HasMaxLength(200).IsRequired();
        b.Property(r => r.TargetId).HasMaxLength(200);
        b.Property(r => r.LastErrorCode).HasMaxLength(100);

        b.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        b.Property(r => r.ExtractedAtUtc).HasColumnType("datetime(6)");
        b.Property(r => r.IngestedAtUtc).HasColumnType("datetime(6)");

        b.HasIndex(r => r.Status);
        b.HasIndex(r => r.ArchiveId);
    }
}
