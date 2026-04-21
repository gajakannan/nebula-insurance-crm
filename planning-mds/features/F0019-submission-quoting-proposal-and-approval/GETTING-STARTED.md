# F0019 — Submission Quoting, Proposal & Approval Workflow — Getting Started

## Prerequisites

- [ ] Read the current release framing in [COMMERCIAL-PC-CRM-RELEASE-PLAN.md](../COMMERCIAL-PC-CRM-RELEASE-PLAN.md)
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
