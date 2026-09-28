using BuildingBlocks.Core.Abstractions.Persistence;
using Tripory.Domain.Entities;

namespace Tripory.Domain.Repositories;

public interface IItineraryRepository : IRepositoryBase<Itinerary, Guid>
{
    Task<Itinerary?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<bool> IsOwnerAsync(Guid itineraryId, Guid userId, CancellationToken ct = default);
    Task<(IReadOnlyList<Itinerary> Items, int TotalCount)> GetMyItinerariesPagedAsync(
        Guid userId,
        int pageIndex,
        int pageSize,
        bool? isPublic,
        CancellationToken ct = default
    );
}