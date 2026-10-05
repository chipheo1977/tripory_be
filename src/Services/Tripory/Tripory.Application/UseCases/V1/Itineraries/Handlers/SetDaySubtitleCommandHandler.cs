using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Itineraries.Commands;
using Tripory.Application.UseCases.V1.Itineraries.Extensions;
using Tripory.Domain.Repositories;

namespace Tripory.Application.UseCases.V1.Itineraries.Handlers;

public class SetDaySubtitleCommandHandler : ICommandHandler<SetDaySubtitleCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IItineraryRepository _itineraryRepository;

    public SetDaySubtitleCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IItineraryRepository itineraryRepository)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _itineraryRepository = itineraryRepository;
    }

    public async Task<Result> Handle(SetDaySubtitleCommand request, CancellationToken ct)
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

        var result = itinerary.SetDaySubtitle(request.DayNumber, request.Subtitle);
        if (result.IsFailure)
            return result;

        await _itineraryRepository.UpdateAsync(itinerary);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
