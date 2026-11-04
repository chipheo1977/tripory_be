# Architecture

**Analysis Date:** 2026-06-01

## Pattern Overview

**Overall:** Clean Architecture + DDD + CQRS (MediatR), single bounded context "Tripory", .NET 10 (`net10.0`), PostgreSQL/PostGIS, SignalR realtime.

**Key Characteristics:**
- Handlers only orchestrate; business rules live on entities and return `Result` / `Result<T>` (`src/Services/Tripory/Tripory.Domain/Entities/Itinerary.cs`).
- Expected failures flow as `Result` with an `Error(code, message)` built from `DomainErrors` (`src/Services/Tripory/Tripory.Domain/Errors/DomainErrors.cs`). Domain exceptions exist for exceptional cases and are mapped by `GlobalExceptionHandler`.
- Repository interfaces live in two places: aggregate repositories in `Tripory.Domain/Repositories/` (e.g. `IItineraryRepository`) and data-access interfaces in `Tripory.Application/Abstractions/Data/` (`IUserRepository`, `IConversationRepository`, `IChatMessageRepository`). Follow whichever the feature already uses; aggregate-root repos go in Domain.
- Persistence is Fluent-API only; entities have private setters and private constructors for EF.
- Conventions come from `AGENTS.md` (Anti-Vibe Coding; design approval before code; PUT, not PATCH, for state changes; controllers log first then check `result.IsFailure`).

## Layers

**BuildingBlocks.Core:**
- Purpose: shared kernel and CQRS/Result abstractions.
- Location: `src/BuildingBlocks/Core/`
- Contains: `Abstractions/Shared/Result.cs`, `ResultT.cs`, `Error.cs`, `PagedResult.cs`; `Abstractions/Persistence/IRepositoryBase.cs`, `IUnitOfWork.cs`; `CQRS/ICommand.cs`, `ICommandHandler.cs`, `IQuery.cs`, `IQueryHandler.cs`, `IDomainEvent.cs`, `IDomainEventHandler.cs`; `Domains/Abstractions/EntityBase.cs`, `EntityAuditBase.cs`, `EntityFullAuditBase.cs`, `DDD/IAggregateRoot.cs`, `DDD/ValueObject.cs`; exceptions (`DomainException`, `NotFoundException`, `ConflictException`, `BadRequestException`).
- Depends on: MediatR 14.2.0 only.
- Used by: Domain, Application (transitively), Persistence, API.

**Domain:**
- Purpose: business logic and invariants (pure C#).
- Location: `src/Services/Tripory/Tripory.Domain/`
- Contains: `Entities/` (User, Role, UserRole, RefreshToken, Conversation, ChatMessage, Itinerary, ItineraryDay, Waypoint), `ValueObjects/` (Email, Handle, ItineraryTitle, ItineraryDescription, WaypointName, Wgs84Coordinate, CallLogData), `Enums/`, `Errors/DomainErrors.cs`, `Exceptions/`, `Catalogs/` (DestinationCatalog, InterestCatalog), `Repositories/IItineraryRepository.cs`, `Abstractions/External/IGisDistanceCalculator.cs` (port).
- Depends on: `BuildingBlocks.Core` only.
- Used by: Application, Persistence, Infrastructure.

**Application:**
- Purpose: orchestration (use cases).
- Location: `src/Services/Tripory/Tripory.Application/`
- Contains: `UseCases/V1/{Auth,Chat,Itineraries,Users}/{Commands,Queries,Handlers,Validators,Responses,Extensions}`, `Abstractions/{Data,Security,Storage,Realtime,DestinationCover}`, `Behaviors/ValidationPipelineBehavior.cs`, `Common/Models/UserDto.cs`, `DependencyInjection.cs` (`AddApplication`).
- Depends on: Domain; FluentValidation 12.1.1.
- Used by: API, Infrastructure, Persistence.

**Persistence:**
- Purpose: EF Core data access.
- Location: `src/Services/Tripory/Tripory.Persistence/`
- Contains: `ApplicationDbContext.cs`, `Configurations/*Configuration.cs`, `Migrations/`, `Repositories/` (`RepositoryBase`, `UnitOfWork`, `UserRepository`, `ConversationRepository`, `ChatMessageRepository`, `ItineraryRepository`), `DependencyInjection.cs` (`AddPersistence`).
- Depends on: Domain, Application, BuildingBlocks.Core; EF Core 10.0.11, Npgsql + NetTopologySuite.

**Infrastructure:**
- Purpose: IO and external adapters implementing Application/Domain ports.
- Location: `src/Services/Tripory/Tripory.Infrastructure/`
- Contains: `Implementations/Security/{BcryptPasswordHasher,JwtTokenService,CurrentUserService}.cs`, `Implementations/Storage/LocalAudioStorageService.cs`, `Implementations/Realtime/ChatNotificationService.cs`, `Implementations/Gis/GisDistanceCalculator.cs`, `Implementations/DestinationCover/DestinationCoverProvider.cs`, `Hubs/ChatHub.cs`, `Configurations/{JwtOptions,DestinationCatalogOptions}.cs`, `DependencyInjection.cs` (`AddInfrastructure`).
- Depends on: Application, Domain; `FrameworkReference Microsoft.AspNetCore.App`; BCrypt.Net-Next, System.IdentityModel.Tokens.Jwt.

**API:**
- Purpose: HTTP gateway and composition root.
- Location: `src/Services/Tripory/Tripory.API/`
- Contains: `Program.cs`, `Controllers/ApiController.cs`, `Controllers/V1/{Auth,Chat,Itineraries,Users}Controller.cs`, `Contracts/V1/**/Requests|Responses`, `Common/Responses/ApiResponse.cs`, `Middlewares/GlobalExceptionHandler.cs`, `Cors/`, `RateLimiting/`.
- Depends on: Application, Infrastructure, Persistence.

**Dependency direction (from csproj files):** API -> Application/Infrastructure/Persistence; Infrastructure -> Application, Domain; Persistence -> Application, Domain, Core; Application -> Domain; Domain -> Core.

## Data Flow

**Command (e.g. publish itinerary):**

1. `ItinerariesController` action logs, builds a command, calls `Sender.Send(command, ct)` (`src/Services/Tripory/Tripory.API/Controllers/V1/ItinerariesController.cs`).
2. `ValidationPipelineBehavior<,>` runs all FluentValidation validators; first failure becomes `Result.Failure` (`Tripory.Application/Behaviors/ValidationPipelineBehavior.cs`).
3. Handler (`PublishItineraryCommandHandler.cs`) loads the aggregate via repository (`GetOwnedItineraryAsync` extension in `Itineraries/Extensions/ItineraryRepositoryExtensions.cs`, uses `ICurrentUserService.UserId`).
4. Handler calls the entity method (`itinerary.Publish()`), returns early on `IsFailure`.
5. `UpdateAsync` then `IUnitOfWork.SaveChangesAsync(ct)`; `ApplicationDbContext.SaveChangesAsync` stamps `UpdatedAt` on `EntityAuditBase<Guid>` entries.
6. Controller: `if (result.IsFailure) return HandlerFailure(result);` else `Ok(ApiResponse...)`.

**Realtime chat:** Chat handlers persist messages then call `IChatNotificationService` (`ChatNotificationService` in Infrastructure) to push via SignalR to group `user_{userId}`; clients connect to `/hubs/chat` (`ChatHub`), JWT taken from `access_token` query string for paths under `/hubs/`.

**State Management:** Stateless HTTP; state in PostgreSQL. Scoped DbContext per request. Singletons: password hasher, JWT service, GIS calculator, destination cover provider.

## Key Abstractions

**Result / Result<T> / Error:**
- Files: `src/BuildingBlocks/Core/Abstractions/Shared/Result.cs`, `ResultT.cs`, `Error.cs`.
- Pattern: railway-style results; handler return types are `Task<Result>` / `Task<Result<T>>`.

**CQRS markers:** `ICommand`, `ICommand<T>`, `IQuery<T>`, `ICommandHandler<>`, `IQueryHandler<,>` in `src/BuildingBlocks/Core/CQRS/`.

**Aggregate roots:** `Itinerary` (owns `ItineraryDay` and `Waypoint` collections, `IAggregateRoot`, `EntityFullAuditBase<Guid>`), `User`, `Conversation`.

**Value Objects:** inherit `ValueObject` (`Wgs84Coordinate`, `ItineraryTitle`, `Email`, `Handle`...) in `src/Services/Tripory/Tripory.Domain/ValueObjects/`.

**Ports/Adapters:** `IGisDistanceCalculator` (Domain) -> `GisDistanceCalculator` (Infrastructure); `IJwtTokenService`, `IPasswordHasher`, `ICurrentUserService`, `IAudioStorageService`, `IChatNotificationService`, `IDestinationCoverProvider` (Application/Abstractions) -> `Infrastructure/Implementations/*`.

## Entry Points

**Web host:**
- Location: `src/Services/Tripory/Tripory.API/Program.cs`
- Triggers: `dotnet run` on `Tripory.API`.
- Responsibilities: calls `AddApplication`, `AddPersistence`, `AddInfrastructure`; controllers; `GlobalExceptionHandler`; JWT bearer auth; Swagger (Development only); CORS (`Cors/CorsExtensions.cs`); rate limiting (`RateLimiting/`); static files; maps controllers and `ChatHub` at `/hubs/chat`.

**Controllers:** routes `api/v1/[controller]` via `ApiController` base (`Controllers/ApiController.cs`).

## Error Handling

**Strategy:** Result-based for expected failures; exceptions for unexpected.

**Patterns:**
- `ApiController.HandlerFailure(Result)` maps `Error.Code` substring to HTTP status: `NotFound` 404, `Conflict`/`AlreadyExists` 409, `Unauthorized` 401, `Forbidden` 403, else 400.
- `GlobalExceptionHandler` maps `NotFoundException` 404, `ConflictException` 409, `BadRequestException` 400, `DomainException` 400 (code = `Title`), others 500 `Server.Error`.
- Response envelope: `ApiResponse` / `ApiResponse<T>` (`Tripory.API/Common/Responses/ApiResponse.cs`).
- Error codes follow `Area.Name` (e.g. `Auth.Unauthorized`, `User.NotFound`); code names must contain the keywords above to get the right status.

## Cross-Cutting Concerns

**Logging:** `ILogger<T>`; every controller action logs with `_logger.LogInformation` first (message text is Vietnamese).
**Validation:** FluentValidation validators auto-registered by assembly scan; run in MediatR pipeline. Shared rule helpers: `Itineraries/Validators/ItineraryRuleBuilderExtensions.cs`, `WaypointValidationExtensions.cs`.
**Authentication:** JWT bearer configured in `Program.cs` from `JwtOptions` section (`Infrastructure/Configurations/JwtOptions.cs`); controllers use `[Authorize]`, public reads use `[AllowAnonymous]`. Current user via `ICurrentUserService` (reads `HttpContext`).
**Options validation:** `JwtOptions` and `DestinationCatalogOptions` bound with `ValidateOnStart()` in `Infrastructure/DependencyInjection.cs`.
**Configuration keys (names only):** `ConnectionStrings:DefaultConnection`, `JwtOptions`, `Cors:AllowedOrigins`, `DestinationCatalog`, auth rate-limit options (`RateLimiting/AuthRateLimitOptions.cs`).
**Auditing:** `ApplicationDbContext.UpdateAuditableEntities` sets `CreatedAt`/`UpdatedAt`.

## Known Deviations From AGENTS.md (observed)

- `Tripory.Persistence/DependencyInjection.cs` enables `EnableSensitiveDataLogging()` unconditionally and has a hardcoded fallback connection string; see CONCERNS.md if produced.
- `Tripory.Infrastructure` hosts `ChatHub` and registers SignalR (HTTP-adjacent code outside API).
- `tests/` directory does not exist; `Tripory.slnx` lists no test projects.

---

*Architecture analysis: 2026-06-01*
