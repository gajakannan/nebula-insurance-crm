# Artifact Trace

## Artifacts Read

- `agents/templates/prompts/evidence-contract/defect-bugfix-operator-friendly.md`
- `agents/templates/gate-decisions-template.md`
- `agents/templates/artifact-trace-template.md`
- `agents/templates/commands-log-template.md`
- `agents/templates/lifecycle-gates-log-template.md`
- `planning-mds/features/archive/F0021-communication-hub-and-activity-capture/README.md`
- `planning-mds/features/archive/F0021-communication-hub-and-activity-capture/PRD.md`
- `planning-mds/features/archive/F0021-communication-hub-and-activity-capture/feature-assembly-plan.md`
- `planning-mds/features/archive/F0021-communication-hub-and-activity-capture/STATUS.md`
- `planning-mds/features/F0038-neuron-day-at-a-glance-shell/README.md`
- `planning-mds/features/F0038-neuron-day-at-a-glance-shell/PRD.md`
- `planning-mds/features/F0038-neuron-day-at-a-glance-shell/STATUS.md`
- `planning-mds/operations/evidence/runs/2026-07-03-ef42a9b4/artifacts/merge/merge-tree-pr47-vs-main.txt`

## Artifacts Created Or Updated

- `planning-mds/operations/evidence/runs/2026-07-03-ef42a9b4/README.md`
- `planning-mds/operations/evidence/runs/2026-07-03-ef42a9b4/action-context.md`
- `planning-mds/operations/evidence/runs/2026-07-03-ef42a9b4/artifact-trace.md`
- `planning-mds/operations/evidence/runs/2026-07-03-ef42a9b4/gate-decisions.md`
- `planning-mds/operations/evidence/runs/2026-07-03-ef42a9b4/commands.log`
- `planning-mds/operations/evidence/runs/2026-07-03-ef42a9b4/lifecycle-gates.log`
- `planning-mds/architecture/decisions/ADR-029-communication-activity-capture-and-redaction.md`
- `planning-mds/features/STORY-INDEX.md`
- `planning-mds/knowledge-graph/canonical-nodes.yaml`
- `planning-mds/knowledge-graph/feature-mappings.yaml`
- `planning-mds/knowledge-graph/coverage-report.yaml`
- `planning-mds/knowledge-graph/code-index.yaml`
- `planning-mds/security/authorization-matrix.md`
- `planning-mds/architecture/feature-assembly-plan.md`
- `planning-mds/features/archive/F0021-communication-hub-and-activity-capture/README.md`
- `planning-mds/features/archive/F0021-communication-hub-and-activity-capture/feature-assembly-plan.md`
- `planning-mds/operations/evidence/F0021-END-TO-END-REFERENCE.md`
- `planning-mds/operations/evidence/runs/2026-07-03-ef42a9b4/architect-analysis.md`
- `planning-mds/operations/evidence/runs/2026-07-03-ef42a9b4/bugfix-brief.md`
- `planning-mds/operations/evidence/runs/2026-07-03-ef42a9b4/quality-report.md`

## Generated Evidence

- `artifacts/merge/merge-base.txt`
- `artifacts/merge/merge-tree-pr47-vs-main.txt`
- `artifacts/validation/generate-story-index.log`
- `artifacts/validation/kg-write-coverage-report.log`
- `artifacts/validation/kg-write-coverage-report-rerun.log`
- `artifacts/validation/kg-validate.log`
- `artifacts/validation/kg-check-drift.log`
- `artifacts/validation/validate-stories-f0021.log`
- `artifacts/validation/generate-story-index-rerun.log`
- `artifacts/validation/validate-trackers.log`
- `artifacts/validation/validate-templates.log`
- `artifacts/test-results/communication-panel-vitest.log`
- `artifacts/validation/frontend-lint.log`
- `artifacts/validation/frontend-lint-theme.log`
- `artifacts/validation/frontend-build.log`
- `artifacts/validation/api-healthz.log`
- `artifacts/validation/frontend-healthz.log`
- `artifacts/validation/docker-compose-ps.log`

## External Or Global Evidence References

- PR #47 conflict URL supplied by operator.
- Upstream `main` fetched from `https://github.com/gajakannan/nebula-insurance-crm.git`.
- PR #47 metadata from GitHub API confirmed head repo `karthik-reddiee/nebula-insurance-crm` and head ref `feature-karthik/f0021-communication-hub`.

## Omissions And Waivers

- Browser E2E omitted because local API/frontend health checks failed (`curl` exit 7) and `docker compose ps` exited 1. Re-run E2E after stack startup.

## Run Environment

- Absolute cwd: `/Users/wallstreet62/Desktop/nebula3/nebula-insurance-crm` — local product root for PR conflict resolution.
- Absolute cwd: `/Users/wallstreet62/Desktop/nebula3/nebula-agents` — local harness root for contract/template validation.
