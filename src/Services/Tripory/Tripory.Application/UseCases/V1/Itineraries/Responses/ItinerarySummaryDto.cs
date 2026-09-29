namespace Tripory.Application.UseCases.V1.Itineraries.Responses;

public record ItinerarySummaryDto(
    Guid Id,
    Guid UserId,
    string Title,
    string? Description,
    string? CoverImageUrl,
    DateOnly? StartDate,
    bool IsPublic,
    double TotalDistanceKm,
    int DaysCount,
    int WaypointsCount,
    DateTimeOffset CreatedAt
    // IReadOnlyList<ItineraryDayDto> Days,
    // IReadOnlyList<WaypointDto> Waypoints
);