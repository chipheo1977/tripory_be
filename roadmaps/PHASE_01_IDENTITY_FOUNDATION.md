# PHASE_01_IDENTITY_FOUNDATION.md – Kế Hoạch Triển Khai Giai Đoạn 1: Nền Tảng & Xác Thực (Identity-First)

> **Dự án:** Tripory Backend (`tripory_be`)  
> **Giai đoạn:** Phase 01 – Technical Foundation & Identity System  
> **Lựa chọn kiến trúc:** Phương án A – Xây dựng Nền tảng Xác thực & Quản lý Người dùng trước (Identity-First)  
> **Tiêu chuẩn tuân thủ:** Clean Architecture + DDD + CQRS (Kế thừa từ `tc-ems-be` và ràng buộc trong `AGENTS.md`)  
> **Nguồn đặc tả nghiệp vụ:** `../tripory/docs/BRD_Travel_Itinerary_App.md` (Mục 4.1, 4.2, 4.3, 4.4, 4.5 & Sprint 1.1 - 1.4)

---

## 🎯 1. Mục Tiêu & Phạm Vi (Scope)

* **Mục tiêu:** Thiết lập khung solution hoàn chỉnh, xây dựng các khối nền tảng (`BuildingBlocks.Core`), và triển khai toàn bộ phân hệ Xác thực (Authentication) & Quản lý Người dùng (User Management) theo chuẩn enterprise.
* **Kết quả đạt được:**
  * Toàn bộ hệ thống sẵn sàng với `UserId` thật, cơ chế bảo mật JWT Bearer (Access Token + Refresh Token), phân quyền Role-based (RBAC).
  * Các phân hệ nghiệp vụ tiếp theo (Lịch trình `ITINERARY_01`, Fork `ITINERARY_02`, Trang cá nhân `USER_PROFILE_01`) sẽ gắn trực tiếp vào `UserId` mà không cần refactor hay mock dữ liệu.

---

## 🏗️ 2. Quy Trình 7 Bước Triển Khai "Từ Trong Ra Ngoài" (Inside-Out)

```
[Bước 1: Khởi tạo Khung CLI]
       │
       ▼
[Bước 2: BuildingBlocks.Core]
       │
       ▼
[Bước 3: Domain Layer (User & Auth)]
       │
       ▼
[Bước 4: Application Layer (CQRS UseCases)]
       │
       ▼
[Bước 5: Persistence Layer (EF Core + PostgreSQL)]
       │
       ▼
[Bước 6: Infrastructure Layer (JWT & Hasher)]
       │
       ▼
[Bước 7: API Layer (Controllers & Middlewares)]
```

---

### BƯỚC 1: Khởi Tạo Solution & 6 Projects Bằng CLI Mặc Định

* **Mục tiêu:** Dựng khung solution và các project classlib/webapi sạch sẽ, không dùng template ngoài.
* **Lệnh thực thi dự kiến:**
  ```bash
  # 1. Tạo solution
  dotnet new sln -n Tripory

  # 2. Tạo BuildingBlocks
  dotnet new classlib -o src/BuildingBlocks/Core -n BuildingBlocks.Core

  # 3. Tạo 5 projects cho Service Tripory
  dotnet new classlib -o src/Services/Tripory/Tripory.Domain -n Tripory.Domain
  dotnet new classlib -o src/Services/Tripory/Tripory.Application -n Tripory.Application
  dotnet new classlib -o src/Services/Tripory/Tripory.Persistence -n Tripory.Persistence
  dotnet new classlib -o src/Services/Tripory/Tripory.Infrastructure -n Tripory.Infrastructure
  dotnet new webapi -o src/Services/Tripory/Tripory.API -n Tripory.API

  # 4. Thêm tất cả vào Solution
  dotnet sln Tripory.sln add src/BuildingBlocks/Core/BuildingBlocks.Core.csproj
  dotnet sln Tripory.sln add src/Services/Tripory/Tripory.Domain/Tripory.Domain.csproj
  dotnet sln Tripory.sln add src/Services/Tripory/Tripory.Application/Tripory.Application.csproj
  dotnet sln Tripory.sln add src/Services/Tripory/Tripory.Persistence/Tripory.Persistence.csproj
  dotnet sln Tripory.sln add src/Services/Tripory/Tripory.Infrastructure/Tripory.Infrastructure.csproj
  dotnet sln Tripory.sln add src/Services/Tripory/Tripory.API/Tripory.API.csproj
  ```
* **Cấu hình tham chiếu 1 chiều (Strict Dependencies):**
  * `Tripory.Domain` $\rightarrow$ `BuildingBlocks.Core`
  * `Tripory.Application` $\rightarrow$ `Tripory.Domain`
  * `Tripory.Persistence` $\rightarrow$ `Tripory.Domain`, `Tripory.Application`
  * `Tripory.Infrastructure` $\rightarrow$ `Tripory.Application`, `Tripory.Domain`
  * `Tripory.API` $\rightarrow$ `Tripory.Application`, `Tripory.Infrastructure`, `Tripory.Persistence`
* **File cấu hình chung:** Tạo `Directory.Build.props` tại root để bật `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` và `<Nullable>enable</Nullable>`.

---

### BƯỚC 2: Xây Dựng `BuildingBlocks.Core` (Primitives Nền Tảng)

* **Trách nhiệm:** Cung cấp các abstractions không phụ thuộc công nghệ/framework.
* **Chi tiết thành phần:**
  * `Domains/Abstractions/`:
    * `EntityBase<TKey>`: Id, DomainEvents collection, ClearEvents().
    * `EntityAuditBase<TKey>`: Kế thừa EntityBase, thêm `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`.
    * `EntityFullAuditBase<TKey>`: Kế thừa EntityAuditBase, thêm `IsDeleted`, `DeletedAt`, `DeletedBy` (Soft-delete).
    * `ValueObject`: Base class override `Equals` và `GetHashCode` theo components.
  * `Common/Results/`:
    * `Result`: Thành công/Thất bại, Error message, Error code.
    * `Result<T>`: Kết quả mang dữ liệu payload.
  * `CQRS/Abstractions/`:
    * `ICommand`, `ICommand<T>`, `ICommandHandler<T>`, `ICommandHandler<T, R>`.
    * `IQuery<T>`, `IQueryHandler<T, R>`.

---

### BƯỚC 3: Tầng `Tripory.Domain` (Module User & Identity - Pure C#)

* **Trách nhiệm:** Bảo vệ toàn bộ invariant nghiệp vụ người dùng, không phụ thuộc EF Core hay thư viện ngoài.
* **Entities:**
  * `User`: Kế thừa `EntityFullAuditBase<Guid>`:
    * Thuộc tính: `Email` (VO), `PasswordHash`, `FullName`, `AvatarUrl`, `Bio`, `Status` (Active, Inactive, Banned).
    * Phương thức nghiệp vụ: `Create(...)`, `UpdateProfile(...)`, `ChangePassword(...)`, `Deactivate()`.
  * `Role`: Kế thừa `EntityBase<int>`:
    * Thuộc tính: `Name` (Admin, Traveler, ServiceProvider), `Description`.
  * `UserRole`: Bảng nối người dùng và quyền hạn.
  * `RefreshToken`: Quản lý token xoay vòng (Rotational refresh tokens), ngày hết hạn, trạng thái thu hồi (`Revoked`).
* **Value Objects:**
  * `Email`: Regex format RFC 5322, tự động chuẩn hóa lowercase + trim.
  * `UserFullName`: Độ dài 1-100 ký tự.
* **Exceptions:**
  * `UserNotFoundException`, `EmailAlreadyExistsException`, `InvalidCredentialsException`, `RefreshTokenExpiredException`.

---

### BƯỚC 4: Tầng `Tripory.Application` (UseCases & Orchestration)

* **Trách nhiệm:** Điều phối luồng xử lý đăng ký, đăng nhập, cấp mới token, đổi mật khẩu.
* **Cấu trúc thư mục:** `Tripory.Application/UserCases/V1/Auth/`
  * `Commands/`:
    * `RegisterCommand(string Email, string Password, string FullName)`
    * `LoginCommand(string Email, string Password)`
    * `RefreshTokenCommand(string AccessToken, string RefreshToken)`
    * `ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword)`
  * `Handlers/`:
    * `RegisterCommandHandler`, `LoginCommandHandler`, `RefreshTokenCommandHandler`, `ChangePasswordCommandHandler`.
    * *Quy tắc:* Handler chỉ gọi Repository kiểm tra tồn tại $\rightarrow$ gọi Entity tạo mới/đổi mật khẩu $\rightarrow$ gọi TokenService sinh token $\rightarrow$ commit $\rightarrow$ trả `Result<AuthResponse>`.
  * `Validators/` (FluentValidation):
    * `RegisterCommandValidator`: Password tối thiểu 8 ký tự, có chữ hoa, chữ số, ký tự đặc biệt.
    * `LoginCommandValidator`: Email đúng định dạng, Password không rỗng.
  * `Responses/`:
    * `AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserDto User)`.
* **Abstractions (Ports cho Infrastructure triển khai):**
  * `IJwtTokenService`: Phương thức `GenerateAccessToken(User, roles)`, `GenerateRefreshToken()`.
  * `IPasswordHasher`: Phương thức `HashPassword(string)`, `VerifyPassword(string, string)`.
  * `ICurrentUserService`: Trích xuất `UserId`, `Email`, `Roles` từ JWT Claims.

---

### BƯỚC 5: Tầng `Tripory.Persistence` (PostgreSQL + EF Core)

* **Trách nhiệm:** Lưu trữ dữ liệu vào PostgreSQL bằng Fluent API thuần túy.
* **NuGet Packages:**
  * `Npgsql.EntityFrameworkCore.PostgreSQL`
  * `Microsoft.EntityFrameworkCore.Design`
* **Cấu trúc:**
  * `ApplicationDbContext`: Chứa `DbSet<User>`, `DbSet<Role>`, `DbSet<UserRole>`, `DbSet<RefreshToken>`.
  * `Configurations/`:
    * `UserConfiguration.cs`: Cấu hình schema `identity`, index `UNIQUE` trên cột `email`, ánh xạ VO `Email` qua `HasConversion`.
    * `RoleConfiguration.cs`, `RefreshTokenConfiguration.cs`.
  * `Extensions/ModelBuilderAuditExtensions.cs`: Tự động áp dụng mapping tên cột snake_case (`id`, `created_at`, `updated_at`, `is_deleted`) và Global Query Filter `!IsDeleted`.
  * `Repositories/`: Triển khai `IVciRepositoryBase<T, TKey>` hoặc repository chuyên biệt.
* **Migrations:** Chạy lệnh tạo migration đầu tiên `Tripory_Identity_InitTable`.

---

### BƯỚC 6: Tầng `Tripory.Infrastructure` (Security & Adapters)

* **Trách nhiệm:** Triển khai các port bảo mật được khai báo ở Application.
* **NuGet Packages:**
  * `System.IdentityModel.Tokens.Jwt`
  * `Microsoft.IdentityModel.Tokens`
  * `BCrypt.Net-Next`
* **Thành phần:**
  * `Implementations/Security/BcryptPasswordHasher.cs`: Hiện thực hóa `IPasswordHasher` sử dụng thuật toán BCrypt work-factor 12.
  * `Implementations/Security/JwtTokenService.cs`: Hiện thực hóa `IJwtTokenService`, ký mã bằng thuật toán `HmacSha256` với SecretKey từ configuration, gán Claims chuẩn (`Sub`, `Email`, `Role`).

---

### BƯỚC 7: Tầng `Tripory.API` (Composition Root & HTTP Endpoints)

* **Trách nhiệm:** Cung cấp REST endpoints chuẩn, kiểm tra validation pipeline, ghi log và cấu hình DI.
* **Thành phần:**
  * `Controllers/V1/AuthController.cs`:
    * `POST /api/v1/auth/register` $\rightarrow$ Đăng ký tài khoản mới.
    * `POST /api/v1/auth/login` $\rightarrow$ Đăng nhập lấy cặp Access + Refresh token.
    * `POST /api/v1/auth/refresh-token` $\rightarrow$ Cấp mới Access token khi hết hạn.
    * `PUT /api/v1/auth/change-password` $\rightarrow$ Đổi mật khẩu (dùng `PUT` theo quy chuẩn `AGENTS.md`).
  * `Controllers/V1/UsersController.cs`:
    * `GET /api/v1/users/me` $\rightarrow$ Lấy thông tin cá nhân của người dùng hiện tại.
    * `PUT /api/v1/users/profile` $\rightarrow$ Cập nhật thông tin profile.
  * `Middlewares/`:
    * `GlobalExceptionHandler`: Bắt domain exceptions (`NotFoundException` $\rightarrow$ 404, `BadRequestException` $\rightarrow$ 400).
    * `ValidationPipelineBehavior`: Tự động bắt lỗi FluentValidation trả về 400.
  * `Program.cs`:
    * Đăng ký Authentication (`JwtBearer`), Authorization (Policies/Roles).
    * Cấu hình Swagger/OpenAPI có nút Authorize Bearer Token.

---

## ✅ 3. Tiêu Chí Nghiệm Thu (Acceptance Criteria / Quality Gate)

Mỗi bước trong roadmap khi hoàn thành phải đạt:
1. `dotnet build` thành công 100% với `0 Errors, 0 Warnings`.
2. Luồng End-to-End hoạt động:
   * Đăng ký tài khoản $\rightarrow$ Nhận được `UserId`.
   * Đăng nhập $\rightarrow$ Trả về JWT Access Token + Refresh Token hợp lệ.
   * Gọi `GET /api/v1/users/me` kèm Header `Authorization: Bearer <token>` $\rightarrow$ Trả về đúng thông tin user.
   * Gửi sai mật khẩu hoặc trùng email $\rightarrow$ Trả về mã lỗi 400 với thông báo chuẩn hóa.
3. Không vi phạm bất kỳ điều cấm nào trong [AGENTS.md](../AGENTS.md).
