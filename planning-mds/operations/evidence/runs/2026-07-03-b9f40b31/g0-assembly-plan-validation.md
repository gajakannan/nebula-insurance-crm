# G0 Assembly Plan Validation — F0022 run 2026-07-03-b9f40b31

## Verdict

PASS

## Inputs Reviewed

- `agents/actions/feature.md`
- `agents/templates/prompts/evidence-contract/feature-operator-friendly.md`
- `agents/templates/feature-assembly-plan-template.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/PRD.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/STATUS.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/F0022-S0001-manage-work-queues-and-memberships.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/F0022-S0002-define-assignment-rules-and-precedence.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/F0022-S0003-route-work-from-tasks-submissions-and-renewals.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/F0022-S0004-manage-backup-coverage-windows.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/F0022-S0005-queue-worklists-and-aging-visibility.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/F0022-S0006-reassign-and-rebalance-queued-work.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/F0022-S0007-routing-audit-permissions-and-exceptions.md`
- `planning-mds/architecture/decisions/ADR-013-operational-routing-and-queue-engine.md`
- `planning-mds/architecture/data-model.md`
- `planning-mds/api/nebula-api.yaml`
- `planning-mds/security/authorization-matrix.md`
- `planning-mds/security/policies/policy.csv`
- `planning-mds/knowledge-graph` lookup output for F0022

## Assembly Plan Checks

| Check | Result | Notes |
|-------|--------|-------|
| Primary spec exists at feature path | PASS | `feature-assembly-plan.md` was authored in G0. |
| Scope split is explicit | PASS | Backend, frontend, QE, security, deployability, G7, and PM closeout responsibilities are separated. |
| Story coverage | PASS | All seven F0022 stories map to build steps and cross-story verification. |
| Mutation traceability | PASS | Queue, rule, coverage, routing, reassignment, and rebalance mutations map to endpoint, service, entity, auth, concurrency, validation, audit, and tests. |
| API contract alignment | PASS | Endpoint list and response tables follow the WorkQueues OpenAPI contract created in Phase B. |
| Security and ABAC | PASS | Queue read/manage/assign Casbin actions and source-record ABAC intersection are required. |
| F0032 boundary | PASS | Local controls only; publish/rollback governance remains downstream. |
| AI scope | PASS | AI routing remains out of scope; AI Engineer not required. |

## Required Signoff Roles

| Role | Required | Reason |
|------|----------|--------|
| Architect | Yes | Owns G0 primary spec and G7 KG reconciliation. |
| Quality Engineer | Yes | Routing, idempotency, coverage, fallback, and UI workflows require structured validation. |
| Code Reviewer | Yes | Shared routing service, source adapters, and API/UI mutations require independent review. |
| Security Reviewer | Yes | Queue visibility and reassignment affect access and work ownership boundaries. |
| DevOps | Yes | Runtime preflight and deployability evidence are forced because backend runtime and migration surfaces are expected to change. |

## Gate Outcome

G0 can proceed to runtime preflight after feature evidence validation passes.
