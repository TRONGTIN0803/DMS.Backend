using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Company> Companies { get; }
    DbSet<SalesPerson> SalesPeople { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Site> Sites { get; }
    DbSet<Item> Items { get; }
    DbSet<Inventory> Inventories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

