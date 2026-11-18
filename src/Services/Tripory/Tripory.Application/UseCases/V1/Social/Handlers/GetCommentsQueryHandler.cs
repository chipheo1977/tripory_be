using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.UseCases.V1.Social.Extensions;
using Tripory.Application.UseCases.V1.Social.Queries;
using Tripory.Application.UseCases.V1.Social.Responses;
using Tripory.Domain.Errors;
using Tripory.Domain.Repositories;

namespace Tripory.Application.UseCases.V1.Social.Handlers;

public class GetCommentsQueryHandler : IQueryHandler<GetCommentsQuery, PagedResult<CommentDto>>
{
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IItineraryCommentRepository _itineraryCommentRepository;
    private readonly IUserRepository _userRepository;

    public GetCommentsQueryHandler(
        IItineraryRepository itineraryRepository,
        IItineraryCommentRepository itineraryCommentRepository,
        IUserRepository userRepository)
    {
        _itineraryRepository = itineraryRepository;
        _itineraryCommentRepository = itineraryCommentRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<PagedResult<CommentDto>>> Handle(GetCommentsQuery request, CancellationToken ct)
    {
        var itineraryId = request.ItineraryId;

        var itinerary = await _itineraryRepository.FindByIdAsync(itineraryId, ct);
        if (itinerary is null)
            return Result.Failure<PagedResult<CommentDto>>(DomainErrors.Itinerary.NotFound);

        var socialResult = itinerary.EnsureSocialAvailable();
        if (socialResult.IsFailure)
            return Result.Failure<PagedResult<CommentDto>>(socialResult.Error);

        var (items, totalCount) = await _itineraryCommentRepository.GetByItineraryPagedAsync(
            itineraryId,
            request.PageIndex,
            request.PageSize,
            ct
        );

        var authorIds = items.Select(x => x.UserId).ToList();
        var authors = await _userRepository.GetSummariesByIdsAsync(authorIds, ct);
        var authorsById = authors.ToDictionary(a => a.Id);

        var dtos = items
            .Select(c => c.ToDto(authorsById.GetValueOrDefault(c.UserId)))
            .ToList();

        var pagedResult = new PagedResult<CommentDto>(
            dtos,
            request.PageIndex,
            request.PageSize,
            totalCount
        );

        return Result.Success(pagedResult);
    }
}
