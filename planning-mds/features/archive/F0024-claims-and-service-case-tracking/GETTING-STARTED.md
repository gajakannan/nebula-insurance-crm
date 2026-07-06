# F0024 — Claims & Service Case Tracking — Getting Started

## Prerequisites

- [ ] Read the current release framing in [ROADMAP.md](../ROADMAP.md)
- [ ] Review completed F0016 Account 360, F0018 Policy Lifecycle & Policy 360, F0021 Communication Hub, and F0004 Task Center foundations
- [x] Obtain G3 Phase A approval on PRD and stories before Architect Phase B
- [x] Complete Architect Phase B draft before any feature/build action
- [ ] Obtain G4/G5 architecture approval before feature action kickoff

## How to Verify

1. Confirm the first release is CRM-side servicing context, not a full claims platform.
2. Define how claim and service records link to accounts, policies, and tasks.
3. Validate tracker sync after refinement.

## Planning Notes

- F0024 is now in the roadmap `Now` bucket by operator decision.
- The first buildable plan should stay inside six vertical stories: intake, visibility, ownership/follow-up, status transition, claim-reference context, and permission-safe audit history.
- External carrier synchronization, reserves/payments, claims adjudication, and external/broker portal visibility are deferred.
- Phase B architecture packet is drafted: architecture plan, API/schema deltas, authorization deltas, ADR-030, schemas, data model, policy, and KG bindings.
