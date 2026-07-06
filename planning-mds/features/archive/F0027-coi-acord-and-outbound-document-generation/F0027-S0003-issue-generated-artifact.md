# Issue Final Generated Artifact With Audit

## Story Header

**Story ID:** F0027-S0003
**Feature:** F0027 — COI, ACORD & Outbound Document Generation
**Title:** Issue final generated artifact with audit
**Priority:** High
**Phase:** CRM Release MVP+

## User Story

**As a** service or distribution user
**I want** to explicitly issue a previewed generated document
**So that** the final customer-facing artifact is stored, traceable, and linked to the CRM source record

## Context & Background

Issuing is the finalization step for generated output. It must create a durable generated artifact and provenance record rather than silently replacing source documents or workflow state.

## Acceptance Criteria

**Happy Path:**
- **Given** a preview succeeded for a published template and accessible source record
- **When** I click Issue
- **Then** Nebula stores a final generated artifact through the document subsystem, links it to the source record, and records template/data provenance.
- **Given** the artifact is issued
- **When** I reload the source record or artifact detail
- **Then** I see the issued artifact, issued status, source record link, template version, issued by, and issued timestamp.

**Alternative Flows / Edge Cases:**
- Issue attempted without successful preview -> blocked with "Preview required before issue."
- Merge data changed since preview and required fields are missing -> issue is blocked with validation errors.
- Unauthorized actor attempts issue -> `403 Forbidden`.
- Source record no longer accessible or no longer exists -> `403` or `404`; no artifact issued.
- Document storage fails -> issue fails safely; no partial final artifact is shown.

**Checklist:**
- [ ] Issued artifact is immutable as an issued output.
- [ ] Issued artifact is linked to the source account, policy, or submission.
- [ ] Issued artifact records template version and source data snapshot/provenance.
- [ ] Successful issue appends audit/timeline evidence where source record supports timeline.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Generate Document Panel after successful Preview | Click Issue | Enabled only after valid preview for accessible source record | Final generated artifact is created and linked through document subsystem | Reload source record shows artifact in documents rail; artifact detail shows Issued state and provenance | Service user, distribution user, underwriter where source access allows, admin |
| Generated Artifact Detail | Download issued artifact | Read-only for issued artifact metadata | No mutation | Artifact can be retrieved by authorized actor | Same as document read/download permissions |

Required checks:
- [ ] Render-only behavior cannot satisfy this story.
- [ ] The issue path validates preview, source access, template status, and merge completeness.
- [ ] A successful issue appends audit/timeline or equivalent generated-artifact history.
- [ ] Tests prove issuing from the named entry point persists state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- `generatedArtifactId`: Stable generated artifact identity.
- `sourceRecordType`, `sourceRecordId`: Source CRM context.
- `templateVersionId`: Template version used.
- `artifactFamily`: COI, ACORD, or Proposal.
- `status`: Issued.
- `issuedBy`, `issuedAt`: Finalization provenance.
- `documentRef`: Link to stored artifact.

**Optional Fields:**
- `previewId`: Prior preview reference, if Phase B persists previews.
- `dataSnapshotHash`: Integrity marker for source data used during render.

**Validation Rules:**
- Issue requires a successful preview.
- Template must still be Published.
- Required merge fields must be complete at issue time.
- Actor must have source-record access and issue permission.

## Role-Based Visibility

**Roles that can issue:**
- Service user — permitted for accessible source records.
- Distribution user — permitted for accessible source records.
- Underwriter — permitted where source access allows.
- Admin — permitted.

**Data Visibility:**
- InternalOnly content: issued artifact metadata, provenance, source-data snapshot references.
- ExternalVisible content: none in v1; real outbound delivery is out of scope.

## Non-Functional Expectations

- Security: document access and download obey existing document classification and parent ABAC.
- Reliability: issue + document linkage + audit history are atomic or fail without partial final state.
- Performance: user receives clear progress if rendering/storage is asynchronous.

## Dependencies

**Depends On:**
- F0027-S0001 — published template.
- F0027-S0002 — successful preview.
- F0020 — document storage, metadata, download.

**Related Stories:**
- F0027-S0004 — retrieval and regeneration history.

## Business Rules

1. **Explicit issue only:** No final artifact is created without user action.
2. **Issued artifacts are historical records:** Regeneration creates another artifact; it does not mutate prior issued output.

## Out of Scope

- Emailing or sending the issued artifact.
- E-signature.
- External portal publication.

## UI/UX Notes

- Screens involved: Generate Document Panel, Source Record Documents Rail, Generated Artifact Detail.
- Key interactions: Issue, success confirmation, artifact link/download.

## Questions & Assumptions

**Open Questions:**
- [ ] None after G1 clarification.

**Assumptions (to be validated):**
- Phase B decides exact atomicity mechanism and whether rendering runs synchronously or asynchronously.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated if story file was added/renamed/moved
