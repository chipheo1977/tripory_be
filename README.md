# Tripory Backend (`tripory_be`)

Backend API cho nền tảng lập kế hoạch & chia sẻ lịch trình du lịch **Tripory**.
Frontend tương ứng: `../tripory` · Đặc tả nghiệp vụ: `../tripory/docs` (BRD + các module).

## Công nghệ

- .NET 10 · ASP.NET Core Web API · SignalR
- Clean Architecture + DDD + CQRS (MediatR, FluentValidation)
- EF Core + PostgreSQL/PostGIS (NetTopologySuite)
- JWT Access Token + Refresh Token

## Cấu trúc

```text
src/
├── BuildingBlocks/Core/          # BaseEntity, ValueObject, Result<T>, CQRS abstractions
└── Services/Tripory/
    ├── Tripory.Domain/           # Entities, Value Objects, Ports, Domain Errors
    ├── Tripory.Application/      # UseCases/V1/<Feature>/{Commands,Queries,Handlers,Validators,Responses}
    ├── Tripory.Persistence/      # DbContext, Fluent Configurations, Migrations, Repositories
    ├── Tripory.Infrastructure/   # JWT, BCrypt, SignalR Hub, Storage, GIS adapter
    └── Tripory.API/              # Controllers V1, Middlewares, Program.cs
```

Chi tiết kiến trúc: [ARCHITECTURE.md](ARCHITECTURE.md) · Quy tắc làm việc: [AGENTS.md](AGENTS.md).

## Chạy local

```bash
docker compose up -d                       # PostgreSQL + PostGIS (cổng 5432)
dotnet tool install --global dotnet-ef     # nếu chưa có

dotnet ef database update \
  --project src/Services/Tripory/Tripory.Persistence \
  --startup-project src/Services/Tripory/Tripory.API

dotnet run --project src/Services/Tripory/Tripory.API   # http://localhost:5251
```

Kịch bản test API chi tiết: [LOCAL_RUN_AND_TEST_GUIDE.md](LOCAL_RUN_AND_TEST_GUIDE.md).

## Cấu hình CORS & JWT

Cấu hình qua `appsettings*.json` hoặc biến môi trường (dùng `__` thay cho `:`).

### CORS (`Cors:AllowedOrigins`)

```json
{
  "Cors": {
    "AllowedOrigins": [ "https://app.example.com", "http://localhost:3000" ]
  }
}
```

Env: `Cors__AllowedOrigins__0=https://app.example.com`, `Cors__AllowedOrigins__1=...`

* Danh sách **không rỗng**: chỉ các origin đó được phép (kèm `AllowCredentials`, `AllowAnyHeader`, `AllowAnyMethod`; áp dụng cả negotiate của SignalR `/hubs/chat`). Không bao giờ kết hợp credentials với wildcard.
* Mỗi origin phải là URL tuyệt đối `http`/`https` chỉ gồm scheme + host (+ port). Dấu `/` cuối được tự bỏ. Giá trị sai (vd `*`, `app.example.com`, có path) → app **không khởi động** và báo lỗi rõ ràng.
* Danh sách **rỗng/thiếu**: giữ hành vi cũ (cho phép mọi origin). Ngoài môi trường Development, app ghi log `WARNING` lúc khởi động nhắc phải cấu hình.
* `appsettings.json` để danh sách rỗng; `appsettings.Development.json` có sẵn `http://localhost:3000`, `https://localhost:3000`, `http://localhost:5173`. Origin production đặt qua biến môi trường, không commit.

### JWT (`JwtOptions`)

| Key | Mặc định (khi thiếu cấu hình) | `appsettings.json` | Ý nghĩa |
|---|:-:|:-:|---|
| `JwtOptions:ExpiryMinutes` | 15 | 60 | Thời hạn access token (phút) |
| `JwtOptions:RefreshTokenExpiryDays` | 7 | 7 | Thời hạn refresh token (ngày); cũng là `expiresAt` trong `AuthResponse`/`TokenResponse` |

Env: `JwtOptions__ExpiryMinutes=30`, `JwtOptions__RefreshTokenExpiryDays=14`. Cả hai phải lớn hơn 0, nếu không app không khởi động (`ValidateOnStart`).

## Lộ trình

| Phase | Module | Tài liệu |
|---|---|---|
| 01 | Identity (Auth, User Profile) | [roadmaps/PHASE_01_IDENTITY_FOUNDATION.md](roadmaps/PHASE_01_IDENTITY_FOUNDATION.md) |
| 02 | User Chat (Text, Voice, Call, SignalR) | [roadmaps/PHASE_02_USER_CHAT.md](roadmaps/PHASE_02_USER_CHAT.md) |
| 03 ✅ | Itinerary Planning (Map + Timeline, PostGIS) | [roadmaps/PHASE_03_ITINERARY_PLANNING.md](roadmaps/PHASE_03_ITINERARY_PLANNING.md) |
