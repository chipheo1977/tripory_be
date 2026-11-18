using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.Domains.Abstractions.DDD;
using Tripory.Domain.Errors;

namespace Tripory.Domain.Entities;

// Khóa ghép (ItineraryId, UserId) cấu hình ở Persistence.
public class ItineraryLike : IAggregateRoot
{
    public Guid ItineraryId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ItineraryLike()
    {
    }

    private ItineraryLike(Guid itineraryId, Guid userId, DateTimeOffset createdAt)
    {
        ItineraryId = itineraryId;
        UserId = userId;
        CreatedAt = createdAt;
    }

    public static Result<ItineraryLike> Create(Itinerary itinerary, Guid userId)
    {
        var socialResult = itinerary.EnsureSocialAvailable();
        if (socialResult.IsFailure)
            return Result.Failure<ItineraryLike>(socialResult.Error);

        if (userId == Guid.Empty)
            return Result.Failure<ItineraryLike>(DomainErrors.Itinerary.InvalidUserId);

        return Result.Success(new ItineraryLike(itinerary.Id, userId, DateTimeOffset.UtcNow));
    }
}
