namespace Tripory.API.Contracts.V1.Itineraries.Requests;

public record AddWaypointRequest(
    int DayNumber,
    string Name,
    string? Address,
    double Longitude,
    double Latitude,
    string? Notes
);
