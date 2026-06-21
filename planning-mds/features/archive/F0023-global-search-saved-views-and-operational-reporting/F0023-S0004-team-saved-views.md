# F0023-S0004: Team saved views and defaults

**Story ID:** F0023-S0004
**Feature:** F0023 - Global Search, Saved Views & Operational Reporting
**Title:** Team saved views and defaults
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution Operations Manager
**I want** to publish team saved views and mark shared defaults
**So that** my team starts daily work from consistent search and report criteria

## Context & Background

Managers need reusable team views for due work, backlog, owner workload, and common search filters. Team views must be governed because shared defaults become operational artifacts.

## Acceptance Criteria

**Happy Path:**
- **Given** I have manager/admin permission
- **When** I publish a saved view to a team scope with a name and description
- **Then** eligible team members see the view in their team saved-view list
- **And** applying the view restores criteria but still filters records by each user's authorization
- **And** marking a team view as default makes it the initial team view for eligible users
- **And** the publish/default action records saved-view audit/timeline evidence or an equivalent immutable administrative audit record

**Alternative Flows / Edge Cases:**
- User lacks team-view management permission -> show forbidden and do not mutate.
- Team scope is missing -> show "Team scope is required" and do not publish.
- Another manager has updated the view -> show conflict and require reload before saving.
- A team member lacks access to some records under the view -> show only authorized results and counts.

**Checklist:**
- [ ] Manager/admin can create, rename, update, archive/delete, and default a team view.
- [ ] Non-manager users can apply team views but cannot mutate them.
- [ ] Default view changes are visible after reload.
- [ ] Team saved views include owner, team scope, description, and last-updated metadata.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Saved Views Drawer -> Publish to Team | Publish view | Enabled for manager/admin roles | Creates team-scoped saved view | Eligible team users see it after reload | Distribution manager, Program manager, Admin |
| Saved Views Drawer -> Team View Settings | Rename/update/default/archive | Enabled for manager/admin roles | Updates metadata, criteria, default flag, or archived state | Changed team view/default persists after reload | Distribution manager, Program manager, Admin |

Required checks for mutation stories:
- [ ] Render-only behavior cannot satisfy the story.
- [ ] Save path has validation and error behavior specified.
- [ ] Successful mutations create audit/timeline evidence or equivalent saved-view administrative audit.
- [ ] Tests prove the user can mutate from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- View name, team scope, owner user ID, criteria JSON, view type, sharing scope, default flag, created/updated timestamps.

**Optional Fields:**
- Description, archived timestamp, last editor.

**Validation Rules:**
- Team view name is required, trimmed, and maximum 80 characters.
- Team view names are unique per team scope.
- Only one default team view exists per team and view type.

## Role-Based Visibility

**Roles that can manage team saved views:**
- Distribution manager, Program manager, Admin - may manage team views within their scope.

**Roles that can apply team saved views:**
- Distribution user, Distribution manager, Relationship manager, Program manager, Underwriter, Admin - may apply team views available to their team/scope.

**Data Visibility:**
- Team views do not grant record access.
- Applying a team view reruns authorization for the current user.

## Non-Functional Expectations

- Performance: team saved-view metadata loads in bounded pages or a bounded list.
- Security: unauthorized users cannot infer hidden team scopes through team-view names or errors.
- Reliability: conflicting updates do not silently overwrite another manager's changes.

## Dependencies

**Depends On:**
- F0023-S0003 - saved-view criteria model.

**Related Stories:**
- F0023-S0005 - team due-work report defaults.
- F0023-S0007 - permission-safe shared-view behavior.

## Business Rules

1. Team saved views are reusable criteria, not materialized record lists.
2. A team view default affects eligible users only; users retain personal views separately.
3. Team-view changes are auditable administrative actions.

## Out of Scope

- External broker/MGA shared views.
- Scheduled report delivery.
- Cross-team global defaults beyond the manager/admin's authorized scope.

## UI/UX Notes

- Screens involved: Saved Views Drawer, Search Results Workspace, Operational Reports Workspace.
- Key interactions: publish, edit, set default, archive/delete, apply.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- "Team" maps to an internal operational scope available from user profile/authorization context; exact model is Phase B design.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0023-S0004-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
