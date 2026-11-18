using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Itineraries.Extensions;
using Tripory.Application.UseCases.V1.Itineraries.Queries;
using Tripory.Application.UseCases.V1.Itineraries.Responses;
using Tripory.Domain.Errors;
using Tripory.Domain.Repositories;

namespace Tripory.Application.UseCases.V1.Itineraries.Handlers;

public class GetItineraryByIdQueryHandler : IQueryHandler<GetItineraryByIdQuery, ItineraryDetailDto>
{
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IItineraryLikeRepository _itineraryLikeRepository;
    private readonly IItineraryCommentRepository _itineraryCommentRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetItineraryByIdQueryHandler(
        IItineraryRepository itineraryRepository,
        IItineraryLikeRepository itineraryLikeRepository,
        IItineraryCommentRepository itineraryCommentRepository,
        ICurrentUserService currentUserService)
    {
        _itineraryRepository = itineraryRepository;
        _itineraryLikeRepository = itineraryLikeRepository;
        _itineraryCommentRepository = itineraryCommentRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ItineraryDetailDto>> Handle(GetItineraryByIdQuery request, CancellationToken ct)
    {
        var itinerary = await _itineraryRepository.GetByIdWithDetailsAsync(request.ItineraryId, ct);

        if (itinerary is null)
            return Result.Failure<ItineraryDetailDto>(DomainErrors.Itinerary.NotFound);

        var currentUserId = _currentUserService.UserId;

        var isOwner = currentUserId == itinerary.UserId;
        if (!itinerary.IsPublic && !isOwner)
            return Result.Failure<ItineraryDetailDto>(DomainErrors.Itinerary.Forbidden);

        if (!itinerary.IsPublic)
            return Result.Success(itinerary.ToDetailDto());

        var likeCount = await _itineraryLikeRepository.CountByItineraryAsync(itinerary.Id, ct);
        var commentCount = await _itineraryCommentRepository.CountByItineraryAsync(itinerary.Id, ct);

        // Khách luôn likedByMe = false, không gọi DB.
        var likedByMe = currentUserId is Guid userId
            && await _itineraryLikeRepository.ExistsAsync(itinerary.Id, userId, ct);

        return Result.Success(itinerary.ToDetailDto(likeCount, commentCount, likedByMe));
    }
}
