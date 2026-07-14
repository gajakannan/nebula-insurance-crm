# Artifact Trace - F0025-commission-producer-splits-and-revenue-tracking run 2026-07-07-9859bad4

## Artifacts Read

- `planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/PRD.md`
- `planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/README.md`
- `planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/STATUS.md`
- `planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/GETTING-STARTED.md`
- `planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/F0025-S0001-commission-workspace-search-and-policy-context.md`
- `planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/F0025-S0002-commission-schedule-maintenance.md`
- `planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/F0025-S0003-producer-split-assignment.md`
- `planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/F0025-S0004-expected-commission-calculation-review.md`
- `planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/F0025-S0005-commission-adjustment-and-approval.md`
- `planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/F0025-S0006-revenue-attribution-rollups.md`
- `planning-mds/architecture/decisions/ADR-032-commission-producer-splits-and-revenue-tracking.md`
- `planning-mds/architecture/feature-assembly-plan.md`
- `planning-mds/api/nebula-api.yaml`
- `planning-mds/schemas/*.schema.json` for F0025 commission schemas
- `planning-mds/security/authorization-matrix.md`
- `planning-mds/security/policies/policy.csv`
- `agents/actions/feature.md`
- `agents/ROUTER.md`
- `agents/agent-map.yaml`
- `agents/docs/AGENT-USE.md`
- `agents/docs/AGENT-OPS.md`
- `agents/architect/SKILL.md`
- `agents/templates/feature-assembly-plan-template.md`
- `agents/templates/evidence-manifest-template.json`

## Artifacts Created Or Updated

- `planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/feature-assembly-plan.md` - created.
- `planning-mds/architecture/feature-assembly-plan.md` - updated to reference F0025 feature-local assembly plan.
- `planning-mds/operations/evidence/runs/2026-07-07-9859bad4/README.md` - created.
- `planning-mds/operations/evidence/runs/2026-07-07-9859bad4/action-context.md` - created.
- `planning-mds/operations/evidence/runs/2026-07-07-9859bad4/artifact-trace.md` - created.
- `planning-mds/operations/evidence/runs/2026-07-07-9859bad4/gate-decisions.md` - created.
- `planning-mds/operations/evidence/runs/2026-07-07-9859bad4/lifecycle-gates.log` - created.
- `planning-mds/operations/evidence/runs/2026-07-07-9859bad4/commands.log` - created.
- `planning-mds/operations/evidence/runs/2026-07-07-9859bad4/evidence-manifest.json` - created.
- `planning-mds/operations/evidence/runs/2026-07-07-9859bad4/g0-assembly-plan-validation.md` - created.
- `planning-mds/operations/evidence/runs/2026-07-07-9859bad4/artifacts/diffs/changed-files.txt` - created.
- `engine/src/Nebula.*` commission revenue source files - created/updated for F0025 backend implementation.
- `experience/src/features/commissions/**` and commission route pages - created for F0025 frontend implementation.
- `planning-mds/knowledge-graph/code-index.yaml` and `planning-mds/knowledge-graph/coverage-report.yaml` - updated for F0025 implementation bindings.
- `planning-mds/operations/evidence/runs/2026-07-07-9859bad4/g2-self-review.md` - created.
- `planning-mds/operations/evidence/runs/2026-07-07-9859bad4/test-plan.md` - created.
- `planning-mds/operations/evidence/runs/2026-07-07-9859bad4/test-execution-report.md` - created.
- `planning-mds/operations/evidence/runs/2026-07-07-9859bad4/coverage-report.md` - created.
- `planning-mds/operations/evidence/runs/2026-07-07-9859bad4/deployability-check.md` - created.
- `planning-mds/operations/evidence/runs/2026-07-07-9859bad4/code-review-report.md` - created.
- `planning-mds/operations/evidence/runs/2026-07-07-9859bad4/security-review-report.md` - created.
- `planning-mds/operations/evidence/runs/2026-07-07-9859bad4/artifacts/security/*.txt` - created from G3 security scan attempts.

## Generated Evidence

- `artifacts/diffs/changed-files.txt` - initial G0 changed-path snapshot.
- `g2-self-review.md`, `test-plan.md`, `test-execution-report.md`, `coverage-report.md`, and `deployability-check.md` - G2 implementation validation evidence.
- `code-review-report.md` and `security-review-report.md` - G3 review evidence.
- `artifacts/security/dependency-scan.txt`, `secrets-scan.txt`, `sast-scan.txt`, and `dast-scan.txt` - raw scan attempt evidence and waiver support.

## External Or Global Evidence References

- None.

## Omissions And Waivers

- G3 security scan waivers are recorded inline in `evidence-manifest.json` because local dependency, secrets, SAST, and DAST scanner execution was blocked by network/tool availability.
