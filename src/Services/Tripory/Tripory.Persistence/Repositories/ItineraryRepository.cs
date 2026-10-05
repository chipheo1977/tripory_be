using Microsoft.EntityFrameworkCore;
using Tripory.Domain.Entities;
using Tripory.Domain.Repositories;

namespace Tripory.Persistence.Repositories;

public class ItineraryRepository : RepositoryBase<Itinerary, Guid>, IItineraryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ItineraryRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Itinerary?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        return FindByIdAsync(id, ct, i => i.Days, i => i.Waypoints);
    }

    public async Task<(IReadOnlyList<Itinerary> Items, int TotalCount)> GetMyItinerariesPagedAsync(
        Guid userId, 
        int pageIndex, 
        int pageSize, 
        bool? isPublic, 
        CancellationToken ct = default
    )
    {
        var query = _dbContext.Set<Itinerary>()
            .Where(i => i.UserId == userId);
        if (isPublic.HasValue)
        {
            query = query.Where(i => i.IsPublic == isPublic);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.UpdatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<bool> IsOwnerAsync(Guid itineraryId, Guid userId, CancellationToken ct = default)
    {
        return await _dbContext.Set<Itinerary>()
            .AnyAsync(i => i.Id == itineraryId && i.UserId == userId, ct);
    }
}