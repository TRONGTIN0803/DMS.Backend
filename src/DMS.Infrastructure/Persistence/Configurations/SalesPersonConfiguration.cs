using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.Infrastructure.Persistence.Configurations;

public sealed class SalesPersonConfiguration : IEntityTypeConfiguration<SalesPerson>
{
    public void Configure(EntityTypeBuilder<SalesPerson> builder)
    {
        builder.ToTable("AR_SalesPerson");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.CompanyId);
        builder.HasOne(x => x.Company).WithMany(x => x.SalesPeople).HasForeignKey(x => x.CompanyId);
        builder.ConfigureAuditable();
    }
}

