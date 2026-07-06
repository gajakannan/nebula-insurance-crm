# Regenerate And Retrieve Generated Artifacts

## Story Header

**Story ID:** F0027-S0004
**Feature:** F0027 — COI, ACORD & Outbound Document Generation
**Title:** Regenerate and retrieve generated artifacts
**Priority:** Medium
**Phase:** CRM Release MVP+

## User Story

**As a** service or distribution user
**I want** to view, download, and regenerate generated artifacts
**So that** I can recover prior issued outputs while creating a traceable new artifact when source data or templates change

## Context & Background

Generated documents are business records. Users need a reliable way to retrieve prior issued artifacts and regenerate a new version without overwriting history.

## Acceptance Criteria

**Happy Path:**
- **Given** an issued generated artifact exists on a source record
- **When** I open the Generated Artifact Detail
- **Then** I see artifact family, source record, template version, issued by, issued at, and download action.
- **Given** I need updated output
- **When** I choose Regenerate from the issued artifact context
- **Then** Nebula starts a new preview/issue flow using the current selected template/source context, leaves the prior issued artifact unchanged, and records regeneration/issue audit history if a new artifact is issued.

**Alternative Flows / Edge Cases:**
- User lacks artifact/document read access -> `403 Forbidden`.
- Artifact binary missing or unavailable -> show user-safe retrieval error and preserve metadata.
- Template version used by prior artifact is retired -> prior artifact remains downloadable if storage permits; regeneration requires a current published template.
- Regeneration validation fails -> prior issued artifact remains unchanged.

**Checklist:**
- [ ] Generated artifact history is visible on source record.
- [ ] Download uses existing document authorization and classification behavior.
- [ ] Regeneration creates a new preview/issue path, not an overwrite.
- [ ] Prior artifact provenance remains readable.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Source Record Documents Rail | Open generated artifact | Read-only metadata for issued artifacts | No mutation | Artifact detail displays same metadata after reload | Authorized internal users with document read access |
| Generated Artifact Detail | Click Regenerate | Starts new generation flow; prior artifact read-only | New preview/issue flow begins; prior artifact unchanged | Reload shows original artifact plus any newly issued artifact after issue | Same roles as preview/issue |

Required checks:
- [ ] Render-only behavior cannot satisfy regeneration; a new generation flow is observable.
- [ ] Regeneration validation and error behavior are specified.
- [ ] Successful regeneration issue appends audit/timeline or equivalent generated-artifact history.
- [ ] Tests prove prior issued artifacts remain immutable after regeneration.

## Data Requirements

**Required Fields:**
- `generatedArtifactId`: Artifact identity.
- `documentRef`: Stored artifact reference.
- `templateVersionId`: Template version that produced the artifact.
- `sourceRecordType`, `sourceRecordId`: Source record.
- `issuedBy`, `issuedAt`: Provenance.

**Optional Fields:**
- `supersedesArtifactId`: Optional relationship from a newly issued regenerated artifact to a prior artifact.

**Validation Rules:**
- Download requires document read/download authorization.
- Regeneration requires a current published template and source-record access.

## Role-Based Visibility

**Roles that can retrieve/regenerate:**
- Service user — permitted for accessible source records.
- Distribution user — permitted for accessible source records.
- Underwriter — permitted where source access allows.
- Admin — permitted.

**Data Visibility:**
- InternalOnly content: artifact history and provenance.
- ExternalVisible content: none in v1.

## Non-Functional Expectations

- Reliability: metadata remains available even if artifact retrieval fails.
- Security: classification and source-record ABAC are enforced.
- Auditability: regeneration/issue creates traceable history.

## Dependencies

**Depends On:**
- F0027-S0003 — issued generated artifact.
- F0020 — document retrieval/download behavior.

**Related Stories:**
- F0027-S0002 — preview.
- F0027-S0003 — issue.

## Business Rules

1. **No overwrite:** Regeneration creates a new artifact path and preserves prior issued records.
2. **Current template for new generation:** retired historical template versions can explain prior artifacts but are not used for new issue unless Phase B explicitly allows it.

## Out of Scope

- Bulk regeneration.
- Automated scheduled regeneration.
- External sharing links.

## UI/UX Notes

- Screens involved: Source Record Documents Rail, Generated Artifact Detail.
- Key interactions: open detail, download, regenerate.

## Questions & Assumptions

**Open Questions:**
- [ ] None after G1 clarification.

**Assumptions (to be validated):**
- Phase B decides whether regenerated artifacts explicitly link to prior artifacts via `supersedesArtifactId`.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated if story file was added/renamed/moved
