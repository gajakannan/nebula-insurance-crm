# KG Reconciliation — F0022

## Binding Delta

- F0022 implementation binds the planned operations-routing nodes to backend domain, application, infrastructure, API, frontend, and policy surfaces.
- G7 reconciliation found and corrected an F0022 policy/matrix drift: runtime `policy.csv` had granted ProgramManager manage/assign and Coordinator queue access, while the approved authorization matrix grants ProgramManager read-only and no Coordinator queue permission. Runtime policy and queue policy regression tests were corrected.
- No feature path move occurred during this run.

## Canonical Nodes

- Feature: `feature:F0022`
- Capability: `capability:operations-routing`
- Entities: `entity:work-queue`, `entity:work-queue-member`, `entity:assignment-rule`, `entity:coverage-window`, `entity:queue-work-item`, `entity:routing-decision-log`
- Source records: `entity:task-item`, `workflow:submission`, `workflow:renewal`
- Policy rules: `policy_rule:queue-read`, `policy_rule:queue-manage`, `policy_rule:queue-assign`
- API endpoints: `endpoint:work-queue-list`, `endpoint:work-queue-create`, `endpoint:work-queue-items-list`, `endpoint:assignment-rule-upsert`, `endpoint:coverage-window-upsert`, `endpoint:queue-work-item-reassign`, `endpoint:work-queue-rebalance`, `endpoint:routing-events-list`

## Validator Results

- `scripts/kg/validate.py --check-drift`: PASS.
- F0022-specific policy-rule warnings cleared after policy/matrix reconciliation.
- Remaining warnings are pre-existing unrelated symbol-reference warnings in renewal/product-schema test stubs and one low-confidence inferred dependency edge.
- `scripts/kg/lookup.py F0022 --tier 1`: confirmed F0022 feature bindings, ADR governance, policy rules, schemas, API contract, and dependencies.

## Handoff to Closeout

- Product Manager closeout should accept or complete the non-blocking G2/G3 recommendations.
- Closeout should keep the corrected queue policy matrix as the source of truth: ProgramManager queue read only; Coordinator no F0022 queue grant.
- Closeout should update final tracker/archive state only after G8 validator passes.
