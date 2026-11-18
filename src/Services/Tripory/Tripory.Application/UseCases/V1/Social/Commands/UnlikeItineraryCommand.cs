using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Social.Responses;

namespace Tripory.Application.UseCases.V1.Social.Commands;

public record UnlikeItineraryCommand (
    Guid ItineraryId
): ICommand<LikeStateDto>;