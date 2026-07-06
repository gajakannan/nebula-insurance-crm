# Action Context — F0017-broker-mga-hierarchy-and-producer-ownership run 2026-06-07-771a5ef6

## Run Identity

| Field | Value |
|-------|-------|
| Action | `feature` (`agents/actions/feature.md`) |
| Feature ID | F0017 |
| Feature Slug | broker-mga-hierarchy-and-producer-ownership |
| Run ID | 2026-06-07-771a5ef6 |
| Run Folder | planning-mds/operations/evidence/runs/2026-06-07-771a5ef6 |
| Feature Index Root | planning-mds/operations/evidence/features/F0017-broker-mga-hierarchy-and-producer-ownership |
| Mode | clean |
| Start Tier / Max Auto Tier | 1 / 2 |
| Rerun Of | null |
| Prior Approved Run | null (clean first run) |
| Contract Effective Date | 2026-06-07 |
| Product Root | /Users/wallstreet290/Documents/WS-PR/nebula-insurance-crm |
| Resume Instruction | Resume this existing run from first incomplete gate after G0/G1; do not create a duplicate run while manifest remains in-progress. |

## Inputs

- `FEATURE_ID=F0017`
- `MODE=clean`
- `SLICE_ORDER_SOURCE=assembly-plan`
- `PRODUCT_ROOT=/Users/wallstreet290/Documents/WS-PR/nebula-insurance-crm`
- Approved plan run: `2026-06-06-5fb353e9` (Phase A + B; G1-G5 PASS)
- Plan-review rerun: `2026-07-02-11251b53` (READY; used as readiness context for this resume)
- Stories: F0017-S0001..S0005 (hierarchy model, navigation, producer ownership effective-dated, territory effective-dated, audit/timeline)
- Architecture: ADR-026 (broker/MGA hierarchy, producer ownership, territory)

## Assumptions

- This session uses the `nebula-agents` harness strictly and resumes the existing feature manifest `2026-06-07-771a5ef6`.
- The feature is runtime-bearing (`engine/` + EF Core migrations), deployment-config-bearing under the harness path classes (EF migration), and UI-bearing (`experience/src/features/distribution/**`).
- Security Reviewer is not forced for this slice (STATUS.md / ADR-026 §6): hierarchy-aware access-control enforcement is deferred to F0037; no recursive access or hierarchy-based permissions are introduced here. `security_sensitive_scope=false`.
- Backend validation uses the local .NET SDK with Docker/Testcontainers support. The sandboxed .NET attempt failed before project execution due MSBuild pipe/socket permissions; the same command passed with approved elevated execution.
- Frontend validation now runs locally in the current macOS workspace; prior WSL `/mnt/c` frontend-toolchain waiver is superseded by current local Vitest/lint/build evidence.

## Scope Boundaries

**In scope (per PRD §Scope):** arbitrary-depth self-referencing broker/MGA hierarchy with cycle/orphan prevention + cached-ancestry read model (S0001), hierarchy navigation/drill-down (S0002), effective-dated producer ownership with point-in-time attribution (S0003), effective-dated territory definition/assignment with overlap prevention (S0004), audit/timeline for all of the above (S0005).

**Out of scope (deferred):** hierarchy-aware rollup reporting (F0037), hierarchy-aware access-control enforcement (F0037), commission/splits (F0025), external producer portal (F0029), carrier appointment detail (F0028), nested territories.

Scope is confined to F0017. No widening beyond this feature boundary.

## Lifecycle Stage

Gate timeline: G0 (architect assembly plan + validation) → G1 (runtime preflight) → implementation → **G2 (self-review + QE + deployability, current)** → G3 (code review; security not forced) → G4 (approval) → G5 (signoff) → G6 (candidate evidence validation) → G7 (architect KG reconciliation) → G8 (PM closeout + supersession + final validation).
