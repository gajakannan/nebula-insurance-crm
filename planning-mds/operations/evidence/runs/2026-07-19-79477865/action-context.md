# Action Context

> Seeded by init-run.py. Fill the judgment sections before G0.

## Run Identity

- **action:** plan
- **contract_effective_date:** 2026-07-11
- **contract_version:** 2026-07-11
- **feature_id:** F0026
- **feature_index_root:** /home/gajap/uSandbox/repos/nebula/nebula-insurance-crm/planning-mds/operations/evidence/features/F0026-billing-invoicing-and-reconciliation
- **feature_slug:** billing-invoicing-and-reconciliation
- **mode:** clean
- **product_root:** /home/gajap/uSandbox/repos/nebula/nebula-insurance-crm
- **run_folder:** /home/gajap/uSandbox/repos/nebula/nebula-insurance-crm/planning-mds/operations/evidence/runs/2026-07-19-79477865
- **run_id:** 2026-07-19-79477865
- **run_id_prior:** None

## Inputs

- `FEATURE_ID=F0026`
- `PHASE=A+B`
- `FEATURE_MODE=existing`
- `PRODUCT_ROOT=/home/gajap/uSandbox/repos/nebula/nebula-insurance-crm`

## Assumptions

- Raw PRDs, ADRs, API/schema contracts, and implemented dependency artifacts outrank KG lookup output.
- F0026 remains an operational billing/reconciliation capability and does not replace a general ledger, bank platform, or tax system.
- Business rules that are not present in approved source artifacts remain open through G1; they are not inferred from generic industry practice.

## Scope Boundaries

- Update the existing F0026 planning shell through Phase A and Phase B.
- Phase A owns the PRD, personas, acceptance-criteria checklist, stories, trackers, and append-only STATUS skeleton.
- Phase B owns the feature assembly plan, ADRs/contracts/schemas, and source KG bindings.
- This plan run creates only base-run evidence. It does not create or consume a feature evidence package or `current-run.json`.

## Lifecycle Stage

- G1 clarification passed after explicit operator resolution of the five-question decision set.
- G2 tracker synchronization passed after the operator-promoted `Now` sequencing was compiled and revalidated with all Phase A validators green.
- G3 Phase A approval passed with explicit token `lets do Phase B`.
- G4 ontology synchronization passed with drift check exit 0.
- G5 exit validation passed in the required order; Phase B approval was attested with explicit token `approve`.
- Plan action is complete. F0026 remains Planned / `Now` and is ready for its future feature action.
