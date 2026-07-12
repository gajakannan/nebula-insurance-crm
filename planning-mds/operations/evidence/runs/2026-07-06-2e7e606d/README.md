# F0037 PRD Alignment Rerun

## Run Summary

Run ID: `2026-07-06-2e7e606d`  
Feature: F0037 - Hierarchy-Aware Access Scoping & Distribution Rollups  
Mode: `drift-reconcile`  
Rerun of: `2026-07-06-74a4efd7`

Operator supplied a screenshot of the Operational Reports -> Distribution rollups page and asked whether it exactly matched the F0037 PRD. Review found one UI alignment gap: the Distribution rollups tab displayed generic operational filters (`Region`, `Line of business`, `Workflow type`) that are not part of the PRD's Distribution Rollups screen expectation.

## Status

Approved. The rerun updates the rollups tab to show only the PRD-required rollup filters: `As of`, `Root node`, `Territory`, `Producer`, `Group by`, and `Metric family`.

## Evidence Index

- `test-execution-report.md`
- `code-review-report.md`
- `security-review-report.md`
- `kg-reconciliation.md`
- `pm-closeout.md`
- `artifacts/test-results/f0037-playwright-results.json`
- `artifacts/screenshots/f0037-sidebar-rollups.png`

## Validation Summary

- Frontend build: PASS.
- Focused Vitest: PASS, 2 tests.
- F0037 Playwright E2E: PASS, 4 tests.

## Open Follow-ups

- Seed visible rollup rows in a deterministic QA fixture if every local run must show row/drilldown examples.
