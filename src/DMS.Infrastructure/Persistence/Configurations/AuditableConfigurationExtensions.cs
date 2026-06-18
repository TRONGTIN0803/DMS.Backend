using DMS.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DMS.Infrastructure.Persistence.Configurations;

internal static class AuditableConfigurationExtensions
{
    public static void ConfigureAuditable<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity
    {
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

