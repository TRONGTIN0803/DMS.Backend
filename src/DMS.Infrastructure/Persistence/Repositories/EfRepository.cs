using DMS.Application.Abstractions;
using DMS.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace DMS.Infrastructure.Persistence.Repositories;

public sealed class EfRepository<TEntity>(ApplicationDbContext dbContext) : IRepository<TEntity>
    where TEntity : Entity
{
    public IQueryable<TEntity> Query() => dbContext.Set<TEntity>();

    public Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        dbContext.Set<TEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public void Add(TEntity entity) => dbContext.Set<TEntity>().Add(entity);

    public void Remove(TEntity entity) => dbContext.Set<TEntity>().Remove(entity);
}
