using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.Infrastructure.Persistence.Configurations;

public sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("OM_SalesOrd");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.OrderNo).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<short>();
        builder.Property(x => x.SubTotal).HasPrecision(18, 2);
        builder.Property(x => x.VatAmount).HasPrecision(18, 2);
        builder.Property(x => x.GrandTotal).HasPrecision(18, 2);
        builder.Property<uint>("xmin").IsRowVersion();
        builder.HasIndex(x => x.OrderNo).IsUnique();
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.SalesPersonId);
        builder.HasIndex(x => x.SiteId);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
        builder.HasOne(x => x.SalesPerson).WithMany().HasForeignKey(x => x.SalesPersonId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Site).WithMany().HasForeignKey(x => x.SiteId);
        builder.ConfigureAuditable();
    }
}
