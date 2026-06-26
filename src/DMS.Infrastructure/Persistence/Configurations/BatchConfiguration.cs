using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.Infrastructure.Persistence.Configurations;

public sealed class BatchConfiguration : IEntityTypeConfiguration<Batch>
{
    public void Configure(EntityTypeBuilder<Batch> builder)
    {
        builder.ToTable("IN_Batch");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.BatchNo).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Type).HasConversion<short>();
        builder.Property(x => x.Status).HasConversion<short>();
        builder.Property(x => x.RefType).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.BatchNo).IsUnique();
        builder.HasIndex(x => x.SiteId);
        builder.HasIndex(x => new { x.RefType, x.RefId });
        builder.HasOne(x => x.Site).WithMany().HasForeignKey(x => x.SiteId);
        builder.ConfigureAuditable();
    }
}
