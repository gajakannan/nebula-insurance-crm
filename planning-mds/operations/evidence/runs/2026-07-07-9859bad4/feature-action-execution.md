# Feature Action Execution - F0025-commission-producer-splits-and-revenue-tracking run 2026-07-07-9859bad4

> Required at G6 per the feature evidence contract. Captures the orchestrator's per-gate execution log.

## Gate

Current/final gate reached: `G6`.

## Execution Timeline

- 2026-07-07T14:38:13+05:30 - G0 entered
  - Inputs: feature stories, ADR-032, API/schema/security artifacts, and `agents/actions/feature.md`.
  - Validators: `validate-feature-evidence.py --stage G0` exited 0.
  - Outputs: `feature-assembly-plan.md`, `g0-assembly-plan-validation.md`, initialized evidence package.
  - Outcome: proceed to G1.

- 2026-07-07T14:43:37+05:30 - G1 entered
  - Inputs: runtime preflight requirements and local application runtime.
  - Validators: `validate-feature-evidence.py --stage G1` exited 0.
  - Outputs: `g1-runtime-preflight.md`.
  - Outcome: proceed to implementation validation.

- 2026-07-07T15:12:00+05:30 - G2 entered
  - Inputs: implemented backend/frontend slice, test evidence, coverage evidence, and deployability evidence.
  - Validators: `validate-feature-evidence.py --stage G2` exited 0.
  - Outputs: `g2-self-review.md`, `test-plan.md`, `test-execution-report.md`, `coverage-report.md`, `deployability-check.md`.
  - Outcome: proceed to code and security review.

- 2026-07-07T15:24:25+05:30 - G3 entered
  - Inputs: Code Reviewer and Security Reviewer reports, security scan attempts, source-record visibility patch, and focused backend validation.
  - Validators: `validate-feature-evidence.py --stage G3` exited 0 after security artifact path formatting was repaired.
  - Outputs: `code-review-report.md`, `security-review-report.md`, `artifacts/security/*.txt`.
  - Outcome: PASS WITH RECOMMENDATIONS; proceed to approval gate.

- 2026-07-07T15:25:00+05:30 - G4 entered
  - Inputs: user approval following G3 PASS WITH RECOMMENDATIONS.
  - Validators: `validate-feature-evidence.py --stage G4` exited 0.
  - Outputs: G4 approval row in `gate-decisions.md`.
  - Outcome: proceed to G5 signoff.

- 2026-07-07T15:26:00+05:30 - G5 entered
  - Inputs: current `STATUS.md` signoff rows and required role evidence.
  - Validators: `validate-feature-evidence.py --stage G5` first found a missing coverage artifact reference in `changed_paths`; manifest was repaired and rerun exited 0.
  - Outputs: `signoff-ledger.md`, STATUS signoff rows, manifest signoff result.
  - Outcome: proceed to G6 candidate evidence validation.

- 2026-07-07T15:27:00+05:30 - G6 entered
  - Inputs: complete G0-G5 evidence package.
  - Validators: `validate-feature-evidence.py --stage G6` exited 0; `validate-trackers.py --feature F0025 --run-id 2026-07-07-9859bad4` exited 0.
  - Outputs: `feature-action-execution.md`.
  - Outcome: proceed to G7 Architect KG reconciliation.
