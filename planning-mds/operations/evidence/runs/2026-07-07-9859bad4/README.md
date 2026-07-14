# Feature Evidence README - F0025-commission-producer-splits-and-revenue-tracking run 2026-07-07-9859bad4

## Run Summary

This feature action run completed F0025 under the `nebula-agents` harness after plan run `2026-07-07-8a9b2629` received Phase B approval. The run delivered the commission/revenue vertical slice, completed G0-G8, archived the feature folder, and published `latest-run.json`.

## Status

`approved`

## Evidence Index

- `evidence-manifest.json` - schema v1 feature-run manifest.
- `action-context.md` - run identity, inputs, assumptions, scope boundaries, and lifecycle stage.
- `artifact-trace.md` - read and created/updated artifacts.
- `gate-decisions.md` - gate decisions.
- `commands.log` - append-only command record.
- `lifecycle-gates.log` - lifecycle validation summary.
- `g0-assembly-plan-validation.md` - Architect validation of the feature assembly plan.
- `g1-runtime-preflight.md` - runtime preflight evidence.
- `g2-self-review.md`, `test-plan.md`, `test-execution-report.md`, `coverage-report.md`, `deployability-check.md` - implementation validation evidence.
- `code-review-report.md`, `security-review-report.md` - G3 review evidence.
- `signoff-ledger.md` - G5 signoff ledger.
- `feature-action-execution.md` - G6 gate execution timeline.
- `kg-reconciliation.md` - G7 Architect KG reconciliation.
- `pm-closeout.md` - G8 PM closeout.
- Feature assembly plan: `planning-mds/features/archive/F0025-commission-producer-splits-and-revenue-tracking/feature-assembly-plan.md`.

## Validation Summary

- G0-G8 feature evidence validations passed.
- Scoped tracker validation passed.
- KG symbol/decision regeneration and drift checks passed.
- Closeout publication wrote `planning-mds/operations/evidence/features/F0025-commission-producer-splits-and-revenue-tracking/latest-run.json`.

## Open Follow-ups

- Deferred hardening follow-ups are recorded in `pm-closeout.md`: source-scope regression tests, CI/staging security scanner reruns, staging DAST, projection-granularity confirmation, schedule-list scoping confirmation, and unrelated frontend localStorage suite cleanup.
