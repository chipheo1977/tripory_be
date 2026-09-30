using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Itineraries.Extensions;
using Tripory.Application.UseCases.V1.Itineraries.Queries;
using Tripory.Application.UseCases.V1.Itineraries.Responses;
using Tripory.Domain.Errors;
using Tripory.Domain.Repositories;

namespace Tripory.Application.UseCases.V1.Itineraries.Handlers;

public class GetMyItinerariesQueryHandler : IQueryHandler<GetMyItinerariesQuery, PagedResult<ItinerarySummaryDto>>
{
    private readonly IItineraryRepository _itineraryRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMyItinerariesQueryHandler(
        IItineraryRepository itineraryRepository,
        ICurrentUserService currentUserService)
    {
        _itineraryRepository = itineraryRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PagedResult<ItinerarySummaryDto>>> Handle(GetMyItinerariesQuery request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure<PagedResult<ItinerarySummaryDto>>(DomainErrors.Auth.Unauthorized);

        var currentUserId = _currentUserService.UserId.Value;

        var (items, totalCount) = await _itineraryRepository.GetMyItinerariesPagedAsync(
            currentUserId,
            request.PageIndex,
            request.PageSize,
            request.IsPublic,
            ct
        );

        var dtos = items.Select(x => x.ToSummaryDto()).ToList();

        var pagedResult = new PagedResult<ItinerarySummaryDto>(
            dtos,
            request.PageIndex,
            request.PageSize,
            totalCount
        );

        return Result.Success(pagedResult);
    }
}
