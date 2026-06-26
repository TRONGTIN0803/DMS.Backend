using DMS.Application.Abstractions;
using DMS.Application.Events;
using DMS.Infrastructure.Jobs;
using DMS.Infrastructure.Messaging;
using DMS.Application.Orders;
using DMS.Infrastructure.Orders;
using DMS.Infrastructure.Persistence;
using DMS.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IOrderNumberGenerator, PostgresOrderNumberGenerator>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<InventoryReconciliationJob>();
        services.AddScoped<AuditCleanupJob>();

        if (bool.TryParse(configuration["RabbitMq:Enabled"], out var rabbitMqEnabled) && rabbitMqEnabled)
        {
            services.AddMassTransit(bus =>
            {
                bus.AddConsumer<SalesOrderApprovedConsumer>();
                bus.UsingRabbitMq((context, cfg) =>
                {
                    var host = configuration["RabbitMq:Host"] ?? "localhost";
                    var virtualHost = configuration["RabbitMq:VirtualHost"] ?? "/";
                    var userName = configuration["RabbitMq:UserName"] ?? "guest";
                    var password = configuration["RabbitMq:Password"] ?? "guest";

                    cfg.Host(host, virtualHost, h =>
                    {
                        h.Username(userName);
                        h.Password(password);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });
            services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        }
        else
        {
            services.AddScoped<IIntegrationEventPublisher, LoggingIntegrationEventPublisher>();
        }

        var seedOnStartupDisabled = string.Equals(configuration["Database:SeedOnStartup"], "false", StringComparison.OrdinalIgnoreCase);
        var hangfireEnabled = string.Equals(configuration["Hangfire:Enabled"], "true", StringComparison.OrdinalIgnoreCase);
        if (!seedOnStartupDisabled &&
            !hangfireEnabled &&
            bool.TryParse(configuration["BackgroundJobs:Enabled"], out var backgroundJobsEnabled) &&
            backgroundJobsEnabled)
        {
            services.AddHostedService<OutboxProcessor>();
            services.AddHostedService<SystemJobsWorker>();
        }

        return services;
    }
}
