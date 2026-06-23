using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.Infrastructure.Persistence.Configurations;

public sealed class SalesOrderDetailConfiguration : IEntityTypeConfiguration<SalesOrderDetail>
{
    public void Configure(EntityTypeBuilder<SalesOrderDetail> builder)
    {
        builder.ToTable("OM_SalesOrdDet");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.VatRate).HasPrecision(5, 2);
        builder.Property(x => x.LineAmount).HasPrecision(18, 2);
        builder.Property(x => x.LineVatAmount).HasPrecision(18, 2);
        builder.HasIndex(x => x.SalesOrderId);
        builder.HasIndex(x => x.ItemId);
        builder.HasOne(x => x.SalesOrder).WithMany(x => x.Details).HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId);
    }
}
