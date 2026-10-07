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

## Lộ trình

| Phase | Module | Tài liệu |
|---|---|---|
| 01 | Identity (Auth, User Profile) | [roadmaps/PHASE_01_IDENTITY_FOUNDATION.md](roadmaps/PHASE_01_IDENTITY_FOUNDATION.md) |
| 02 | User Chat (Text, Voice, Call, SignalR) | [roadmaps/PHASE_02_USER_CHAT.md](roadmaps/PHASE_02_USER_CHAT.md) |
| 03 ✅ | Itinerary Planning (Map + Timeline, PostGIS) | [roadmaps/PHASE_03_ITINERARY_PLANNING.md](roadmaps/PHASE_03_ITINERARY_PLANNING.md) |
