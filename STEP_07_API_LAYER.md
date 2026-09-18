# HƯỚNG DẪN TRIỂN KHAI BƯỚC 7: API LAYER (CONTROLLERS, MIDDLEWARES & PROGRAM.CS)
> **Mục tiêu:** Cung cấp tài liệu 5W1H và mã nguồn chuẩn cho tầng HTTP Gateway & Composition Root `Tripory.API`.  
> **Nguyên tắc:** Kế thừa chuẩn RESTful từ `tc-ems-be`, Controller mỏng (chỉ gọi `Sender.Send`), log đầu mỗi action, định dạng Response chuẩn BRD 4.4, Global Exception Handler và cấu hình Swagger JWT Bearer.

---

## 1. Cập Nhật Packages: `Tripory.API.csproj`
* **What:** File cấu hình dự án của tầng Web API.
* **Why:** Cần cài đặt gói xác thực JWT Bearer, công cụ sinh tài liệu Swagger/OpenAPI kèm cấu hình Security Definition.
* **Where:** `src/Services/Tripory/Tripory.API/Tripory.API.csproj`
* **How:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.11" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.11" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="7.3.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Tripory.Application\Tripory.Application.csproj" />
    <ProjectReference Include="..\Tripory.Infrastructure\Tripory.Infrastructure.csproj" />
    <ProjectReference Include="..\Tripory.Persistence\Tripory.Persistence.csproj" />
  </ItemGroup>

</Project>
```

---

## 2. Chuẩn Hóa Response: `Common/Responses/ApiResponse.cs`
* **What:** Generic class đóng gói cấu trúc phản hồi HTTP chuẩn cho toàn bộ hệ thống Tripory.
* **Why:** 
  * Khớp 100% tài liệu BRD mục 4.4: `{ "status": "success" | "error", "data": {...}, "message": "...", "timestamp": "..." }`.
  * Giúp Frontend (Web/Mobile) dễ dàng bắt lỗi và hiển thị dữ liệu đồng nhất.
* **Where:** `src/Services/Tripory/Tripory.API/Common/Responses/ApiResponse.cs`
* **How:**
```csharp
namespace Tripory.API.Common.Responses;

public class ApiResponse<T>
{
    public string Status { get; init; } = "success";
    public T? Data { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public static ApiResponse<T> Success(T data, string message = "Thành công") =>
        new()
        {
            Status = "success",
            Data = data,
            Message = message
        };

    public static ApiResponse<T> Failure(string errorCode, string message) =>
        new()
        {
            Status = "error",
            Data = default,
            ErrorCode = errorCode,
            Message = message
        };
}

public static class ApiResponse
{
    public static ApiResponse<object> Success(string message = "Thành công") =>
        ApiResponse<object>.Success(new { }, message);

    public static ApiResponse<object> Failure(string errorCode, string message) =>
        ApiResponse<object>.Failure(errorCode, message);
}
```

---

## 3. Base Controller: `Controllers/ApiController.cs`
* **What:** Base class cho toàn bộ Controller trong hệ thống.
* **Why:** 
  * Inject sẵn `ISender` để Controller con chỉ cần gọi `await Sender.Send(command)`.
  * Cung cấp phương thức trợ giúp `HandlerFailure(Result result)` tự động ánh xạ mã lỗi nghiệp vụ sang các HTTP Status Code chuẩn: 400 (Bad Request), 404 (Not Found), 409 (Conflict).
* **Where:** `src/Services/Tripory/Tripory.API/Controllers/ApiController.cs`
* **How:**
```csharp
using BuildingBlocks.Core.Abstractions.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tripory.API.Common.Responses;

namespace Tripory.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class ApiController : ControllerBase
{
    protected ISender Sender { get; }

    protected ApiController(ISender sender)
    {
        Sender = sender;
    }

    protected IActionResult HandlerFailure(Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Không thể xử lý lỗi trên một Result thành công.");

        var response = ApiResponse.Failure(result.Error.Code, result.Error.Message);

        return result.Error.Code switch
        {
            var c when c.Contains("NotFound") => NotFound(response),
            var c when c.Contains("Conflict") || c.Contains("AlreadyExists") => Conflict(response),
            var c when c.Contains("Unauthorized") => Unauthorized(response),
            var c when c.Contains("Forbidden") => StatusCode(StatusCodes.Status403Forbidden, response),
            _ => BadRequest(response)
        };
    }
}
```

---

## 4. `Controllers/V1/AuthController.cs`
* **What:** Controller tiếp nhận các yêu cầu xác thực người dùng.
* **Why:** 
  * Cung cấp 4 API cốt lõi của mục 4.1 BRD: Đăng ký, Đăng nhập, Refresh Token xoay vòng và Đổi mật khẩu.
  * Tuân thủ quy tắc `AGENTS.md`: Ghi log `_logger.LogInformation(...)` đầu mỗi action, kiểm tra `if (result.IsFailure) return HandlerFailure(result);` và dùng động từ HTTP `PUT` cho thao tác đổi mật khẩu.
* **Where:** `src/Services/Tripory/Tripory.API/Controllers/V1/AuthController.cs`
* **How:**
```csharp
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
```

---

## 5. `Controllers/V1/UsersController.cs`
* **What:** Controller quản lý hồ sơ thông tin cá nhân của người dùng.
* **Why:** Cung cấp API đọc hồ sơ cá nhân (`GET /me`) và cập nhật thông tin profile (`PUT /profile`) theo chuẩn mục 4.2 BRD.
* **Where:** `src/Services/Tripory/Tripory.API/Controllers/V1/UsersController.cs`
* **How:**
```csharp
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
```

---

## 6. `Middlewares/GlobalExceptionHandler.cs`
* **What:** Bộ xử lý ngoại lệ tập trung triển khai interface `IExceptionHandler` của ASP.NET Core.
* **Why:** 
  * Ngăn chặn việc lộ Stack Trace nhạy cảm ra ngoài client khi gặp lỗi không mong muốn.
  * Tự động bắt các Domain Exceptions (`NotFoundException` $ightarrow$ 404, `ConflictException` $ightarrow$ 409, `BadRequestException` $ightarrow$ 400) và trả về chuẩn `ApiResponse`.
* **Where:** `src/Services/Tripory/Tripory.API/Middlewares/GlobalExceptionHandler.cs`
* **How:**
```csharp
using BuildingBlocks.Core.Domains.Abstractions.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Tripory.API.Common.Responses;

namespace Tripory.API.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Đã xảy ra lỗi không xử lý được: {Message}", exception.Message);

        var (statusCode, errorCode, message) = exception switch
        {
            NotFoundException nf => (StatusCodes.Status404NotFound, "NotFound", nf.Message),
            ConflictException cf => (StatusCodes.Status409Conflict, "Conflict", cf.Message),
            BadRequestException br => (StatusCodes.Status400BadRequest, "BadRequest", br.Message),
            DomainException de => (StatusCodes.Status400BadRequest, de.Title, de.Message),
            _ => (StatusCodes.Status500InternalServerError, "Server.Error", "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.")
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var response = ApiResponse.Failure(errorCode, message);
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
```

---

## 7. Cấu Hình Ứng Dụng: `appsettings.json`
* **What:** File cấu hình kết nối DB và bí mật JWT Token.
* **Where:** `src/Services/Tripory/Tripory.API/appsettings.json`
* **How:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=tripory_db;Username=postgres;Password=postgres"
  },
  "JwtOptions": {
    "SecretKey": "TriporySuperSecretKeyForDevelopmentPhase2026!@#$%^&*",
    "Issuer": "TriporyBackend",
    "Audience": "TriporyClientApp",
    "ExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  }
}
```

---

## 8. Composition Root: `Program.cs`
* **What:** Nơi ráp nối Dependency Injection duy nhất của toàn bộ hệ thống Backend.
* **Why:** 
  * Đăng ký DI của 3 tầng: `AddApplication()`, `AddPersistence()`, `AddInfrastructure()`.
  * Cấu hình Authentication (`JwtBearer`) và Swagger UI có nút `Authorize` để test Token trực tiếp.
  * Thiết lập CORS cho Frontend.
* **Where:** `src/Services/Tripory/Tripory.API/Program.cs`
* **How:**
```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Tripory.API.Middlewares;
using Tripory.Application;
using Tripory.Infrastructure;
using Tripory.Infrastructure.Configurations;
using Tripory.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký các tầng kiến trúc
builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Cấu hình Controllers & Global Exception Handler
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 3. Cấu hình JWT Authentication
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Chưa cấu hình JwtOptions trong appsettings.json.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 4. Cấu hình Swagger kèm nút Authorize JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Tripory API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập Token theo cú pháp: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 5. Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Pipeline Middleware
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tripory API v1"));
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

---

## 🎯 Tiêu Chuẩn Xác Nhận (Verification)
Sau khi hoàn tất gõ các file trên vào `Tripory.API`, anh chạy lệnh:
```bash
dotnet build Tripory.slnx
```
**Mục tiêu:** Toàn bộ solution build thành công 100%: `Build succeeded. 0 Warning(s), 0 Error(s)`.
