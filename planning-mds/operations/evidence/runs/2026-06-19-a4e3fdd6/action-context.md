# Action Context

## Run Identity

- Feature: `F0023`
- Run: `2026-06-19-a4e3fdd6`
- Mode: `clean`
- Slice order source: `assembly-plan`
- Product root: `/home/gajap/uSandbox/repos/nebula/nebula-insurance-crm`
- Feature path: `/home/gajap/uSandbox/repos/nebula/nebula-insurance-crm/planning-mds/features/F0023-global-search-saved-views-and-operational-reporting`
- Archive feature path: `/home/gajap/uSandbox/repos/nebula/nebula-insurance-crm/planning-mds/features/archive/F0023-global-search-saved-views-and-operational-reporting`
- Feature index root: `/home/gajap/uSandbox/repos/nebula/nebula-insurance-crm/planning-mds/operations/evidence/features/F0023-global-search-saved-views-and-operational-reporting`
- Prior approved run: `None` (no `latest-run.json` for F0023)
- Rerun of: `None`

## Inputs

- `FEATURE_ID=F0023`
- `MODE=clean`
- `SLICE_ORDER_SOURCE=assembly-plan`
- `PRODUCT_ROOT` resolved via `agents/docs/AGENT-USE.md` fallback rule 3 (`../nebula-insurance-crm`)
- Tier defaults (clean): `start_tier=1`, `max_auto_tier=2`

## Assumptions

- `feature-assembly-plan.md` already exists (authored during plan run `2026-06-19-2f180001`); G0 reconciles/validates it rather than authoring from scratch.
- Scope booleans set true from known assembly-plan scope: `runtime_bearing`, `frontend_in_scope`, `security_sensitive_scope`, `deployment_config_changed` (EF migration + backfill command). Reconciled against `changed_paths[]` at G2.

## Scope Boundaries

- In scope: `F0023` read-side SearchReporting module — global search, personal/team saved views, operational workload/aging reports, permission-safe filtering.
- New backend surface under `engine/src/Nebula.*` (SearchReporting), frontend under `experience/src/features/search/**` and `experience/src/features/reports/**`.
- Out of scope (deferred to F0037): hierarchy-aware access enforcement, distribution rollups. Out of scope (MVP): external search engine, report scheduling, free-form report builder, document-content search.

## Precondition Result

- Plan signoff: `MET` — F0023 STATUS.md records Phase A + B approval (plan run `2026-06-19-2f180001`).
- KG validate.py: `MET` — exits 0 after pre-existing stale `coverage-report.yaml` regeneration.
- Runtime containers: `MET` — `nebula-api`, `nebula-db` (healthy), authentik, temporal all up (docker compose ps).
- Concurrent-run reconciliation: prior draft run `2026-06-19-c66da29b` marked `superseded` (operator chose fresh run).

## Lifecycle Stage

Feature evidence execution (G0–G8).
