using BuildingBlocks.Core.CQRS;

namespace Tripory.Application.UseCases.V1.Itineraries.Command;

public record ReorderWaypointsCommand(
    Guid ItineraryId,
    int DayNumber,
    IReadOnlyList<Guid> OrderedWaypointIds
) : ICommand;
