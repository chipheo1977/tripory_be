namespace Tripory.Application.UseCases.V1.Itineraries.Responses;

public record ItineraryDetailDto(
    Guid Id,
    Guid UserId,
    string Title,
    string? Description,
    string? CoverImageUrl,
    IReadOnlyList<string> CoverImages,
    int? SelectedCoverIndex,
    string? DestinationCode,
    IReadOnlyList<string> Interests,
    DateOnly? StartDate,
    bool IsPublic,
    double TotalDistanceKm,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ItineraryDayDto> Days,
    IReadOnlyList<WaypointDto> Waypoints
);