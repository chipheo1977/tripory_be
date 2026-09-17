# HƯỚNG DẪN TRIỂN KHAI BƯỚC 4: APPLICATION LAYER (`Tripory.Application`)
### Kèm Phân Tích Kiến Trúc 5W1H Để Tech Lead Tự Gõ Mã Nguồn

> **Dự án:** Tripory Backend (`tripory_be`)  
> **Giai đoạn:** Phase 01 – Identity Foundation  
> **Bước thực hiện:** Bước 04 – Application Layer (CQRS UseCases, Ports, Behaviors & Validation)  
> **Tiêu chuẩn tuân thủ:** Clean Architecture + DDD + CQRS (Pattern 1 & Pattern 2 từ `AGENTS.md`)  
> **Tài liệu đặc tả đối chiếu:** `../tripory/docs/AUTH_AND_USER_01/auth_and_user.md` (Mục 3, BR_AUTH_01 đến BR_AUTH_05)

---

## 📌 MỤC LỤC TRÌNH TỰ THỰC HIỆN

1. [Cài đặt Thư viện Phụ thuộc (CLI)](#-1-cài-đặt-thư-viện-phụ-thuộc-cli)
2. [Khối 1: Các Cổng Trừu Tượng (Abstractions / Ports - DIP)](#-khối-1-các-cổng-trừu-tượng-abstractions--ports---dip)
   * 1.1 `IUserRepository.cs`
   * 1.2 `IPasswordHasher.cs`
   * 1.3 `IJwtTokenService.cs`
   * 1.4 `ICurrentUserService.cs`
3. [Khối 2: Models & Response DTOs Chia Sẻ](#-khối-2-models--response-dtos-chia-sẻ)
   * 2.1 `UserDto.cs`
   * 2.2 `AuthResponse.cs`
   * 2.3 `TokenResponse.cs`
4. [Khối 3: Pipeline Behavior Bắt Lỗi Validation Tự Động](#-khối-3-pipeline-behavior-bắt-lỗi-validation-tự-động)
   * 3.1 `ValidationPipelineBehavior.cs`
5. [Khối 4: Vertical Feature Slice – Phân Hệ Xác Thực (`Auth`)](#-khối-4-vertical-feature-slice--phân-hệ-xác-thực-auth)
   * 4.1 UseCase Đăng Ký Tài Khoản (`RegisterCommand`)
   * 4.2 UseCase Đăng Nhập (`LoginCommand`)
   * 4.3 UseCase Cấp Mới Token Xoay Vòng (`RefreshTokenCommand`)
   * 4.4 UseCase Đổi Mật Khẩu (`ChangePasswordCommand`)
6. [Khối 5: Vertical Feature Slice – Phân Hệ Hồ Sơ Người Dùng (`Users`)](#-khối-5-vertical-feature-slice--phân-hệ-hồ-sơ-người-dùng-users)
   * 5.1 `UserProfileResponse.cs`
   * 5.2 UseCase Xem Hồ Sơ Hiện Tại (`GetCurrentUserProfileQuery`)
   * 5.3 UseCase Cập Nhật Hồ Sơ (`UpdateUserProfileCommand`)
7. [Khối 6: Đăng Ký Dependency Injection & Kiểm Chứng](#-khối-6-đăng-ký-dependency-injection--kiểm-chứng)
   * 6.1 `DependencyInjection.cs`
   * 6.2 Lệnh Build & Verification

---

## 💻 1. CÀI ĐẶT THƯ VIỆN PHỤ THUỘC (CLI)

Tầng Application chỉ phụ thuộc vào `FluentValidation` để tự động hóa kiểm tra dữ liệu đầu vào của các Command/Query.

Chạy 2 lệnh CLI sau tại thư mục gốc `tripory_be`:

```bash
dotnet add src/Services/Tripory/Tripory.Application/Tripory.Application.csproj package FluentValidation --version 12.1.1
dotnet add src/Services/Tripory/Tripory.Application/Tripory.Application.csproj package FluentValidation.DependencyInjectionExtensions --version 12.1.1
```

---

## 🏛️ KHỐI 1: CÁC CỔNG TRỪU TƯỢNG (ABSTRACTIONS / PORTS - DIP)

### 1.1 `IUserRepository.cs`
* **Why (Tại sao):** Tuân thủ Dependency Inversion Principle (DIP). Handler cần đọc/ghi dữ liệu `User` Aggregate nhưng tuyệt đối không được tham chiếu trực tiếp `DbContext` hay thư viện ORM cụ thể của Persistence.
* **What (Là cái gì):** Interface định nghĩa hợp đồng thao tác với Aggregate Root `User`.
* **Who (Ai phụ trách):** Tầng `Application` sở hữu interface; Tầng `Persistence` sẽ hiện thực hóa sau.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Application/Abstractions/Data/IUserRepository.cs`
* **When (Khi nào gọi):** Trong các Handler khi cần tìm User theo Email, Id, Handle hoặc lưu trữ User.
* **How (Mã nguồn):**

```csharp
using Tripory.Domain.Entities;
using Tripory.Domain.ValueObjects;

namespace Tripory.Application.Abstractions.Data;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default);
    Task<User?> GetByHandleAsync(Handle handle, CancellationToken ct = default);
    Task<bool> IsEmailUniqueAsync(Email email, CancellationToken ct = default);
    Task<bool> IsHandleUniqueAsync(Handle handle, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}
```

---

### 1.2 `IPasswordHasher.cs`
* **Why (Tại sao):** Handler không được tự ý băm mật khẩu hay phụ thuộc thư viện mã hóa cụ thể (`BCrypt.Net`). Cần trừu tượng hóa để dễ thay đổi thuật toán mã hóa hoặc mock trong Unit Test.
* **What (Là cái gì):** Interface cung cấp hàm băm mật khẩu an toàn và hàm kiểm tra mật khẩu trùng khớp.
* **Who (Ai phụ trách):** Tầng `Application` định nghĩa; Tầng `Infrastructure` triển khai bằng BCrypt work-factor 12.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Application/Abstractions/Security/IPasswordHasher.cs`
* **When (Khi nào gọi):** Khi Đăng ký (`Register`), Đăng nhập (`Login`) và Đổi mật khẩu (`ChangePassword`).
* **How (Mã nguồn):**

```csharp
namespace Tripory.Application.Abstractions.Security;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
```

---

### 1.3 `IJwtTokenService.cs`
* **Why (Tại sao):** Quá trình sinh token liên quan đến việc ký mã mật mã học (`System.IdentityModel.Tokens.Jwt`). Application không nên gánh trách nhiệm mã hóa này mà chỉ yêu cầu sinh token dựa trên entity `User` và danh sách vai trò.
* **What (Là cái gì):** Interface quản lý sinh Access Token (chứa Claims), Refresh Token ngẫu nhiên cryptographically secure, và đọc Claim từ token hết hạn.
* **Who (Ai phụ trách):** Tầng `Application` định nghĩa; Tầng `Infrastructure` hiện thực hóa.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Application/Abstractions/Security/IJwtTokenService.cs`
* **When (Khi nào gọi):** Khi Đăng ký thành công, Đăng nhập thành công, và khi Xoay vòng Refresh Token.
* **How (Mã nguồn):**

```csharp
using System.Security.Claims;
using Tripory.Domain.Entities;

namespace Tripory.Application.Abstractions.Security;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
```

---

### 1.4 `ICurrentUserService.cs`
* **Why (Tại sao):** Các nghiệp vụ cần biết danh tính người dùng đang thực hiện request (như đổi mật khẩu, xem profile, tạo lịch trình), nhưng tầng Application không được truy cập trực tiếp `HttpContext` của ASP.NET Core.
* **What (Là cái gì):** Interface đọc thông tin người dùng từ ClaimsPrincipal của HTTP request hiện hành.
* **Who (Ai phụ trách):** Tầng `Application` định nghĩa; Tầng `Infrastructure` (hoặc `API`) hiện thực hóa qua `IHttpContextAccessor`.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Application/Abstractions/Security/ICurrentUserService.cs`
* **When (Khi nào gọi):** Bất kỳ Handler nào cần truy xuất `UserId` của phiên đăng nhập hiện hành.
* **How (Mã nguồn):**

```csharp
namespace Tripory.Application.Abstractions.Security;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
}
```

---

## 📦 KHỐI 2: MODELS & RESPONSE DTOS CHIA SẺ

### 2.1 `UserDto.cs`
* **Why (Tại sao):** Tuyệt đối không trả Domain Entity `User` ra ngoài API response (tránh lộ `PasswordHash`, `RefreshTokens`). Cần DTO phẳng, an toàn để hiển thị.
* **What (Là cái gì):** C# Record chứa thông tin tóm tắt của User.
* **Who (Ai phụ trách):** Tầng `Application`.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Application/Common/Models/UserDto.cs`
* **When (Khi nào gọi):** Làm payload kèm theo trong `AuthResponse` hoặc các truy vấn danh sách người dùng.
* **How (Mã nguồn):**

```csharp
namespace Tripory.Application.Common.Models;

public record UserDto(
    Guid Id,
    string Email,
    string Handle,
    string FullName,
    string? Bio,
    string AvatarUrl,
    string Status,
    IReadOnlyList<string> Roles
);
```

---

### 2.2 `AuthResponse.cs` & 2.3 `TokenResponse.cs`
* **Why (Tại sao):** Chuẩn hóa cấu trúc dữ liệu trả về cho client sau khi xác thực thành công.
* **What (Là cái gì):** C# Record chứa Access Token, Refresh Token, thời hạn hết hạn và thông tin người dùng.
* **Where (Vị trí file):** 
  * `src/Services/Tripory/Tripory.Application/UseCases/V1/Auth/Responses/AuthResponse.cs`
  * `src/Services/Tripory/Tripory.Application/UseCases/V1/Auth/Responses/TokenResponse.cs`
* **How (Mã nguồn):**

*Tạo file `AuthResponse.cs`:*
```csharp
using Tripory.Application.Common.Models;

namespace Tripory.Application.UseCases.V1.Auth.Responses;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    UserDto User
);
```

*Tạo file `TokenResponse.cs`:*
```csharp
namespace Tripory.Application.UseCases.V1.Auth.Responses;

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt
);
```

---

## ⚙️ KHỐI 3: PIPELINE BEHAVIOR BẮT LỖI VALIDATION TỰ ĐỘNG

### 3.1 `ValidationPipelineBehavior.cs`
* **Why (Tại sao):** Tránh viết lặp đi lặp lại code kiểm tra `validator.Validate(request)` ở đầu mỗi Handler. Mọi request đi qua MediatR sẽ tự động được kiểm tra qua tất cả các FluentValidation Validator tương ứng. Nếu có lỗi, ngắt luồng và trả về `Result.Failure` ngay lập tức.
* **What (Là cái gì):** MediatR `IPipelineBehavior<TRequest, TResponse>` chặn trước khi request tới Handler.
* **Who (Ai phụ trách):** Tầng `Application`.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Application/Behaviors/ValidationPipelineBehavior.cs`
* **When (Khi nào gọi):** Tự động kích hoạt mỗi khi `Sender.Send(command/query)` được gọi trong Controller.
* **How (Mã nguồn):**

```csharp
using BuildingBlocks.Core.Abstractions.Shared;
using FluentValidation;
using MediatR;

namespace Tripory.Application.Behaviors;

public class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
        {
            var firstFailure = failures[0];
            var error = new Error(firstFailure.ErrorCode ?? "Validation.Error", firstFailure.ErrorMessage);

            // Xử lý tạo kết quả Failure cho cả Result và Result<T>
            if (typeof(TResponse) == typeof(Result))
            {
                return (TResponse)(object)Result.Failure(error);
            }

            var resultType = typeof(TResponse);
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var valueType = resultType.GetGenericArguments()[0];
                var failureMethod = typeof(Result)
                    .GetMethods()
                    .First(m => m.Name == nameof(Result.Failure) && m.IsGenericMethod)
                    .MakeGenericMethod(valueType);

                return (TResponse)failureMethod.Invoke(null, new object[] { error })!;
            }

            return (TResponse)(object)Result.Failure(error);
        }

        return await next();
    }
}
```

---

## 🛡️ KHỐI 4: VERTICAL FEATURE SLICE – PHÂN HỆ XÁC THỰC (`Auth`)

### 4.1 UseCase Đăng Ký Tài Khoản (`RegisterCommand`)

#### File 1: Command Record
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Auth/Commands/RegisterCommand.cs`
* **Why:** Định nghĩa dữ liệu đầu vào bất biến (Immutable Record) để tạo mới tài khoản.
* **How:**

```csharp
using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Auth.Responses;

namespace Tripory.Application.UseCases.V1.Auth.Commands;

public record RegisterCommand(
    string Email,
    string Password,
    string FullName
) : ICommand<AuthResponse>;
```

#### File 2: Validator
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Auth/Validators/RegisterCommandValidator.cs`
* **Why:** Bảo đảm mật khẩu có độ phức tạp cao, họ tên không rỗng trước khi chạm vào Domain logic.
* **How:**

```csharp
using FluentValidation;
using Tripory.Application.UseCases.V1.Auth.Commands;

namespace Tripory.Application.UseCases.V1.Auth.Validators;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Định dạng email không hợp lệ.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên không được để trống.")
            .MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(8).WithMessage("Mật khẩu phải có tối thiểu 8 ký tự.")
            .Matches(@"[A-Z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ hoa.")
            .Matches(@"[a-z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ thường.")
            .Matches(@"[0-9]").WithMessage("Mật khẩu phải chứa ít nhất một chữ số.")
            .Matches(@"[\!\?\*\@\#\$\%\^\&\+\=]").WithMessage("Mật khẩu phải chứa ít nhất một ký tự đặc biệt.");
    }
}
```

#### File 3: Handler
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Auth/Handlers/RegisterCommandHandler.cs`
* **Why:** Đóng vai trò Orchestrator (Pattern 2): Kiểm tra trùng lặp email $\rightarrow$ Băm mật khẩu $\rightarrow$ Gọi Entity `User.Create` $\rightarrow$ Sinh token $\rightarrow$ Lưu DB $\rightarrow$ Trả `AuthResponse`.
* **How:**

```csharp
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
        var handle = Handle.GenerateDefault(prefix);

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
```

---

### 4.2 UseCase Đăng Nhập (`LoginCommand`)

#### File 1: Command Record
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Auth/Commands/LoginCommand.cs`
* **How:**

```csharp
using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Auth.Responses;

namespace Tripory.Application.UseCases.V1.Auth.Commands;

public record LoginCommand(
    string Email,
    string Password
) : ICommand<AuthResponse>;
```

#### File 2: Validator
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Auth/Validators/LoginCommandValidator.cs`
* **How:**

```csharp
using FluentValidation;
using Tripory.Application.UseCases.V1.Auth.Commands;

namespace Tripory.Application.UseCases.V1.Auth.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Định dạng email không hợp lệ.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.");
    }
}
```

#### File 3: Handler
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Auth/Handlers/LoginCommandHandler.cs`
* **Why:** Kiểm tra User tồn tại, kiểm tra tài khoản có bị khóa (`Banned`), đối soát hash mật khẩu, cấp cặp token mới và lưu RefreshToken.
* **How:**

```csharp
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
```

---

### 4.3 UseCase Cấp Mới Token Xoay Vòng (`RefreshTokenCommand`)

#### File 1: Command Record
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Auth/Commands/RefreshTokenCommand.cs`
* **How:**

```csharp
using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Auth.Responses;

namespace Tripory.Application.UseCases.V1.Auth.Commands;

public record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken
) : ICommand<TokenResponse>;
```

#### File 2: Validator
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Auth/Validators/RefreshTokenCommandValidator.cs`
* **How:**

```csharp
using FluentValidation;
using Tripory.Application.UseCases.V1.Auth.Commands;

namespace Tripory.Application.UseCases.V1.Auth.Validators;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.AccessToken).NotEmpty().WithMessage("AccessToken không được để trống.");
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("RefreshToken không được để trống.");
    }
}
```

#### File 3: Handler
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Auth/Handlers/RefreshTokenCommandHandler.cs`
* **Why:** Thực thi quy tắc an ninh `BR_AUTH_05`: Refresh Token Rotation. Token cũ bị thu hồi, cấp cặp token mới. Nếu phát hiện token cũ đã bị thu hồi trước đó (bị tấn công tái sử dụng), toàn bộ refresh tokens của user bị thu hồi ngay lập tức.
* **How:**

```csharp
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
```

---

### 4.4 UseCase Đổi Mật Khẩu (`ChangePasswordCommand`)

#### File 1: Command Record
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Auth/Commands/ChangePasswordCommand.cs`
* **How:**

```csharp
using BuildingBlocks.Core.CQRS;

namespace Tripory.Application.UseCases.V1.Auth.Commands;

public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword
) : ICommand;
```

#### File 2: Validator
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Auth/Validators/ChangePasswordCommandValidator.cs`
* **How:**

```csharp
using FluentValidation;
using Tripory.Application.UseCases.V1.Auth.Commands;

namespace Tripory.Application.UseCases.V1.Auth.Validators;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Mật khẩu hiện tại không được để trống.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
            .MinimumLength(8).WithMessage("Mật khẩu mới phải có tối thiểu 8 ký tự.")
            .Matches(@"[A-Z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ hoa.")
            .Matches(@"[a-z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ thường.")
            .Matches(@"[0-9]").WithMessage("Mật khẩu phải chứa ít nhất một chữ số.")
            .Matches(@"[\!\?\*\@\#\$\%\^\&\+\=]").WithMessage("Mật khẩu phải chứa ít nhất một ký tự đặc biệt.")
            .NotEqual(x => x.CurrentPassword).WithMessage("Mật khẩu mới không được trùng với mật khẩu hiện tại.");
    }
}
```

#### File 3: Handler
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Auth/Handlers/ChangePasswordCommandHandler.cs`
* **Why:** Đổi mật khẩu thành công sẽ gọi `user.ChangePassword(...)`, phương thức này tự động thu hồi toàn bộ Refresh Tokens để buộc đăng nhập lại trên mọi thiết bị.
* **How:**

```csharp
using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Auth.Commands;

namespace Tripory.Application.UseCases.V1.Auth.Handlers;

public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure(new Error("Auth.Unauthorized", "Yêu cầu đăng nhập để thực hiện thao tác."));

        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId.Value, ct);
        if (user is null)
            return Result.Failure(new Error("User.NotFound", "Không tìm thấy thông tin người dùng."));

        // Xác thực mật khẩu cũ
        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return Result.Failure(new Error("Auth.InvalidCurrentPassword", "Mật khẩu hiện tại không chính xác."));

        // Băm mật khẩu mới & Cập nhật qua Domain Entity
        var newHash = _passwordHasher.HashPassword(request.NewPassword);
        user.ChangePassword(newHash);

        await _userRepository.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
```

---

## 👤 KHỐI 5: VERTICAL FEATURE SLICE – PHÂN HỆ HỒ SƠ NGƯỜI DÙNG (`Users`)

### 5.1 `UserProfileResponse.cs`
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Users/Responses/UserProfileResponse.cs`
* **How:**

```csharp
namespace Tripory.Application.UseCases.V1.Users.Responses;

public record UserProfileResponse(
    Guid Id,
    string Email,
    string Handle,
    string FullName,
    string? Bio,
    string AvatarUrl,
    string Status,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt
);
```

---

### 5.2 UseCase Xem Hồ Sơ Hiện Tại (`GetCurrentUserProfileQuery`)

#### File 1: Query Record
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Users/Queries/GetCurrentUserProfileQuery.cs`
* **How:**

```csharp
using BuildingBlocks.Core.CQRS;
using Tripory.Application.UseCases.V1.Users.Responses;

namespace Tripory.Application.UseCases.V1.Users.Queries;

public record GetCurrentUserProfileQuery : IQuery<UserProfileResponse>;
```

#### File 2: Handler
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Users/Handlers/GetCurrentUserProfileQueryHandler.cs`
* **How:**

```csharp
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Users.Queries;
using Tripory.Application.UseCases.V1.Users.Responses;
using Tripory.Domain.Enums;

namespace Tripory.Application.UseCases.V1.Users.Handlers;

public class GetCurrentUserProfileQueryHandler : IQueryHandler<GetCurrentUserProfileQuery, UserProfileResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserProfileQueryHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<UserProfileResponse>> Handle(GetCurrentUserProfileQuery request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure<UserProfileResponse>(new Error("Auth.Unauthorized", "Yêu cầu đăng nhập để xem thông tin."));

        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId.Value, ct);
        if (user is null)
            return Result.Failure<UserProfileResponse>(new Error("User.NotFound", "Không tìm thấy người dùng."));

        var roles = user.UserRoles.Select(r => ((UserRoleType)r.RoleId).ToString()).ToList();

        var response = new UserProfileResponse(
            user.Id,
            user.Email.Value,
            user.Handle.Value,
            user.FullName,
            user.Bio,
            user.AvatarUrl,
            user.Status.ToString(),
            roles,
            user.CreatedAt
        );

        return Result.Success(response);
    }
}
```

---

### 5.3 UseCase Cập Nhật Hồ Sơ (`UpdateUserProfileCommand`)

#### File 1: Command Record
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Users/Commands/UpdateUserProfileCommand.cs`
* **How:**

```csharp
using BuildingBlocks.Core.CQRS;

namespace Tripory.Application.UseCases.V1.Users.Commands;

public record UpdateUserProfileCommand(
    string FullName,
    string? Bio,
    string? AvatarUrl
) : ICommand;
```

#### File 2: Validator
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Users/Validators/UpdateUserProfileCommandValidator.cs`
* **How:**

```csharp
using FluentValidation;
using Tripory.Application.UseCases.V1.Users.Commands;

namespace Tripory.Application.UseCases.V1.Users.Validators;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên không được để trống.")
            .MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự.");

        RuleFor(x => x.Bio)
            .MaximumLength(250).WithMessage("Tiểu sử không được vượt quá 250 ký tự.");
    }
}
```

#### File 3: Handler
* **Where:** `src/Services/Tripory/Tripory.Application/UseCases/V1/Users/Handlers/UpdateUserProfileCommandHandler.cs`
* **How:**

```csharp
using BuildingBlocks.Core.Abstractions.Persistence;
using BuildingBlocks.Core.Abstractions.Shared;
using BuildingBlocks.Core.CQRS;
using Tripory.Application.Abstractions.Data;
using Tripory.Application.Abstractions.Security;
using Tripory.Application.UseCases.V1.Users.Commands;

namespace Tripory.Application.UseCases.V1.Users.Handlers;

public class UpdateUserProfileCommandHandler : ICommandHandler<UpdateUserProfileCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateUserProfileCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(UpdateUserProfileCommand request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Result.Failure(new Error("Auth.Unauthorized", "Yêu cầu đăng nhập."));

        var user = await _userRepository.GetByIdAsync(_currentUserService.UserId.Value, ct);
        if (user is null)
            return Result.Failure(new Error("User.NotFound", "Không tìm thấy người dùng."));

        // Gọi phương thức Domain Entity để tự bảo vệ Invariant
        var updateResult = user.UpdateProfile(request.FullName, request.Bio, request.AvatarUrl);
        if (updateResult.IsFailure)
            return updateResult;

        await _userRepository.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
```

---

## 🔌 KHỐI 6: ĐĂNG KÝ DEPENDENCY INJECTION & KIỂM CHỨNG

### 6.1 `DependencyInjection.cs`
* **Why (Tại sao):** Tự động hóa đăng ký tất cả Handlers, Validators và Validation Behaviors của Tầng Application vào DI Service Collection.
* **What (Là cái gì):** Extension method `AddApplication(this IServiceCollection services)`.
* **Where (Vị trí file):** `src/Services/Tripory/Tripory.Application/DependencyInjection.cs`
* **How (Mã nguồn):**

```csharp
using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tripory.Application.Behaviors;

namespace Tripory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Đăng ký MediatR Handlers
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            
            // Đăng ký Validation Pipeline Behavior
            cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

        // Đăng ký tất cả FluentValidation Validators
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
```

---

## 🎯 6.2 LỆNH BUILD & XÁC THỰC (VERIFICATION)

Sau khi anh hoàn thành việc gõ các file trên, chạy lệnh kiểm tra tính toàn vẹn và chuẩn cú pháp:

```bash
dotnet build src/Services/Tripory/Tripory.Application/Tripory.Application.csproj
```

**Tiêu chuẩn nghiệm thu:** Lệnh chạy trả về:
```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```
Không có bất kỳ cảnh báo hoặc lỗi biên dịch nào!
