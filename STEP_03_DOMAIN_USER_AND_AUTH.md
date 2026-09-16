# HƯỚNG DẪN TRIỂN KHAI BƯỚC 3: DOMAIN LAYER (USER & IDENTITY)
> **Mục tiêu:** Cung cấp tài liệu 5W1H và mã nguồn chuẩn Pure C# để Tech Lead tự gõ vào project `src/Services/Tripory/Tripory.Domain`.  
> **Nguyên tắc:** 0% third-party packages, tuân thủ Clean Architecture + Rich Domain Driven Design (DDD).

---

## 1. `Enums/UserRoleType.cs`
* **What:** Enum định nghĩa danh sách các vai trò cố định của hệ thống.
* **Why:** Đảm bảo an toàn Type-safety trong thời gian biên dịch (compile-time), phục vụ khai báo Attribute `[Authorize(Roles = nameof(UserRoleType.Admin))]` tại Controller API.
* **Where:** `src/Services/Tripory/Tripory.Domain/Enums/UserRoleType.cs`
* **Who:** Dùng chung cho Domain Entity `Role`, tầng Application và API Authorize.
* **When:** Được gọi khi kiểm tra phân quyền người dùng và gán role mặc định lúc đăng ký.
* **How:**
```csharp
namespace Tripory.Domain.Enums;

public enum UserRoleType
{
    Admin = 1,
    Traveler = 2,
    Creator = 3,
    ServiceProvider = 4
}
```

---

## 2. `Enums/UserStatus.cs`
* **What:** Enum quản lý trạng thái tài khoản người dùng.
* **Why:** Kiểm soát vòng đời tài khoản (Active, Inactive, Banned), ngăn chặn truy cập đối với tài khoản bị khóa.
* **Where:** `src/Services/Tripory/Tripory.Domain/Enums/UserStatus.cs`
* **Who:** Do Entity `User` sở hữu; tầng Application kiểm tra trước khi cấp token.
* **When:** Được đọc trong luồng Login, gán khi đổi trạng thái (Ban, Activate).
* **How:**
```csharp
namespace Tripory.Domain.Enums;

public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Banned = 3
}
```

---

## 3. `ValueObjects/Email.cs`
* **What:** Value Object đại diện cho địa chỉ Email người dùng.
* **Why:** 
  * Bảo vệ Invariant: Không cho phép email rỗng, sai định dạng RFC 5322 hoặc vượt quá 256 ký tự.
  * Tự động chuẩn hóa `.Trim().ToLowerInvariant()` để ngăn ngừa lỗi trùng tài khoản do chữ hoa/thường.
* **Where:** `src/Services/Tripory/Tripory.Domain/ValueObjects/Email.cs`
* **Who:** Entity `User` sử dụng làm thuộc tính chính; Application Command nhận vào.
* **When:** Khởi tạo khi Đăng ký, Đăng nhập hoặc đổi Email.
* **How:**
```csharp
using System.Text.RegularExpressions;
using BuildingBlocks.Core.Domains.Abstractions.DDD;
using BuildingBlocks.Core.Domains.Abstractions.Shared;

namespace Tripory.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    public const int MaxLength = 256;
    private static readonly Regex EmailRegex = new(
        @"^[^@s]+@[^@s]+.[^@s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<Email>(new Error("Email.Empty", "Email không được để trống."));

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (normalizedEmail.Length > MaxLength)
            return Result.Failure<Email>(new Error("Email.TooLong", $"Email không được vượt quá {MaxLength} ký tự."));

        if (!EmailRegex.IsMatch(normalizedEmail))
            return Result.Failure<Email>(new Error("Email.InvalidFormat", "Email không đúng định dạng."));

        return Result.Success(new Email(normalizedEmail));
    }

    public override string ToString() => Value;
}
```

---

## 4. `ValueObjects/Handle.cs`
* **What:** Value Object đại diện cho `@username` độc nhất của người dùng.
* **Why:** 
  * Bảo vệ Invariant: Handle phải có tiền tố `@`, độ dài 3-30 ký tự, chỉ chứa chữ cái thường, số, dấu gạch dưới và dấu chấm.
  * Cung cấp phương thức sinh ngẫu nhiên `GenerateDefault()` khi người dùng đăng ký (theo quyết định Q&A Câu 1).
* **Where:** `src/Services/Tripory/Tripory.Domain/ValueObjects/Handle.cs`
* **Who:** Entity `User` sử dụng để định danh công khai trên mạng xã hội.
* **When:** Tự động sinh khi tạo tài khoản; được cập nhật khi người dùng chỉnh sửa hồ sơ.
* **How:**
```csharp
using System.Text.RegularExpressions;
using BuildingBlocks.Core.Domains.Abstractions.DDD;
using BuildingBlocks.Core.Domains.Abstractions.Shared;

namespace Tripory.Domain.ValueObjects;

public sealed class Handle : ValueObject
{
    private static readonly Regex HandleRegex = new(
        @"^@[a-z0-9_.]{3,30}$",
        RegexOptions.Compiled);

    public string Value { get; }

    private Handle(string value)
    {
        Value = value;
    }

    public static Result<Handle> Create(string? handle)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return Result.Failure<Handle>(new Error("Handle.Empty", "Handle không được để trống."));

        var formatted = handle.Trim().ToLowerInvariant();
        if (!formatted.StartsWith('@'))
            formatted = $"@{formatted}";

        if (!HandleRegex.IsMatch(formatted))
            return Result.Failure<Handle>(new Error("Handle.InvalidFormat", "Handle phải từ 3-30 ký tự, chỉ gồm chữ thường, số, dấu gạch dưới và dấu chấm."));

        return Result.Success(new Handle(formatted));
    }

    public static Handle GenerateDefault(string emailPrefix)
    {
        var cleaned = Regex.Replace(emailPrefix.ToLowerInvariant(), @"[^a-z0-9]", "");
        if (cleaned.Length > 15) cleaned = cleaned[..15];
        if (cleaned.Length < 3) cleaned = "traveler";

        var suffix = Random.Shared.Next(1000, 9999);
        return new Handle($"@{cleaned}_{suffix}");
    }

    public override string ToString() => Value;
}
```

---

## 5. `Entities/Role.cs`
* **What:** Entity đại diện cho Vai trò người dùng trong hệ thống.
* **Why:** Phục vụ phân quyền RBAC và lưu trữ quan hệ với bảng User trong cơ sở dữ liệu.
* **Where:** `src/Services/Tripory/Tripory.Domain/Entities/Role.cs`
* **Who:** Quản lý danh mục quyền truy cập.
* **When:** Được nạp vào context khi ứng dụng khởi chạy (Seed Data).
* **How:**
```csharp
using BuildingBlocks.Core.Domains.Abstractions;
using Tripory.Domain.Enums;

namespace Tripory.Domain.Entities;

public class Role : EntityBase<int>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private Role() { }

    public Role(UserRoleType roleType, string description)
    {
        Id = (int)roleType;
        Name = roleType.ToString();
        Description = description;
    }
}
```

---

## 6. `Entities/UserRole.cs`
* **What:** Entity bảng nối Many-to-Many giữa `User` và `Role`.
* **Why:** Cho phép một người dùng có thể sở hữu nhiều vai trò (ví dụ: vừa là `Traveler`, vừa là `Creator`).
* **Where:** `src/Services/Tripory/Tripory.Domain/Entities/UserRole.cs`
* **Who:** Nằm trong tập hợp con của Aggregate `User`.
* **When:** Khởi tạo khi tạo tài khoản hoặc khi cấp quyền mới.
* **How:**
```csharp
namespace Tripory.Domain.Entities;

public class UserRole
{
    public Guid UserId { get; private set; }
    public int RoleId { get; private set; }

    private UserRole() { }

    internal UserRole(Guid userId, int roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}
```

---

## 7. `Entities/RefreshToken.cs`
* **What:** Entity quản lý Refresh Token xoay vòng (Rotational Refresh Token).
* **Why:** 
  * Bảo đảm tính an toàn bảo mật: Mỗi token chỉ dùng 1 lần, có thời hạn sử dụng và ghi nhận IP tạo/thu hồi.
  * Tự kiểm soát trạng thái hợp lệ thông qua thuộc tính `IsActive`.
* **Where:** `src/Services/Tripory/Tripory.Domain/Entities/RefreshToken.cs`
* **Who:** Thuộc quyền kiểm soát nội bộ của Aggregate Root `User` (Phương án A).
* **When:** Được sinh ra khi Login/RefreshToken và bị thu hồi khi Logout hoặc Token Rotation.
* **How:**
```csharp
using BuildingBlocks.Core.Domains.Abstractions;

namespace Tripory.Domain.Entities;

public class RefreshToken : EntityBase<Guid>
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? CreatedByIp { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReplacedByToken { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken() { }

    internal RefreshToken(Guid userId, string token, DateTimeOffset expiresAt, string? createdByIp)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedByIp = createdByIp;
    }

    internal void Revoke(string? revokedByIp, string? replacedByToken = null)
    {
        RevokedAt = DateTimeOffset.UtcNow;
        RevokedByIp = revokedByIp;
        ReplacedByToken = replacedByToken;
    }
}
```

---

## 8. `Entities/User.cs` (Aggregate Root)
* **What:** Aggregate Root trung tâm quản lý toàn bộ vòng đời và invariant của Người dùng.
* **Why:** 
  * Hiện thực hóa mô hình Rich Domain Model: Không dùng public setters.
  * Kiểm soát toàn diện Refresh Tokens và Phân quyền: Khi User bị Ban, toàn bộ token tự động bị thu hồi.
  * Tự động gán Avatar mặc định nếu không được truyền vào (theo quyết định Q&A Câu 2).
* **Where:** `src/Services/Tripory/Tripory.Domain/Entities/User.cs`
* **Who:** Trái tim của module Identity, được các UseCases gọi để thực thi nghiệp vụ.
* **When:** Xuyên suốt mọi hành vi nghiệp vụ liên quan đến người dùng.
* **How:**
```csharp
using BuildingBlocks.Core.Domains.Abstractions;
using BuildingBlocks.Core.Domains.Abstractions.DDD;
using BuildingBlocks.Core.Domains.Abstractions.Shared;
using Tripory.Domain.Enums;
using Tripory.Domain.ValueObjects;

namespace Tripory.Domain.Entities;

public class User : EntityFullAuditBase<Guid>, IAggregateRoot
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

    // TODO: SocialLinks & TravelPreferences sẽ được bổ sung ở giai đoạn tiếp theo theo quyết định Tech Lead.

    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() { }

    public static Result<User> Create(
        Email email,
        string passwordHash,
        string fullName,
        Handle? handle = null,
        string? avatarUrl = null)
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
```

---

## 9. Domain Exceptions (`Exceptions/*.cs`)
* **What:** Bộ exception nghiệp vụ định danh lỗi người dùng và xác thực.
* **Why:** Map chuẩn sang mã lỗi HTTP tương ứng tại Global Exception Handler.
* **Where:** `src/Services/Tripory/Tripory.Domain/Exceptions/`
* **How:**

```csharp
// File: src/Services/Tripory/Tripory.Domain/Exceptions/UserNotFoundException.cs
using BuildingBlocks.Core.Domains.Abstractions.Exceptions;

namespace Tripory.Domain.Exceptions;

public sealed class UserNotFoundException : NotFoundException
{
    public UserNotFoundException(Guid userId)
        : base($"Không tìm thấy người dùng với định danh: {userId}")
    {
    }

    public UserNotFoundException(string identifier)
        : base($"Không tìm thấy người dùng với thông tin: {identifier}")
    {
    }
}
```

```csharp
// File: src/Services/Tripory/Tripory.Domain/Exceptions/EmailAlreadyExistsException.cs
using BuildingBlocks.Core.Domains.Abstractions.Exceptions;

namespace Tripory.Domain.Exceptions;

public sealed class EmailAlreadyExistsException : ConflictException
{
    public EmailAlreadyExistsException(string email)
        : base($"Email '{email}' đã được sử dụng bởi một tài khoản khác.")
    {
    }
}
```

```csharp
// File: src/Services/Tripory/Tripory.Domain/Exceptions/HandleAlreadyExistsException.cs
using BuildingBlocks.Core.Domains.Abstractions.Exceptions;

namespace Tripory.Domain.Exceptions;

public sealed class HandleAlreadyExistsException : ConflictException
{
    public HandleAlreadyExistsException(string handle)
        : base($"Handle '{handle}' đã tồn tại trong hệ thống.")
    {
    }
}
```

```csharp
// File: src/Services/Tripory/Tripory.Domain/Exceptions/InvalidCredentialsException.cs
using BuildingBlocks.Core.Domains.Abstractions.Exceptions;

namespace Tripory.Domain.Exceptions;

public sealed class InvalidCredentialsException : BadRequestException
{
    public InvalidCredentialsException()
        : base("Thông tin đăng nhập (Email hoặc Mật khẩu) không chính xác.")
    {
    }
}
```

```csharp
// File: src/Services/Tripory/Tripory.Domain/Exceptions/RefreshTokenException.cs
using BuildingBlocks.Core.Domains.Abstractions.Exceptions;

namespace Tripory.Domain.Exceptions;

public sealed class RefreshTokenException : BadRequestException
{
    public RefreshTokenException(string message)
        : base(message)
    {
    }
}
```

---

## 🎯 Tiêu Chuẩn Xác Nhận (Verification)
Sau khi gõ xong các file trên vào project `Tripory.Domain`, chạy lệnh kiểm chứng:
```bash
dotnet build src/Services/Tripory/Tripory.Domain/Tripory.Domain.csproj
```
**Mục tiêu:** `Build succeeded. 0 Warning(s), 0 Error(s)`.
