# Artifact Trace - F0037 feature run 2026-07-06-76799554

## Artifacts Read

- `agents/templates/prompts/evidence-contract/feature-automation-safe.md`
- `agents/templates/prompts/evidence-contract/feature-operator-friendly.md`
- `agents/actions/feature.md`
- `agents/docs/AGENT-OPS.md`
- `agents/templates/evidence-manifest-template.json`
- `agents/templates/feature-evidence-readme-template.md`
- `agents/templates/artifact-trace-template.md`
- `agents/templates/gate-decisions-template.md`
- `planning-mds/operations/evidence/runs/2026-07-06-6e3851ab/gate-decisions.md`
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/feature-assembly-plan.md`
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/STATUS.md`
- `agents/backend-developer/SKILL.md`
- `agents/frontend-developer/SKILL.md`
- Backend references: clean architecture, .NET best practices, enterprise patterns, EF Core patterns.
- Frontend references: UX audit ruleset and frontend code patterns.
- `planning-mds/security/authorization-matrix.md`
- `planning-mds/security/policies/policy.csv`
- `planning-mds/knowledge-graph/feature-mappings.yaml`

## Artifacts Created Or Updated

- `planning-mds/operations/evidence/runs/2026-07-06-76799554/evidence-manifest.json`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/README.md`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/action-context.md`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/artifact-trace.md`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/gate-decisions.md`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/g0-assembly-plan-validation.md`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/commands.log`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/lifecycle-gates.log`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/artifacts/diffs/changed-files.txt`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/g1-runtime-preflight.md`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/g2-self-review.md`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/test-plan.md`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/test-execution-report.md`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/coverage-report.md`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/deployability-check.md`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/code-review-report.md`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/security-review-report.md`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/signoff-ledger.md`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/feature-action-execution.md`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/kg-reconciliation.md`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/pm-closeout.md`
- `planning-mds/operations/evidence/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/latest-run.json`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/artifacts/security/secrets-scan.md`
- `planning-mds/operations/evidence/runs/2026-07-06-76799554/artifacts/security/sast-scan.md`
- Backend implementation: distribution scope interfaces/service/repository, direct-read no-leak endpoint checks, projection visibility predicates, operational rollup service/API, search/report/broker insight scope integration, validators, dependency injection, and tests.
- Frontend implementation: operational report hierarchy filters, distribution rollup view, search hierarchy filters, saved-view reapplication, page wiring, and component tests.

## Generated Evidence

- `g0-assembly-plan-validation.md` records the initial Architect validation verdict.
- `g1-runtime-preflight.md` records runtime/toolchain readiness.
- `g2-self-review.md`, `test-plan.md`, `test-execution-report.md`, `coverage-report.md`, and `deployability-check.md` record G2 implementation/QE validation.
- `artifacts/security/secrets-scan.md` and `artifacts/security/sast-scan.md` record local security-sensitive scan evidence for G2 handoff.
- `code-review-report.md` records G3 re-review approval after blocker remediation.
- `security-review-report.md` records required security review approval for the access-control scope.
- `signoff-ledger.md` records G5 story-level signoff across Architect, Quality Engineer, Code Reviewer, and Security Reviewer.
- `feature-action-execution.md` records the G6 candidate timeline.
- `kg-reconciliation.md` records G7 as-built KG reconciliation.
- `pm-closeout.md` records G8 closeout, archive decision, and final validation outcomes.

## External Or Global Evidence References

- Backend coverage artifact from focused G3 remediation `dotnet test` run: `engine/tests/Nebula.Tests/TestResults/f4f5596e-4666-4827-b5fc-a78c89ba68c5/coverage.cobertura.xml`.

## Omissions And Waivers

- `latest-run.json` was published at G8 after `patch-prior-manifest.py` exited 0.
- Dependency vulnerability audit and authenticated DAST are waived at G2 due network/runtime branch constraints; recorded in `evidence-manifest.json`.

## Run Environment

- Absolute cwd: `/Users/srinivasubezawada/Desktop/nebula3/nebula-agents` - sibling harness repo used for contract scripts and role prompts.
- Absolute cwd: `/Users/srinivasubezawada/Desktop/nebula3/nebula-insurance-crm` - product root for implementation, validation, and evidence.
