using Microsoft.EntityFrameworkCore;
using Tripory.Domain.Entities;
using Tripory.Domain.Repositories;

namespace Tripory.Persistence.Repositories;

/// <summary>
/// Lượt thích dùng khóa ghép nên không kế thừa RepositoryBase.
/// AddIfNotExistsAsync và RemoveAsync ghi xuống DB ngay, không đi qua IUnitOfWork.
/// </summary>
public class ItineraryLikeRepository : IItineraryLikeRepository
{
    private readonly ApplicationDbContext _context;

    public ItineraryLikeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(Guid itineraryId, Guid userId, CancellationToken ct)
    {
        return await _context.ItineraryLikes
            .AnyAsync(l => l.ItineraryId == itineraryId && l.UserId == userId, ct);
    }

    public async Task<int> CountByItineraryAsync(Guid itineraryId, CancellationToken ct)
    {
        return await _context.ItineraryLikes
            .CountAsync(l => l.ItineraryId == itineraryId, ct);
    }

    public async Task<bool> AddIfNotExistsAsync(ItineraryLike like, CancellationToken ct)
    {
        var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO itinerary.itinerary_likes ("ItineraryId", "UserId", "CreatedAt")
            VALUES ({like.ItineraryId}, {like.UserId}, {like.CreatedAt})
            ON CONFLICT ("ItineraryId", "UserId") DO NOTHING
            """, ct);

        return affected > 0;
    }

    public async Task<bool> RemoveAsync(Guid itineraryId, Guid userId, CancellationToken ct)
    {
        var affected = await _context.ItineraryLikes
            .Where(l => l.ItineraryId == itineraryId && l.UserId == userId)
            .ExecuteDeleteAsync(ct);

        return affected > 0;
    }
}
