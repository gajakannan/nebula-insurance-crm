# F0023-S0007: Permission-safe search and reporting behavior

**Story ID:** F0023-S0007
**Feature:** F0023 - Global Search, Saved Views & Operational Reporting
**Title:** Permission-safe search and reporting behavior
**Priority:** Critical
**Phase:** MVP

## User Story

**As a** CRM user
**I want** search, saved views, and reports to respect my record permissions
**So that** I only see data I am allowed to use and cannot infer hidden records

## Context & Background

Search and reporting aggregate across many source objects. F0023 must treat authorization as a core product requirement, not a UI-only filter, because counts, snippets, and suggestions can leak hidden data.

## Acceptance Criteria

**Happy Path:**
- **Given** I search, apply a saved view, or open an operational report
- **When** matching records include both authorized and unauthorized records
- **Then** I see only authorized rows, snippets, suggestions, groups, counts, and drilldowns
- **And** applying the same saved view as another user returns results based on that user's authorization

**Alternative Flows / Edge Cases:**
- All matches are unauthorized -> show the same no-results state used when no authorized records match.
- A saved view references a team scope I can no longer access -> hide or disable the view with a no-access message.
- A source record becomes unauthorized after the result list loads -> source navigation returns forbidden without exposing additional details.
- A report has partially unauthorized source types -> include only authorized source types and show no hidden-source count.

**Checklist:**
- [ ] Authorization is enforced before search/report data leaves the server.
- [ ] Counts and suggestions use only authorized records.
- [ ] Saved views store criteria, not elevated access.
- [ ] Security Reviewer is required for feature closeout because search/reporting crosses visibility boundaries.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A - read-only/security behavior story. The story validates authorization behavior across search, saved views, and reports.

## Data Requirements

**Required Fields:**
- Current user identity and roles from authenticated session.
- Source object type and source record access attributes.
- Criteria for search, saved view, or report request.

**Optional Fields:**
- Team scope, region, line of business, owner, workflow state.

**Validation Rules:**
- Requests without authenticated identity are rejected.
- Criteria that reference unauthorized team scopes or objects return no-access behavior without hidden data.

## Role-Based Visibility

**Roles that can use permission-safe search/reporting:**
- Distribution user, Distribution manager, Relationship manager, Program manager, Underwriter, Admin - results depend on authorization scope.

**Data Visibility:**
- InternalOnly content never appears to unauthorized users.
- External broker/MGA users are out of scope for F0023 global search/reporting.
- Authorization applies to rows, snippets, suggestions, counts, saved-view previews, report summaries, and drilldowns.

## Non-Functional Expectations

- Performance: authorization filtering must scale with result and report workloads defined in Phase B.
- Security: no hidden-record existence leakage through counts, suggestions, timing-specific messaging, or saved-view metadata.
- Reliability: permission changes take effect on the next search/report/saved-view application.

## Dependencies

**Depends On:**
- F0009 - authenticated identity and role-based login.
- Existing Casbin ABAC policy boundaries for source objects.
- F0023-S0001 through F0023-S0006 - all search/report surfaces.

**Related Stories:**
- All F0023 stories.

## Business Rules

1. Saved views never grant access. They only reapply criteria under current permissions.
2. Unauthorized matches are indistinguishable from non-matches in user-facing result states.
3. Aggregate counts are computed after authorization filtering.

## Out of Scope

- Introducing hierarchy-aware access-control enforcement; F0037 owns that scope.
- External broker/MGA global search.
- Replacing source feature authorization policies.

## UI/UX Notes

- Screens involved: Global Search Overlay, Search Results Workspace, Saved Views Drawer, Operational Reports Workspace.
- Key interactions: search, apply view, inspect report, open source record after permission changes.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Phase B will decide whether additional policy rules are needed for cross-object search/reporting or whether existing source-object policies compose directly.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A - read-only/security behavior; saved-view mutations are audited in F0023-S0003 and F0023-S0004)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0023-S0007-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
