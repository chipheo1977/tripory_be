namespace Tripory.API.Contracts.V1.Itineraries.Requests;

public record CreateItineraryRequest(
    string? Title,
    string? DestinationCode,
    int? DayCount,
    DateOnly? StartDate,
    string[]? Interests
);
