# F0027 — COI, ACORD & Outbound Document Generation — Getting Started

## Prerequisites

- Read the current release framing in [ROADMAP.md](../ROADMAP.md).
- Review completed foundations first:
  - F0018 Policy Lifecycle & Policy 360
  - F0020 Document Management & ACORD Intake
  - F0019 Submission Quoting, Proposal & Approval Workflow
- Complete Phase B architecture before implementation.
- Do not create `feature-assembly-plan.md` during plan; the later feature action G0 owns that artifact.

## v1 Decisions

- Artifact scope: COI, ACORD, and reusable proposal templates.
- Document lifecycle: Preview first, then explicit Issue.
- Role split: Admin edits templates; service/distribution users issue final artifacts.

## How to Verify Phase A

1. Confirm the PRD includes v1 artifact scope, non-goals, workflows, screens, and ASCII layouts.
2. Confirm every story has acceptance criteria, edge cases, permissions, audit expectations, and mutation interaction contracts.
3. Confirm F0019 proposal packet ownership remains separate from F0027 reusable rendering.
4. Regenerate the story index and validate trackers before Phase A approval.
