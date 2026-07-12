# Test Plan

## Scope

Validate that the F0032 implementation compiles and preserves the existing smoke baseline while introducing backend/frontend admin configuration surfaces.

## Test Matrix

| Check | Command | Expected |
|-------|---------|----------|
| Backend API build | `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore --disable-build-servers -v:minimal` | PASS |
| Backend test project build | `dotnet build engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-restore --disable-build-servers -v:minimal` | PASS |
| Frontend production build | `corepack pnpm --dir experience build` | PASS |
| Backend smoke tests | `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-build --filter FullyQualifiedName~SmokeTests --logger console;verbosity=minimal` | PASS |

## Risk-Based Focus

- Compile safety across new domain, application, infrastructure, API, and frontend files.
- Runtime migration registration through API project build.
- Existing smoke baseline stability after adding F0032 services/endpoints.
- Frontend route/workspace type safety.
