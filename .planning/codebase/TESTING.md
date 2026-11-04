# Testing Patterns

**Analysis Date:** 2026-06-01

## Current State

**No automated tests exist.** There is no `tests/` directory, `Tripory.slnx` references no test projects, and no `.csproj` references xUnit, NUnit, MSTest, Moq, NSubstitute, FluentAssertions, or Testcontainers. `AGENTS.md` and `ARCHITECTURE.md` plan `tests/UnitTests/` and `tests/IntegrationTests/`, but neither has been created.

Verification today is:
1. `dotnet build` with zero warnings/errors (quality gate in `AGENTS.md` section 7).
2. Manual API testing via Swagger UI and cURL, per `LOCAL_RUN_AND_TEST_GUIDE.md` (note: its paths reference a macOS location and cover only the early identity endpoints).
3. Endpoint reference in `API_ENDPOINTS.md`.

## Test Framework

**Runner:** Not configured. Recommended default for this stack (net10.0): xUnit with `Microsoft.NET.Test.Sdk`.

**Assertion Library:** Not configured.

**Run Commands (once projects exist):**
```bash
dotnet build                                   # Build whole solution (current gate)
dotnet test Tripory.slnx                       # Run all tests
dotnet test --filter "FullyQualifiedName~Itinerary"   # Filter
dotnet test --collect:"XPlat Code Coverage"    # Coverage
```

## Test File Organization

**Planned layout (from `AGENTS.md`):**
```
tests/
├── UnitTests/          # Domain + Application, no I/O
└── IntegrationTests/   # API + Persistence against PostgreSQL/PostGIS
```

**Recommended conventions when creating them:**
- Mirror the source project: `tests/UnitTests/Tripory.Domain.Tests/ValueObjects/ItineraryTitleTests.cs`.
- Name `<TypeUnderTest>Tests.cs`, methods `Method_Scenario_ExpectedResult`.
- Create with default `dotnet new xunit` (AGENTS.md forbids third-party templates) and add to `Tripory.slnx` with `dotnet sln Tripory.slnx add <path>`.
- Add only the NuGet packages a step needs (YAGNI).

## Where Tests Add The Most Value

Pure, I/O-free logic that can be unit tested without mocks:

- Value objects returning `Result<T>`: `src/Services/Tripory/Tripory.Domain/ValueObjects/` (`ItineraryTitle.cs` rules: empty, whitespace, trim, over 100 chars; `Wgs84Coordinate.cs`, `Email.cs`, `Handle.cs`, `WaypointName.cs`, `ItineraryDescription.cs`).
- Entity state and invariants: `src/Services/Tripory/Tripory.Domain/Entities/Itinerary.cs` (`CreateQuickDraft`, `UpdateMetadata`, cover selection, waypoint reorder), `Conversation.cs`, `ChatMessage.cs`, `RefreshToken.cs`, `User.cs`.
- Catalog lookups: `Tripory.Domain/Catalogs/DestinationCatalog.cs`, `InterestCatalog.cs`.
- FluentValidation validators: `Tripory.Application/UseCases/V1/*/Validators/*.cs` using `FluentValidation.TestHelper` (`validator.TestValidate(command).ShouldHaveValidationErrorFor(...)`), asserting `ErrorCode` values such as `ItineraryTitle.Empty`.
- `ValidationPipelineBehavior` (`Tripory.Application/Behaviors/ValidationPipelineBehavior.cs`): both `Result` and `Result<T>` failure paths use reflection and are easy to break.
- `ApiController.HandlerFailure` error-code to status mapping (`Tripory.API/Controllers/ApiController.cs`).

## Test Structure (recommended pattern)

```csharp
public class ItineraryTitleTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyValue_ReturnsEmptyError(string? input)
    {
        var result = ItineraryTitle.Create(input);

        Assert.True(result.IsFailure);
        Assert.Equal("ItineraryTitle.Empty", result.Error.Code);
    }

    [Fact]
    public void Create_PaddedValue_TrimsWhitespace()
    {
        var result = ItineraryTitle.Create("  Da Lat  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Da Lat", result.Value.Value);
    }
}
```

## Mocking

**Framework:** None installed. When handler tests are needed, mock the abstractions, which are all interfaces:
- `IUnitOfWork` (`src/BuildingBlocks/Core/Abstractions/Persistence/IUnitOfWork.cs`)
- `IItineraryRepository` (`Tripory.Domain/Repositories/IItineraryRepository.cs`)
- `ICurrentUserService`, `IJwtTokenService`, `IPasswordHasher` (`Tripory.Application/Abstractions/Security/`)
- `IChatNotificationService`, `IAudioStorageService`, `IDestinationCoverProvider`, `IGisDistanceCalculator`

**What to mock:** repositories, unit of work, current user, storage, realtime notifier, external adapters.

**What NOT to mock:** value objects, entities, `Result` types, validators. Use real instances.

Handlers contain no `DbContext` access (rule from `AGENTS.md`), so they are testable with mocked repositories. Extension `GetOwnedItineraryAsync` (`Itineraries/Extensions/ItineraryRepositoryExtensions.cs`) is an extension method over `IItineraryRepository`; it works with a mocked repository but cannot itself be mocked.

## Fixtures and Factories

**Test Data:** None exist. Create builders in the test project (for example `ItineraryBuilder` calling `Itinerary.CreateQuickDraft(...)` with a valid `ItineraryTitle`) because entity constructors are private. Use `Guid.NewGuid()` for user ids (`Guid.Empty` is rejected by `CreateQuickDraft`).

## Integration Testing Notes

- Database is PostgreSQL with PostGIS (`docker-compose.yml`, service `tripory-postgres`); EF Core providers `Npgsql.EntityFrameworkCore.PostgreSQL` and `...NetTopologySuite` (`Tripory.Persistence/Tripory.Persistence.csproj`). SQLite/InMemory providers do not support `PrimitiveCollection` arrays with `'{}'` defaults or PostGIS types, so use a real PostgreSQL container (Testcontainers or the compose service).
- Apply migrations with:
  ```bash
  dotnet ef database update --project src/Services/Tripory/Tripory.Persistence --startup-project src/Services/Tripory/Tripory.API
  ```
- API tests can use `WebApplicationFactory<Program>`; `Program.cs` is in `Tripory.API`. It would require `public partial class Program` if not already exposed.
- Supply secrets (JWT signing key, connection string) via environment variables or test configuration; never commit values. Configuration sections: `JwtOptions` (`Tripory.Infrastructure/Configurations/JwtOptions.cs`), `DestinationCatalog` (`DestinationCatalogOptions.cs`).
- Rate limiting (`Tripory.API/RateLimiting/`) and CORS (`Tripory.API/Cors/`) can interfere with repeated auth calls; override `AuthRateLimitOptions` in test configuration.
- SignalR hub `Tripory.Infrastructure/Hubs/ChatHub.cs` needs a SignalR client test harness.

## Coverage

**Requirements:** None enforced. No CI configuration detected in the repo (no `.github/workflows`).

**Suggested priority (High to Low):**
1. High: value objects, `Itinerary` entity methods, validators, `ValidationPipelineBehavior`, auth handlers (`Tripory.Application/UseCases/V1/Auth/Handlers/`).
2. Medium: itinerary and chat handlers, `HandlerFailure` mapping, `GlobalExceptionHandler`.
3. Low: EF configurations and migrations (cover via integration smoke test).

## Test Types

**Unit Tests:** planned in `tests/UnitTests/`; Domain and Application only, no I/O.

**Integration Tests:** planned in `tests/IntegrationTests/`; real PostgreSQL/PostGIS, `WebApplicationFactory`.

**E2E Tests:** Not used. Manual Swagger/cURL flows only (`LOCAL_RUN_AND_TEST_GUIDE.md`).

## Common Patterns

**Result assertions:**
```csharp
Assert.True(result.IsFailure);
Assert.Equal("Itinerary.NotFound", result.Error.Code);
```

**Async handler test:**
```csharp
var result = await handler.Handle(command, CancellationToken.None);
Assert.True(result.IsSuccess);
```

**Exception assertions** (only for the exception-based paths in `Tripory.Domain/Exceptions/`):
```csharp
await Assert.ThrowsAsync<UserNotFoundException>(() => handler.Handle(query, CancellationToken.None));
```

---

*Testing analysis: 2026-06-01*
