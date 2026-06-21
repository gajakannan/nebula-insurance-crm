# F0023-S0003: Personal saved views

**Story ID:** F0023-S0003
**Feature:** F0023 - Global Search, Saved Views & Operational Reporting
**Title:** Personal saved views
**Priority:** High
**Phase:** MVP

## User Story

**As a** Relationship Manager
**I want** to save and reuse my own search, list, and report criteria
**So that** I do not rebuild the same operational filters every session

## Context & Background

Repeated search and report filters are a daily workflow. Personal saved views make those criteria durable for the owner without changing team-wide defaults.

## Acceptance Criteria

**Happy Path:**
- **Given** I have active search or report criteria
- **When** I save a personal view with a name
- **Then** the view appears in my saved-view list
- **And** applying the view restores the saved criteria
- **And** reloading the browser keeps the view available to me
- **And** the create action records saved-view audit/timeline evidence or an equivalent immutable saved-view audit record

**Alternative Flows / Edge Cases:**
- Missing name -> show "View name is required" and do not save.
- Duplicate personal name -> show "A personal view with this name already exists" and do not overwrite without explicit update.
- Invalid or obsolete criteria -> apply the valid criteria, show which criteria were ignored, and keep the saved view editable.
- Unauthorized records under saved criteria -> omit unauthorized results and show only authorized counts.

**Checklist:**
- [ ] User can create, rename, update, delete, and apply a personal saved view.
- [ ] Personal saved views are visible only to the owner.
- [ ] Saved criteria include query, filters, sort, and report/view type where applicable.
- [ ] Deleting a personal view requires confirmation.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Search Results Workspace -> Save View | Save personal view | Enabled for authenticated internal users with search access | Creates saved criteria owned by current user | View appears after browser reload and restores criteria when applied | Owner only |
| Saved Views Drawer -> Rename/Update/Delete | Rename, update criteria, or delete | Enabled for owner; read-only for non-owner | Updates or deletes the saved view | Drawer reflects mutation after reload/query refresh | Owner only |

Required checks for mutation stories:
- [ ] Render-only behavior cannot satisfy the story.
- [ ] Save path has validation and error behavior specified.
- [ ] Successful mutations create audit/timeline evidence or an equivalent saved-view audit record.
- [ ] Tests prove the user can mutate from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- View name, owner user ID, scope `personal`, criteria JSON, view type, created/updated timestamps.

**Optional Fields:**
- Description, pinned/default-for-owner flag.

**Validation Rules:**
- Name is required, trimmed, and maximum 80 characters.
- Personal view names are unique per owner.
- Criteria are stored as structured fields, not only as opaque UI text.

## Role-Based Visibility

**Roles that can manage personal saved views:**
- Distribution user, Distribution manager, Relationship manager, Program manager, Underwriter, Admin - may manage their own personal views.

**Data Visibility:**
- Personal saved views do not grant record access.
- Applying a saved view reruns authorization against current user permissions.

## Non-Functional Expectations

- Performance: saved-view list loads without delaying initial shell render.
- Security: users cannot read, update, or delete another user's personal views.
- Reliability: update/delete conflicts return a user-safe message and leave the prior view intact.

## Dependencies

**Depends On:**
- F0023-S0001 - searchable result criteria.
- F0023-S0002 - filter and sort criteria.

**Related Stories:**
- F0023-S0004 - team saved views.
- F0023-S0007 - saved-view authorization behavior.

## Business Rules

1. Personal saved views are private to their owner.
2. Saved views store reusable criteria; they do not store a static list of records.
3. Applying a saved view always re-evaluates authorization and current data.

## Out of Scope

- Cross-user personal view impersonation.
- Report scheduling.
- Email delivery of saved views.

## UI/UX Notes

- Screens involved: Search Results Workspace, Operational Reports Workspace, Saved Views Drawer.
- Key interactions: save, apply, rename, update, delete.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- A personal default view is an owner preference, not a team-wide setting.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0023-S0003-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
