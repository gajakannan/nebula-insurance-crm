# Deployability Check - F0025 run 2026-07-07-9859bad4

## Runtime Bearing

F0025 adds API endpoints, EF entities/configuration, repositories, DI registrations, and a database migration, so the feature is runtime-bearing.

## Migration

- Added migration: `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260707150000_F0025_CommissionRevenue.cs`.
- `dotnet ef migrations add` hung earlier in the run, so the migration was manually authored against the compiled model. `Nebula.Api` builds successfully.
- G6 should apply the migration against the local Postgres service and verify startup after migration.

## Frontend

- Added protected routes `/commissions` and `/commissions/:expectedCommissionId`.
- Added sidebar navigation item.
- `pnpm --dir experience build` passed.

## Configuration

- No committed environment file changes.
- A local ignored `.env` was restored from `.env.example` with dev-only values to make Authentik healthy during G1.

## Result

Deployability is PASS for G2 with G6 migration-apply verification required before closeout.
