using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Itineraries.Responses;

namespace Tripory.Application.UseCases.V1.Itineraries.Commands;

public record AddWaypointCommand(
    Guid ItineraryId,
    int DayNumber,
    string Name,
    string? Address,
    double Longitude,
    double Latitude,
    string? Notes
): ICommand<WaypointDto>, IWaypointPayload;