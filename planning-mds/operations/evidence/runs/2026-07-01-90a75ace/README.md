# Feature Evidence README — F0038-neuron-day-at-a-glance-shell run 2026-07-01-90a75ace

## Run Summary

`feature` action for **F0038 — Neuron Day-at-a-Glance Shell**: the first runnable Neuron
companion vertical slice (Day-at-a-Glance shell + Renewals zone live + assisted renewal
outreach draft + mock-send + CRM-scope guard + companion telemetry). Stands up a new
`neuron/` Python runtime, a `experience/` Day-at-a-Glance shell with zone dispatch, and
`engine/` scope-guard / read endpoints. LLM is **mocked** for this run (deterministic
evidence, no live provider). Run driven by the multi-role feature orchestrator.

## Status

`in-progress` — must agree with `evidence-manifest.json` `status`. Transitions: draft (G0) →
in-progress (G1–G7) → approved (G8). Currently past G0–G2.

## Evidence Index

- `evidence-manifest.json` — schema v1 (§11)
- `action-context.md` — Run Identity, Inputs, Assumptions, Scope Boundaries, Lifecycle Stage
- `artifact-trace.md` — read/written artifacts + Run Environment
- `gate-decisions.md` — pass/fail/skip per gate row (§17 stage matrix)
- `commands.log` — JSON Lines per §13
- `lifecycle-gates.log` — lifecycle gate run summary
- Role and gate reports — `g0-assembly-plan-validation.md`, `g1-runtime-preflight.md`,
  `g2-self-review.md`, `test-plan.md`, `test-execution-report.md`, `coverage-report.md`,
  `deployability-check.md`, `code-review-report.md`, `security-review-report.md`,
  `signoff-ledger.md`, `feature-action-execution.md`, `kg-reconciliation.md`, `pm-closeout.md`

## Validation Summary

Populated as gates pass. Mirrors `lifecycle-gates.log` and manifest `gate_results`.

- Session-start prep: `scripts/kg/validate.py` exits 0 after `--write-coverage-report`
  (pre-existing symbol-drift warnings noted, out of F0038 scope).
- **G0 PASS** (assembly-plan), **G1 PASS** (runtime preflight), **G2 PASS** — self-review +
  QE (engine 491 / neuron 116 / FE 17 green; neuron 88% / FE 86.2% coverage) + deployability
  (Neuron container build + health/readiness smoke). Security scans: SAST 0 findings, secrets
  0 leaks, engine + neuron dependency SCA clean; DAST waived (internal auth-gated service).

## Open Follow-ups

- **Candidate dead code (FE):** `NeuronPanel.tsx` + `useNeuronChat.ts` at 0% coverage, likely
  superseded by the S0007 composer flow — confirm/remove at G3 code review.
- **Frontend dependency advisories (9):** pre-existing, not F0038-introduced — recommend a
  repo-level SCA waiver (owner: PM/Security at closeout).
- **DAST deferred:** waived at G2 (internal auth-gated service, no new unauthenticated dynamic
  surface); Security Reviewer to ratify at G3, DAST run at platform/CI against a deployed env.
