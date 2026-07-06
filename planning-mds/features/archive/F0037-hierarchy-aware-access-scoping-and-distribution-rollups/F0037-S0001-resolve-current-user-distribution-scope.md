# Resolve current user distribution scope

**Story ID:** F0037-S0001
**Feature:** F0037 - Hierarchy-Aware Access Scoping & Distribution Rollups
**Title:** Resolve current user distribution scope
**Priority:** Critical
**Phase:** CRM Release MVP+

## User Story

**As a** security-conscious distribution operations manager
**I want** the CRM to resolve my current distribution scope from hierarchy, territory, producer ownership, role, and as-of date
**So that** every downstream search, report, insight, and rollup uses one consistent visibility boundary.

## Context & Background

F0017 delivered the structural broker/MGA hierarchy, effective-dated producer ownership, and territory assignments. F0023 delivered projection-based search and reporting. F0037 needs a shared scope resolver so each surface uses the same user-specific visibility contract instead of duplicating role and hierarchy logic.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated internal user with role, hierarchy, territory, producer ownership, and source-record permissions
- **When** a search, report, insight, rollup, or direct-read workflow requests distribution scope for an `asOf` date
- **Then** the system returns a scope object containing allowed distribution node ids, broker ids, territory ids, producer user ids, source-record predicates, role context, `asOf`, and an explanation code suitable for audit and test assertions.

**Alternative Flows / Edge Cases:**
- Admin users resolve full internal distribution scope for the selected `asOf` date.
- ProgramManager and DistributionManager users resolve only configured hierarchy, program, territory, or delegated authority scope.
- RelationshipManager, DistributionUser, Underwriter, and ServiceUser users resolve only owned, assigned, territory, or source-record-permitted scope.
- BrokerUser and ExternalUser users resolve no internal F0037 scope by default.
- Missing or future-dated assignments are ignored unless effective on the selected `asOf` date.
- Invalid `asOf` input returns validation failure without falling back to broad access.

**Checklist:**
- [ ] Scope resolution is implemented once and reused by affected read paths.
- [ ] The default `asOf` date is the operator's current business date.
- [ ] Scope results are deterministic for the same user, role claims, and `asOf`.
- [ ] Tests cover Admin full scope, manager subtree scope, territory scope, producer scope, sibling exclusion, external denial, and expired/future effective dates.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A - read-only story.

## Data Requirements

**Required Fields:**
- User id: Identifies the principal requesting scope.
- Role claims: Determine baseline role authority.
- Distribution node ids: Identify visible hierarchy branches.
- Broker ids: Identify visible broker records.
- Territory ids: Identify visible territory assignments.
- Producer user ids: Identify visible producer ownership.
- As-of date: Evaluates effective-dated hierarchy, ownership, and territory records.

**Optional Fields:**
- Program id: Narrows scope when program-specific authority exists.
- Region: Preserves existing projection visibility rules where applicable.
- Explanation code: Supports audit, tests, and support diagnostics without exposing hidden records.

**Validation Rules:**
- `asOf` must be a valid date.
- Empty scope is a valid result and must not be widened automatically.
- External roles must not receive internal hierarchy scope unless a later approved gate changes scope.

## Role-Based Visibility

**Roles that can resolve internal distribution scope:**
- Admin - Full internal scope.
- ProgramManager - Program/hierarchy/territory scope approved by policy.
- DistributionManager - Managed subtree, territory, and delegated authority scope.
- RelationshipManager - Owned/assigned producer and broker relationship scope.
- DistributionUser - Assigned source-record and territory scope.
- Underwriter - Source-record and assigned workload scope.
- ServiceUser - Assigned service/workflow source-record scope.

**Data Visibility:**
- InternalOnly content: Scope internals, hidden-record predicates, explanation codes, and all rollup inputs.
- ExternalVisible content: None for F0037 internal rollup/search/report scope by default.

## Non-Functional Expectations

- Performance: Scope resolution must be efficient enough to run on every affected read path without creating visible report/search latency regressions.
- Security: Scope must fail closed on missing role, invalid user, invalid date, or resolver errors.
- Reliability: Scope resolution must be testable independently from each consuming surface.

## Dependencies

**Depends On:**
- F0017 - Hierarchy, producer ownership, territory assignment, and effective dating.
- F0023 - Projection visibility consumers that will call the resolver.

**Related Stories:**
- F0037-S0002 - Applies the resolved scope to distribution/source reads.
- F0037-S0003 - Applies the resolved scope to search, saved views, insights, and reports.
- F0037-S0004 - Applies the resolved scope to rollups.

## Business Rules

1. **Fail closed:** Any unresolved or invalid scope state returns empty/denied scope, not broad visibility.
2. **As-of consistency:** Every consuming surface must evaluate the same `asOf` date for one request.
3. **No external expansion:** BrokerUser and ExternalUser remain denied for internal scope unless Phase B explicitly approves an external-safe contract.

## Out of Scope

- Creating or editing hierarchy, producer ownership, or territory assignments.
- External portal visibility.
- New materialized rollup infrastructure.

## UI/UX Notes

- Screens involved: None directly; this is a backend application-service story.
- Key interactions: Affected UI surfaces receive consistent scoped results through later stories.

## Questions & Assumptions

**Open Questions:**
- [ ] Phase B must confirm whether ProgramManager scope is keyed by program, hierarchy node, territory, or a combination already present in policy data.

**Assumptions (to be validated):**
- Existing user role and assignment data is sufficient to resolve internal scope without adding a new user-management feature.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A - resolver returns explanation metadata but does not mutate business state)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F{NNNN}-S{NNNN}-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
