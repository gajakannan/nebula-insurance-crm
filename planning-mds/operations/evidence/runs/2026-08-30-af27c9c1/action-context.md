# Action Context

> Seeded by init-run.py. Fill the judgment sections before G0.

## Run Identity

- **action:** plan
- **contract_effective_date:** 2026-07-11
- **contract_version:** 2026-07-11
- **feature_id:** F0040
- **feature_index_root:** /home/gajap/uSandbox/repos/nebula/nebula-insurance-crm/planning-mds/operations/evidence/features/F0040-neuron-second-specialist-head
- **feature_slug:** neuron-second-specialist-head
- **mode:** clean
- **product_root:** /home/gajap/uSandbox/repos/nebula/nebula-insurance-crm
- **run_folder:** /home/gajap/uSandbox/repos/nebula/nebula-insurance-crm/planning-mds/operations/evidence/runs/2026-08-30-af27c9c1
- **run_id:** 2026-08-30-af27c9c1
- **run_id_prior:** None

## Inputs

- Operator clarification: G1 passed with `broker_activity` approved as the second live specialist domain.
- Existing F0040 provisional feature skeleton.
- F0038 signed-off epic intake, PRD, zone/stub stories, and delivered Neuron shell artifacts.
- F0039 delivered durable-thread and trusted intent-routing artifacts.
- F0001-S0004 and F0002-S0007 established Broker activity feed/timeline behavior.
- Current product code for the inactive Broker head, intent catalog, zone dispatch, registered components, and existing broker activity feed.

## Assumptions

- Existing authorized Broker `ActivityTimelineEvent` reads and Broker 360 navigation remain the product source for the new Neuron read.
- F0040 adds no Broker/Contact/Timeline business mutation and no new authorization rule.
- F0038 and F0039 delivered contracts remain available; Phase B must explicitly design any compatibility change.
- Initial second-head usage establishes an adoption baseline before a growth target is set.

## Scope Boundaries

- In scope: newest 20 authorized Broker events in the live glance zone, direct `broker_activity.list` routing, durable replay, two-live-head failure isolation, safe registered rendering, and bounded telemetry.
- Out of scope: broker writes, filters/search/aggregation, AI summaries/recommendations, cross-zone ranking/composition, third heads, F0041 adjudication, external hosts, and authorization-policy changes.
- PM owns Phase A requirements/stories; Architect owns Phase B assembly plan, contracts, ADRs, final required-role matrix, and `kg-source/**` changes.

## Lifecycle Stage

- Phase A approved and attested at G3.
- Architect-owned Phase B artifacts and ontology bindings are authored.
- G4 KG compile/drift validation passed.
- All automatic G5 exit operations passed and the operator approved Phase B at `approve-phase-b` on 2026-08-31.
