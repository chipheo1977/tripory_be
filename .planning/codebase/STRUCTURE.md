# Codebase Structure

**Analysis Date:** 2026-06-01

## Directory Layout

```
tripory_be/
├── Tripory.slnx                     # Solution (XML slnx), 5 service projects + Core
├── AGENTS.md                        # Mandatory dev/AI rules
├── ARCHITECTURE.md                  # Project architecture doc
├── API_ENDPOINTS.md                 # Endpoint reference
├── README.md, LOCAL_RUN_AND_TEST_GUIDE.md
├── docker-compose.yml               # Local infrastructure
├── ai-memories/                     # Knowledge notes (INDEX.md, gotchas-and-bugs.md, ...)
├── roadmaps/                        # PHASE_01_IDENTITY_FOUNDATION.md, PHASE_02_USER_CHAT.md, PHASE_03_ITINERARY_PLANNING.md
├── .planning/codebase/              # GSD codebase maps
├── src/
│   ├── BuildingBlocks/Core/         # BuildingBlocks.Core (shared kernel)
│   │   ├── Abstractions/{Persistence,Shared}/
│   │   ├── CQRS/
│   │   └── Domains/Abstractions/{DDD,Exceptions}/
│   └── Services/Tripory/
│       ├── Tripory.Domain/          # Entities, ValueObjects, Enums, Errors, Exceptions, Catalogs, Repositories, Abstractions/External
│       ├── Tripory.Application/     # UseCases/V1/<Feature>/..., Abstractions/, Behaviors/, Common/
│       ├── Tripory.Persistence/     # ApplicationDbContext, Configurations/, Migrations/, Repositories/
│       ├── Tripory.Infrastructure/  # Implementations/, Hubs/, Configurations/
│       └── Tripory.API/             # Program.cs, Controllers/, Contracts/, Middlewares/, Cors/, RateLimiting/, Common/
└── (tests/ does not exist yet)
```

## Directory Purposes

**`src/BuildingBlocks/Core/`:**
- Purpose: base types with no business meaning.
- Key files: `Abstractions/Shared/Result.cs`, `CQRS/ICommandHandler.cs`, `Domains/Abstractions/EntityFullAuditBase.cs`, `Abstractions/Persistence/IUnitOfWork.cs`.

**`Tripory.Domain/`:**
- Purpose: entities and rules. No EF/HTTP types.
- Key files: `Entities/Itinerary.cs`, `Entities/User.cs`, `ValueObjects/Wgs84Coordinate.cs`, `Errors/DomainErrors.cs`, `Repositories/IItineraryRepository.cs`, `Abstractions/External/IGisDistanceCalculator.cs`.

**`Tripory.Application/UseCases/V1/<Feature>/`:**
- Features: `Auth`, `Chat`, `Itineraries`, `Users`.
- Subfolders: `Commands/`, `Queries/`, `Handlers/`, `Validators/`, `Responses/`, `Extensions/` (not every feature has all).
- Key files: `Itineraries/Handlers/PublishItineraryCommandHandler.cs`, `Itineraries/Extensions/ItineraryRepositoryExtensions.cs`, `Chat/Handlers/SendTextMessageCommandHandler.cs`.

**`Tripory.Application/Abstractions/`:**
- Subfolders: `Data/` (repositories), `Security/` (`IJwtTokenService`, `IPasswordHasher`, `ICurrentUserService`), `Storage/`, `Realtime/`, `DestinationCover/`.

**`Tripory.Persistence/`:**
- `Configurations/` one `<Entity>Configuration.cs` per entity (auto-applied via `ApplyConfigurationsFromAssembly`).
- `Migrations/` timestamped EF migrations plus `ApplicationDbContextModelSnapshot.cs` (generated, committed).
- `Repositories/` implementations, inheriting `RepositoryBase.cs`.

**`Tripory.Infrastructure/`:**
- `Implementations/<Concern>/` adapters; `Hubs/ChatHub.cs`; `Configurations/*Options.cs` for strongly-typed options.

**`Tripory.API/`:**
- `Controllers/V1/` feature controllers; `Contracts/V1/<Feature>/Requests|Responses` HTTP-only DTOs (distinct from Application DTOs); `Common/Responses/ApiResponse.cs`.
- `appsettings.json`, `appsettings.Development.json`, `Properties/launchSettings.json`, `Tripory.API.http`.

## Key File Locations

**Entry Points:**
- `src/Services/Tripory/Tripory.API/Program.cs`: host and middleware pipeline.

**DI composition:**
- `src/Services/Tripory/Tripory.Application/DependencyInjection.cs` (`AddApplication`)
- `src/Services/Tripory/Tripory.Persistence/DependencyInjection.cs` (`AddPersistence`)
- `src/Services/Tripory/Tripory.Infrastructure/DependencyInjection.cs` (`AddInfrastructure`)

**Configuration:**
- `src/Services/Tripory/Tripory.API/appsettings.json` (sections: Logging, Cors, ConnectionStrings, JwtOptions, DestinationCatalog). Do not commit real secret values.

**Core Logic:** `src/Services/Tripory/Tripory.Domain/Entities/`

**Testing:** none present; planned location `tests/UnitTests/` and `tests/IntegrationTests/` per `AGENTS.md`.

## Naming Conventions

**Files:** one type per file, PascalCase, file name equals type name.
- Commands: `<Verb><Noun>Command.cs`; Queries: `Get<Noun>Query.cs`; Handlers: `<Request>Handler.cs` (e.g. `UpdateWaypointCommandHandler.cs`); Validators: `<Request>Validator.cs`.
- EF configs: `<Entity>Configuration.cs`; repositories: `<Entity>Repository.cs`; interfaces `I<Name>.cs`.
- Migrations: `yyyyMMddHHmmss_Snake_Case_Name`.
- Controllers: `<Feature>Controller.cs` deriving from `ApiController`; request records: `<Action>Request.cs`.

**Directories:** PascalCase; feature folders plural (`Itineraries`); API version folder `V1`.

**Namespaces:** mirror folder path (`Tripory.Application.UseCases.V1.Itineraries.Handlers`; `BuildingBlocks.Core.CQRS`). Note `Tripory.Domain.Repositories` for domain repos.

**Routes:** `api/v1/[controller]`; state-changing endpoints use PUT, never PATCH.

## Where to Add New Code

**New feature (use case):**
- Command/Query: `Tripory.Application/UseCases/V1/<Feature>/Commands|Queries/`
- Handler: `.../Handlers/<Name>Handler.cs` (orchestrate only; call entity methods)
- Validator: `.../Validators/<Name>Validator.cs` (auto-registered)
- Response DTO: `.../Responses/`; mapping extension: `.../Extensions/`
- Controller action: `Tripory.API/Controllers/V1/<Feature>Controller.cs` with `_logger.LogInformation` first and `if (result.IsFailure) return HandlerFailure(result);`
- HTTP request model: `Tripory.API/Contracts/V1/<Feature>/Requests/`
- Update `API_ENDPOINTS.md`.

**New entity/aggregate:**
- Entity: `Tripory.Domain/Entities/`; VO: `Tripory.Domain/ValueObjects/`; error constants in `Tripory.Domain/Errors/DomainErrors.cs`.
- Mapping: `Tripory.Persistence/Configurations/<Entity>Configuration.cs`; add `DbSet` to `ApplicationDbContext.cs`; migration via EF CLI into `Tripory.Persistence/Migrations/`.
- Repository: interface in `Tripory.Domain/Repositories/` (aggregate) or `Tripory.Application/Abstractions/Data/`; implementation in `Tripory.Persistence/Repositories/`; register in `Tripory.Persistence/DependencyInjection.cs`.

**New external service:**
- Port in `Tripory.Application/Abstractions/<Concern>/` (or `Tripory.Domain/Abstractions/External/` if Domain needs it); adapter in `Tripory.Infrastructure/Implementations/<Concern>/`; register in `Tripory.Infrastructure/DependencyInjection.cs`; options class in `Tripory.Infrastructure/Configurations/` with `ValidateOnStart()`.

**New cross-cutting HTTP concern:** `Tripory.API/<Concern>/` with an `Add<Concern>` extension called from `Program.cs`.

**Shared base types:** `src/BuildingBlocks/Core/`.

## Special Directories

**`src/Services/Tripory/Tripory.Persistence/Migrations/`:**
- Generated: Yes. Committed: Yes. Do not hand-edit `*.Designer.cs` or the snapshot.

**`bin/`, `obj/`:** Generated, ignored.

**`ai-memories/`:** Committed notes; read `INDEX.md` first.

**`roadmaps/`:** Phase plans (Identity, Chat, Itinerary Planning).

---

*Structure analysis: 2026-06-01*
