using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Itineraries.Responses;

namespace Tripory.Application.UseCases.V1.Itineraries.Queries;

public record GetMyItinerariesQuery(
    int PageIndex = 1,
    int PageSize = 10,
    bool? IsPublic = null
) : IQuery<PagedResult<ItinerarySummaryDto>>;
