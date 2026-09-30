using BuildingBlocks.Core.CQRS;

namespace Tripory.Application.UseCases.V1.Itineraries.Command;

public record SetDaySubtitleCommand(
    Guid ItineraryId,
    int DayNumber,
    string? Subtitle
) : ICommand;
