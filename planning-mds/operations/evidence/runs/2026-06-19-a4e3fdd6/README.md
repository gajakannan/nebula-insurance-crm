# Feature Evidence Run — F0023 2026-06-19-a4e3fdd6

## Run Summary

`feature` action for F0023 (Global Search, Saved Views & Operational Reporting). Builds a read-side SearchReporting module (permission-filtered global search, personal/team saved views, operational workload/aging reports) as a vertical slice across `engine/` and `experience/`. Run executed by the feature orchestrator with role switches per gate.

## Status

`approved` — closeout sealed at G8 (2026-06-20). Feature archived; latest-run.json written.

## Evidence Index

- `evidence-manifest.json` — schema v1
- `action-context.md` — Run Identity, Inputs, Assumptions, Scope Boundaries, Lifecycle Stage
- `artifact-trace.md` — read/written/generated/referenced artifacts
- `gate-decisions.md` — pass/fail/skip per gate
- `commands.log` — JSON Lines per §13
- `lifecycle-gates.log` — lifecycle/validator invocation summary
- Gate/role reports — populated as gates complete (`g0-…`, `g1-…`, `g2-…`, `test-plan.md`, `coverage-report.md`, reviews, `signoff-ledger.md`, `kg-reconciliation.md`, `pm-closeout.md`)

## Validation Summary

All gates G0–G8 PASS. Validator results (exit 0 unless noted):
- feature-evidence `--stage` G0,G1,G2,G3,G5,G6,closeout → exit 0
- validate-trackers (G6 + closeout) → PASS (0 errors/0 warnings)
- KG: `--check-symbols`, `--check-drift` → exit 0; `--write-coverage-report` (post-archive-move) → exit 0
- `--regenerate-symbols` → env-blocked (extractors unavailable); committed symbol-index.yaml preserved (follow-up)
- generate-story-index, validate_templates → PASS
- Backend 17 unit + Frontend 5 component tests green; container build+migrate+route-smoke green; deps scanned (0 backend; pre-existing frontend); secrets/sast/dast waived (tooling unavailable)
- Manifest status `approved`; feature_state `Archived`; latest-run.json written; no prior approved manifests to supersede.

## Open Follow-ups

- **[pre-existing, non-F0023]** `experience/src/features/session-continuity/tests/sessionTelemetry.test.ts` — 1 test fails in isolation (deferred-telemetry drain `fetchMock` not called). Untouched by F0023; flagged for the session-continuity owner. Does not block F0023 (frontend F0023 suite is green).
- **[deferred]** Search uses PostgreSQL trigram ILIKE (codebase convention) rather than full tsvector ranking; `Score` is a placeholder. Full-text ranking is a follow-up optimization.
- **[deferred]** Projection `Region` is denormalized only where directly available on the source; deeper per-module region/territory denormalization for visibility parity is a follow-up (visibility falls back to owner-match for null-region rows).
- **[env/follow-up]** `validate.py --regenerate-symbols` unavailable in this environment (csharp/ts extractors broken); committed `symbol-index.yaml` preserved; F0023 symbol entries not yet in the symbol layer — regenerate where extractors work. `--check-symbols`/`--check-drift` are green.
