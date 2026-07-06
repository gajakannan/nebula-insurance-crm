# Preview Generated Document Before Issue

## Story Header

**Story ID:** F0027-S0002
**Feature:** F0027 — COI, ACORD & Outbound Document Generation
**Title:** Preview generated document before issue
**Priority:** High
**Phase:** CRM Release MVP+

## User Story

**As a** service or distribution user
**I want** to preview a COI, ACORD, or proposal document from CRM data before issuing it
**So that** missing data or incorrect merge output is caught before a customer-facing artifact is finalized

## Context & Background

Preview is the safety gate for F0027. It validates required merge data against the selected source record and template version without creating a final issued artifact.

## Acceptance Criteria

**Happy Path:**
- **Given** I am on an account, policy, or submission detail screen with access to generate documents
- **When** I select a published COI, ACORD, or proposal template and click Preview
- **Then** Nebula validates required merge data and shows a preview result without issuing a final artifact.
- **Given** preview succeeds
- **When** I reload the source record
- **Then** no final issued artifact appears unless I explicitly issue it in F0027-S0003.

**Alternative Flows / Edge Cases:**
- Required merge field missing -> preview is blocked with a field-level missing-data summary.
- Template is Draft or retired -> selection is blocked or preview returns validation error; no artifact is issued.
- User lacks source-record access -> `403 Forbidden`.
- Source record does not exist -> `404`.
- Rendering fails -> show user-safe failure and record a generation failure audit signal.

**Checklist:**
- [ ] Preview is available for COI, ACORD, and proposal families.
- [ ] Preview uses published templates only.
- [ ] Preview shows source record and template version provenance.
- [ ] Preview does not create final issued document state.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Source Record Detail > Documents > Generate | Select family/template and click Preview | Enabled for authorized internal users on accessible source records | Preview result generated; no final issue state | Reload source record shows no final artifact unless Issue was clicked | Service user, distribution user, underwriter where source access allows |
| Source Record Detail > Generate Validation | Resolve missing source data outside this panel | Preview panel itself does not edit source business data | No source mutation from preview | Reload still shows validation until source record data is updated elsewhere | Same as source record access |

Required checks:
- [ ] Render-only behavior cannot satisfy this story because preview generation and validation must execute.
- [ ] The save path has validation and error behavior specified; preview does not finalize.
- [ ] A successful preview records provenance or an explicit preview audit strategy from Phase B.
- [ ] Tests prove preview can be run from the named entry point and does not create an issued artifact after reload.

## Data Requirements

**Required Fields:**
- `sourceRecordType`: Account, Policy, or Submission.
- `sourceRecordId`: Selected source record.
- `templateVersionId`: Published template version.
- `artifactFamily`: COI, ACORD, or Proposal.
- `mergeValidationResult`: Complete or missing field list.

**Optional Fields:**
- `previewId`: Temporary preview identifier if Phase B chooses to persist preview metadata.

**Validation Rules:**
- Template must be Published.
- Source record must be accessible to the actor.
- Required merge fields must be present before preview succeeds.

## Role-Based Visibility

**Roles that can preview:**
- Service user — permitted for accessible source records.
- Distribution user — permitted for accessible source records.
- Underwriter — permitted where submission/policy access allows.
- Admin — permitted.

**Data Visibility:**
- InternalOnly content: generated preview and merge validation output.
- ExternalVisible content: none in v1.

## Non-Functional Expectations

- Performance: preview returns within a product-standard interactive response budget or shows async progress if Phase B requires asynchronous rendering.
- Security: preview enforces source-record ABAC and document-template access.
- Reliability: rendering errors are user-safe and auditable.

## Dependencies

**Depends On:**
- F0027-S0001 — published template versions.
- F0018 — policy data.
- F0020 — document/template storage.
- F0019 — proposal packet context for proposal previews.

**Related Stories:**
- F0027-S0003 — issues final artifact after preview.

## Business Rules

1. **Preview before issue:** v1 requires a preview step before final Issue.
2. **No workflow mutation:** preview does not change submission, policy, account, or quote/proposal packet workflow state.

## Out of Scope

- Editing source business data from the preview panel.
- Real outbound send.
- E-signature.

## UI/UX Notes

- Screens involved: account/policy/submission detail document rail; Generate Document Panel.
- Key interactions: select template, preview, view missing-field summary.

## Questions & Assumptions

**Open Questions:**
- [ ] None after G1 clarification.

**Assumptions (to be validated):**
- Phase B decides whether preview output is transient or stored as draft metadata.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged or preview failure audit equivalent recorded
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated if story file was added/renamed/moved
