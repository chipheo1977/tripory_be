using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Itineraries.Responses;

namespace Tripory.Application.UseCases.V1.Itineraries.Commands;

public record UpdateItineraryMetadataCommand(
    Guid ItineraryId,
    string Title,
    string? Description,
    DateOnly? StartDate,
    string? CoverImageUrl
) : ICommand;