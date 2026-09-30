using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Itineraries.Command;
using Tripory.Application.UseCases.V1.Itineraries.Extensions;
using Tripory.Domain.Abstractions.External;
using Tripory.Domain.Repositories;
using Tripory.Domain.ValueObjects;

namespace Tripory.Application.UseCases.V1.Itineraries.Handlers;

public class UpdateWaypointCommandHandler : ICommandHandler<UpdateWaypointCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IGisDistanceCalculator _gisCalculator;

    public UpdateWaypointCommandHandler(
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

    public async Task<Result> Handle(UpdateWaypointCommand request, CancellationToken ct)
    {
        var itineraryResult = await _itineraryRepository.GetOwnedItineraryAsync(
            request.ItineraryId,
            _currentUserService.UserId,
            includeDetails: true,
            ct
        );
        if (itineraryResult.IsFailure)
            return Result.Failure(itineraryResult.Error);

        var nameResult = WaypointName.Create(request.Name);
        if (nameResult.IsFailure)
            return Result.Failure(nameResult.Error);

        var coordResult = Wgs84Coordinate.Create(request.Longitude, request.Latitude);
        if (coordResult.IsFailure)
            return Result.Failure(coordResult.Error);

        var itinerary = itineraryResult.Value;

        var updateResult = itinerary.UpdateWaypoint(
            request.WaypointId,
            nameResult.Value,
            request.Address,
            coordResult.Value,
            request.Notes
        );
        if (updateResult.IsFailure)
            return updateResult;

        // Tự động tính toán lại cự ly theo quy chuẩn BR_01 / PT-05
        itinerary.RecalculateDistances(_gisCalculator);

        await _itineraryRepository.UpdateAsync(itinerary);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
