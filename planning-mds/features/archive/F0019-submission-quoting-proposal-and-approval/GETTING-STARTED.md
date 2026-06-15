# F0019 — Submission Quoting, Proposal & Approval Workflow — Getting Started

## Prerequisites

- [ ] Read the current release framing in [ROADMAP.md](../ROADMAP.md)
- [ ] Review current submission workflow states and approved blueprint transitions
- [ ] Read F0006 and confirm its boundary: intake ends at `ReadyForUWReview`
- [ ] Confirm the current runtime still rejects `ReadyForUWReview -> InReview` and later transitions before starting F0019 work
- [ ] Confirm F0006 closeout no longer claims submission soft delete and capture that F0019 now owns any future submission archive/deactivate contract
- [ ] Refine this feature into stories and an implementation contract before coding

## How to Verify

1. Confirm the feature covers quote, proposal, approval, bind, decline, and withdrawal decision flow.
2. Define what remains internal workflow versus later external integration.
3. Create an explicit story that activates downstream transitions beginning with `ReadyForUWReview -> InReview` and references F0006 as the prior boundary owner.
4. Define whether submission archive/deactivate behavior exists in MVP, which lifecycle states permit it, and how archived submissions remain visible for audit, reporting, or historical lookup.
5. Verify the implementation plan includes authorization, UI, API, and regression-test updates for moving the boundary beyond F0006 and for any archive/deactivate contract.
6. Validate tracker sync after refinement.

## Architecture (Phase B)

- Governing decision: **ADR-025** — `planning-mds/architecture/decisions/ADR-025-submission-downstream-workflow-quote-approval-bind-and-archive.md` (applies ADR-011 workflow + ADR-012 documents).
- **Data model additions:** `SubmissionQuotePacket` (1:1 with submission; status + recorded reference facts + F0020 doc links + `RowVersion`), `SubmissionApprovalDecision` (append-only), and `Submission.IsArchived/ArchivedAt/ArchivedByUserId` (distinct from `IsDeleted`). Downstream states are already declared on `workflow:submission`.
- **API additions (finalized in `planning-mds/api/nebula-api.yaml` at implementation):** `POST /submissions/{id}/transitions` (downstream activated), `GET|PUT /submissions/{id}/quote-packet`, `POST /submissions/{id}/approval`, `POST /submissions/{id}/bind`, `POST /submissions/{id}/archive` + `/reactivate`. ProblemDetails errors; `If-Match`/`RowVersion` → 412 on stale writes.
- **Authorization deltas:** new Casbin actions `submission:approve` and `submission:archive` (Underwriter, Admin) in `planning-mds/security/policies/policy.csv` + `authorization-matrix.md` §2.8.
- **Boundary:** F0019 is CRM status/workflow — recorded, never computed. No rating/pricing/scoring. A boundary regression must assert no rating/calculation endpoint or computed-pricing field exists.
