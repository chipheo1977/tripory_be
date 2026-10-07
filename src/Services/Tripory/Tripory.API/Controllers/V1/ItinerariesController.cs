using BuildingBlocks.Core.Abstractions.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tripory.API.Common.Responses;
using Tripory.API.Contracts.V1.Itineraries.Requests;
using Tripory.Application.UseCases.V1.Itineraries.Commands;
using Tripory.Application.UseCases.V1.Itineraries.Queries;
using Tripory.Application.UseCases.V1.Itineraries.Responses;

namespace Tripory.API.Controllers.V1;

[Authorize]
public class ItinerariesController : ApiController
{
    private readonly ILogger<ItinerariesController> _logger;

    public ItinerariesController(ISender sender, ILogger<ItinerariesController> logger) : base(sender)
    {
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ItineraryDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateItineraryRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Khởi tạo nhanh bản nháp hành trình với tiêu đề: {Title}", request.Title);

        var result = await Sender.Send(new CreateQuickDraftItineraryCommand(request.Title), ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value.Id },
            ApiResponse<ItineraryDetailDto>.Success(result.Value, "Tạo bản nháp hành trình thành công."));
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ItinerarySummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMine(
        CancellationToken ct,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isPublic = null)
    {
        _logger.LogInformation(
            "Lấy danh sách hành trình cá nhân, pageIndex: {PageIndex}, pageSize: {PageSize}, isPublic: {IsPublic}",
            pageIndex, pageSize, isPublic);

        var result = await Sender.Send(new GetMyItinerariesQuery(pageIndex, pageSize, isPublic), ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse<PagedResult<ItinerarySummaryDto>>.Success(result.Value));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ItineraryDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        _logger.LogInformation("Lấy chi tiết hành trình với ID: {ItineraryId}", id);

        var result = await Sender.Send(new GetItineraryByIdQuery(id), ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse<ItineraryDetailDto>.Success(result.Value));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMetadata(
        [FromRoute] Guid id,
        [FromBody] UpdateItineraryMetadataRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation("Cập nhật thông tin tổng quan hành trình với ID: {ItineraryId}", id);

        var command = new UpdateItineraryMetadataCommand(
            id, request.Title, request.Description, request.StartDate, request.CoverImageUrl);
        var result = await Sender.Send(command, ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse.Success("Cập nhật hành trình thành công."));
    }

    [HttpPut("{id:guid}/publish")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish([FromRoute] Guid id, CancellationToken ct)
    {
        _logger.LogInformation("Xuất bản hành trình với ID: {ItineraryId}", id);

        var result = await Sender.Send(new PublishItineraryCommand(id), ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse.Success("Xuất bản hành trình thành công."));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        _logger.LogInformation("Xóa hành trình với ID: {ItineraryId}", id);

        var result = await Sender.Send(new DeleteItineraryCommand(id), ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse.Success("Xóa hành trình thành công."));
    }

    [HttpPost("{id:guid}/waypoints")]
    [ProducesResponseType(typeof(ApiResponse<WaypointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddWaypoint(
        [FromRoute] Guid id,
        [FromBody] AddWaypointRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation("Thêm điểm dừng chân vào ngày {DayNumber} của hành trình {ItineraryId}", request.DayNumber, id);

        var command = new AddWaypointCommand(
            id, request.DayNumber, request.Name, request.Address, request.Longitude, request.Latitude, request.Notes);
        var result = await Sender.Send(command, ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse<WaypointDto>.Success(result.Value, "Thêm điểm dừng chân thành công."));
    }

    [HttpPut("{id:guid}/waypoints/{waypointId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWaypoint(
        [FromRoute] Guid id,
        [FromRoute] Guid waypointId,
        [FromBody] UpdateWaypointRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation("Cập nhật điểm dừng chân {WaypointId} của hành trình {ItineraryId}", waypointId, id);

        var command = new UpdateWaypointCommand(
            id, waypointId, request.Name, request.Address, request.Longitude, request.Latitude, request.Notes);
        var result = await Sender.Send(command, ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse.Success("Cập nhật điểm dừng chân thành công."));
    }

    [HttpDelete("{id:guid}/waypoints/{waypointId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWaypoint(
        [FromRoute] Guid id,
        [FromRoute] Guid waypointId,
        CancellationToken ct)
    {
        _logger.LogInformation("Xóa điểm dừng chân {WaypointId} của hành trình {ItineraryId}", waypointId, id);

        var result = await Sender.Send(new DeleteWaypointCommand(id, waypointId), ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse.Success("Xóa điểm dừng chân thành công."));
    }

    [HttpPut("{id:guid}/days/{dayNumber:int}/reorder-waypoints")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReorderWaypoints(
        [FromRoute] Guid id,
        [FromRoute] int dayNumber,
        [FromBody] ReorderWaypointsRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation("Sắp xếp lại điểm dừng chân ngày {DayNumber} của hành trình {ItineraryId}", dayNumber, id);

        var result = await Sender.Send(new ReorderWaypointsCommand(id, dayNumber, request.OrderedWaypointIds), ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse.Success("Sắp xếp điểm dừng chân thành công."));
    }

    [HttpPut("{id:guid}/days/{dayNumber:int}/subtitle")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDaySubtitle(
        [FromRoute] Guid id,
        [FromRoute] int dayNumber,
        [FromBody] SetDaySubtitleRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation("Đặt phụ đề cho ngày {DayNumber} của hành trình {ItineraryId}", dayNumber, id);

        var result = await Sender.Send(new SetDaySubtitleCommand(id, dayNumber, request.Subtitle), ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse.Success("Cập nhật phụ đề ngày thành công."));
    }
}
