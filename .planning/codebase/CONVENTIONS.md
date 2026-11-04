# Coding Conventions

**Analysis Date:** 2026-06-01

Sources: `AGENTS.md`, `ARCHITECTURE.md`, `ai-memories/`, and the code under `src/`. No `.editorconfig`, `Directory.Build.props`, or analyzer packages exist in the repo, so conventions are enforced by review and `AGENTS.md`, not tooling.

## Naming Patterns

**Files:**
- One public type per file, file name equals type name (PascalCase): `src/Services/Tripory/Tripory.Domain/ValueObjects/ItineraryTitle.cs`.
- Use case artifacts use suffixes: `<Verb><Noun>Command.cs`, `<Verb><Noun>Query.cs`, `<Command|Query>Handler.cs`, `<Command|Query>Validator.cs`. Example: `UpdateItineraryMetadataCommand.cs`, `UpdateItineraryMetadataCommandHandler.cs`, `UpdateItineraryMetadataCommandValidator.cs`.
- Response DTOs end in `Dto` or `Response`: `Responses/ItineraryDetailDto.cs`, `Responses/AuthResponse.cs`.
- HTTP request contracts end in `Request`: `src/Services/Tripory/Tripory.API/Contracts/V1/Itineraries/Requests/CreateItineraryRequest.cs`.
- Entity EF mappings: `<Entity>Configuration.cs` in `Tripory.Persistence/Configurations/`.
- Domain exceptions: `<Thing>Exception.cs` in `Tripory.Domain/Exceptions/`.
- Migrations follow EF naming `yyyyMMddHHmmss_Snake_Case_Description.cs` in `Tripory.Persistence/Migrations/`. Never edit them by hand.

**Types and members:**
- Interfaces prefixed `I` (`IItineraryRepository`, `ICurrentUserService`, `IJwtTokenService`).
- Private fields use `_camelCase` (`_unitOfWork`, `_itineraryRepository`).
- Constants are PascalCase (`ItineraryTitle.MaxLength`, `DestinationCatalogOptions.SectionName`).
- Async methods end in `Async` in repositories/services (`GetOwnedItineraryAsync`); `Handle` is the MediatR exception.
- Namespaces mirror folders and use file-scoped syntax (`namespace Tripory.Application.UseCases.V1.Itineraries.Handlers;`).

**Error codes:**
- Format `<Area>.<Reason>` (`ItineraryTitle.Empty`, `Itinerary.NotFound`, `Auth.InvalidCredentials`). Central registry: `src/Services/Tripory/Tripory.Domain/Errors/DomainErrors.cs` (nested static classes with `static readonly Error` fields, or factory methods like `NotFoundWithId(Guid id)`).
- The code string drives HTTP status mapping in `ApiController.HandlerFailure`: containing `NotFound` gives 404, `Conflict`/`AlreadyExists` gives 409, `Unauthorized` gives 401, `Forbidden` gives 403, anything else gives 400. Pick error code names with this in mind.

## Code Style

**Formatting:**
- No formatter config. Follow existing style: 4-space indent, Allman braces, file-scoped namespaces, `using` directives at the top outside the namespace.
- Single-statement `if` bodies are written without braces on the next line:
  ```csharp
  if (itineraryResult.IsFailure)
      return Result.Failure(itineraryResult.Error);
  ```
- Target framework `net10.0`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>` in every `.csproj` (for example `src/Services/Tripory/Tripory.API/Tripory.API.csproj`). Treat nullable warnings as defects: use `string?` for optional values and `= null!` only for EF-populated properties (`Itinerary.Title`).
- Prefer collection expressions (`[]`) and primary-style records for commands (`public record UpdateItineraryMetadataCommand(...) : ICommand`).

**Linting:**
- Not configured. `dotnet build` with zero warnings is the quality gate defined in `AGENTS.md` section 7.

**Language of text:**
- Code identifiers, comments' technical terms, and file names are English.
- User-facing messages (validation messages, `Error` messages, log templates, XML doc summaries) are written in Vietnamese with diacritics, e.g. `"Tiêu đề hành trình không được để trống."`. Keep this when adding new messages.
- `AGENTS.md`, `ARCHITECTURE.md`, and `ai-memories/*` are in Vietnamese.

## Import Organization

**Order (observed, not enforced):**
1. `BuildingBlocks.Core.*`
2. Third-party (`MediatR`, `FluentValidation`, `Microsoft.*`)
3. `Tripory.*` namespaces

Order is not strictly alphabetical and some files have a stray blank line between groups (`UpdateItineraryMetadataCommandHandler.cs`). Do not reorder existing files just for style.

**Path aliases:** None.

## Layering Rules (from `AGENTS.md`)

Dependency direction: `API` to `Infrastructure`/`Persistence` to `Application` to `Domain` to `BuildingBlocks.Core`.

- `Tripory.Domain`: pure C#. No EF Core, no `HttpClient`, no data annotations. Ports live in `Tripory.Domain/Abstractions/External/` (`IGisDistanceCalculator.cs`); the adapter lives in `Tripory.Infrastructure/Implementations/Gis/GisDistanceCalculator.cs`. Repository interface for itineraries lives in `Tripory.Domain/Repositories/IItineraryRepository.cs`.
- `Tripory.Application`: handlers orchestrate only. Application-side abstractions live in `Tripory.Application/Abstractions/{Data,Security,Storage,Realtime,DestinationCover}/`.
- `Tripory.Persistence`: Fluent API only, in `Configurations/`; repositories in `Repositories/`.
- `Tripory.Infrastructure`: implementations in `Implementations/<Area>/`, options classes in `Configurations/` (`JwtOptions.cs`, `DestinationCatalogOptions.cs`), SignalR hub in `Hubs/ChatHub.cs`.
- `Tripory.API`: thin controllers, middlewares, DI wiring in `Program.cs`.

Each project exposes a `DependencyInjection.cs` extension (`Tripory.Application/DependencyInjection.cs`, `Tripory.Infrastructure/DependencyInjection.cs`, `Tripory.Persistence/DependencyInjection.cs`). Register new services there, not in `Program.cs`. When adding a new interface, make sure both the interface file and its `using` exist in the consuming layer; a missing `IJwtTokenService` reference (`Tripory.Application/Abstractions/Security/IJwtTokenService.cs`) was a recent build error.

## Error Handling

**Result pattern (primary):**
- Expected failures return `Result` / `Result<T>` from `src/BuildingBlocks/Core/Abstractions/Shared/Result.cs` and `ResultT.cs`. `Result<T>.Value` throws `InvalidOperationException` on failure, so always check `IsFailure` first.
- Value objects expose `Create(...)` returning `Result<T>`, never throwing (`ItineraryTitle.Create`).
- Entities return `Result` from mutating methods (`Itinerary.UpdateMetadata`, `Itinerary.CreateQuickDraft`).
- Handler pattern:
  ```csharp
  var title = ItineraryTitle.Create(request.Title);
  if (title.IsFailure)
      return Result.Failure(title.Error);
  ```

**Exceptions (secondary):**
- Base types in `src/BuildingBlocks/Core/Domains/Abstractions/Exceptions/` (`DomainException`, `NotFoundException`, `ConflictException`, `BadRequestException`). Concrete ones in `Tripory.Domain/Exceptions/`.
- `src/Services/Tripory/Tripory.API/Middlewares/GlobalExceptionHandler.cs` maps them to 404/409/400 and everything else to 500 with a generic message. Unhandled exceptions are logged with `LogError`.

**Validation:**
- FluentValidation validators in `UseCases/V1/<Feature>/Validators/`. `Tripory.Application/Behaviors/ValidationPipelineBehavior.cs` runs all validators and returns the first failure as `Result`/`Result<T>` (code from `.WithErrorCode(...)`, fallback `Validation.Error`).
- Set `.WithErrorCode("<Area>.<Reason>")` on every rule so the code matches the value-object/domain code. Shared rules go in extension classes (`ItineraryRuleBuilderExtensions.cs`, `WaypointValidationExtensions.cs`).
- Validators duplicate the cheap format checks of value objects; value objects remain the authority.

## Logging

**Framework:** `Microsoft.Extensions.Logging` (`ILogger<T>`), structured templates with named placeholders.

**Patterns:**
- Every controller action logs `_logger.LogInformation(...)` as its first statement (required by `AGENTS.md`), see `src/Services/Tripory/Tripory.API/Controllers/V1/ItinerariesController.cs`.
- Handlers do not log by default.
- Never log passwords, tokens, or secret configuration values.

## Comments

**When to Comment:**
- Short Vietnamese `//` comments explain why (for example the soft-delete query filter in `ItineraryConfiguration.cs`).
- XML `<summary>` on non-obvious members (`UpdateItineraryMetadataCommand.TrimmedCoverImages`).
- Business rules are documented in `../tripory/docs` (BRD), outside this repo.

## Function Design

**Handlers:** one class per handler, constructor injection into `private readonly` fields, signature `Task<Result> Handle(TCommand request, CancellationToken ct)`. Steps: read via repository, call value objects/entity methods, persist via `IUnitOfWork.SaveChangesAsync(ct)`, return `Result`. No business `if (status == X)` rules in handlers.

**Commands/Queries:** `record` types implementing `ICommand`, `ICommand<T>`, or `IQuery<T>` from `src/BuildingBlocks/Core/CQRS/`. Handlers implement `ICommandHandler<T>` / `IQueryHandler<T,R>`.

**Controllers:**
- Inherit `ApiController` (`[ApiController]`, route `api/v1/[controller]`).
- Template:
  ```csharp
  _logger.LogInformation("...", id);
  var result = await Sender.Send(new XCommand(...), ct);
  if (result.IsFailure)
      return HandlerFailure(result);
  return Ok(ApiResponse<T>.Success(result.Value));
  ```
- Decorate with `[ProducesResponseType(typeof(ApiResponse<T>), StatusCodes.X)]` for each outcome. Accept a `CancellationToken ct` and pass it to `Sender.Send`.
- Request bodies are separate `*Request` classes in `Tripory.API/Contracts/V1/<Feature>/Requests/`, mapped to commands in the controller.
- State-changing operations use `PUT`; never `PATCH` (`AGENTS.md` Pattern 7). Status-change naming: `ChangeStatus<Entity>...`.
- All responses are wrapped in `ApiResponse<T>` (`Tripory.API/Common/Responses/ApiResponse.cs`).

**Entities:**
- Inherit `EntityBase`/`EntityAuditBase`/`EntityFullAuditBase<TId>` and `IAggregateRoot` from `src/BuildingBlocks/Core/Domains/Abstractions/`.
- Private setters, private parameterless constructor for EF, private constructor plus static factory (`Itinerary.CreateQuickDraft`) returning `Result<T>`.
- Collections exposed as `IReadOnlyCollection<T>` backed by private `List<T>` fields.
- Soft delete via `IsDeleted` plus a global query filter.

**Value objects:** `sealed class X : ValueObject`, `const MaxLength/MinLength`, private constructor, static `Create` returning `Result<X>`, trim input. Defined in `Tripory.Domain/ValueObjects/`. Plain strings without invariants stay primitive.

## Module Design

**Exports:** Types are `public` by default across projects; no `internal` convention or `InternalsVisibleTo`.

**Barrel files:** Not applicable (C#).

**Persistence mapping:**
- `IEntityTypeConfiguration<T>` per entity with `builder.ToTable("<table>", "<schema>")` (schemas such as `itinerary`, `identity`); snake_case table names.
- Value objects mapped with `HasConversion` or `OwnsOne`; arrays with `PrimitiveCollection(...)` and `HasDefaultValueSql("'{}'")`.
- Use `.UsePropertyAccessMode(PropertyAccessMode.Field)` for navigation collections backed by private fields.

**Configuration/options:**
- Options classes expose `public const string SectionName` and `{ get; init; }` properties with defaults (`src/Services/Tripory/Tripory.Infrastructure/Configurations/DestinationCatalogOptions.cs`). Secrets (JWT key, DB connection string) come from configuration/environment variables, never from source.

## Process Conventions

- Anti-"vibe coding" workflow in `AGENTS.md`: propose 2-3 options with trade-offs, wait for approval, then implement in small steps. No controllers or migrations before the domain model is approved.
- YAGNI on NuGet packages.
- Record lessons in `ai-memories/` (`INDEX.md` lists files; `gotchas-and-bugs.md` is currently empty).

---

*Convention analysis: 2026-06-01*
