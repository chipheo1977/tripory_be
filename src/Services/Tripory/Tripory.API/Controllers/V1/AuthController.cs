using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tripory.API.Common.Responses;
using Tripory.Application.UseCases.V1.Auth.Commands;
using Tripory.Application.UseCases.V1.Auth.Responses;

namespace Tripory.API.Controllers.V1;

public class AuthController : ApiController
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ISender sender, ILogger<AuthController> logger)
        : base(sender)
    {
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Nhận yêu cầu đăng ký tài khoản với email: {Email}", command.Email);

        var result = await Sender.Send(command, ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse<AuthResponse>.Success(result.Value, "Đăng ký tài khoản thành công."));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Nhận yêu cầu đăng nhập từ email: {Email}", command.Email);

        var result = await Sender.Send(command, ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse<AuthResponse>.Success(result.Value, "Đăng nhập thành công."));
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Nhận yêu cầu làm mới Access Token.");

        var result = await Sender.Send(command, ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse<TokenResponse>.Success(result.Value, "Làm mới Token thành công."));
    }

    [Authorize]
    [HttpPut("change-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Nhận yêu cầu đổi mật khẩu tài khoản.");

        var result = await Sender.Send(command, ct);
        if (result.IsFailure)
            return HandlerFailure(result);

        return Ok(ApiResponse.Success("Mật khẩu đã được thay đổi thành công. Vui lòng đăng nhập lại."));
    }
}
