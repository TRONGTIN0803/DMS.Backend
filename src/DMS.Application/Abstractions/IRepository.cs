using DMS.Domain.Common;

namespace DMS.Application.Abstractions;

public interface IRepository<TEntity>
    where TEntity : Entity
{
    IQueryable<TEntity> Query();
    Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    void Add(TEntity entity);
    void Remove(TEntity entity);
}
