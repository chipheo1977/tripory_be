using BuildingBlocks.Core.CQRS;

namespace Tripory.Application.UseCases.V1.Itineraries.Commands;

public record PublishItineraryCommand(Guid ItineraryId) : ICommand;
