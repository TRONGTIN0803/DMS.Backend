using DMS.Application.Abstractions;

namespace DMS.Infrastructure.Persistence.Repositories;

public sealed class EfUnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
