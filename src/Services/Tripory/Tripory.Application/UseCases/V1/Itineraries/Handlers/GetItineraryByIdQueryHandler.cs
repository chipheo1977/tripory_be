using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Itineraries.Extensions;
using Tripory.Application.UseCases.V1.Itineraries.Queries;
using Tripory.Application.UseCases.V1.Itineraries.Responses;
using Tripory.Domain.Errors;
using Tripory.Domain.Repositories;

namespace Tripory.Application.UseCases.V1.Itineraries.Handlers;

public class GetItineraryByIdQueryHandler : IQueryHandler<GetItineraryByIdQuery, ItineraryDetailDto>
{
    private readonly IItineraryRepository _itineraryRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetItineraryByIdQueryHandler(
        IItineraryRepository itineraryRepository,
        ICurrentUserService currentUserService)
    {
        _itineraryRepository = itineraryRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ItineraryDetailDto>> Handle(GetItineraryByIdQuery request, CancellationToken ct)
    {
        var itinerary = await _itineraryRepository.GetByIdWithDetailsAsync(request.ItineraryId, ct);
        if (itinerary is null)
            return Result.Failure<ItineraryDetailDto>(DomainErrors.Itinerary.NotFound);

        // Quy tắc bảo mật phân quyền truy cập:
        // - Chuyến đi công khai (IsPublic == true): Bất kỳ ai cũng có quyền đọc.
        // - Chuyến đi bản nháp (IsPublic == false): Chỉ DUY NHẤT tác giả mới được phép đọc.
        if (!itinerary.IsPublic)
        {
            if (!_currentUserService.UserId.HasValue || itinerary.UserId != _currentUserService.UserId.Value)
            {
                return Result.Failure<ItineraryDetailDto>(DomainErrors.Itinerary.Forbidden);
            }
        }

        return Result.Success(itinerary.ToDetailDto());
    }
}
