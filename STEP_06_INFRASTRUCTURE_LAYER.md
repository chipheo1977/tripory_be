# HƯỚNG DẪN TRIỂN KHAI BƯỚC 6: INFRASTRUCTURE LAYER (SECURITY, BCRYPT & JWT)
> **Mục tiêu:** Cung cấp tài liệu 5W1H và mã nguồn chuẩn cho tầng bảo mật & tích hợp ngoài `Tripory.Infrastructure`.  
> **Nguyên tắc:** Hiện thực hóa các Ports bảo mật từ Application (`IPasswordHasher`, `IJwtTokenService`, `ICurrentUserService`), tuân thủ chuẩn mã hóa BCrypt work-factor 12 và JWT Access Token / Refresh Token an toàn.

---

## 1. Cập Nhật Packages: `Tripory.Infrastructure.csproj`
* **What:** File cấu hình dự án của tầng Infrastructure.
* **Why:** 
  * Thêm `<FrameworkReference Include="Microsoft.AspNetCore.App" />` để sử dụng `IHttpContextAccessor`, `ClaimsPrincipal` và các thành phần xác thực ASP.NET Core gốc mà không bị xung đột gói.
  * Cài đặt `BCrypt.Net-Next` cho băm mật khẩu và `System.IdentityModel.Tokens.Jwt` cho sinh/giải mã JWT.
* **Where:** `src/Services/Tripory/Tripory.Infrastructure/Tripory.Infrastructure.csproj`
* **How:**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\Tripory.Application\Tripory.Application.csproj" />
    <ProjectReference Include="..\Tripory.Domain\Tripory.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.14.0" />
    <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="8.14.0" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

---

## 2. `Configurations/JwtOptions.cs`
* **What:** POCO class chứa các tham số cấu hình JWT Token.
* **Why:** Đóng gói cài đặt (SecretKey, Issuer, Audience, thời hạn Access Token và Refresh Token) theo Options Pattern, dễ dàng đọc từ `appsettings.json`.
* **Where:** `src/Services/Tripory/Tripory.Infrastructure/Configurations/JwtOptions.cs`
* **How:**
```csharp
namespace Tripory.Infrastructure.Configurations;

public class JwtOptions
{
    public const string SectionName = "JwtOptions";

    public string SecretKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int ExpiryMinutes { get; init; } = 15;
    public int RefreshTokenExpiryDays { get; init; } = 7;
}
```

---

## 3. `Implementations/Security/BcryptPasswordHasher.cs`
* **What:** Hiện thực hóa interface `IPasswordHasher` từ Application Layer.
* **Why:** 
  * Sử dụng thuật toán BCrypt với chế độ tăng cường (Enhanced) và work-factor = 12, chống lại các cuộc tấn công brute-force và rainbow table.
  * Tách biệt thuật toán băm khỏi Domain và Application (Application chỉ gọi hàm băm mà không phụ thuộc thư viện BCrypt cụ thể).
* **Where:** `src/Services/Tripory/Tripory.Infrastructure/Implementations/Security/BcryptPasswordHasher.cs`
* **Who:** Đăng ký Singleton; được các Handlers (`RegisterCommandHandler`, `LoginCommandHandler`, `ChangePasswordCommandHandler`) inject để sử dụng.
* **How:**
```csharp
using Tripory.Application.Abstractions.Security;

namespace Tripory.Infrastructure.Implementations.Security;

public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash);
    }
}
```

---

## 4. `Implementations/Security/JwtTokenService.cs`
* **What:** Hiện thực hóa interface `IJwtTokenService` từ Application Layer.
* **Why:** 
  * Sinh Access Token chuẩn RFC 7519 ký bằng thuật toán an toàn `HmacSha256`.
  * Nhúng các Claims định danh cốt lõi (`NameIdentifier`, `Email`, `handle`, `name`, `Role`).
  * Sinh Refresh Token an toàn mật mã học bằng `RandomNumberGenerator` (không dùng chuỗi ngẫu nhiên giả lập).
  * Phương thức `GetPrincipalFromExpiredToken()` hỗ trợ đọc thông tin từ Token đã hết hạn khi thực hiện xoay vòng token (Refresh Token Rotation).
* **Where:** `src/Services/Tripory/Tripory.Infrastructure/Implementations/Security/JwtTokenService.cs`
* **How:**
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tripory.Application.Abstractions.Security;
using Tripory.Domain.Entities;
using Tripory.Infrastructure.Configurations;

namespace Tripory.Infrastructure.Implementations.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateAccessToken(User user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email.Value),
            new("handle", user.Handle.Value),
            new("name", user.FullName)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
            ValidateLifetime = false // Bỏ qua thời hạn để đọc claims từ token cũ
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        return principal;
    }
}
```

---

## 5. `Implementations/Security/CurrentUserService.cs`
* **What:** Hiện thực hóa interface `ICurrentUserService` từ Application Layer.
* **Why:** 
  * Cung cấp thông tin người dùng hiện tại (ID, Email, Roles) cho các Handlers một cách trong suốt thông qua `IHttpContextAccessor`.
  * Giúp các UseCase (ví dụ: `UpdateUserProfileCommand`, `ChangePasswordCommand`, lấy hồ sơ cá nhân `GetCurrentUserProfileQuery`) không cần nhận `UserId` từ body client gửi lên mà đọc trực tiếp từ token xác thực của server.
* **Where:** `src/Services/Tripory/Tripory.Infrastructure/Implementations/Security/CurrentUserService.cs`
* **How:**
```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Tripory.Application.Abstractions.Security;

namespace Tripory.Infrastructure.Implementations.Security;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    public IReadOnlyList<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
```

---

## 6. `DependencyInjection.cs`
* **What:** Extension method `AddInfrastructure()` gom toàn bộ cấu hình dịch vụ tầng Infrastructure.
* **Why:** Tự đóng gói DI, tầng API chỉ cần gọi đúng 1 dòng: `builder.Services.AddInfrastructure(builder.Configuration);`.
* **Where:** `src/Services/Tripory/Tripory.Infrastructure/DependencyInjection.cs`
* **How:**
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tripory.Application.Abstractions.Security;
using Tripory.Infrastructure.Configurations;
using Tripory.Infrastructure.Implementations.Security;

namespace Tripory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind JwtOptions
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // HttpContextAccessor
        services.AddHttpContextAccessor();

        // Security Services
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
```

---

## 🎯 Tiêu Chuẩn Xác Nhận (Verification)
Sau khi anh gõ xong các file trên vào project `Tripory.Infrastructure`, anh chạy lệnh:
```bash
dotnet build src/Services/Tripory/Tripory.Infrastructure/Tripory.Infrastructure.csproj
```
**Mục tiêu:** `Build succeeded. 0 Warning(s), 0 Error(s)`.
