# Action Context — F0027 feature run 2026-07-02-b9316621

## Run Identity

- Action: `feature`
- Feature ID: `F0027`
- Feature slug: `coi-acord-and-outbound-document-generation`
- Run ID: `2026-07-02-b9316621`
- Product root: `/Users/wallstreet288/Nebula_pr/nebula-insurance-crm`
- Harness root: `/Users/wallstreet288/Nebula_pr/nandini-nebula-agents`
- Feature path at run start: `planning-mds/features/F0027-coi-acord-and-outbound-document-generation`
- Prior plan run: `2026-07-02-e8a31f35`

## Inputs

- Plan action completed through G5 Phase B approval.
- F0027 v1 scope: COI, ACORD, reusable proposal template.
- Lifecycle: Preview then explicit Issue.
- Roles: Admin manages outbound templates; service/distribution users issue artifacts.

## Assumptions

- G0 is an architect planning gate only; runtime commands and security scans start at later feature gates.
- F0027 remains in the active feature folder until G8 closeout archive movement.
- Existing full historical tracker/evidence failures outside this run remain legacy cleanup and are not a G0 blocker for this new run.

## Scope Boundaries

- In scope: feature implementation planning and vertical-slice implementation for F0027 only.
- Out of scope: F0019 workflow ownership changes, rating/pricing computation, e-signature, outbound sending, OCR/extraction.
- `feature-assembly-plan.md` is authored at G0 by this feature action.

## Lifecycle Stage

- Current gate: G0 Architect assembly plan validation.
