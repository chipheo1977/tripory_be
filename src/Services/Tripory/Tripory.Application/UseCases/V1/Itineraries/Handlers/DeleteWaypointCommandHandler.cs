using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Itineraries.Command;
using Tripory.Application.UseCases.V1.Itineraries.Extensions;
using Tripory.Domain.Abstractions.External;
using Tripory.Domain.Repositories;

namespace Tripory.Application.UseCases.V1.Itineraries.Handlers;

public class DeleteWaypointCommandHandler : ICommandHandler<DeleteWaypointCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IGisDistanceCalculator _gisCalculator;

    public DeleteWaypointCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IItineraryRepository itineraryRepository,
        IGisDistanceCalculator gisCalculator)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _itineraryRepository = itineraryRepository;
        _gisCalculator = gisCalculator;
    }

    public async Task<Result> Handle(DeleteWaypointCommand request, CancellationToken ct)
    {
        var itineraryResult = await _itineraryRepository.GetOwnedItineraryAsync(
            request.ItineraryId,
            _currentUserService.UserId,
            includeDetails: true,
            ct
        );
        if (itineraryResult.IsFailure)
            return Result.Failure(itineraryResult.Error);

        var itinerary = itineraryResult.Value;

        var removeResult = itinerary.RemoveWaypoint(request.WaypointId);
        if (removeResult.IsFailure)
            return removeResult;

        // Tự động chuẩn hóa lại cự ly các ngày và toàn bộ hành trình theo BR_01 / PT-05
        itinerary.RecalculateDistances(_gisCalculator);

        await _itineraryRepository.UpdateAsync(itinerary);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
