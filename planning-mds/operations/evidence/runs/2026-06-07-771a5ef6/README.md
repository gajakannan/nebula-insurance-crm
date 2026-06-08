# Feature Evidence README — F0017-broker-mga-hierarchy-and-producer-ownership run 2026-06-07-771a5ef6

> `{PRODUCT_ROOT}/planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/README.md` (§8).

## Run Summary

`feature` action for **F0017 — Broker/MGA Hierarchy, Producer Ownership & Territory Management**, a clean first run (no prior approved run). Builds the vertical slice across `engine/` (self-referencing hierarchy + cached ancestry, effective-dated producer ownership, effective-dated territory management with overlap prevention, audit/timeline) and `experience/` (hierarchy/ownership/territory panels). Operator-driven; backend validation runs in application runtime containers; frontend toolchain validation is deferred to CI (local `/mnt/c` WSL mount cannot run the experience toolchain) and documented as a waiver in `coverage-report.md`. Proceeding on approved plan run `2026-06-06-5fb353e9`; the STATUS.md plan-review-rerun note was explicitly skipped per operator decision (see `action-context.md`).

## Status

`draft` (G0 in progress) — must agree with `evidence-manifest.json` `status`. Transitions: `draft`@G0 → `in-progress`@G1–G7 → `approved`@G8.

## Evidence Index

- `evidence-manifest.json` — schema v1 (§11)
- `action-context.md` — Run Identity, Inputs, Assumptions, Scope Boundaries, Lifecycle Stage
- `artifact-trace.md` — read/written artifacts
- `gate-decisions.md` — pass/fail/skip per gate row (§17 stage matrix)
- `commands.log` — JSON Lines per §13
- `lifecycle-gates.log` — lifecycle gate run summary
- Role and gate reports — `g0-assembly-plan-validation.md`, `g1-runtime-preflight.md`, `g2-self-review.md`, `test-plan.md`, `test-execution-report.md`, `coverage-report.md`, `deployability-check.md`, `code-review-report.md`, `signoff-ledger.md`, `feature-action-execution.md`, `kg-reconciliation.md`, `pm-closeout.md`

## Validation Summary

- **G0** `validate-feature-evidence.py --stage G0` → exit 0 (PASS). Assembly plan authored + validated.
- **G1** `validate-feature-evidence.py --stage G1` → exit 0 (PASS). Runtime preflight green.
- **Step 1 (in progress — hierarchy vertical):**
  - `dotnet build Nebula.Infrastructure` → 0 errors (entities + EF configs + DbSets).
  - `dotnet test Nebula.slnx --filter DistributionEndpointTests|BrokerEndpointTests` (sdk 10.0 + Testcontainers postgres:16) → **20/20 passed** (8 F0017 hierarchy integration tests + 12 baseline broker tests). Evidence: `artifacts/test-results/step1-hierarchy-integration-tests.txt`.
- G2–G8 stage validations: pending (next session).

## Open Follow-ups

- **Multi-session build.** This run completed the DistributionNode (hierarchy) vertical end-to-end, tested. Remaining for continuation: producer-ownership + territory services/endpoints/tests, the frontend `distribution` slice, then gates G2 (self-review/QE/coverage/deployability), G3 (code review), G4/G5 (approval/signoff), G6 (candidate), G7 (architect KG reconciliation), G8 (PM closeout). Resume from task list #4–#10.
- **Branch migration-snapshot drift (pre-existing).** `dotnet ef migrations add` produced a polluted diff because this branch's `AppDbContextModelSnapshot.cs` is stale vs uncommitted in-progress work (F0019 etc.). The F0017 migration was hand-scoped to its 4 tables; the snapshot was restored to baseline. The broader snapshot drift is a pre-existing branch condition to reconcile separately (not F0017-caused).
- **Frontend toolchain** (vitest/lint/build) cannot run on the local `/mnt/c` WSL mount; frontend validation deferred to CI. To be recorded as a coverage waiver in `coverage-report.md` at G2.
