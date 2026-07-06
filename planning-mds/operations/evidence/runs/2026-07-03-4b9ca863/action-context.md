# Action Context

## Inputs

- `FEATURE_ID=F0008`
- `PHASE=A+B`
- `FEATURE_MODE=existing`

## Resolved Paths

- Product root: `/Users/wallstreet48/nebula-feature-26/nebula-insurance-crm`
- Agents root: `/Users/wallstreet48/nebula-feature-26/nebula-agents`
- Feature folder: `/Users/wallstreet48/nebula-feature-26/nebula-insurance-crm/planning-mds/features/F0008-broker-insights`
- Plan run evidence: `/Users/wallstreet48/nebula-feature-26/nebula-insurance-crm/planning-mds/operations/evidence/runs/2026-07-03-4b9ca863`

## Process Contract

- Use Nebula agents harness strictly.
- Execute the `plan` action under the base run evidence profile.
- Do not create a feature evidence package during this plan action.
- Load required context in the order defined by `agents/templates/prompts/evidence-contract/plan-operator-friendly.md`.
- Keep all shell commands in `commands.log`.
- Stop at approval gates where required.

## Approval State

- Phase A must receive explicit approval before Phase B.
- Phase B must receive explicit approval before any implementation action.
