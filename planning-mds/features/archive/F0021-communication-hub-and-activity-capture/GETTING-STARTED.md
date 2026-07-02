# F0021 — Communication Hub & Activity Capture — Getting Started

## Prerequisites

- [ ] Read the current release framing in [ROADMAP.md](../ROADMAP.md)
- [ ] Review the Phase A requirements package: [PRD.md](./PRD.md), story files, and [STATUS.md](./STATUS.md)
- [ ] Review existing timeline and task capabilities already in Nebula
- [ ] Confirm `scripts/kg/lookup.py F0021` resolves the F0021 feature and stories after ontology sync
- [ ] Do not start implementation until Phase B is approved and the feature action creates `feature-assembly-plan.md` at G0

## How to Verify

1. Confirm the first release focuses on structured activity capture and contextual visibility.
2. Confirm real outbound send, marketing automation, connector ingestion, and broad messaging integrations remain out of scope.
3. Confirm communication follow-up creates a normal task rather than a second workflow system.
4. Confirm correction/redaction preserves audit history.
5. Validate stories, tracker sync, API/schema planning, and KG checks before starting implementation.

## Implementation Notes For Feature Action

- Feature action G0 must create `feature-assembly-plan.md`; no assembly plan is produced by this plan action.
- Security Reviewer is required because F0021 includes sensitive free-text communication content and redaction behavior.
- AI Engineer is not required for the MVP slice unless Phase B adds AI-generated communication behavior, which is currently out of scope.
