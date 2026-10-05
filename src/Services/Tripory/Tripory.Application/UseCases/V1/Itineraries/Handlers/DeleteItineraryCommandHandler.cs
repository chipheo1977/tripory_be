using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Itineraries.Commands;
using Tripory.Domain.Repositories;

using Tripory.Application.UseCases.V1.Itineraries.Extensions;
using Tripory.Domain.Errors;

namespace Tripory.Application.UseCases.V1.Itineraries.Handlers;

public class DeleteItineraryCommandHandler : ICommandHandler<DeleteItineraryCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IItineraryRepository _itineraryRepository;

    public DeleteItineraryCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IItineraryRepository itineraryRepository)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _itineraryRepository = itineraryRepository;
    }

    public async Task<Result> Handle(DeleteItineraryCommand request, CancellationToken ct)
    {
        var itineraryResult = await _itineraryRepository.GetOwnedItineraryAsync(
            request.ItineraryId, _currentUserService.UserId, includeDetails: false, ct);
        if (itineraryResult.IsFailure)
            return itineraryResult;

        var itinerary = itineraryResult.Value;

        itinerary.MarkAsDeleted(_currentUserService.UserId!.Value);
        await _itineraryRepository.UpdateAsync(itinerary);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
