namespace Tripory.Application.UseCases.V1.Itineraries.Responses;

public record ItineraryDayDto(
    Guid Id,
    int DayNumber,
    string? Subtitle,
    double? DayDistanceKm
);