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
| Product Root | /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm |

## Inputs

- `FEATURE_ID=F0017`
- `MODE=clean`
- `SLICE_ORDER_SOURCE=assembly-plan` (read sequence from `feature-assembly-plan.md` once authored at G0)
- `PRODUCT_ROOT` = sister-repo default `../nebula-insurance-crm`
- Approved plan run: `2026-06-06-5fb353e9` (Phase A + B; G1–G5 PASS)
- Stories: F0017-S0001..S0005 (hierarchy model, navigation, producer ownership effective-dated, territory effective-dated, audit/timeline)
- Architecture: ADR-026 (broker/MGA hierarchy, producer ownership, territory)

## Assumptions

- Plan-review-rerun (STATUS.md note "rerun plan-review before feature/build action") is **explicitly skipped** per operator decision; the feature action proceeds on approved plan run `2026-06-06-5fb353e9`. No fresh readiness report is produced this run.
- The feature is runtime-bearing (`engine/` + EF Core migrations) and UI-bearing (`experience/` panels).
- Security Reviewer is **not forced** for this slice (STATUS.md / ADR-026 §6): hierarchy-aware access-control enforcement is deferred to F0037; no recursive access or hierarchy-based permissions introduced here. `security_sensitive_scope=false`.
- Frontend toolchain validation is deferred to CI: the local `/mnt/c` WSL mount cannot run the experience toolchain (recorded environment constraint).
- Backend compile/test execution runs inside the application runtime containers.

## Scope Boundaries

**In scope (per PRD §Scope):** arbitrary-depth self-referencing broker/MGA hierarchy with cycle/orphan prevention + cached-ancestry read model (S0001), hierarchy navigation/drill-down (S0002), effective-dated producer ownership with point-in-time attribution (S0003), effective-dated territory definition/assignment with overlap prevention (S0004), audit/timeline for all of the above (S0005).

**Out of scope (deferred):** hierarchy-aware rollup reporting (F0037), hierarchy-aware access-control enforcement (F0037), commission/splits (F0025), external producer portal (F0029), carrier appointment detail (F0028), nested territories.

Scope is confined to F0017. No widening beyond this feature boundary.

## Lifecycle Stage

Gate timeline: G0 (architect assembly plan + validation) → G1 (runtime preflight) → Step 1 parallel implementation → G2 (self-review + QE + deployability) → G3 (code review; security not forced) → G4 (approval) → G5 (signoff) → G6 (candidate evidence validation) → G7 (architect KG reconciliation) → G8 (PM closeout + supersession + final validation). Current stage: **G0**.
