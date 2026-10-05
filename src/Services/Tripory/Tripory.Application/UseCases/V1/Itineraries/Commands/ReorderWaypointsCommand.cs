using BuildingBlocks.Core.CQRS;

namespace Tripory.Application.UseCases.V1.Itineraries.Commands;

public record ReorderWaypointsCommand(
    Guid ItineraryId,
    int DayNumber,
    IReadOnlyList<Guid> OrderedWaypointIds
) : ICommand;
