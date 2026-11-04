# External Integrations

**Analysis Date:** 2026-11-03

## APIs & External Services

**Destination cover images:**
- Static image host (Vercel-hosted frontend, `/destinations/` path) - the backend does not call it; it builds cover image URLs from a base URL.
  - Implementation: `src/Services/Tripory/Tripory.Infrastructure/Implementations/DestinationCover/DestinationCoverProvider.cs` (port `IDestinationCoverProvider` in `Tripory.Application/Abstractions/DestinationCover`)
  - Config: `DestinationCatalog:CoverImageBaseUrl` bound to `src/Services/Tripory/Tripory.Infrastructure/Configurations/DestinationCatalogOptions.cs`
  - Auth: none

**Backend-for-Frontend (BFF) caller:**
- A trusted BFF forwards the real client IP in `X-Forwarded-For`, authenticated by the `X-Bff-Secret` header.
  - Implementation: `src/Services/Tripory/Tripory.API/RateLimiting/ClientIpResolver.cs`
  - Auth: shared secret in config key `AuthRateLimitOptions:BffSharedSecret` (compared with `CryptographicOperations.FixedTimeEquals`)

**Not detected:** No outbound `HttpClient`/SDK usage, geocoding, mail, SMS, payment, or push services. `GisDistanceCalculator` (`src/Services/Tripory/Tripory.Infrastructure/Implementations/Gis/GisDistanceCalculator.cs`) is a local Haversine computation (earth radius 6371 km), not an external call.

## Data Storage

**Databases:**
- PostgreSQL 16 with PostGIS 3.4 (`postgis/postgis:16-3.4` in `docker-compose.yml`, container `tripory-postgres`, database `tripory_db`, port 5432)
  - Connection: config key `ConnectionStrings:DefaultConnection` (env var form `ConnectionStrings__DefaultConnection`)
  - Client: EF Core 10 + Npgsql + NetTopologySuite, `src/Services/Tripory/Tripory.Persistence/ApplicationDbContext.cs`
  - Mapping: Fluent API in `src/Services/Tripory/Tripory.Persistence/Configurations/*Configuration.cs`
  - Repositories and unit of work: `src/Services/Tripory/Tripory.Persistence/Repositories/` (`ItineraryRepository`, `UserRepository`, `ConversationRepository`, `ChatMessageRepository`, `UnitOfWork`)
  - Migrations: `src/Services/Tripory/Tripory.Persistence/Migrations/`; apply with `dotnet ef database update` (no auto-migrate on startup in `Program.cs`)

**File Storage:**
- Local filesystem only. Voice messages are saved under `wwwroot/<upload folder>` by `src/Services/Tripory/Tripory.Infrastructure/Implementations/Storage/LocalAudioStorageService.cs` (port `IAudioStorageService`) and served by `app.UseStaticFiles()` in `src/Services/Tripory/Tripory.API/Program.cs`. `AGENTS.md` mentions AWS S3 as a possible Infrastructure adapter, but no S3 code exists.

**Caching:**
- None. `AGENTS.md` lists Redis as a possible adapter, but no Redis package or code exists.

## Authentication & Identity

**Auth Provider:**
- Custom JWT bearer authentication with refresh tokens.
  - Token issuing: `src/Services/Tripory/Tripory.Infrastructure/Implementations/Security/JwtTokenService.cs` (port `IJwtTokenService` in `Tripory.Application/Abstractions/Security`)
  - Validation: `AddJwtBearer` in `src/Services/Tripory/Tripory.API/Program.cs` (issuer, audience, lifetime, signing key validated; `ClockSkew = TimeSpan.Zero`; symmetric HMAC key)
  - SignalR: token is accepted from the `access_token` query string only for paths starting with `/hubs/`
  - Password hashing: BCrypt, `src/Services/Tripory/Tripory.Infrastructure/Implementations/Security/BcryptPasswordHasher.cs`
  - Current user: `src/Services/Tripory/Tripory.Infrastructure/Implementations/Security/CurrentUserService.cs` (via `IHttpContextAccessor`)
  - Persistence of identity: `UserConfiguration`, `RoleConfiguration`, `UserRoleConfiguration`, `RefreshTokenConfiguration` in `Tripory.Persistence/Configurations/`
  - Endpoints: `src/Services/Tripory/Tripory.API/Controllers/V1/AuthController.cs`, `UsersController.cs`
  - Config keys: `JwtOptions:SecretKey`, `JwtOptions:Issuer`, `JwtOptions:Audience`, `JwtOptions:ExpiryMinutes`, `JwtOptions:RefreshTokenExpiryDays`
- Rate limiting on auth endpoints (login, register, refresh): `src/Services/Tripory/Tripory.API/RateLimiting/RateLimitingExtensions.cs`, policies in `RateLimitPolicies.cs`; middleware order `UseCors` -> `UseRateLimiter` -> `UseAuthentication` -> `UseAuthorization`.

## Realtime

- SignalR hub `/hubs/chat`: `src/Services/Tripory/Tripory.Infrastructure/Hubs/ChatHub.cs`; server-to-client pushes through `src/Services/Tripory/Tripory.Infrastructure/Implementations/Realtime/ChatNotificationService.cs` (port `IChatNotificationService`). Uses the default in-memory backplane (no Redis/Azure SignalR), so it works for a single instance only.
- Chat REST endpoints: `src/Services/Tripory/Tripory.API/Controllers/V1/ChatController.cs`.

## Monitoring & Observability

**Error Tracking:**
- None (no Sentry/AppInsights). Unhandled errors go through `src/Services/Tripory/Tripory.API/Middlewares/GlobalExceptionHandler.cs` and ProblemDetails (`AddExceptionHandler` + `AddProblemDetails` in `Program.cs`).

**Logs:**
- Built-in `ILogger` with console output; levels set under `Logging:LogLevel` in `appsettings.json`. Controllers and services log with `_logger.LogInformation(...)` (convention from `AGENTS.md`). No Serilog or structured sink.
- EF Core `EnableSensitiveDataLogging()` and `EnableDetailedErrors()` are on unconditionally in `src/Services/Tripory/Tripory.Persistence/DependencyInjection.cs`.

## CI/CD & Deployment

**Hosting:**
- Not defined (no Dockerfile, no manifests).

**CI Pipeline:**
- None detected (no `.github/workflows`, no pipeline YAML).

## Environment Configuration

**Required env vars / config keys (names only):**
- `ASPNETCORE_ENVIRONMENT`
- `ConnectionStrings__DefaultConnection`
- `JwtOptions__SecretKey`, `JwtOptions__Issuer`, `JwtOptions__Audience`, `JwtOptions__ExpiryMinutes`, `JwtOptions__RefreshTokenExpiryDays`
- `DestinationCatalog__CoverImageBaseUrl`
- `Cors__AllowedOrigins__0` (indexed list form)
- `AuthRateLimitOptions__BffSharedSecret`
- Compose-only: `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` (`docker-compose.yml`)

**Secrets location:**
- Development values are committed in `src/Services/Tripory/Tripory.API/appsettings.json`, `appsettings.Development.json` and `docker-compose.yml`. No secret manager, vault, or `.env` file is configured. `.env` is gitignored.

**CORS:**
- `src/Services/Tripory/Tripory.API/Cors/CorsExtensions.cs` builds policy `TriporyCors` from `Cors:AllowedOrigins`. A non-empty list restricts origins with `AllowCredentials`; an empty list allows any origin with credentials (a warning is emitted by `WarnIfCorsIsOpen`). `appsettings.Development.json` allows localhost ports 3000 and 5173.

## Webhooks & Callbacks

**Incoming:**
- None.

**Outgoing:**
- None.

---

*Integration audit: 2026-11-03*
