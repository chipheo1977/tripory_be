using System.Security.Claims;
using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Auth.Commands;
using Tripory.Application.UseCases.V1.Auth.Responses;
using Tripory.Domain.Enums;

namespace Tripory.Application.UseCases.V1.Auth.Handlers;

public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, TokenResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<TokenResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal is null)
            return Result.Failure<TokenResponse>(new Error("Token.InvalidAccessToken", "AccessToken không hợp lệ."));

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Result.Failure<TokenResponse>(new Error("Token.InvalidAccessToken", "Không thể định danh người dùng từ token."));

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null || user.Status == UserStatus.Banned)
            return Result.Failure<TokenResponse>(new Error("Token.UserUnavailable", "Người dùng không tồn tại hoặc đã bị khóa."));

        // Kiểm tra token hiện tại trong danh sách
        var existingRefreshToken = user.RefreshTokens.FirstOrDefault(t => t.Token == request.RefreshToken);

        if (existingRefreshToken is null)
            return Result.Failure<TokenResponse>(new Error("Token.InvalidRefreshToken", "RefreshToken không tồn tại."));

        // BẢO VỆ CHỐNG TÁI SỬ DỤNG TOKEN (Token Reuse Detection - BR_AUTH_05)
        if (existingRefreshToken.IsRevoked)
        {
            user.RevokeAllRefreshTokens("Phát hiện tái sử dụng Refresh Token.");
            await _userRepository.UpdateAsync(user, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result.Failure<TokenResponse>(new Error("Token.Compromised", "Phiên đăng nhập bị xâm phạm, vui lòng đăng nhập lại."));
        }

        if (existingRefreshToken.IsExpired)
            return Result.Failure<TokenResponse>(new Error("Token.Expired", "RefreshToken đã hết hạn, vui lòng đăng nhập lại."));

        // Xoay vòng token: Cấp token mới & thu hồi token cũ
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newExpiresAt = DateTimeOffset.UtcNow.AddDays(7);

        user.RevokeRefreshToken(request.RefreshToken, ipAddress: null, replacedByToken: newRefreshToken);
        user.AddRefreshToken(newRefreshToken, newExpiresAt, ipAddress: null);

        var roles = user.UserRoles.Select(r => ((UserRoleType)r.RoleId).ToString()).ToList();
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user, roles);

        await _userRepository.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new TokenResponse(newAccessToken, newRefreshToken, newExpiresAt));
    }
}
