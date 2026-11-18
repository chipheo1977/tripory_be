using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Social.Queries;
using Tripory.Application.UseCases.V1.Social.Responses;
using Tripory.Domain.Errors;
using Tripory.Domain.Repositories;

namespace Tripory.Application.UseCases.V1.Social.Handlers;

public class GetLikeStateQueryHandler : IQueryHandler<GetLikeStateQuery, LikeStateDto>
{
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IItineraryLikeRepository _itineraryLikeRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetLikeStateQueryHandler(
        IItineraryRepository itineraryRepository,
        IItineraryLikeRepository itineraryLikeRepository,
        ICurrentUserService currentUserService)
    {
        _itineraryRepository = itineraryRepository;
        _itineraryLikeRepository = itineraryLikeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<LikeStateDto>> Handle(GetLikeStateQuery request, CancellationToken ct)
    {
        var itinerary = await _itineraryRepository.FindByIdAsync(request.ItineraryId, ct);
        if (itinerary is null)
            return Result.Failure<LikeStateDto>(DomainErrors.Itinerary.NotFound);

        var socialResult = itinerary.EnsureSocialAvailable();
        if (socialResult.IsFailure)
            return Result.Failure<LikeStateDto>(socialResult.Error);

        var likeCount = await _itineraryLikeRepository.CountByItineraryAsync(itinerary.Id, ct);

        // Khách luôn likedByMe = false, không gọi DB.
        var likedByMe = _currentUserService.UserId is Guid userId
            && await _itineraryLikeRepository.ExistsAsync(itinerary.Id, userId, ct);

        return Result.Success(new LikeStateDto(likeCount, likedByMe));
    }
}
