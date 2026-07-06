# Action Context

## Run Identity

| Field | Value |
|-------|-------|
| Feature | `F0032` |
| Feature Name | Admin Configuration & Reference Data Console |
| Action | `feature` |
| Run ID | `2026-07-06-f0ef8526` |
| Product Root | `/Users/wallstreet123/Desktop/nebula-workspace-2/nebula-insurance-crm` |
| Framework Root | `/Users/wallstreet123/Desktop/nebula-workspace-2/nebula-agents` |
| Current Gate | `G0` |

## Inputs

- `agents/actions/feature.md`
- `agents/ROUTER.md`
- `agents/agent-map.yaml`
- `agents/architect/SKILL.md`
- `agents/templates/feature-assembly-plan-template.md`
- `planning-mds/features/F0032-admin-configuration-and-reference-data-console/PRD.md`
- `planning-mds/features/F0032-admin-configuration-and-reference-data-console/ARCHITECTURE.md`
- `planning-mds/features/F0032-admin-configuration-and-reference-data-console/STATUS.md`
- `planning-mds/api/nebula-api.yaml`
- `planning-mds/architecture/data-model.md`
- `planning-mds/security/policies/policy.csv`
- `scripts/kg/lookup.py F0032`

## Assumptions

- Phase A and Phase B planning were approved by the operator before this feature action.
- First implementation uses in-process refresh status; cross-instance invalidation is deferred.
- F0032 owns governance records and the admin facade, not module-owned execution behavior.

## Scope Boundaries

- In scope: admin configuration catalog, draft/update, validate/compare, publish, rollback, audit, authorization, frontend console.
- Out of scope: F0022 routing execution replacement, F0023 search/report projection replacement, F0027 document generation/template upload, F0034 schema authoring, identity/secrets/infrastructure administration.

## Lifecycle Stage

G0 creates the implementation-ready feature assembly plan and validates the evidence package before runtime preflight and code edits.
