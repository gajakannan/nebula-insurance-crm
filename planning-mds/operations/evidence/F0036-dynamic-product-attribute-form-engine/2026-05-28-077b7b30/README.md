# Feature Evidence README — F0036-dynamic-product-attribute-form-engine run 2026-05-28-077b7b30

## Run Summary

`feature` action for **F0036 — Form Engine and Form-State Preservation (RHF + AJV + Widget Registry)**, MODE `clean`, run by the orchestrator on 2026-05-28. This run realizes the ADR-021 dynamic form engine for LOB product attributes (Cyber pilot) and registers every in-scope mutation form (the new RHF product-attribute form plus the existing controlled CRUD forms via a controlled-form dirty-tracker adapter) with the F0035 form-state-preservation registry. Frontend-only (`experience/**`); no backend/schema/bundle/deploy change. G0 (Architect assembly-plan authoring + validation) is complete; implementation gates (Step 1 / G1–G4.7) follow.

## Status

Final state for this run: `draft` (G0 complete; manifest flips to `in-progress` once G0 passes stage validation). Must agree with `evidence-manifest.json` `status`.

## Evidence Index

- `evidence-manifest.json` — schema v1 (§11); `status: draft` → `in-progress` at G0 close.
- `action-context.md` — Run Identity, Inputs, Assumptions, Scope Boundaries, Lifecycle Stage.
- `artifact-trace.md` — read/written/generated artifacts + omissions.
- `gate-decisions.md` — per-gate pass/fail/skip (§17 stage matrix).
- `commands.log` — JSON Lines per §13.
- `lifecycle-gates.log` — lifecycle/validator invocation summary.
- `g0-assembly-plan-validation.md` — Architect G0 Step 0.5 validation output.
- `artifacts/diffs/changed-files.txt` — SCM change set (G0: planning docs).
- Implementation-gate reports (`g1-…`, `g2-self-review.md`, `test-plan.md`, `test-execution-report.md`, `coverage-report.md`, `deployability-check.md`, `code-review-report.md`, `security-review-report.md`, `signoff-ledger.md`, `feature-action-execution.md`, `pm-closeout.md`) — added as their gates complete.

## Validation Summary

| Validator | Stage | Exit | Notes |
|-----------|-------|------|-------|
| `validate-feature-evidence.py` | G0 | 0 | G0 artifacts present; assembly_plan_validation gate PASS. F0036 is a non-terminal Active feature, so the single-run path short-circuits to exit 0 until PM marks the feature terminal at closeout — full evidence validation binds at `--stage closeout`. |
| `kg/validate.py` | session start | 0 | KG integrity PASS (1 unrelated low-confidence-edge warning on F0028). |

## Open Follow-ups

- None at G0. (Implementation, runtime preflight, QE/coverage, code+security review, and PM closeout are pending gates, not follow-ups.)
