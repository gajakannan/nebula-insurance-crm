# Deployability Check

## Runtime Build

| Surface | Result | Evidence |
|---------|--------|----------|
| Backend API | PASS | `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore --disable-build-servers -v:minimal` |
| Backend tests assembly | PASS | `dotnet build engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-restore --disable-build-servers -v:minimal` |
| Frontend production bundle | PASS | `corepack pnpm --dir experience build` |

## Migration And Configuration

- Added manual migration `20260706140000_F0032_AdminConfiguration`.
- Added EF configurations and `AppDbContext` sets for F0032 governance tables.
- No new external service, secret, container, or environment variable is required.
- Runtime refresh is in-process and recorded through `ConfigurationRefreshStatuses`.

## Deployability Risks

- EF model snapshot still needs regeneration/reconciliation before final signoff.
- The API startup migrator will apply F0032 tables; validate against the shared dev database before G5.
- Direct `pnpm` is unavailable on PATH in this shell; use `corepack pnpm`.
