using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Itineraries.Command;
using Tripory.Application.UseCases.V1.Itineraries.Extensions;
using Tripory.Application.UseCases.V1.Itineraries.Responses;
using Tripory.Domain.Abstractions.External;
using Tripory.Domain.Errors;
using Tripory.Domain.Repositories;
using Tripory.Domain.ValueObjects;

namespace Tripory.Application.UseCases.V1.Itineraries.Handlers;

public class AddWaypointCommandHandler : ICommandHandler<AddWaypointCommand, WaypointDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IGisDistanceCalculator _gisCalculator;
    public AddWaypointCommandHandler(
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

    public async Task<Result<WaypointDto>> Handle(AddWaypointCommand request, CancellationToken ct)
    {
        var currentUserId = _currentUserService.UserId;
        var itinerary = await _itineraryRepository.GetOwnedItineraryAsync(
            request.ItineraryId,
            currentUserId,
            includeDetails: true,
            ct
        );
        if (itinerary.IsFailure)
            return Result.Failure<WaypointDto>(itinerary.Error);

        var waypoint = WaypointName.Create(request.Name);
        if (waypoint.IsFailure)
            return Result.Failure<WaypointDto>(waypoint.Error);

        var coordinate = Wgs84Coordinate.Create(request.Longitude, request.Latitude);
        if (coordinate.IsFailure)
            return Result.Failure<WaypointDto>(coordinate.Error);

        var addResult = itinerary.Value.AddWaypoint(
            request.DayNumber,
            waypoint.Value,
            request.Address,
            coordinate.Value,
            request.Notes
        );

        itinerary.Value.RecalculateDistances(_gisCalculator);

        return Result.Success(addResult.Value.ToDto());
    }
}