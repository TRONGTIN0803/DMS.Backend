using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("SYS_OutboxMessage");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Error).HasMaxLength(1000);
        builder.HasIndex(x => x.ProcessedOn);
        builder.HasIndex(x => x.OccurredOn);
    }
}
