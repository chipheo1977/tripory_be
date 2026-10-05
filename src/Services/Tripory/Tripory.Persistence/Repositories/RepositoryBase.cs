using System.Linq.Expressions;
using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Domains.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Tripory.Persistence.Repositories;

public abstract class RepositoryBase<TEntity, TKey> : IRepositoryBase<TEntity, TKey>
    where TEntity : EntityBase<TKey>
{
    protected readonly DbContext _dbContext;

    public RepositoryBase(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TEntity entity, CancellationToken ct = default)
    {
        await _dbContext.Set<TEntity>().AddAsync(entity, ct);
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        await _dbContext.Set<TEntity>().AddRangeAsync(entities, ct);
    }

    public IQueryable<TEntity> FindAll(Expression<Func<TEntity, bool>>? predicate = null, params Expression<Func<TEntity, object>>[] includeProperties)
    {
        IQueryable<TEntity> query = _dbContext.Set<TEntity>();
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        foreach (var property in includeProperties)
        {
            query = query.Include(property);
        }

        return query;
    }

    public async Task<TEntity?> FindByIdAsync(TKey id, CancellationToken ct = default, params Expression<Func<TEntity, object>>[] includeProperties)
    {
        if (includeProperties.Length == 0)
        {
            return await _dbContext.Set<TEntity>().FindAsync([id], ct);
        }

        return await FindAll(null, includeProperties).FirstOrDefaultAsync(x => x.Id!.Equals(id), ct);
    }

    public Task<TEntity?> FindSingleAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default, params Expression<Func<TEntity, object>>[] includeProperties)
    {
        var query = FindAll(predicate, includeProperties).FirstOrDefaultAsync(ct);
        return query;
    }

    public Task RemoveAsync(TEntity entity)
    {
        _dbContext.Set<TEntity>().Remove(entity);
        return Task.CompletedTask;
    }

    public Task RemoveRangeAsync(IEnumerable<TEntity> entities)
    {
        _dbContext.Set<TEntity>().RemoveRange(entities);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TEntity entity)
    {
        _dbContext.Set<TEntity>().Update(entity);
        return Task.CompletedTask;
    }

    public Task UpdateRangeAsync(IEnumerable<TEntity> entities)
    {
        _dbContext.Set<TEntity>().UpdateRange(entities);
        return Task.CompletedTask;
    }
}