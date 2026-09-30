using BuildingBlocks.Core.Abstractions.Shared;
using Tripory.Domain.Entities;
using Tripory.Domain.Errors;
using Tripory.Domain.Repositories;

namespace Tripory.Application.UseCases.V1.Itineraries.Extensions;

public static class ItineraryRepositoryExtensions
{
    public static async Task<Result<Itinerary>> GetOwnedItineraryAsync(
        this IItineraryRepository repository,
        Guid itineraryId,
        Guid? currentUserId,
        bool includeDetails = false,
        CancellationToken ct = default)
    {
        if (!currentUserId.HasValue)
            return Result.Failure<Itinerary>(DomainErrors.Auth.Unauthorized);

        var itinerary = includeDetails
            ? await repository.GetByIdWithDetailsAsync(itineraryId, ct)
            : await repository.FindByIdAsync(itineraryId, ct);

        if (itinerary is null)
            return Result.Failure<Itinerary>(DomainErrors.Itinerary.NotFound);

        if (itinerary.UserId != currentUserId.Value)
            return Result.Failure<Itinerary>(DomainErrors.Itinerary.Forbidden);

        return Result.Success(itinerary);
    }
}
