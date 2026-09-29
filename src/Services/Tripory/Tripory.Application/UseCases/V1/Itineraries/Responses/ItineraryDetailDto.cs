namespace Tripory.Application.UseCases.V1.Itineraries.Responses;

public record ItineraryDetailDto(
    Guid Id,
    Guid UserId,
    string Title,
    string? Description,
    string? CoverImageUrl,
    DateOnly? StartDate,
    bool IsPublic,
    double TotalDistanceKm,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ItineraryDayDto> Days,
    IReadOnlyList<WaypointDto> Waypoints
);