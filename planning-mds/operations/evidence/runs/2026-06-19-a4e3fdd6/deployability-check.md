# Deployability Check — F0023 run 2026-06-19-a4e3fdd6

**Role:** DevOps · **Date:** 2026-06-20

## Runtime / Deployment Config Changes

No new external services, containers, ports, or compose changes. F0023 is an in-process read-side module in the existing `nebula-api` service. No Dockerfile, docker-compose, or CI workflow edits. Casbin runtime policy is the existing embedded resource (`planning-mds/security/policies/policy.csv`, already containing F0023 rows) — no runtime policy file change.

## Migrations / Rollback

- **Migration:** `20260620023855_F0023_SearchSavedViewsOperationalReporting` — creates 4 tables (`SearchDocuments`, `SavedViews`, `SavedViewAuditEvents`, `OperationalReportProjections`) + indexes (incl. `SearchText` trigram GIN) + 1 FK. Additive only; no changes to existing tables.
- **Apply path:** auto-applied at API startup via `Database.MigrateAsync()`; verified live (`__EFMigrationsHistory` contains the migration; 4 tables present).
- **Rollback:** `dotnet ef database update <prior>` drops the 4 new tables (Down() reverses cleanly); no data migration of existing tables.
- **Snapshot note:** pre-existing stale EF model snapshot was reconciled (regenerated to current model) so the migration is F0023-only — see `kg-reconciliation.md` / gate-decisions.

## Env / Config Contract

No new environment variables or configuration keys. Projection backfill is invoked via `ISearchProjectionService.BackfillAsync` (no new scheduler/worker added for MVP; on-demand/initial backfill).

## Manifest Boolean Cross-Check

| Boolean | Value | Justification vs changed_paths |
|---------|-------|-------------------------------|
| `runtime_bearing` | true | `engine/**` services/endpoints/repos + EF migration |
| `frontend_in_scope` | true | `experience/src/features/{search,reports}/**`, pages, TopBar, App |
| `deployment_config_changed` | true | EF migration (schema/deploy-affecting) |
| `security_sensitive_scope` | true | cross-object visibility filtering, saved-view authorization, Casbin rows |

All four booleans consistent with `changed_paths[]` (no §7 path-class false-negative).

## Build / Start / Smoke Results

- `docker compose build api` → image `nebula-insurance-crm-api:latest` built (full `dotnet publish` in `sdk:10.0` build stage).
- `docker compose up -d api` → started; `/healthz` 200 after ~4s.
- Route smoke: `/search-results`, `/saved-views`, `/operational-reports/{workload,workflow-aging}` → 401 (mapped + auth-enforced); `/does-not-exist` → 404.
- API startup logs clean (no migration errors; "Now listening on http://[::]:8080").

## Runtime Warnings

- Frontend production bundle >500 kB (pre-existing app-wide chunk-size warning; not F0023-specific).

## Result

`PASS` — feature builds and runs in application runtime containers; additive migration applies and is reversible; no runtime regressions.
