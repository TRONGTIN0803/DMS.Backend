using DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Companies.AnyAsync(cancellationToken))
        {
            return;
        }

        var company = new Company
        {
            Code = "NPP001",
            Name = "Default Distributor",
            TaxCode = "0000000000",
            Address = "Ho Chi Minh City",
            Phone = "0900000000",
            Email = "ops@example.com"
        };

        var salesPerson = new SalesPerson
        {
            Code = "SP001",
            Name = "Default Sales",
            Company = company,
            Phone = "0900000001"
        };

        var site = new Site
        {
            Code = "WH001",
            Name = "Main Warehouse",
            Company = company,
            Address = "Ho Chi Minh City"
        };

        var item = new Item
        {
            Code = "ITEM001",
            Name = "Sample Item",
            Unit = "case",
            Barcode = "893000000001",
            Price = 100000m,
            VatRate = 8m
        };

        var customer = new Customer
        {
            Code = "CUS001",
            Name = "Default Retail Customer",
            Company = company,
            SalesPerson = salesPerson,
            Address = "Ho Chi Minh City",
            Phone = "0900000002"
        };

        var inventory = new Inventory
        {
            Site = site,
            Item = item,
            Quantity = 100m,
            ReservedQuantity = 0m
        };

        dbContext.AddRange(company, salesPerson, site, item, customer, inventory);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

