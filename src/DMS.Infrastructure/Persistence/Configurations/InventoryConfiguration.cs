using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.Infrastructure.Persistence.Configurations;

public sealed class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("IN_Inventory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.ReservedQuantity).HasPrecision(18, 3).HasDefaultValue(0);
        builder.Property<uint>("xmin").IsRowVersion();
        builder.HasIndex(x => x.SiteId);
        builder.HasIndex(x => x.ItemId);
        builder.HasIndex(x => new { x.SiteId, x.ItemId }).IsUnique();
        builder.HasOne(x => x.Site).WithMany(x => x.Inventories).HasForeignKey(x => x.SiteId);
        builder.HasOne(x => x.Item).WithMany(x => x.Inventories).HasForeignKey(x => x.ItemId);
    }
}
