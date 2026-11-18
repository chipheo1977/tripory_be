
using Tripory.Domain.Entities;

namespace Tripory.Domain.Repositories;

public interface IItineraryLikeRepository {
    Task<bool> ExistsAsync(Guid itineraryId, Guid userId, CancellationToken ct);
    Task<int> CountByItineraryAsync(Guid itineraryId, CancellationToken ct);
    Task<bool> AddIfNotExistsAsync(ItineraryLike like, CancellationToken ct);
    Task<bool> RemoveAsync(Guid itineraryId, Guid userId, CancellationToken ct);

}