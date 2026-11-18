using BuildingBlocks.Core.Abstractions.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tripory.API.Common.Responses;
using Tripory.API.Contracts.V1.Itineraries.Requests;
using Tripory.API.RateLimiting;
using Tripory.Application.UseCases.V1.Social.Commands;
using Tripory.Application.UseCases.V1.Social.Queries;
using Tripory.Application.UseCases.V1.Social.Responses;

namespace Tripory.API.Controllers.V1;

public partial class ItinerariesController
{
    private const int MaxCommentBodyBytes = 4 * 1024;

    [HttpPut("{id:guid}/like")]
    [EnableRateLimiting(RateLimitPolicies.SocialLike)]
    [ProducesResponseType(typeof(ApiResponse<LikeStateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Like([FromRoute] Guid id, CancellationToken ct)
    {
        _logger.LogInformation("Thích hành trình với ID: {ItineraryId}", id);

        var result = await Sender.Send(new LikeItineraryCommand(id), ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse<LikeStateDto>.Success(result.Value));
    }

    [HttpDelete("{id:guid}/like")]
    [EnableRateLimiting(RateLimitPolicies.SocialLike)]
    [ProducesResponseType(typeof(ApiResponse<LikeStateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Unlike([FromRoute] Guid id, CancellationToken ct)
    {
        _logger.LogInformation("Bỏ thích hành trình với ID: {ItineraryId}", id);

        var result = await Sender.Send(new UnlikeItineraryCommand(id), ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse<LikeStateDto>.Success(result.Value));
    }

    [HttpGet("{id:guid}/like")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LikeStateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLikeState([FromRoute] Guid id, CancellationToken ct)
    {
        _logger.LogInformation("Lấy trạng thái thích của hành trình với ID: {ItineraryId}", id);

        var result = await Sender.Send(new GetLikeStateQuery(id), ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse<LikeStateDto>.Success(result.Value));
    }

    [HttpGet("{id:guid}/comments")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CommentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComments(
        [FromRoute] Guid id,
        CancellationToken ct,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation(
            "Lấy danh sách bình luận của hành trình {ItineraryId}, pageIndex: {PageIndex}, pageSize: {PageSize}",
            id, pageIndex, pageSize);

        var result = await Sender.Send(new GetCommentsQuery(id, pageIndex, pageSize), ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse<PagedResult<CommentDto>>.Success(result.Value));
    }

    [HttpPost("{id:guid}/comments")]
    [RequestSizeLimit(MaxCommentBodyBytes)]
    [EnableRateLimiting(RateLimitPolicies.SocialCommentCreate)]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> AddComment(
        [FromRoute] Guid id,
        [FromBody] AddCommentRequest request,
        CancellationToken ct)
    {
        // Không ghi nội dung bình luận vào log.
        _logger.LogInformation("Gửi bình luận cho hành trình với ID: {ItineraryId}", id);

        var result = await Sender.Send(new AddCommentCommand(id, request.Content), ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<CommentDto>.Success(result.Value, "Gửi bình luận thành công."));
    }

    [HttpDelete("{id:guid}/comments/{commentId:guid}")]
    [EnableRateLimiting(RateLimitPolicies.SocialCommentDelete)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DeleteComment(
        [FromRoute] Guid id,
        [FromRoute] Guid commentId,
        CancellationToken ct)
    {
        _logger.LogInformation("Xóa bình luận {CommentId} của hành trình {ItineraryId}", commentId, id);

        var result = await Sender.Send(new DeleteCommentCommand(id, commentId), ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse.Success("Xóa bình luận thành công."));
    }
}
