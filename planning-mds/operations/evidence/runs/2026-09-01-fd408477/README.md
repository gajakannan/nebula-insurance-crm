# Feature Evidence README — F####-{slug} run {run-id}

> Template for `{PRODUCT_ROOT}/planning-mds/operations/evidence/runs/{run-id}/README.md`
> per §8 of the Feature Evidence Package Standardization contract.

## Run Summary

Feature action for F0040 run `2026-09-01-fd408477` continued after the required G1 retry. The implementation adds the permission-scoped Engine Broker projection, activates the Neuron Broker specialist head and direct read-only route, extracts shared head execution/component validation/telemetry, and adds the registered frontend Broker activity list with retry behavior. This is the existing `feature` lifecycle run; no new RUN_ID was created.

## Status

Final state for this run: `in-progress` (G0/G1 passed; G2 evidence is being recorded).

## Evidence Index

- `evidence-manifest.json` — schema v1 (§11)
- `action-context.md` — Run Identity, Inputs, Assumptions, Scope Boundaries, Lifecycle Stage
- `artifact-trace.md` — read/written artifacts + Run Environment when needed
- `gate-decisions.md` — pass/fail/skip per gate row (§17 stage matrix)
- `commands.log` — JSON Lines per §13
- `lifecycle-gates.log` — lifecycle gate run summary
- Role and gate reports — list `g0-…`, `g1-…`, `g2-…`, `test-plan.md`, etc.

## Validation Summary

G0 and the recovered G1 feature-evidence validators passed. Neuron tests, frontend tests/build/lint checks, changed Engine tests, and Engine unit tests passed. The repository-wide Engine integration run was not accepted as a feature pass because 21 tests failed on unrelated Postgres test-port/fixture availability; the changed timeline/telemetry slice remained green.

## Open Follow-ups

- Re-run the full Engine integration suite when the shared Postgres test fixture/port is stable; the changed F0040 tests already pass.
- Repository-wide frontend lint retains one pre-existing unrelated error in `tests/e2e/f0037-distribution-rollups.spec.ts`; changed frontend files have no errors.
