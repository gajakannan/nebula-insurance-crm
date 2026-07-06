# F0028-S0002: Carrier and market profile management

**Story ID:** F0028-S0002
**Feature:** F0028 — Carrier & Market Relationship Management
**Title:** Carrier and market profile management
**Priority:** Critical
**Phase:** CRM Release MVP+

## User Story

**As a** relationship manager
**I want** to create and update carrier and market profile records
**So that** Nebula has a reliable source of market relationship context

## Context & Background

Carrier/market profiles are the parent records for contacts, appetite notes, appointments, activity, and related work. F0028 stores recorded relationship facts only; it does not synchronize with carrier systems.

## Acceptance Criteria

**Happy Path:**
- **Given** I am authorized to manage market profiles
- **When** I create or update a carrier/market profile with required fields
- **Then** the profile is saved
- **And** reloading the workspace shows the saved name, type, status, owner, segments, geography, and notes
- **And** an activity/timeline event records the create or update

**Alternative Flows / Edge Cases:**
- Missing name, type, or status -> rejected with field-level validation.
- Duplicate active profile name and market type -> rejected or requires Phase B duplicate resolution contract.
- Unauthorized actor attempts create/update -> `403 Forbidden`.
- Archived profile update attempted -> rejected unless Phase B defines a reactivate path.

**Checklist:**
- [ ] Profile status values are explicit and not free text.
- [ ] Profile owner is visible on the workspace.
- [ ] Profile changes survive reload/query invalidation.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Market Directory → New Market | Enter required fields and Save | Enabled for authorized internal users | New market profile created | Reload directory and workspace show the new profile | Distribution leader/manager, relationship manager, admin |
| Market Workspace → Edit profile | Update profile fields and Save | Enabled for active/monitoring profiles | Profile fields updated and audit event appended | Reload workspace shows saved values and timeline event | Distribution leader/manager, relationship manager, admin; archived profiles read-only |

Required checks:
- [ ] Render-only behavior cannot satisfy this story.
- [ ] Save path has validation and error behavior.
- [ ] Successful mutations append audit/timeline events.
- [ ] Tests prove create/update from named entry points and persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Name: carrier or market display name.
- Market type: admitted carrier, non-admitted carrier, MGA/program market, or wholesaler market.
- Status: active, inactive, monitoring, or archived.
- Owner: internal user/team responsible for relationship maintenance.

**Optional Fields:**
- Market segments, lines of business, geography, website, NAIC or market reference identifier, notes.

**Validation Rules:**
- Name is required and trimmed.
- Type and status must be selected from controlled values.
- Archived records are read-only unless a later story defines reactivation.

## Role-Based Visibility

**Roles that can create/update:**
- Distribution leader, distribution manager, relationship manager, admin — permitted within ABAC scope.
- Underwriter, program manager — read-only unless Phase B grants manage permission.

**Data Visibility:**
- InternalOnly content: profile metadata, owner, notes, relationship status.
- ExternalVisible content: none in F0028.

## Non-Functional Expectations

- Security: unauthorized create/update returns `403`.
- Reliability: save, audit/timeline append, and search-index update are handled as one consistent mutation boundary defined in Phase B.

## Dependencies

**Depends On:**
- F0028-S0001 — directory entry point.

**Related Stories:**
- F0028-S0003 — contacts attach to profiles.
- F0028-S0004 — appetite notes attach to profiles.
- F0028-S0005 — appointments attach to profiles.

## Business Rules

1. **CRM-side record:** the profile is manually maintained CRM context, not a carrier-system mirror.
2. **Recorded facts only:** F0028 stores classifications and relationship fields; it does not compute market fit.

## Out of Scope

- Carrier system synchronization.
- Automated duplicate merge.
- Rating, pricing, or recommendation logic.

## UI/UX Notes

- Screens involved: Market Directory, Market Relationship Workspace profile panel.
- Key interactions: new profile, edit profile, save, cancel, view audit.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Profile duplicate handling can be resolved in Phase B as reject-with-message or controlled duplicate warning.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0028-S0002-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
