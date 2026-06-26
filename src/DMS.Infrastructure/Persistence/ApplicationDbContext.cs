using DMS.Application.Abstractions;
using DMS.Domain.Common;
using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace DMS.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<SalesPerson> SalesPeople => Set<SalesPerson>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderDetail> SalesOrderDetails => Set<SalesOrderDetail>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<BatchDetail> BatchDetails => Set<BatchDetail>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>("OM_SalesOrderNoSeq")
            .StartsAt(1)
            .IncrementsBy(1);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        AddAuditLogs();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditFields()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = _currentUserService?.UserId;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = userId;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userId;
            }
        }
    }

    private void AddAuditLogs()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = _currentUserService?.UserId;
        var auditEntries = ChangeTracker.Entries()
            .Where(IsAuditedEntry)
            .Select(entry => new AuditLog
            {
                UserId = userId,
                OccurredOn = now,
                EntityName = entry.Metadata.ClrType.Name,
                EntityId = GetEntityId(entry),
                Action = entry.State.ToString(),
                OldValue = entry.State == EntityState.Added ? null : SerializeValues(entry.OriginalValues),
                NewValue = entry.State == EntityState.Deleted ? null : SerializeValues(entry.CurrentValues)
            })
            .ToList();

        if (auditEntries.Count > 0)
        {
            AuditLogs.AddRange(auditEntries);
        }
    }

    private static bool IsAuditedEntry(EntityEntry entry)
    {
        if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            return false;
        }

        return entry.Entity is SalesOrder or Inventory or Invoice;
    }

    private static string GetEntityId(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null)
        {
            return string.Empty;
        }

        return string.Join(",", key.Properties.Select(property => entry.Property(property.Name).CurrentValue?.ToString() ?? string.Empty));
    }

    private static string SerializeValues(PropertyValues values)
    {
        var dictionary = values.Properties.ToDictionary(
            property => property.Name,
            property => values[property.Name]);

        return JsonSerializer.Serialize(dictionary, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
