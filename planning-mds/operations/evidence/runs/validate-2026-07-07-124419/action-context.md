# Action Context

Run ID: validate-2026-07-07-124419
Date: 2026-07-07
Action: validate
Trigger: operator requested strict use of the `nebula-agents` harness for project work.

## Resolved Roots

- Framework root: `/Users/msig4/Documents/NEBULA/nebula-agents`
- Product root: `/Users/msig4/Documents/NEBULA/nebula-insurance-crm`

## Harness Boundary

- Session working directory for framework docs and role contracts: `nebula-agents`
- Product-owned implementation and planning target: `{PRODUCT_ROOT}` = `/Users/msig4/Documents/NEBULA/nebula-insurance-crm`
- `.agentignore` was checked before broad product reads. `planning-mds/operations/**` was treated as cold archive except for this run folder and exact validation evidence files.

## Scope Decision

`lifecycle-stage.yaml` declares `current_stage: implementation`, but `implementation_scope.active_feature` is empty. `REGISTRY.md` has no active features, and ROADMAP `Now` is empty. Therefore the harness entry point is `agents/actions/validate.md` at full-project scope.
