using Microsoft.EntityFrameworkCore;
using Tripory.Domain.Entities;
using Tripory.Domain.Repositories;

namespace Tripory.Persistence.Repositories;

public class ItineraryCommentRepository : RepositoryBase<ItineraryComment, Guid>, IItineraryCommentRepository
{
    public ItineraryCommentRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<(IReadOnlyList<ItineraryComment> Items, int TotalCount)> GetByItineraryPagedAsync(
        Guid itineraryId,
        int pageIndex,
        int pageSize,
        CancellationToken ct
    )
    {
        var query = _dbContext.Set<ItineraryComment>()
            .AsNoTracking()
            .Where(c => c.ItineraryId == itineraryId);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<int> CountByItineraryAsync(Guid itineraryId, CancellationToken ct)
    {
        return await _dbContext.Set<ItineraryComment>()
            .CountAsync(c => c.ItineraryId == itineraryId, ct);
    }
}
