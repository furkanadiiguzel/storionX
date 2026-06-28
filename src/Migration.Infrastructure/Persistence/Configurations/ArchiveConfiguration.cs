using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Migration.Domain.Entities;
using Migration.Domain.Enums;

namespace Migration.Infrastructure.Persistence.Configurations;

internal sealed class ArchiveConfiguration : IEntityTypeConfiguration<Archive>
{
    public void Configure(EntityTypeBuilder<Archive> b)
    {
        b.HasKey(a => a.ArchiveId);
        b.Property(a => a.ArchiveId).HasMaxLength(200).ValueGeneratedNever();

        b.Property(a => a.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        b.Property(a => a.OwnerUpn).HasMaxLength(320);
        b.Property(a => a.VaultStore).HasMaxLength(200).IsRequired();
    }
}
