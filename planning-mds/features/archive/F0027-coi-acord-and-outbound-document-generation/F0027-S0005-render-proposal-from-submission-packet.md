# Render Proposal From Submission Packet Context

## Story Header

**Story ID:** F0027-S0005
**Feature:** F0027 — COI, ACORD & Outbound Document Generation
**Title:** Render proposal from submission packet context
**Priority:** Medium
**Phase:** CRM Release MVP+

## User Story

**As a** distribution user
**I want** to render a reusable proposal output from an existing submission quote/proposal packet
**So that** recorded packet facts can produce a customer-facing proposal without changing the F0019 workflow state

## Context & Background

F0019 created the submission-bound quote/proposal packet as a CRM coordination record. F0027 owns reusable rendering from that packet context, but must not take over packet workflow state, approval readiness, bind decisions, or quote computation.

## Acceptance Criteria

**Happy Path:**
- **Given** a submission has an existing F0019 quote/proposal packet with recorded terms and linked documents
- **When** an authorized user selects a published proposal template and previews it
- **Then** Nebula renders preview output from recorded packet facts and source record context without changing packet or submission workflow state.
- **Given** preview succeeds
- **When** the user explicitly issues the proposal artifact
- **Then** the issued proposal is stored as a generated artifact linked to the submission and packet context, with generated-artifact audit history.

**Alternative Flows / Edge Cases:**
- Packet does not exist -> proposal preview is blocked with "quote/proposal packet required."
- Packet required facts are missing -> preview/issue blocked with missing-data summary.
- Submission is inaccessible to actor -> `403 Forbidden`.
- Submission is terminal but accessible -> generated artifacts may be read; new proposal issue is blocked unless Phase B explicitly allows terminal-state issue.
- Any attempt to modify packet status, approval state, bind state, or quote figures from F0027 -> rejected/out of scope.

**Checklist:**
- [ ] Proposal rendering uses recorded packet facts only.
- [ ] F0027 does not compute premium, limits, eligibility, rating, or scoring.
- [ ] F0027 does not transition submission workflow state.
- [ ] Issued proposal artifact links to submission and packet context.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Submission Detail > Quote/Proposal Packet > Generate Proposal | Preview proposal | Packet data read-only inside F0027 | Preview generated from recorded packet context; no packet mutation | Reload shows submission/packet state unchanged | Underwriter, distribution user, admin with submission access |
| Submission Detail > Generate Proposal | Issue proposal artifact | Enabled after successful preview and valid packet context | Generated proposal artifact stored and linked to submission/packet context | Reload submission documents rail shows issued proposal; packet status unchanged | Underwriter, distribution user, admin with submission access |

Required checks:
- [ ] Render-only behavior cannot satisfy this story because preview/issue generation must execute.
- [ ] Save path has validation and error behavior specified.
- [ ] A successful issue has audit/timeline or generated-artifact history expectation.
- [ ] Tests prove proposal issue persists while submission packet workflow state remains unchanged.

## Data Requirements

**Required Fields:**
- `submissionId`: Source submission.
- `quotePacketId`: Existing F0019 packet.
- `templateVersionId`: Published proposal template version.
- `recordedPacketFacts`: Packet facts consumed as source data.
- `generatedArtifactId`: Issued proposal artifact identity.

**Optional Fields:**
- `linkedDocumentRefs`: Source packet documents reflected in proposal output.

**Validation Rules:**
- Existing packet required for proposal rendering.
- Packet facts are validated for presence/format only.
- F0027 must not write packet status, submission workflow state, approval decision, bind handoff, or quote values.

## Role-Based Visibility

**Roles that can render proposal from packet:**
- Underwriter — permitted with submission access.
- Distribution user — permitted with submission access.
- Admin — permitted.

**Data Visibility:**
- InternalOnly content: packet facts, generated proposal metadata, provenance.
- ExternalVisible content: none in v1; real outbound delivery is out of scope.

## Non-Functional Expectations

- Security: submission ABAC and document classification are enforced.
- Reliability: issue + artifact storage is atomic; packet state is not mutated.
- Auditability: issued proposal records source packet and template version.

## Dependencies

**Depends On:**
- F0019-S0002 — submission quote/proposal packet lifecycle.
- F0027-S0001 — proposal template governance.
- F0027-S0002 — preview.
- F0027-S0003 — issue.

**Related Stories:**
- F0019-S0003 — approval checkpoint remains owned by F0019.
- F0019-S0004 — bind handoff remains owned by F0019.

## Business Rules

1. **Recorded, never computed:** F0027 renders recorded packet facts only; it does not calculate quote values.
2. **Workflow boundary:** F0027 never changes submission or packet workflow state.
3. **Artifact boundary:** F0027 owns generated proposal artifact provenance and storage linkage.

## Out of Scope

- Packet creation or update.
- Approval readiness.
- Bind decision evidence.
- Rating/pricing/comparison.

## UI/UX Notes

- Screens involved: Submission Detail, Quote/Proposal Packet panel, Generate Document Panel.
- Key interactions: select proposal template, preview, issue, open generated proposal artifact.

## Questions & Assumptions

**Open Questions:**
- [ ] None after G1 clarification.

**Assumptions (to be validated):**
- Terminal-state proposal issue defaults to blocked in Phase A to avoid post-bind customer-facing document drift; Phase B may refine this with explicit rules.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated if story file was added/renamed/moved
