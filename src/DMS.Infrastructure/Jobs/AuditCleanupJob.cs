using DMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DMS.Infrastructure.Jobs;

public sealed class AuditCleanupJob(ApplicationDbContext dbContext)
{
    public Task<int> CleanupAsync(int retentionDays = 180, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);
        return dbContext.AuditLogs
            .Where(x => x.OccurredOn < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
