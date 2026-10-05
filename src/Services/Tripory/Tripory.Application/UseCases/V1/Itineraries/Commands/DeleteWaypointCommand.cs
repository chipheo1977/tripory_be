using BuildingBlocks.Core.CQRS;

namespace Tripory.Application.UseCases.V1.Itineraries.Commands;

public record DeleteWaypointCommand(
    Guid ItineraryId,
    Guid WaypointId
) : ICommand;
