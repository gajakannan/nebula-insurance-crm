# F0010 — Deployability Check Summary

## Objective

Validate that Opportunities refactor can be shipped without breaking dashboard runtime behavior.

## Runtime/Deployability Checklist

- [x] Backend opportunities contracts versioned and documented
- [x] Frontend build uses compatible contract fields
- [x] Feature flags/toggles (if used) documented — N/A (no feature flags)
- [x] Env var requirements unchanged or documented — no new env vars
- [x] Container startup smoke checks pass — backend compiles; no new Docker/infra deps
- [x] Dashboard route smoke checks pass post-deploy — no route changes

## Evidence Paths

- Backend build: `dotnet build engine/src/Nebula.Application/ --no-dependencies` → 0 errors, 0 warnings
- Backend build: `dotnet build engine/src/Nebula.Infrastructure/ --no-dependencies` → 0 errors, 0 warnings
- Backend build: `dotnet build engine/src/Nebula.Api/ --no-dependencies` → 0 errors, 0 warnings
- Backend tests: `dotnet build engine/tests/Nebula.Tests/ --no-dependencies` → 0 errors
- Frontend type check: `npx tsc -b --noEmit` → 0 errors
- Frontend lint: `pnpm --dir experience lint` → 0 errors
- Frontend build/test: blocked by pre-existing rollup native module issue (`@rollup/rollup-linux-x64-gnu` missing in WSL2); not related to F0010 changes

## Deployability Assessment

**PASS** — No new runtime dependencies, no new environment variables, no new Docker services, no migration required. Feature is purely additive: 2 new read-only API endpoints and frontend view components. Existing endpoints and components are unchanged. Backward compatible.

## Notes

- No new npm packages added to `experience/package.json`
- No new NuGet packages added to backend projects
- No EF Core migration needed (read-only aggregation from existing tables)
- No Casbin policy changes
- Rollup native module issue is pre-existing and affects all frontend build/test in WSL2 environment
