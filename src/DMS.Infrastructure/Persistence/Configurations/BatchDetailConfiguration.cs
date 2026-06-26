using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.Infrastructure.Persistence.Configurations;

public sealed class BatchDetailConfiguration : IEntityTypeConfiguration<BatchDetail>
{
    public void Configure(EntityTypeBuilder<BatchDetail> builder)
    {
        builder.ToTable("IN_BatchDetail");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.HasIndex(x => x.BatchId);
        builder.HasIndex(x => x.ItemId);
        builder.HasOne(x => x.Batch).WithMany(x => x.Details).HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId);
    }
}
