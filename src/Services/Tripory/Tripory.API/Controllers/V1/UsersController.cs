using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tripory.API.Common.Responses;
using Tripory.Application.UseCases.V1.Users.Commands;
using Tripory.Application.UseCases.V1.Users.Queries;
using Tripory.Application.UseCases.V1.Users.Responses;

namespace Tripory.API.Controllers.V1;

[Authorize]
public class UsersController : ApiController
{
    private readonly ILogger<UsersController> _logger;

    public UsersController(ISender sender, ILogger<UsersController> logger)
        : base(sender)
    {
        _logger = logger;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        _logger.LogInformation("Người dùng yêu cầu lấy thông tin hồ sơ cá nhân.");

        var result = await Sender.Send(new GetCurrentUserProfileQuery(), ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse<UserProfileResponse>.Success(result.Value));
    }

    [HttpPut("profile")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Người dùng yêu cầu cập nhật hồ sơ cá nhân.");

        var result = await Sender.Send(command, ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse.Success("Cập nhật hồ sơ cá nhân thành công."));
    }
}
