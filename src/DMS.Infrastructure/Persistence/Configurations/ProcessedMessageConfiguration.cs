using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.Infrastructure.Persistence.Configurations;

public sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("SYS_ProcessedMessage");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Handler).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => new { x.Id, x.Handler }).IsUnique();
    }
}
