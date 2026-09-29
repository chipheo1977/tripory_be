using BuildingBlocks.Core.CQRS;

namespace Tripory.Application.UseCases.V1.Itineraries.Responses;

public record CreateQuickDraftItineraryCommand(string Title) : ICommand<ItineraryDetailDto>;