using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Social.Responses;

namespace Tripory.Application.UseCases.V1.Social.Queries;

public record GetCommentsQuery(
    Guid ItineraryId,
    int PageIndex = 1,
    int PageSize = 10
) : IQuery<PagedResult<CommentDto>>;