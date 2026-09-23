using BuildingBlocks.Core.Domains.Abstractions;
using BuildingBlocks.Core.Domains.Abstractions.DDD;
using BuildingBlocks.Core.Abstractions.Shared;
using Tripory.Domain.Enums;
using Tripory.Domain.ValueObjects;

namespace Tripory.Domain.Entities;

public class User : EntityAuditBase<Guid>, IAggregateRoot
{
    public const string DefaultAvatarUrl = "https://api.dicebear.com/7.x/identicon/svg?seed=tripory";
    public const int MaxBioLength = 250;

    public Email Email { get; private set; } = null!;
    public Handle Handle { get; private set; } = null!;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? Bio { get; private set; }
    public string AvatarUrl { get; private set; } = DefaultAvatarUrl;
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public string? BannedReason { get; private set; }

    // SocialLinks & TravelPreferences sẽ được bổ sung ở giai đoạn tiếp.

    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() { }

    public static Result<User> Create(
        Email email,
        Handle handle,
        string passwordHash,
        string fullName,
        string? bio = null,
        string? avatarUrl = null
    )
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure<User>(new Error("User.InvalidPassword", "Mật khẩu mã hóa không hợp lệ."));

        if (string.IsNullOrWhiteSpace(fullName))
            return Result.Failure<User>(new Error("User.InvalidFullName", "Họ và tên không được để trống."));
    
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? DefaultAvatarUrl : avatarUrl.Trim(),
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Tự động sinh Handle nếu chưa có
        user.Handle = handle ?? Handle.GenerateDefault(email.Value.Split('@')[0]);

        // Mặc định gán vai trò Traveler
        user._userRoles.Add(new UserRole(user.Id, (int)UserRoleType.Traveler));

        return Result.Success(user);
    }
    
    public Result UpdateProfile(string fullName, string? bio, string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return Result.Failure(new Error("User.InvalidFullName", "Họ và tên không được để trống."));

        if (bio?.Length > MaxBioLength)
            return Result.Failure(new Error("User.BioTooLong", $"Tiểu sử không được vượt quá {MaxBioLength} ký tự."));

        FullName = fullName.Trim();
        Bio = bio?.Trim();

        if (!string.IsNullOrWhiteSpace(avatarUrl))
            AvatarUrl = avatarUrl.Trim();

        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public void ChangeHandle(Handle newHandle)
    {
        Handle = newHandle;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTimeOffset.UtcNow;

        // Thu hồi toàn bộ Refresh Tokens để bắt buộc đăng nhập lại trên mọi thiết bị
        RevokeAllRefreshTokens("Password changed");
    }

    public void Ban(string reason)
    {
        Status = UserStatus.Banned;
        BannedReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
        RevokeAllRefreshTokens($"Banned: {reason}");
    }

    public void Activate()
    {
        Status = UserStatus.Active;
        BannedReason = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddRefreshToken(string token, DateTimeOffset expiresAt, string? ipAddress)
    {
        // Thu hồi các token đã hết hạn trước khi thêm mới
        _refreshTokens.RemoveAll(t => t.IsExpired && t.IsRevoked);
        _refreshTokens.Add(new RefreshToken(Id, token, expiresAt, ipAddress));
    }

    public Result RevokeRefreshToken(string token, string? ipAddress, string? replacedByToken = null)
    {
        var existingToken = _refreshTokens.FirstOrDefault(t => t.Token == token);
        if (existingToken is null || !existingToken.IsActive)
            return Result.Failure(new Error("Token.Invalid", "Token không tồn tại hoặc đã bị thu hồi."));

        existingToken.Revoke(ipAddress, replacedByToken);
        return Result.Success();
    }

    public void RevokeAllRefreshTokens(string? ipAddress)
    {
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            token.Revoke(ipAddress);
        }
    }

    public void AssignRole(UserRoleType roleType)
    {
        if (_userRoles.All(r => r.RoleId != (int)roleType))
        {
            _userRoles.Add(new UserRole(Id, (int)roleType));
        }
    }
}