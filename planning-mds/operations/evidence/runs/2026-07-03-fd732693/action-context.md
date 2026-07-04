# Action Context - F0008 Broker Insights

## Run Identity

- Feature ID: F0008
- Feature slug: broker-insights
- Action: feature
- Mode: clean
- Run ID: 2026-07-03-fd732693
- Product root: `/Users/wallstreet48/nebula-feature-26/nebula-insurance-crm`
- Agents root: `/Users/wallstreet48/nebula-feature-26/nebula-agents`
- Feature path at run start: `planning-mds/features/F0008-broker-insights`
- Prior approved run: none found

## Inputs

- Operator requested continuation into F0008 implementation.
- Operator required strict use of the `nebula-agents` harness.
- Approved planning source: plan run `2026-07-03-4b9ca863`.

## Assumptions

- F0008 uses the current approved OpenAPI, schema, authorization, and ADR artifacts from the plan run.
- F0008 remains read-only; no user-triggered mutation endpoint is introduced.
- Security-sensitive scope is reconciled at G2; dependency audit and authenticated DAST waivers are recorded in the manifest.
- Runtime commands must be executed after G1 preflight.

## Scope Boundaries

- In scope: F0008 Broker Insights read-only broker scorecards, trends, benchmarks, review snapshots, permission-safe behavior, API/UI/tests/evidence.
- Out of scope: F0037 hierarchy management changes, unrelated CRM features, and unrelated pre-existing worktree changes.
- AI scope: not currently in scope; no `neuron/` changes planned.

## Lifecycle Stage

- Current gate: G8 PM closeout validation.
- Manifest status: approved.
