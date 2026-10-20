using BuildingBlocks.Core.CQRS;

namespace Tripory.Application.UseCases.V1.Auth.Commands;

/// <summary>
/// Đăng xuất: thu hồi đúng một Refresh Token thuộc về người dùng đang đăng nhập.
/// </summary>
public record LogoutCommand(
    string RefreshToken
) : ICommand;
