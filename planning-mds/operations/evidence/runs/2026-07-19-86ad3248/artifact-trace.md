# Artifact Trace — F0026-billing-invoicing-and-reconciliation run 2026-07-19-86ad3248

## Artifacts Read

- `planning-mds/features/F0026-billing-invoicing-and-reconciliation/` — PRD, six stories, acceptance checklist, STATUS, README, getting-started guide, and primary assembly plan.
- `planning-mds/BLUEPRINT.md` — F0026 feature, screen, service-boundary, and data-model references.
- `planning-mds/architecture/SOLUTION-PATTERNS.md` — authorization, audit, API, frontend, concurrency, testing, caching, and container patterns.
- `planning-mds/architecture/decisions/ADR-034-agency-bill-invoicing-and-exact-reconciliation.md` — governing decision.
- `planning-mds/api/nebula-api.yaml` and F0026 JSON schemas under `planning-mds/schemas/` — interface contracts.
- `planning-mds/operations/evidence/runs/2026-07-19-79477865/{evidence-manifest.json,action-context.md,README.md,commands.log}` — exact prior-plan reconciliation inputs.

## Artifacts Created Or Updated

- `evidence-manifest.json` — initialized and reconciled with planned conditional scope and required roles.
- `README.md`, `action-context.md`, `artifact-trace.md`, `gate-decisions.md` — initialized G0 evidence context.
- `g0-assembly-plan-validation.md` — Architect-owned G0 verdict.
- `g1-runtime-preflight.md` — DevOps-owned runtime readiness verdict.
- `artifacts/diffs/changed-files.txt` — initial durable change-set seed; refreshed as implementation lands.
- `g2-self-review.md`, `test-plan.md`, `test-execution-report.md`, `coverage-report.md`, `deployability-check.md` — G2 role and gate reports.
- `artifacts/test-results/` — focused/regression TRX and JUnit results plus runtime persistence/smoke evidence.
- `artifacts/coverage/` — backend and frontend feature-scoped coverage evidence.
- `artifacts/screenshots/` — six light/dark and desktop/mobile feature-level visual artifacts.
- `artifacts/security/` — dependency, redacted Gitleaks, Semgrep SARIF, and ZAP JSON/HTML/raw outputs.

## Generated Evidence

- Runtime status, health, migration/table checks, authorization probes, invalid-UTF-8 handling, and the persisted invoice/receipt/exact-application/reload flow are captured in `commands.log` and `artifacts/test-results/`.
- Test, coverage, accessibility, visual, and raw security scan outputs are present at G2. Security owns final scanner interpretation at G3.

## External Or Global Evidence References

- F0025 approved dependency evidence: `planning-mds/operations/evidence/runs/2026-07-07-9859bad4/evidence-manifest.json` (consult only if dependency verification requires it).
- No global frontend lane substitutes for feature-level F0026 role evidence.

## Omissions And Waivers

- No waiver is requested. Three non-blocking repository test-harness follow-ups are listed in `README.md` with owners; F0026-focused lanes and real runtime persistence pass.

## Run Environment (conditional)

- Absolute cwd: `/home/gajap/uSandbox/repos/nebula/nebula-agents` — framework command root resolved by the managed workspace.
- Absolute cwd: `/home/gajap/uSandbox/repos/nebula/nebula-insurance-crm` — sister product repository resolved as PRODUCT_ROOT for this run.

## Candidate Validation

- `feature-action-execution.md` — G6 candidate summary, slice results, scope-boolean reconciliation, and early-closeout exclusions.
- `artifacts/test-results/g6-validator.txt` — explicit G6 evidence validator output.
- `artifacts/test-results/g6-trackers.txt` — pre-closeout tracker validator output.
- `artifacts/test-results/g6-run-gate.txt` — G6 lifecycle-gate output.

## Knowledge-graph Reconciliation

- `kg-reconciliation.md` — Architect-owned as-built semantic reconciliation and G8 handoff.
- `artifacts/test-results/g7-kg-compile.txt` — canonical-source compilation.
- `artifacts/test-results/g7-kg-symbols-decisions.txt` — symbol and decision regeneration/checks.
- `artifacts/test-results/g7-kg-drift.txt` — pre-archive drift validation.
- `artifacts/test-results/g7-kg-lookup.txt` — compiled F0026 semantic lookup.

## PM Closeout

- `pm-closeout.md` — final story status, archive decision, deferrals, tracker updates, and validator summary.
- `artifacts/test-results/g8-story-index.txt` — post-archive story-index regeneration.
- `artifacts/test-results/g8-kg-compile.txt` — post-archive KG/tracker compilation.
- `artifacts/test-results/g8-patch-prior-manifest.txt` — successful prior-manifest check before publication.
- `planning-mds/operations/evidence/features/F0026-billing-invoicing-and-reconciliation/latest-run.json` — approved-run pointer published only after the prior-manifest step exited 0.
