using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DMS.Infrastructure.Jobs;

public sealed class SystemJobsWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SystemJobsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "System jobs cycle failed.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var reconciliationJob = scope.ServiceProvider.GetRequiredService<InventoryReconciliationJob>();
        var cleanupJob = scope.ServiceProvider.GetRequiredService<AuditCleanupJob>();

        var mismatches = await reconciliationJob.RecalculateInventoryAsync(cancellationToken);
        var deletedAuditLogs = await cleanupJob.CleanupAsync(cancellationToken: cancellationToken);
        logger.LogInformation("System jobs completed: inventory mismatches={MismatchCount}, deleted audit logs={DeletedAuditLogs}", mismatches, deletedAuditLogs);
    }
}
