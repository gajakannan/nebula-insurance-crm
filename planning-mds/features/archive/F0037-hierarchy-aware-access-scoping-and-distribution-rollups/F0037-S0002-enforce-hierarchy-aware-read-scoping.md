# Enforce hierarchy-aware read scoping

**Story ID:** F0037-S0002
**Feature:** F0037 - Hierarchy-Aware Access Scoping & Distribution Rollups
**Title:** Enforce hierarchy-aware read scoping
**Priority:** Critical
**Phase:** CRM Release MVP+

## User Story

**As a** compliance owner
**I want** distribution structure and source-record reads to enforce hierarchy-aware visibility
**So that** users cannot discover or open broker, producer, territory, submission, renewal, policy, task, or activity records outside their allowed scope.

## Context & Background

F0017 made hierarchy and ownership data available, while existing read paths still rely on broader role checks or projection visibility. This story applies the distribution scope from F0037-S0001 to direct and supporting read paths so hidden records are omitted before response materialization.

## Acceptance Criteria

**Happy Path:**
- **Given** an internal user with a resolved distribution scope
- **When** the user lists or opens distribution structure or source records
- **Then** only records within that resolved hierarchy, territory, producer, and source-record scope are returned.

**Alternative Flows / Edge Cases:**
- Direct access to a hidden record returns no-leak not-found behavior, preferably 404, unless authentication or broad resource authorization fails first.
- Sibling hierarchy branches are excluded from rows and counts.
- Expired or future ownership/territory links are excluded for the selected `asOf`.
- Empty scope returns an empty list or no-access state without exposing hidden counts.
- Admin can validate full-scope parity against unfiltered authorized records.

**Checklist:**
- [ ] Distribution-node, broker, producer ownership, territory, and source-record read predicates use the shared scope resolver.
- [ ] Hidden records are omitted before pagination, counts, and response DTO materialization.
- [ ] No direct-read endpoint leaks existence through status body, count, breadcrumb, related-link, or timing-sensitive messaging.
- [ ] Tests cover direct hidden-record access and sibling exclusion.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A - read-only story.

## Data Requirements

**Required Fields:**
- Scope object from F0037-S0001: Drives all filtering.
- Source record ids: Submission, renewal, policy, broker, task, activity, and insight source ids under evaluation.
- Visibility predicate inputs: Broker id, distribution node id, territory id, producer user id, owner user id, region, and `asOf`.

**Optional Fields:**
- Drilldown source type: Allows consistent filtering across related-record navigation.
- Explanation code: Supports test diagnostics without exposing hidden records to users.

**Validation Rules:**
- Missing scope means no visible records.
- Pagination and total counts must be computed after scope filtering.
- Related-record expansion must reapply scope for each expanded type.

## Role-Based Visibility

**Roles that can read scoped internal records:**
- Admin - Full internal scope.
- ProgramManager - Program/hierarchy/territory scoped records.
- DistributionManager - Managed subtree and territory scoped records.
- RelationshipManager - Assigned broker, producer, and territory scoped records.
- DistributionUser - Assigned source-record scoped records.
- Underwriter - Assigned workload and source-record scoped records.
- ServiceUser - Assigned service/workflow source-record scoped records.

**Data Visibility:**
- InternalOnly content: All source-record and hierarchy detail.
- ExternalVisible content: None through F0037 internal read-scoping surfaces.

## Non-Functional Expectations

- Performance: Filtering should be expressed in repository/database predicates where possible, not by post-filtering large result sets.
- Security: No-leak behavior must be consistent across list and direct-detail reads.
- Reliability: Tests must prove predicates run before pagination, counts, and related links.

## Dependencies

**Depends On:**
- F0037-S0001 - Shared scope resolver.
- F0017 - Effective-dated hierarchy, producer ownership, and territory data.

**Related Stories:**
- F0037-S0003 - Applies the same no-leak behavior to search/report/insight projections.
- F0037-S0006 - Requires security evidence for no-leak enforcement.

## Business Rules

1. **Predicate first:** Visibility predicates run before rows, counts, facets, suggestions, drilldowns, related links, and DTOs.
2. **404 for hidden detail:** Hidden direct records should behave as not found unless the caller lacks broad resource permission.
3. **No count leaks:** Empty or filtered-away states must not disclose the number of hidden records.

## Out of Scope

- Creating new source-record ownership models.
- External broker portal read behavior.
- Bulk export or data warehouse controls.

## UI/UX Notes

- Screens involved: Existing distribution, broker, source-record, and related-record detail/list surfaces.
- Key interactions: Hidden records simply disappear from lists; direct hidden links produce no-leak not-found behavior.

## Questions & Assumptions

**Open Questions:**
- [ ] Phase B must identify the exact endpoint and repository list affected by direct source-record read scoping.

**Assumptions (to be validated):**
- Existing source records already carry enough broker, territory, owner, producer, region, or projection metadata to apply the scope without data migration beyond Phase B deltas.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A - read-only enforcement; security evidence captures behavior)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F{NNNN}-S{NNNN}-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
