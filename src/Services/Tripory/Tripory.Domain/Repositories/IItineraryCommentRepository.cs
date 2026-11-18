using BuildingBlocks.Core.Abstractions.Persistence;
using Tripory.Domain.Entities;

namespace Tripory.Domain.Repositories;

public interface IItineraryCommentRepository : IRepositoryBase<ItineraryComment, Guid>
{
    Task<(IReadOnlyList<ItineraryComment> Items, int TotalCount)> GetByItineraryPagedAsync(
        Guid itineraryId,
        int pageIndex,
        int pageSize,
        CancellationToken ct
    );
    Task<int> CountByItineraryAsync(Guid itineraryId, CancellationToken ct);
}