# Artifact Trace — F0022-work-queues-assignment-rules-and-coverage-management run 2026-07-03-8aa72827

## Artifacts Read

- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/PRD.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/STATUS.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/README.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/GETTING-STARTED.md`
- `planning-mds/features/ROADMAP.md`
- `planning-mds/features/REGISTRY.md`
- `planning-mds/features/TRACKER-GOVERNANCE.md`
- `planning-mds/BLUEPRINT.md`
- `planning-mds/knowledge-graph/feature-mappings.yaml`
- `agents/templates/prompts/evidence-contract/plan-operator-friendly.md`
- `agents/actions/plan.md`

## Artifacts Created Or Updated

- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/PRD.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/README.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/STATUS.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/GETTING-STARTED.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/F0022-S0001-manage-work-queues-and-memberships.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/F0022-S0002-define-assignment-rules-and-precedence.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/F0022-S0003-route-work-from-tasks-submissions-and-renewals.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/F0022-S0004-manage-backup-coverage-windows.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/F0022-S0005-queue-worklists-and-aging-visibility.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/F0022-S0006-reassign-and-rebalance-queued-work.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/F0022-S0007-routing-audit-permissions-and-exceptions.md`
- `planning-mds/features/STORY-INDEX.md`
- `planning-mds/knowledge-graph/feature-mappings.yaml`
- `planning-mds/knowledge-graph/canonical-nodes.yaml`
- `planning-mds/knowledge-graph/code-index.yaml`
- `planning-mds/knowledge-graph/coverage-report.yaml`
- `planning-mds/architecture/decisions/ADR-013-operational-routing-and-queue-engine.md`
- `planning-mds/architecture/data-model.md`
- `planning-mds/architecture/error-codes.md`
- `planning-mds/architecture/application-assembly-plan.md`
- `planning-mds/architecture/feature-architecture-inventory-f0006-f0032.md`
- `planning-mds/BLUEPRINT.md`
- `planning-mds/api/nebula-api.yaml`
- `planning-mds/security/authorization-matrix.md`
- `planning-mds/security/policies/policy.csv`
- `planning-mds/schemas/work-queue.schema.json`
- `planning-mds/schemas/work-queue-upsert-request.schema.json`
- `planning-mds/schemas/queue-member-upsert-request.schema.json`
- `planning-mds/schemas/assignment-rule.schema.json`
- `planning-mds/schemas/assignment-rule-upsert-request.schema.json`
- `planning-mds/schemas/coverage-window.schema.json`
- `planning-mds/schemas/coverage-window-upsert-request.schema.json`
- `planning-mds/schemas/queue-work-item.schema.json`
- `planning-mds/schemas/queue-reassignment-request.schema.json`
- `planning-mds/schemas/queue-rebalance-request.schema.json`
- `planning-mds/schemas/routing-event.schema.json`
- `planning-mds/operations/evidence/runs/2026-07-03-8aa72827/README.md`
- `planning-mds/operations/evidence/runs/2026-07-03-8aa72827/action-context.md`
- `planning-mds/operations/evidence/runs/2026-07-03-8aa72827/artifact-trace.md`
- `planning-mds/operations/evidence/runs/2026-07-03-8aa72827/gate-decisions.md`
- `planning-mds/operations/evidence/runs/2026-07-03-8aa72827/commands.log`
- `planning-mds/operations/evidence/runs/2026-07-03-8aa72827/lifecycle-gates.log`

## Generated Evidence

- G2 story validation passed for seven F0022 story files.
- G2 tracker validation passed with plan-compatible feature-evidence skip.
- G2 KG validation and drift check passed after coverage regeneration.
- G2 template validation passed.
- G4 API contract validation passed with pre-existing non-F0022 warnings.
- G4 JSON schema syntax validation passed for 11 F0022 schema files.
- G4 KG coverage regeneration, validation, and drift checks passed after code-index bindings were corrected to existing Phase B artifacts.
- G4 tracker and template validations passed.

## External Or Global Evidence References

- None yet.

## Omissions And Waivers

- Feature evidence package artifacts are intentionally omitted during plan; they belong to `agents/actions/feature.md`.

## Run Environment

- Absolute product root: `/Users/wallstreet/Desktop/nebula_workspace/nebula-insurance-crm` — local workspace path used for operator-run planning.
- Absolute framework root: `/Users/wallstreet/Desktop/nebula_workspace/nebula-agents` — sibling Nebula Agents harness path.
