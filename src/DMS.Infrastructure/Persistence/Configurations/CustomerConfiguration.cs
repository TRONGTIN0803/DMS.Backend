using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("AR_Customer");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CustomerType).HasConversion<short>();
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.SalesPersonId);
        builder.HasOne(x => x.Company).WithMany(x => x.Customers).HasForeignKey(x => x.CompanyId);
        builder.HasOne(x => x.SalesPerson).WithMany(x => x.Customers).HasForeignKey(x => x.SalesPersonId).OnDelete(DeleteBehavior.SetNull);
        builder.ConfigureAuditable();
    }
}

