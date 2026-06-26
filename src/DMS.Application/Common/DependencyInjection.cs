using FluentValidation;
using DMS.Application.Catalog;
using DMS.Application.Orders;
using Microsoft.Extensions.DependencyInjection;

namespace DMS.Application.Common;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<ISalesOrderService, SalesOrderService>();
        services.AddScoped<IInventoryBatchService, InventoryBatchService>();
        return services;
    }
}
