# Template Library Governance For Outbound Artifacts

## Story Header

**Story ID:** F0027-S0001
**Feature:** F0027 — COI, ACORD & Outbound Document Generation
**Title:** Template library governance for outbound artifacts
**Priority:** High
**Phase:** CRM Release MVP+

## User Story

**As a** admin
**I want** to manage reusable COI, ACORD, and proposal templates with version and publish state
**So that** operating users issue outbound documents only from governed template versions

## Context & Background

F0020 delivered document storage and a template library foundation. F0027 v1 uses that foundation for outbound document generation, but template editing must stay controlled because templates shape customer-facing outputs.

## Acceptance Criteria

**Happy Path:**
- **Given** I am an admin on the Template Library
- **When** I upload or replace a COI, ACORD, or proposal template with required metadata
- **Then** the template version is saved as Draft and is visible in the library with family, version, status, and last updated metadata.
- **Given** a draft template passes required metadata validation
- **When** I publish the version
- **Then** service/distribution users can select that published version for preview and issue, and a template audit record captures the publish action.

**Alternative Flows / Edge Cases:**
- Missing template family, name, or source file -> reject save with actionable validation; no version is created.
- Non-admin attempts template create, replace, or publish -> `403 Forbidden`; no template is changed.
- Replacing a published template -> creates a new draft version; prior published versions remain available for provenance.
- Template family outside COI, ACORD, or Proposal -> rejected for v1.

**Checklist:**
- [ ] Template families are limited to COI, ACORD, and Proposal in v1.
- [ ] Template version history is visible to authorized users.
- [ ] Published templates cannot be silently overwritten.
- [ ] Template metadata records source file, family, version, status, updated by, and updated at.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Documents > Templates > Upload | Upload template and Save Draft | Editable for Admin only | Draft template version created | Reload library shows new version as Draft | Admin only |
| Documents > Templates > Template Detail | Publish version | Enabled for valid Draft template | Template version status becomes Published | Reload detail/list shows Published version selectable for generation | Admin only |
| Documents > Templates > Template Detail | Replace published template | Existing published version read-only; replacement creates new Draft | New Draft version created; prior Published remains unchanged | Reload shows both versions | Admin only |

Required checks:
- [ ] Render-only behavior cannot satisfy this story.
- [ ] Save path has validation and error behavior specified.
- [ ] A successful mutation records template audit history; source-record timeline is N/A because templates are governed library records.
- [ ] Tests prove the user can perform the action from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- `templateId`: Stable template identity.
- `family`: COI, ACORD, or Proposal.
- `name`: User-visible template name.
- `version`: Monotonic version label or number.
- `status`: Draft or Published.
- `sourceDocumentRef`: Link to the stored template file.
- `updatedBy`, `updatedAt`: Governance provenance.

**Optional Fields:**
- `description`: Admin-readable usage note.
- `retiredAt`: Future retirement metadata.

**Validation Rules:**
- Family must be one of COI, ACORD, Proposal.
- Name and source document are required.
- Only one active published version per template identity unless Phase B explicitly designs a multi-published model.

## Role-Based Visibility

**Roles that can manage templates:**
- Admin — create, replace, publish, view.
- Service/distribution users — view/select published templates only.
- Underwriter — view/select published proposal templates where submission context allows.

**Data Visibility:**
- InternalOnly content: draft templates, version metadata, publish history.
- ExternalVisible content: none in v1.

## Non-Functional Expectations

- Security: template mutation requires admin authorization.
- Reliability: template version changes are auditable and do not mutate historical generated artifacts.
- Performance: template library list loads within the product-standard page-load budget.

## Dependencies

**Depends On:**
- F0020 — document storage and template library foundation.
- ADR-012 — shared document storage and metadata architecture.

**Related Stories:**
- F0027-S0002 — consumes published templates for preview.
- F0027-S0003 — consumes published templates for issue.

## Business Rules

1. **Admin-governed templates:** Admin edits reusable templates; operating users generate from published versions only.
2. **Published versions are provenance anchors:** an issued artifact records the template version that produced it.

## Out of Scope

- Real outbound sending.
- E-signature templates.
- External broker template management.

## UI/UX Notes

- Screens involved: Template Library, Template Detail.
- Key interactions: upload, replace, save draft, publish, view version history.

## Questions & Assumptions

**Open Questions:**
- [ ] None after G1 clarification.

**Assumptions (to be validated):**
- Template file validation is metadata/format validation in v1; semantic validation of every merge placeholder is refined in Phase B.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged or template audit equivalent recorded
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated if story file was added/renamed/moved
