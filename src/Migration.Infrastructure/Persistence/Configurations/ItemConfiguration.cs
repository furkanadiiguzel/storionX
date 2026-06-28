using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EvStorionX.Domain.Entities;
using EvStorionX.Infrastructure.Persistence;

namespace EvStorionX.Infrastructure.Persistence.Configurations;

internal sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> b)
    {
        b.HasKey(i => i.ItemId);
        b.Property(i => i.ItemId).HasMaxLength(200).ValueGeneratedNever();
        b.Property(i => i.ArchiveId).HasMaxLength(200).IsRequired();
        b.Property(i => i.FolderPath).HasMaxLength(1000).IsRequired();
        b.Property(i => i.Subject).HasMaxLength(998).IsRequired();
        b.Property(i => i.From).HasMaxLength(320).IsRequired();
        b.Property(i => i.RetentionCategory).HasMaxLength(200).IsRequired();
        b.Property(i => i.MessageClass).HasMaxLength(100).IsRequired();

        b.Property(i => i.SentDateUtc).HasColumnType("datetime(6)");

        b.Property(i => i.To).AsJsonList();
        b.Property(i => i.Cc).AsJsonList();
        b.Property(i => i.Bcc).AsJsonList();
        b.Property(i => i.ContentPartIds).AsJsonList();

        b.HasIndex(i => i.ArchiveId);
    }
}
