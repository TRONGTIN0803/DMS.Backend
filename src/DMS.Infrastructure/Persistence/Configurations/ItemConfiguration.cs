using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.Infrastructure.Persistence.Configurations;

public sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("IN_Item");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Barcode).HasMaxLength(50);
        builder.Property(x => x.Price).HasPrecision(18, 2);
        builder.Property(x => x.VatRate).HasPrecision(5, 2);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.ConfigureAuditable();
    }
}

