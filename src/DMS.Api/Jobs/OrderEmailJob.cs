using Microsoft.Extensions.Logging;

namespace DMS.Api.Jobs;

public sealed class OrderEmailJob(ILogger<OrderEmailJob> logger)
{
    public Task SendOrderApprovedEmailAsync(long salesOrderId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Simulated sales order approval email queued for order {SalesOrderId}", salesOrderId);
        return Task.CompletedTask;
    }
}
