namespace Tripory.API.Contracts.V1.Itineraries.Requests;

public record UpdateWaypointRequest(
    string Name,
    string? Address,
    double Longitude,
    double Latitude,
    string? Notes
);
