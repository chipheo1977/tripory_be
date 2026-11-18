using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Social.Responses;

namespace Tripory.Application.UseCases.V1.Social.Queries;

public record GetLikeStateQuery(
    Guid ItineraryId
) : IQuery<LikeStateDto>;
