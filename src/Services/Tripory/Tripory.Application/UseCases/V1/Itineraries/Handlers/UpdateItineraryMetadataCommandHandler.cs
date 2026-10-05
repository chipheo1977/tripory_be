using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Itineraries.Commands;
using Tripory.Application.UseCases.V1.Itineraries.Responses;
using Tripory.Domain.Repositories;
using Tripory.Domain.ValueObjects;

using Tripory.Application.UseCases.V1.Itineraries.Extensions;
using Tripory.Domain.Errors;

namespace Tripory.Application.UseCases.V1.Itineraries.Handlers;

public class UpdateItineraryMetadataCommandHandler : ICommandHandler<UpdateItineraryMetadataCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IItineraryRepository _itineraryRepository;

    public UpdateItineraryMetadataCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IItineraryRepository itineraryRepository)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _itineraryRepository = itineraryRepository;
    }

    public async Task<Result> Handle(UpdateItineraryMetadataCommand request, CancellationToken ct)
    {
        var itineraryResult = await _itineraryRepository.GetOwnedItineraryAsync(
            request.ItineraryId, _currentUserService.UserId, includeDetails: false, ct);
        if (itineraryResult.IsFailure)
            return Result.Failure(itineraryResult.Error);

        var itinerary = itineraryResult.Value;

        var title = ItineraryTitle.Create(request.Title);
        if (title.IsFailure)
            return Result.Failure(title.Error);

        var description = ItineraryDescription.Create(request.Description);
        if (description.IsFailure)
            return Result.Failure(description.Error);

        itinerary.UpdateMetadata(title.Value, description.Value, request.StartDate, request.CoverImageUrl);
        await _itineraryRepository.UpdateAsync(itinerary);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}