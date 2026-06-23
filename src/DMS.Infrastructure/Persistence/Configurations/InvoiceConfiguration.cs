using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("OM_Invoice");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.InvoiceNo).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<short>();
        builder.Property(x => x.SubTotal).HasPrecision(18, 2);
        builder.Property(x => x.VatAmount).HasPrecision(18, 2);
        builder.Property(x => x.GrandTotal).HasPrecision(18, 2);
        builder.HasIndex(x => x.InvoiceNo).IsUnique();
        builder.HasIndex(x => x.SalesOrderId).IsUnique();
        builder.HasIndex(x => x.CustomerId);
        builder.HasOne(x => x.SalesOrder).WithOne(x => x.Invoice).HasForeignKey<Invoice>(x => x.SalesOrderId);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
        builder.ConfigureAuditable();
    }
}
