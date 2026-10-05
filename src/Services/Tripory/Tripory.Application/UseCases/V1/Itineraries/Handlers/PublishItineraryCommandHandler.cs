using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Itineraries.Commands;
using Tripory.Domain.Repositories;

using Tripory.Application.UseCases.V1.Itineraries.Extensions;
using Tripory.Domain.Errors;

namespace Tripory.Application.UseCases.V1.Itineraries.Handlers;

public class PublishItineraryCommandHandler : ICommandHandler<PublishItineraryCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IItineraryRepository _itineraryRepository;

    public PublishItineraryCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IItineraryRepository itineraryRepository)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _itineraryRepository = itineraryRepository;
    }

    public async Task<Result> Handle(PublishItineraryCommand request, CancellationToken ct)
    {
        var itineraryResult = await _itineraryRepository.GetOwnedItineraryAsync(
            request.ItineraryId, _currentUserService.UserId, includeDetails: true, ct);
        if (itineraryResult.IsFailure)
            return itineraryResult;

        var itinerary = itineraryResult.Value;

        var publishResult = itinerary.Publish();
        if (publishResult.IsFailure)
            return publishResult;

        await _itineraryRepository.UpdateAsync(itinerary);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
