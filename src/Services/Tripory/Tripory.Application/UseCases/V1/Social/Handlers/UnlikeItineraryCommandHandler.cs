using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Social.Commands;
using Tripory.Application.UseCases.V1.Social.Responses;
using Tripory.Domain.Errors;
using Tripory.Domain.Repositories;

namespace Tripory.Application.UseCases.V1.Social.Handlers;

public class UnlikeItineraryCommandHandler : ICommandHandler<UnlikeItineraryCommand, LikeStateDto>
{
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IItineraryLikeRepository _itineraryLikeRepository;
    private readonly ICurrentUserService _currentUserService;

    public UnlikeItineraryCommandHandler(
        IItineraryRepository itineraryRepository,
        IItineraryLikeRepository itineraryLikeRepository,
        ICurrentUserService currentUserService)
    {
        _itineraryRepository = itineraryRepository;
        _itineraryLikeRepository = itineraryLikeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<LikeStateDto>> Handle(UnlikeItineraryCommand request, CancellationToken ct)
    {
        if (_currentUserService.UserId is not Guid userId)
            return Result.Failure<LikeStateDto>(DomainErrors.Auth.Unauthorized);

        var itinerary = await _itineraryRepository.FindByIdAsync(request.ItineraryId, ct);
        if (itinerary is null)
            return Result.Failure<LikeStateDto>(DomainErrors.Itinerary.NotFound);

        var socialResult = itinerary.EnsureSocialAvailable();
        if (socialResult.IsFailure)
            return Result.Failure<LikeStateDto>(socialResult.Error);

        // false = chưa like, vẫn là thành công (idempotent). Repo ghi thẳng xuống DB.
        await _itineraryLikeRepository.RemoveAsync(itinerary.Id, userId, ct);

        var likeCount = await _itineraryLikeRepository.CountByItineraryAsync(itinerary.Id, ct);

        return Result.Success(new LikeStateDto(likeCount, false));
    }
}
