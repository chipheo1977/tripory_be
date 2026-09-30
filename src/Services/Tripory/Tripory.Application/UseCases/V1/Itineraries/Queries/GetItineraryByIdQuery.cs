using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Itineraries.Responses;

namespace Tripory.Application.UseCases.V1.Itineraries.Queries;

public record GetItineraryByIdQuery(Guid ItineraryId) : IQuery<ItineraryDetailDto>;
