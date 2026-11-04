namespace Tripory.Application.UseCases.V1.Itineraries.Responses;

public record ItinerarySummaryDto(
    Guid Id,
    Guid UserId,
    string Title,
    string? Description,
    string? CoverImageUrl,
    IReadOnlyList<string> CoverImages,
    int? SelectedCoverIndex,
    string? DestinationCode,
    DateOnly? StartDate,
    bool IsPublic,
    double TotalDistanceKm,
    int DaysCount,
    int WaypointsCount,
    DateTimeOffset CreatedAt
    // IReadOnlyList<ItineraryDayDto> Days,
    // IReadOnlyList<WaypointDto> Waypoints
);