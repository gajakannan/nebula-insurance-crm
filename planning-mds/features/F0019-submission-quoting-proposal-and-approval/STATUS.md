# F0019 — Submission Quoting, Proposal & Approval Workflow — Status

**Overall Status:** Draft
**Last Updated:** 2026-03-31

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|

## Refinement Guardrails

- F0006 remains closed at `ReadyForUWReview`; downstream submission transitions must remain disabled until F0019 stories explicitly turn them on.
- The first F0019 story must own activation of `ReadyForUWReview -> InReview` and later downstream transitions, with direct reference to F0006 as the prior workflow boundary.
- F0019 refinement is incomplete until the implementation contract identifies the code path, authorization changes, UI exposure, and regression coverage that deliberately move the shared submission workflow beyond F0006.
- If F0006 descopes submission soft delete, F0019 refinement must explicitly define the replacement submission archive/deactivate contract, including allowed states, action shape, list visibility, and audit retention before coding starts.

## Required Signoff Roles (Set in Planning)

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Workflow state, approval behavior, and regression validation will be required. | Architect | TBD |
| Code Reviewer | Yes | Workflow orchestration and approval logic require independent review. | Architect | TBD |
| Security Reviewer | TBD | Set during refinement if approval authority or data-boundary risk is introduced. | Architect | TBD |
| DevOps | TBD | Set during refinement if workflow execution or background processing changes are introduced. | Architect | TBD |
| Architect | TBD | Set during refinement if workflow modeling or orchestration decisions require explicit approval. | Architect | TBD |

## Story Signoff Provenance

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0019-S0001 | Quality Engineer | - | N/A | - | - | Populate after story breakdown is created. |
| F0019-S0001 | Code Reviewer | - | N/A | - | - | Populate after story breakdown is created. |
