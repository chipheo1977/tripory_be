using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Itineraries.Responses;

namespace Tripory.Application.UseCases.V1.Itineraries.Commands;

public record CreateQuickDraftItineraryCommand(
    string? Title,
    string? DestinationCode,
    int? DayCount,
    DateOnly? StartDate,
    string[]? Interests
) : ICommand<ItineraryDetailDto>;