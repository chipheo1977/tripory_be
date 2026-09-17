using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.Common.Models;
using Tripory.Application.UseCases.V1.Auth.Commands;
using Tripory.Application.UseCases.V1.Auth.Responses;
using Tripory.Domain.Enums;
using Tripory.Domain.ValueObjects;

namespace Tripory.Application.UseCases.V1.Auth.Handlers;

public class LoginCommandHandler : ICommandHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure<AuthResponse>(new Error("Auth.InvalidCredentials", "Tài khoản hoặc mật khẩu không chính xác."));

        var user = await _userRepository.GetByEmailAsync(emailResult.Value, ct);
        if (user is null)
            return Result.Failure<AuthResponse>(new Error("Auth.InvalidCredentials", "Tài khoản hoặc mật khẩu không chính xác."));

        if (user.Status == UserStatus.Banned)
            return Result.Failure<AuthResponse>(new Error("Auth.UserBanned", $"Tài khoản đã bị khóa. Lý do: {user.BannedReason ?? "Vi phạm chính sách."}"));

        // Kiểm tra mật khẩu băm
        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
            return Result.Failure<AuthResponse>(new Error("Auth.InvalidCredentials", "Tài khoản hoặc mật khẩu không chính xác."));

        // Nếu trạng thái là Inactive -> Tự động kích hoạt lại theo State Machine trong BRD
        if (user.Status == UserStatus.Inactive)
        {
            user.Activate();
        }

        // Lấy danh sách Roles
        var roles = user.UserRoles
            .Select(r => ((UserRoleType)r.RoleId).ToString())
            .ToList();

        if (roles.Count == 0)
        {
            roles.Add(UserRoleType.Traveler.ToString());
        }

        // Sinh Tokens mới
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        user.AddRefreshToken(refreshToken, expiresAt, ipAddress: null);

        await _userRepository.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var userDto = new UserDto(
            user.Id,
            user.Email.Value,
            user.Handle.Value,
            user.FullName,
            user.Bio,
            user.AvatarUrl,
            user.Status.ToString(),
            roles
        );

        return Result.Success(new AuthResponse(accessToken, refreshToken, expiresAt, userDto));
    }
}
