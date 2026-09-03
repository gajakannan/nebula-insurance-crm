# Action Context

> Seeded by init-run.py. Fill the judgment sections before G0.

## Run Identity

- **action:** feature
- **contract_effective_date:** 2026-07-11
- **contract_version:** 2026-07-11
- **feature_id:** F0040
- **feature_index_root:** /home/gajap/uSandbox/repos/nebula/nebula-insurance-crm/planning-mds/operations/evidence/features/F0040-neuron-second-specialist-head
- **feature_slug:** neuron-second-specialist-head
- **mode:** clean
- **product_root:** /home/gajap/uSandbox/repos/nebula/nebula-insurance-crm
- **run_folder:** /home/gajap/uSandbox/repos/nebula/nebula-insurance-crm/planning-mds/operations/evidence/runs/2026-09-01-fd408477
- **run_id:** 2026-09-01-fd408477
- **run_id_prior:** None

## Inputs

- Approved F0040 Phase B assembly plan and stories S0001–S0003.
- Existing Engine timeline authorization/projection, Neuron F0038/F0039 orchestration and persistence, and the registered frontend timeline presentation.
- G1 runtime retry evidence confirming the existing Compose stack is available.

## Assumptions

- F0040 remains read-only for CRM data: no Broker, Contact, Timeline, or workflow mutation is added.
- Engine remains the authorization boundary; Neuron forwards the authenticated user token and does not recreate broker scope.
- The existing newest-20 Broker feed and Broker 360 route are the authoritative product contracts.
- The pre-existing full Engine integration failures are environment/fixture failures outside the changed timeline and telemetry tests; they are recorded as a G2 follow-up rather than silently treated as passes.

## Scope Boundaries

- In scope: scoped internal Broker timeline projection, Neuron Broker head/direct route/shared executor/component registry/telemetry, registered frontend list and retry, and tests/evidence.
- Explicitly out of scope: Broker filters/search/pagination UI, summaries/recommendations/writes, Tasks/Pipeline activation, cross-zone composition, new services, ports, secrets, migrations, or deployment topology.

## Lifecycle Stage

- implementation and G2 self-review in progress
