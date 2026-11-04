# Codebase Concerns

**Analysis Date:** 2026-11-04

Scope: `src/BuildingBlocks/Core`, `src/Services/Tripory/{Tripory.Domain,Tripory.Application,Tripory.Persistence,Tripory.Infrastructure,Tripory.API}`. No `tests/` directory exists. Findings are based on reading the files cited; severity reflects impact on correctness and security.

## Tech Debt

**Committed development JWT signing key and DB password:**
- Issue: `appsettings.json` ships a literal `JwtOptions:SecretKey` and a `ConnectionStrings:DefaultConnection` containing the Postgres password. `docker-compose.yml` also sets `POSTGRES_PASSWORD` inline. `AddPersistence` falls back to a hard-coded localhost connection string with the same credentials when the config key is missing.
- Files: `src/Services/Tripory/Tripory.API/appsettings.json`, `src/Services/Tripory/Tripory.Persistence/DependencyInjection.cs`, `docker-compose.yml`
- Impact: Anyone with repo access can mint valid JWTs if the same key reaches a deployed environment. Missing config silently connects to a default database instead of failing.
- Fix approach: Move `JwtOptions__SecretKey`, `ConnectionStrings__DefaultConnection` and `POSTGRES_PASSWORD` to environment variables / user-secrets / a vault. Remove the connection-string fallback in `src/Services/Tripory/Tripory.Persistence/DependencyInjection.cs` and throw if absent. Add a `JwtOptions` validation for minimum key length and non-empty `SecretKey`/`Issuer`/`Audience` in `src/Services/Tripory/Tripory.Infrastructure/DependencyInjection.cs` (only `ExpiryMinutes` and `RefreshTokenExpiryDays` are validated today).

**Dev BFF shared secret in `appsettings.Development.json`:**
- Issue: `AuthRateLimitOptions:BffSharedSecret` has a placeholder value committed. If a production config omits the key, behavior depends on `ClientIpResolver` (empty secret means the header is ignored, which is safe), but a deployed Development profile would trust the known value.
- Files: `src/Services/Tripory/Tripory.API/appsettings.Development.json`, `src/Services/Tripory/Tripory.API/RateLimiting/ClientIpResolver.cs`, `src/Services/Tripory/Tripory.API/RateLimiting/AuthRateLimitOptions.cs`
- Fix approach: Supply via environment variable (`AuthRateLimitOptions__BffSharedSecret`) per environment; keep the committed file free of values.

**Unresolved `@TODO` markers:**
- `src/Services/Tripory/Tripory.API/Program.cs` line ~14: split `Program.cs` into per-concern extension methods (JWT, Swagger, pipeline). It is 135 lines of inline wiring; JWT and Swagger setup belong in `Extensions/` classes like the existing `src/Services/Tripory/Tripory.API/Cors/CorsExtensions.cs` and `src/Services/Tripory/Tripory.API/RateLimiting/RateLimitingExtensions.cs`.
- `src/Services/Tripory/Tripory.Infrastructure/Implementations/Security/JwtTokenService.cs`: "log claims" TODO on role assignment.
- `src/Services/Tripory/Tripory.Application/UseCases/V1/Auth/Validators/RegisterCommandValidator.cs`: TODO asking how the file is used (validators are auto-discovered by `ValidationPipelineBehavior`; remove or answer the comment).

**Layering deviations from `AGENTS.md`:**
- `src/Services/Tripory/Tripory.Application/Abstractions/Data/` (`IUserRepository`, `IConversationRepository`, `IChatMessageRepository`) holds repository ports while `IItineraryRepository` lives in `src/Services/Tripory/Tripory.Domain/Repositories/IItineraryRepository.cs`. Repository interface placement is inconsistent between aggregates.
- `src/Services/Tripory/Tripory.Infrastructure/Hubs/ChatHub.cs` is an API-surface class (SignalR endpoint) living in Infrastructure; `Program.cs` references `Tripory.Infrastructure.Hubs` directly. AGENTS.md assigns HTTP gateway concerns to `Tripory.API`.
- `src/Services/Tripory/Tripory.Persistence/Repositories/RepositoryBase.cs` takes a plain `DbContext`, while `UserRepository`, `ConversationRepository`, `ChatMessageRepository`, `UnitOfWork` take `ApplicationDbContext` and do not extend `RepositoryBase`. Two repository styles coexist.
- `Tripory.Domain` contains `Catalogs/DestinationCatalog.cs` and `Catalogs/InterestCatalog.cs` as static reference data. Confirm these stay pure data; a catalog change needs a code deploy.

**Inconsistent language in code and messages:**
- Log messages, validator messages, error text and comments are Vietnamese (for example `src/Services/Tripory/Tripory.API/Middlewares/GlobalExceptionHandler.cs`), while identifiers are English. Clients receive Vietnamese `ApiResponse` messages with no localization mechanism.
- Parameter typos: `enviroment` in the `LocalAudioStorageService` constructor (`src/Services/Tripory/Tripory.Infrastructure/Implementations/Storage/LocalAudioStorageService.cs`).

## Known Bugs

**No confirmed runtime bugs found.** Prior compile errors in this session (`DestinationCatalogOptions.cs` syntax error; `IJwtTokenService` not found) are no longer present: `src/Services/Tripory/Tripory.Infrastructure/Configurations/DestinationCatalogOptions.cs` is syntactically valid and `src/Services/Tripory/Tripory.Application/Abstractions/Security/IJwtTokenService.cs` exists. Run `dotnet build Tripory.slnx` to confirm.

**Potential: ILIKE wildcard injection in user search:**
- Symptoms: Search text containing `%` or `_` acts as a wildcard (for example `%` returns every user).
- Files: `src/Services/Tripory/Tripory.Persistence/Repositories/UserRepository.cs` (`SearchUsersAsync`)
- Trigger: `GET` users search with `%` or `_` in the term. SQL injection itself is avoided by `FromSqlInterpolated`.
- Workaround: Escape `\`, `%`, `_` in the term before building the pattern, or use `EF.Functions.ILike(..., escapeChar)`.

**Potential: hard-coded enum value in raw SQL:**
- `"Status" <> 3` in `SearchUsersAsync` duplicates `UserStatus.Banned` from `src/Services/Tripory/Tripory.Domain/Enums/UserStatus.cs`. Changing the enum silently breaks the filter, and the non-raw branch uses the enum. Also hard-codes table `identity.users` and column casing.

## Security Considerations

**Access token accepted from query string on all `/hubs/` paths:**
- Risk: Tokens in URLs are written to proxy and server access logs.
- Files: `src/Services/Tripory/Tripory.API/Program.cs` (`OnMessageReceived`)
- Current mitigation: Restricted to paths starting with `/hubs/`.
- Recommendations: Keep, but ensure request logging scrubs `access_token`; prefer short-lived tokens for SignalR.

**SignalR hub lacks authorization checks on room and call signaling:**
- Risk: `JoinConversation(conversationId)` adds any authenticated connection to `conv_{id}` without verifying participation. `CallUser`, `AcceptCall`, `RejectCall`, `EndCall`, `SendIceCandidate` forward arbitrary `object` payloads to any `targetUserId` with no check that a conversation or relationship exists, and no rate limit. Any authenticated user can ring or spam any other user. Payloads typed `object` are unvalidated and unbounded in size beyond SignalR's message limit.
- Files: `src/Services/Tripory/Tripory.Infrastructure/Hubs/ChatHub.cs`
- Current mitigation: `[Authorize]` on the hub only.
- Recommendations: Check conversation membership (via `IConversationRepository`) before joining groups and before relaying call signals; type the SDP/ICE payloads; add per-connection throttling. `Clients.User(...)` depends on `UserIdentifier`, which defaults to `ClaimTypes.NameIdentifier`; this matches `JwtTokenService` claims but is implicit.

**Voice message URL is client-supplied and unvalidated:**
- Risk: `SendVoiceMessageCommand.VoiceUrl` is only checked `NotEmpty`. A client can store any URL (external tracking, `javascript:` or internal addresses) that other users' clients will load.
- Files: `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Validators/SendVoiceMessageCommandValidator.cs`, `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Handlers/SendVoiceMessageCommandHandler.cs`, `src/Services/Tripory/Tripory.API/Controllers/V1/ChatController.cs`
- Recommendations: Accept only URLs produced by `IAudioStorageService` (same host and `/uploads/voices/` prefix), or return/store an opaque file id from the upload endpoint.

**Audio upload trusts client MIME type and file name:**
- Risk: `ValidateAudioFile` checks `file.ContentType` (client-controlled) and size only; no magic-byte inspection. `ResolveFileExtension` uses the extension from the client file name verbatim (`Path.GetExtension`), so `.html`, `.svg` or `.aspx` can be written under `wwwroot/uploads/voices` and served by `app.UseStaticFiles()`, enabling stored XSS on the API origin.
- Files: `src/Services/Tripory/Tripory.API/Controllers/V1/ChatController.cs`, `src/Services/Tripory/Tripory.Infrastructure/Implementations/Storage/LocalAudioStorageService.cs`, `src/Services/Tripory/Tripory.API/Program.cs`
- Recommendations: Derive the extension from the validated MIME type only (allow-list), verify file signatures, and serve uploads with `Content-Disposition`/`X-Content-Type-Options: nosniff`. Add `[RequestSizeLimit]` on the upload action. Uploaded files are publicly readable without authentication (static files).

**Upload file cleanup is incomplete:**
- Uploaded files that are never attached to a message are never deleted. `DeleteAudioAsync` exists but `ResolveLocalFilePath` joins a path derived from a caller URL under `WebRootPath` without verifying the result stays inside the uploads folder (path traversal risk if a user-controlled URL ever reaches it).
- Files: `src/Services/Tripory/Tripory.Infrastructure/Implementations/Storage/LocalAudioStorageService.cs`

**Sensitive EF logging enabled unconditionally:**
- Risk: `options.EnableSensitiveDataLogging()` and `EnableDetailedErrors()` are on in every environment, logging parameter values (emails, password hashes, refresh tokens) at Information level and above when queries log.
- Files: `src/Services/Tripory/Tripory.Persistence/DependencyInjection.cs`
- Recommendations: Gate behind `IHostEnvironment.IsDevelopment()`.

**Refresh token handling:**
- `RefreshTokenCommandHandler` loads the user with all `RefreshTokens` and compares `t.Token == request.RefreshToken` in memory; tokens are stored and compared in plaintext (`src/Services/Tripory/Tripory.Domain/Entities/RefreshToken.cs`, `src/Services/Tripory/Tripory.Persistence/Configurations/RefreshTokenConfiguration.cs`). A DB leak exposes live sessions. Store a SHA-256 hash.
- `RefreshTokenCommandHandler` passes `ipAddress: null` to `RevokeRefreshToken`/`AddRefreshToken`, so the IP audit fields are never filled.
- `JwtTokenService.GetPrincipalFromExpiredToken` calls `ValidateToken`, which throws `SecurityTokenException` on a malformed or badly signed token. The handler expects `null`, so invalid input produces an unhandled exception that `GlobalExceptionHandler` maps to HTTP 500 instead of 401/400. It also sets `ValidateIssuer = false` and `ValidateAudience = false`.
- Files: `src/Services/Tripory/Tripory.Infrastructure/Implementations/Security/JwtTokenService.cs`, `src/Services/Tripory/Tripory.Application/UseCases/V1/Auth/Handlers/RefreshTokenCommandHandler.cs`
- Access tokens are not revoked on logout (valid until expiry; `ExpiryMinutes` is 60 in `appsettings.json` while the `JwtOptions` default is 15).
- Two concurrent refresh requests with the same token race: both can pass the `IsRevoked` check before either saves. No concurrency token on `RefreshToken`.

**Banned/deleted users keep valid access tokens:**
- Authorization relies on JWT claims only; `User.Status` is checked in the refresh handler but not per request (`src/Services/Tripory/Tripory.Infrastructure/Implementations/Security/CurrentUserService.cs`).

**Unhandled exception details:**
- `GlobalExceptionHandler` returns `de.Message` for `DomainException`s and logs every exception at Error, including expected 4xx domain exceptions, creating noisy logs. Unhandled `OperationCanceledException` (client abort) is logged as an error and returned as 500.
- Files: `src/Services/Tripory/Tripory.API/Middlewares/GlobalExceptionHandler.cs`

**Transport and host settings:**
- `AllowedHosts: "*"` in `src/Services/Tripory/Tripory.API/appsettings.json`. No `UseHttpsRedirection`/`UseHsts` in `src/Services/Tripory/Tripory.API/Program.cs`; rely on a proxy and document it.
- `Cors:AllowedOrigins` is empty in base config; `CorsExtensions.WarnIfCorsIsOpen` warns rather than failing (`src/Services/Tripory/Tripory.API/Cors/CorsExtensions.cs`).
- Rate limiting covers auth endpoints only (`src/Services/Tripory/Tripory.API/RateLimiting/RateLimitingExtensions.cs`); chat, upload and itinerary endpoints are unthrottled. Rate-limit partition key trusts `X-Forwarded-For` only when the BFF secret header matches (`ClientIpResolver.cs`), which is sound but depends on the secret being set in every environment.
- Swagger is limited to Development; the `Bearer` definition uses `SecuritySchemeType.ApiKey` instead of `Http`/`bearer`, requiring users to type the `Bearer ` prefix manually.

## Performance Bottlenecks

**Full user aggregate load on every auth operation:**
- Problem: `UserRepository.GetByIdAsync/GetByEmailAsync/GetByHandleAsync` always `Include` `UserRoles` and `RefreshTokens`. `RefreshTokens` grows unbounded per user (no purge of expired/revoked rows).
- Files: `src/Services/Tripory/Tripory.Persistence/Repositories/UserRepository.cs`, `src/Services/Tripory/Tripory.Domain/Entities/User.cs`
- Improvement path: Query the single token by value; add a cleanup job for expired tokens.

**`RepositoryBase.FindAll` exposes `IQueryable` to handlers:**
- Handlers can compose arbitrary queries, risking N+1 and untracked/tracked inconsistency. Review use in `src/Services/Tripory/Tripory.Application/UseCases/V1/Itineraries/Extensions/ItineraryRepositoryExtensions.cs`.
- Files: `src/Services/Tripory/Tripory.Persistence/Repositories/RepositoryBase.cs`

**Itinerary aggregate size:**
- `src/Services/Tripory/Tripory.Domain/Entities/Itinerary.cs` (313 lines) loads days and waypoints together; waypoint reorder/add rewrites ordering in memory. Acceptable at current scale; watch for large itineraries and the PostGIS tables.

**Chat paging:**
- `GetMessagesQuery` uses `page`/`pageSize` offset paging (`src/Services/Tripory/Tripory.API/Controllers/V1/ChatController.cs`); deep pages on large conversations are slow and unstable under inserts. Prefer keyset paging on `CreatedAt`.

## Fragile Areas

**Chat notification swallowing exceptions:**
- Files: `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Handlers/SendVoiceMessageCommandHandler.cs` (and the equivalent in `SendTextMessageCommandHandler.cs`, `LogCallSessionCommandHandler.cs`)
- Why fragile: Bare `catch { }` with no logging hides SignalR failures; the pattern is copy-pasted across handlers. Application layer also depends on `IChatNotificationService` realtime concerns inside handlers.
- Safe modification: Log the exception in the catch and extract one shared helper or a domain-event handler.

**Duplicated handler boilerplate:**
- The "get current user, load conversation, check participant" sequence is repeated across `SendTextMessageCommandHandler`, `SendVoiceMessageCommandHandler`, `LogCallSessionCommandHandler`, `MarkConversationAsReadCommandHandler`, `GetMessagesQueryHandler` in `src/Services/Tripory/Tripory.Application/UseCases/V1/Chat/Handlers/`. A change to the authorization rule must be made in each file.

**`SaveChangesAsync` called from handlers with no transaction boundary or concurrency control:**
- `src/Services/Tripory/Tripory.Persistence/Repositories/UnitOfWork.cs` is a thin wrapper. `Conversation.UpdateLastMessage` and `GetOrCreateConversationCommandHandler` have races (two simultaneous creates for the same pair). Verify a unique index on the participant pair in `src/Services/Tripory/Tripory.Persistence/Configurations/ConversationConfiguration.cs` and handle `DbUpdateException`.
- No `RowVersion`/xmin concurrency tokens on `Itinerary`, `Conversation`, `User` configurations.

**Domain events defined but not dispatched:**
- `src/BuildingBlocks/Core/CQRS/IDomainEvent.cs` and `IDomainEventHandler.cs` exist; `src/Services/Tripory/Tripory.Persistence/ApplicationDbContext.cs` and `UnitOfWork.cs` contain no dispatch. Treat as unused scaffolding until implemented.

**Raw SQL tied to schema naming:**
- `UserRepository.SearchUsersAsync` embeds `identity.users` and quoted column names; renaming a column in `src/Services/Tripory/Tripory.Persistence/Configurations/UserConfiguration.cs` breaks it at runtime, not compile time.

**Migrations:**
- Four migrations plus snapshot in `src/Services/Tripory/Tripory.Persistence/Migrations/`. Migrations are never applied automatically (no `Database.Migrate()` in `Program.cs`); deployment must run them separately. Migration timestamps (`20260918...` to `20261103...`) should be checked against the real clock when ordering new migrations.

## Scaling Limits

**Local disk audio storage:**
- Current capacity: Files stored under `wwwroot/uploads/voices` on the API instance (`src/Services/Tripory/Tripory.Infrastructure/Implementations/Storage/LocalAudioStorageService.cs`).
- Limit: Breaks with more than one API instance or ephemeral containers (files lost or unreachable). Public URL is built from the request `Scheme`/`Host`, so it changes behind proxies unless forwarded headers are configured (not configured in `Program.cs`).
- Scaling path: Implement S3-compatible `IAudioStorageService` in Infrastructure (AGENTS.md names S3) and store object keys instead of absolute URLs.

**SignalR single-node:**
- `services.AddSignalR()` with no backplane (`src/Services/Tripory/Tripory.Infrastructure/DependencyInjection.cs`). Group and `Clients.User` messaging only reaches clients on the same server. Add Redis backplane before scaling out. WebRTC calls have no TURN/STUN configuration in the backend.

**Rate limiter is in-memory per instance** (`src/Services/Tripory/Tripory.API/RateLimiting/RateLimitingExtensions.cs`).

## Dependencies at Risk

**.NET 10 target (`net10.0`) across all `*.csproj`:**
- Risk: Requires the matching SDK on all dev/CI machines; package compatibility (Npgsql, NetTopologySuite, FluentValidation 12.1.1, Swashbuckle `Microsoft.OpenApi.Models` usage in `Program.cs`) should be rechecked on upgrades, since OpenAPI v2 type namespaces changed in newer Microsoft.OpenApi releases.
- Files: `src/Services/Tripory/Tripory.Application/Tripory.Application.csproj`, `src/Services/Tripory/Tripory.API/Tripory.API.csproj`

**`GisDistanceCalculator` hand-rolled geodesic math:**
- `src/Services/Tripory/Tripory.Infrastructure/Implementations/Gis/GisDistanceCalculator.cs`: verify against PostGIS results; no tests.

**Static external image host:**
- `DestinationCatalog:CoverImageBaseUrl` points at a third-party Vercel deployment (`src/Services/Tripory/Tripory.API/appsettings.json`); cover images break if that site moves. Validation requires HTTPS and a trailing `/` (`src/Services/Tripory/Tripory.Infrastructure/DependencyInjection.cs`).

## Missing Critical Features

**Automated tests:**
- Problem: AGENTS.md specifies `tests/UnitTests` and `tests/IntegrationTests`; neither directory exists and `Tripory.slnx` has no test projects.
- Blocks: Safe refactoring of the domain rules (`Itinerary`, `Conversation`, `User`), regression checks for auth token rotation, CI quality gate.

**CI/CD pipeline:**
- No pipeline definition detected at the repo root (only `docker-compose.yml` for Postgres/PostGIS). No container image for the API, no health-check endpoints (`MapHealthChecks` absent in `Program.cs`).

**Observability:**
- Logging uses default `ILogger` only. No structured sink, correlation id, metrics or tracing. Controllers log on entry (per AGENTS.md) but `UsersController`/`AuthController` should be audited for consistency.

**Auth feature gaps:**
- No email verification, password reset, or account lockout detected in `src/Services/Tripory/Tripory.Application/UseCases/V1/Auth/` (commands: Register, Login, Logout, RefreshToken, ChangePassword). Login relies on rate limiting alone.

**Authorization model:**
- Roles are issued into JWT (`ClaimTypes.Role`), but no policy-based authorization or role-restricted endpoints were found beyond `[Authorize]`. `GET` users listing (`src/Services/Tripory/Tripory.Application/UseCases/V1/Users/Handlers/GetUsersQueryHandler.cs`) searches by email; confirm DTOs do not expose emails of other users.

## Test Coverage Gaps

**Entire solution is untested:**
- What's not tested: Domain invariants (`src/Services/Tripory/Tripory.Domain/ValueObjects/*`, `Entities/Itinerary.cs`, `Entities/Conversation.cs`, `Entities/User.cs`), `ValidationPipelineBehavior` (`src/Services/Tripory/Tripory.Application/Behaviors/ValidationPipelineBehavior.cs`), all handlers, `JwtTokenService`, repositories with PostGIS, SignalR hub.
- Risk: Auth refresh rotation, reorder-waypoint logic, and chat participant checks can regress unnoticed.
- Priority: High for Auth handlers and `Itinerary` aggregate; Medium for chat handlers; Low for DTO mapping extensions.

---

*Concerns audit: 2026-11-04*
