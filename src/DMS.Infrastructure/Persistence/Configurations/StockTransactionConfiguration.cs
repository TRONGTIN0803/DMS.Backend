using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.Infrastructure.Persistence.Configurations;

public sealed class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> builder)
    {
        builder.ToTable("IN_StockTransaction");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.TransType).HasConversion<short>();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.BalanceAfter).HasPrecision(18, 3);
        builder.Property(x => x.RefType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.HasIndex(x => x.SiteId);
        builder.HasIndex(x => x.ItemId);
        builder.HasIndex(x => new { x.SiteId, x.ItemId, x.CreatedAt });
        builder.HasIndex(x => new { x.RefType, x.RefId });
        builder.HasOne(x => x.Site).WithMany().HasForeignKey(x => x.SiteId);
        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId);
    }
}
