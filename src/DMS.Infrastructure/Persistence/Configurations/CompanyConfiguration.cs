using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.Infrastructure.Persistence.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("SYS_Company");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TaxCode).HasMaxLength(20);
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.ConfigureAuditable();
    }
}

