using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.Infrastructure.Persistence.Configurations;

public sealed class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.ToTable("IN_Site");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.CompanyId);
        builder.HasOne(x => x.Company).WithMany(x => x.Sites).HasForeignKey(x => x.CompanyId);
        builder.ConfigureAuditable();
    }
}

