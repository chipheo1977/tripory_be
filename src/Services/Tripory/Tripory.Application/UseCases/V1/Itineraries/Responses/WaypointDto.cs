namespace Tripory.Application.UseCases.V1.Itineraries.Responses;

public record WaypointDto(
    Guid Id,
    int DayNumber,
    int OrderIndex,
    string Name,
    string? Address,
    double Longitude,
    double Latitude,
    string? Notes
);