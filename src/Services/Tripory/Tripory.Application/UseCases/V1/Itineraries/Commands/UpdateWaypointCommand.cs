using BuildingBlocks.Core.CQRS;

namespace Tripory.Application.UseCases.V1.Itineraries.Commands;

public record UpdateWaypointCommand(
    Guid ItineraryId,
    Guid WaypointId,
    string Name,
    string? Address,
    double Longitude,
    double Latitude,
    string? Notes
) : ICommand, IWaypointPayload;
