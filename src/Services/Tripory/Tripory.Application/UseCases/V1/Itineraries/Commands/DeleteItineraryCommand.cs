using BuildingBlocks.Core.CQRS;

namespace Tripory.Application.UseCases.V1.Itineraries.Commands;

public record DeleteItineraryCommand(Guid ItineraryId) : ICommand;
