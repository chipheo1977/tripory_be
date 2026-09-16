using System.Linq.Expressions;

namespace BuildingBlocks.Core.Abstractions.Persistence;

public interface IRepositoryBase<TEntity, in TKey> where TEntity : class
{
    Task<TEntity?> FindByIdAsync(TKey id, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includeProperties);
    Task<TEntity?> FindSingleAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includeProperties);
    IQueryable<TEntity> FindAll(Expression<Func<TEntity, bool>>? predicate = null, params Expression<Func<TEntity, object>>[] includeProperties);
    Task AddAsync(TEntity entity, CancellationToken ct = default);
    Task UpdateAsync(TEntity entity);
    Task RemoveAsync(TEntity entity);
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<TEntity> entities);
    Task RemoveRangeAsync(IEnumerable<TEntity> entities);
}