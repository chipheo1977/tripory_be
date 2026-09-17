using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.Common.Models;
using Tripory.Application.UseCases.V1.Auth.Commands;
using Tripory.Application.UseCases.V1.Auth.Responses;
using Tripory.Domain.Entities;
using Tripory.Domain.Enums;
using Tripory.Domain.ValueObjects;
using UserHandle = Tripory.Domain.ValueObjects.Handle;

namespace Tripory.Application.UseCases.V1.Auth.Handlers;

public class RegisterCommandHandler : ICommandHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public RegisterCommandHandler(
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

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken ct)
    {
        // 1. Kiểm tra Value Object Email
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure<AuthResponse>(emailResult.Error);

        var email = emailResult.Value;

        // 2. Kiểm tra tính duy nhất của Email
        var isUnique = await _userRepository.IsEmailUniqueAsync(email, ct);
        if (!isUnique)
            return Result.Failure<AuthResponse>(new Error("User.EmailAlreadyExists", "Email này đã được đăng ký trong hệ thống."));

        // 3. Băm mật khẩu
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // 4. Sinh Handle tự động từ prefix email theo BR_AUTH_02
        var prefix = email.Value.Split('@')[0];
        var handle = UserHandle.GenerateDefault(prefix);

        // 5. Khởi tạo Entity User (Domain Invariants được bảo vệ tại Factory Create)
        var userResult = User.Create(
            email: email,
            handle: handle,
            passwordHash: passwordHash,
            fullName: request.FullName
        );

        if (userResult.IsFailure)
            return Result.Failure<AuthResponse>(userResult.Error);

        var user = userResult.Value;

        // 6. Sinh Tokens
        var roles = new[] { UserRoleType.Traveler.ToString() };
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        // Lưu Refresh Token vào User Aggregate Root
        user.AddRefreshToken(refreshToken, expiresAt, ipAddress: null);

        // 7. Lưu trữ qua Repository & UnitOfWork
        await _userRepository.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 8. Trả kết quả chuẩn hóa
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
