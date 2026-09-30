using BuildingBlocks.Core.CQRS;

namespace Tripory.Application.UseCases.V1.Itineraries.Command;

public record PublishItineraryCommand(Guid ItineraryId) : ICommand;
