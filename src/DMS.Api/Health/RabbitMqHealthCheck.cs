using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace DMS.Api.Health;

public sealed class RabbitMqHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!bool.TryParse(configuration["RabbitMq:Enabled"], out var enabled) || !enabled)
        {
            return Task.FromResult(HealthCheckResult.Degraded("RabbitMQ is not enabled."));
        }

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMq:Host"] ?? "localhost",
                VirtualHost = configuration["RabbitMq:VirtualHost"] ?? "/",
                UserName = configuration["RabbitMq:UserName"] ?? "guest",
                Password = configuration["RabbitMq:Password"] ?? "guest",
                RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
            };

            using var connection = factory.CreateConnection();
            return Task.FromResult(connection.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ is reachable.")
                : HealthCheckResult.Unhealthy("RabbitMQ connection did not open."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ is unreachable.", ex));
        }
    }
}
