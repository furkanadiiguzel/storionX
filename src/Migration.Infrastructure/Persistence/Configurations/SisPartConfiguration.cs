using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Migration.Domain.Entities;

namespace Migration.Infrastructure.Persistence.Configurations;

internal sealed class SisPartConfiguration : IEntityTypeConfiguration<SisPart>
{
    public void Configure(EntityTypeBuilder<SisPart> b)
    {
        b.HasKey(p => p.PartId);
        b.Property(p => p.PartId).HasMaxLength(200).ValueGeneratedNever();
        b.Property(p => p.Sha256).HasMaxLength(64).IsRequired();
        b.Property(p => p.DataRef).HasMaxLength(2048).IsRequired();
    }
}
