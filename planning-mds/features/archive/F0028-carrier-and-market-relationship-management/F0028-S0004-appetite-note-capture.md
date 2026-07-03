# F0028-S0004: Appetite note capture and freshness

**Story ID:** F0028-S0004
**Feature:** F0028 — Carrier & Market Relationship Management
**Title:** Appetite note capture and freshness
**Priority:** Critical
**Phase:** CRM Release MVP+

## User Story

**As a** underwriter
**I want** to capture carrier appetite notes with source and freshness context
**So that** placement teams can evaluate markets using current recorded guidance instead of memory or scattered notes

## Context & Background

Appetite notes describe recorded market guidance such as lines of business, geography, risk characteristics, source, confidence, and review date. They are recorded context only; F0028 does not compute eligibility or recommend markets.

## Acceptance Criteria

**Happy Path:**
- **Given** an active market profile and an authorized user
- **When** the user creates or updates an appetite note with required fields
- **Then** the note is saved with LOB, geography, appetite summary, source, confidence, effective date, and next-review date
- **And** the workspace shows freshness state based on next-review date
- **And** the change appends an audit/timeline event

**Alternative Flows / Edge Cases:**
- Missing LOB/geography/source/confidence/summary -> rejected with field-level validation.
- Next-review date before effective date -> rejected with validation feedback.
- Unauthorized actor attempts mutation -> `403 Forbidden`.
- Expired/stale note -> visible as stale; not deleted or hidden by default.
- User attempts to enter rating/pricing recommendation -> saved only as text if allowed by validation; no computation or recommendation result is produced.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Market Workspace → Appetite → Add note | Enter appetite details and Save | Enabled for active/monitoring profiles | Appetite note created with freshness metadata | Reload shows note, source, confidence, next-review, freshness state, and timeline event | Underwriter, relationship manager, distribution manager/leader, admin |
| Market Workspace → Appetite → Edit note | Update note details and Save | Enabled for current notes | Note updated and prior history remains auditable | Reload shows updated values and activity entry | Same manage roles; archived profiles read-only |

Required checks:
- [ ] Render-only behavior cannot satisfy this story.
- [ ] Save path has validation and error behavior.
- [ ] Successful mutations append audit/timeline events.
- [ ] Tests prove capture/update and persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Line of business.
- Geography or jurisdiction.
- Appetite summary.
- Source type: conversation, email, document, market meeting, or internal review.
- Confidence: low, medium, or high.
- Effective date.
- Next-review date.

**Optional Fields:**
- Risk characteristics, excluded classes, contact reference, document reference, notes.

**Validation Rules:**
- Required fields must be present.
- Next-review date must be on or after effective date.
- Confidence uses controlled values.
- Appetite note does not create rating, pricing, or eligibility output.

## Role-Based Visibility

**Roles that can capture/update appetite:**
- Underwriter, relationship manager, distribution manager, distribution leader, admin.

**Data Visibility:**
- InternalOnly content: appetite guidance, source, confidence, and notes.
- ExternalVisible content: none in F0028.

## Non-Functional Expectations

- Security: appetite notes are internal and require explicit read/manage policy.
- Reliability: stale notes remain visible with freshness state so users can decide whether to validate them.

## Dependencies

**Depends On:**
- F0028-S0002 — carrier/market profile exists.

**Related Stories:**
- F0028-S0001 — appetite freshness appears in directory filters.
- F0028-S0006 — appetite changes appear in activity history and related work context.

## Business Rules

1. **Recorded, never computed:** Appetite notes capture human-provided guidance; Nebula does not calculate market fit in F0028.
2. **Freshness required:** Every appetite note must carry a next-review date.
3. **Source required:** Every appetite note must identify where the guidance came from.

## Out of Scope

- Automated appetite ingestion.
- Quote recommendation, rating, or scoring.
- External sharing of appetite notes.

## UI/UX Notes

- Screens involved: Market Relationship Workspace appetite tab/panel.
- Key interactions: add note, edit note, view freshness state, filter stale/current notes.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Freshness state is derived from next-review date in Phase B; exact thresholds are architecture/product-contract detail.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0028-S0004-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
