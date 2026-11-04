using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.DestinationCover;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Itineraries.Commands;
using Tripory.Application.UseCases.V1.Itineraries.Responses;
using Tripory.Domain.Entities;
using Tripory.Domain.Repositories;
using Tripory.Domain.ValueObjects;
using Tripory.Application.UseCases.V1.Itineraries.Extensions;

using Tripory.Domain.Errors;

namespace Tripory.Application.UseCases.V1.Itineraries.Handlers;

public class CreateQuickDraftItineraryCommandHandler : ICommandHandler<CreateQuickDraftItineraryCommand, ItineraryDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IDestinationCoverProvider _destinationCoverProvider;

    public CreateQuickDraftItineraryCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IItineraryRepository itineraryRepository,
        IDestinationCoverProvider destinationCoverProvider
    )
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _itineraryRepository = itineraryRepository;
        _destinationCoverProvider = destinationCoverProvider;
    }

    public async Task<Result<ItineraryDetailDto>> Handle(CreateQuickDraftItineraryCommand request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure<ItineraryDetailDto>(DomainErrors.Auth.Unauthorized);
    
        var currentUserId = _currentUserService.UserId.Value;
        var titleResult = ItineraryTitle.Create(request.Title);
        if (titleResult.IsFailure)
            return Result.Failure<ItineraryDetailDto>(titleResult.Error);

        var defaultCoverUrl = _destinationCoverProvider.GetDefaultCoverUrl(request.DestinationCode);
        string[]? coverImageUrls = defaultCoverUrl is null ? null : [defaultCoverUrl];

        var itineraryResult = Itinerary.CreateQuickDraft(
            currentUserId,
            titleResult.Value,
            request.DayCount,
            request.DestinationCode,
            request.StartDate,
            request.Interests,
            coverImageUrls
        );
        if (itineraryResult.IsFailure)
            return Result.Failure<ItineraryDetailDto>(itineraryResult.Error);

        await _itineraryRepository.AddAsync(itineraryResult.Value, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(itineraryResult.Value.ToDetailDto());
    }
}