# Neon wiring report (Tripory BE)

## What changed (commit de1903b, local only, not pushed)
- `src/Services/Tripory/Tripory.Persistence/NpgsqlConnectionStringNormalizer.cs` (new, internal): converts a `postgresql://` / `postgres://` URL (Neon format, including `sslmode` and `channel_binding`) into an Npgsql key=value string. Key=value strings pass through unchanged, so docker-compose local development is unaffected.
- `src/Services/Tripory/Tripory.Persistence/DependencyInjection.cs`: `AddPersistence` wraps the existing `ConnectionStrings:DefaultConnection` lookup with the normalizer. Same config key, no new options, no secrets or hostnames committed. Layering is unchanged (the code is in Persistence only). No migrations were touched.

Usage (names only):
- Runtime: `ConnectionStrings__DefaultConnection` = `DATABASE_URL` (pooled, `-pooler` host).
- EF migrations: `ConnectionStrings__DefaultConnection` = `DATABASE_URL_UNPOOLED` (direct, no `-pooler`). Neon's skill says migrations must use the direct URL because PgBouncer in transaction mode breaks session state.
- The URLs already carry `sslmode=require` (and `channel_binding=require`), both honored.

## Proof
- `dotnet build Tripory.slnx`: `Build succeeded. 0 Warning(s) 0 Error(s)`.
- Throwaway branch: `neon checkout dev --create` created branch `dev` (br-crimson-surf-aovlv6p1, TTL 2026-11-12 from neon.ts policy) and re-pulled `.env.local` to that branch. All tests ran there.
- PostGIS: `CREATE EXTENSION IF NOT EXISTS postgis` on dev succeeded. `pg_extension` shows postgis 3.6.4. On production: 0 postgis extension rows.
- Migrations on dev with the direct URL (`dotnet ef database update --project src/Services/Tripory/Tripory.Persistence --startup-project src/Services/Tripory/Tripory.API`): the first run's console output was lost by the tool. A second run printed "No migrations were applied. The database is already up to date." `__EFMigrationsHistory` on dev lists all 5 migrations (20260918103036_Initial_Identity_Tables through 20261104034058_Clean_Itinerary_Cover_Images).
- API started in the background on the pooled URL (`http://localhost:5251`, Development). HTTP results:
  - POST /api/v1/auth/register -> 200
  - POST /api/v1/auth/login -> 200
  - POST /api/v1/auth/refresh-token -> 200 (body needs both `accessToken` and `refreshToken`)
  - GET /api/v1/users/me -> 200 (email matched)
  - POST /api/v1/itineraries (destinationCode `vn-da-nang`) -> 201
  - GET /api/v1/itineraries/{id} -> 200
- Rows in Neon dev (SQL): `identity.users` with `neon.e2e.%` emails = 4, `identity.refresh_tokens` = 9 (1 revoked by rotation), `itinerary.itineraries` = 1.
- API process stopped afterwards (no `Tripory.API` process left).
- Production branch (br-bitter-wind-aov21ndh): only read-only checks; no `__EFMigrationsHistory`, no postgis. Nothing was applied there.

## Anything that failed
- First `dotnet ef database update` output was swallowed by the tool (see above). The result was confirmed by the second run and the history table.
- My E2E script had two mistakes (status lines captured into variables; refresh-token body missing `accessToken`), which caused a 400 and 401s on earlier attempts. Those runs created extra test users (4 `neon.e2e.*` rows instead of 1). They are on the throwaway dev branch only.
- No psql installed; SQL was run through the Neon MCP `run_sql` tool.
- Development config logs EF SQL parameters (`Persistence:EnableSensitiveDataLogging` is true in appsettings.Development.json), which put refresh tokens in the API log. I deleted those log files. Do not run production with that flag.

## Production migration (not run; commands for you)
```powershell
neon checkout production          # re-pulls .env.local for production
# enable PostGIS once (psql or Neon SQL editor): CREATE EXTENSION IF NOT EXISTS postgis;
foreach ($l in Get-Content .env.local) { if ($l -match '^(\w+)="?(.*?)"?$') { Set-Item "env:$($Matches[1])" $Matches[2] } }
$env:ConnectionStrings__DefaultConnection = $env:DATABASE_URL_UNPOOLED   # direct URL
dotnet ef database update --project src/Services/Tripory/Tripory.Persistence --startup-project src/Services/Tripory/Tripory.API
```
Run the app with `ConnectionStrings__DefaultConnection` set to the pooled `DATABASE_URL`, plus `JwtOptions__SecretKey` and `AuthRateLimitOptions__BffSharedSecret`. Back up or branch production first (`neon checkout` a branch from it) per the repo migration rules.

## Left for you to decide (untracked or modified, not staged)
`Dockerfile`, `.dockerignore`, `neon.ts`, `package.json`, `package-lock.json`, modified `.gitignore`. Also untracked: `.agents/tasks/neon-wiring/` (report, scripts, logs). `.env.local`, `.neon`, `node_modules` are gitignored and not committed. Note `.neon` now pins branch `dev`; run `neon checkout production` to switch back.

## Security note
`neon init` minted an account-wide MCP API key (id 3501169) and wrote it to several global MCP config files. Revoke it with `neon api-keys revoke 3501169` if you do not want it.
