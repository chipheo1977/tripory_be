namespace Tripory.API.Contracts.V1.Itineraries.Requests;

public record UpdateItineraryMetadataRequest(
    string Title,
    string? Description,
    DateOnly? StartDate,
    string? CoverImageUrl
);
