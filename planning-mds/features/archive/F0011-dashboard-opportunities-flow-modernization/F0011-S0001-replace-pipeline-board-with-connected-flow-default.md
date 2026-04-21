# F0011-S0001: Replace Pipeline Board Tiles with Connected Flow-First Canvas Default

**Story ID:** F0011-S0001
**Feature:** F0011 — Dashboard Opportunities Flow-First Modernization (Connected Pipeline + Terminal Outcomes)
**Title:** Replace Pipeline Board tiles with connected flow-first canvas default
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution User or Underwriter
**I want** the Opportunities widget to default to a connected left-to-right flow canvas
**So that** I can understand stage progression and bottlenecks in one scan

## Context & Background

F0010 made Pipeline Board the default and improved clarity versus Sankey. Feedback from the latest dashboard review still shows a disconnected tile pattern, which slows flow comprehension compared with modern connected timeline/flow layouts.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated internal user opens Dashboard
- **When** the Opportunities widget loads
- **Then** the default view is a connected flow canvas (not disconnected stage tiles)
- **And** both submissions and renewals render as left-to-right stage sequences
- **And** stage nodes show status label and count
- **And** the 30d/90d/180d/365d selector updates all rendered stage counts

**Interaction + Permission:**
- **Given** the user can read opportunities data
- **When** they select a stage node
- **Then** a stage drilldown opens with ABAC-scoped mini-cards
- **And** unauthorized users do not receive out-of-scope records

**Alternative Flows / Edge Cases:**
- No open opportunities in selected period -> show an explicit empty-flow state while keeping controls active.
- Partial data failure for one entity type -> show non-blocking inline error for that section and keep the other section usable.
- Unknown or unmapped stage from backend -> show fallback label and continue rendering remaining stages.
- Read-only guard -> this story does not create, update, delete, or transition domain records; audit/timeline mutation events are not required.

**Checklist:**
- [ ] Connected flow canvas renders as default opportunities view
- [ ] Left-to-right stage ordering is deterministic
- [ ] Period switching updates flow counts
- [ ] Stage drilldowns remain available
- [ ] Empty/error/loading states are defined

## Data Requirements

**Required Fields:**
- EntityType (`submission` or `renewal`)
- Stage key
- Stage display label
- Stage display order
- Open count by stage

**Optional Fields:**
- Stage emphasis hint (`normal`, `active`, `blocked`) when available

**Validation Rules:**
- Stage counts are non-negative integers.
- Stage ordering follows backend-provided sequence.
- Scope filtering is applied before aggregate counts are returned.

## Role-Based Visibility

**Roles that can view Opportunities flow canvas:**
- DistributionUser
- DistributionManager
- Underwriter
- RelationshipManager
- ProgramManager
- Admin

**Data Visibility:**
- Opportunities stage aggregates and drilldowns are InternalOnly and ABAC scoped.

## Non-Functional Expectations

- Performance: opportunities flow payload and first render p95 < 500ms after dashboard frame render.
- Security: authorization behavior remains aligned with existing dashboard opportunities policies.
- Reliability: one flow subsection failure does not block the entire opportunities widget.

## Dependencies

**Depends On:**
- F0010 opportunities baseline components and period controls
- Existing opportunities summary and drilldown contracts

**Related Stories:**
- F0011-S0002 — Add terminal outcomes rail and outcome drilldowns
- F0011-S0003 — Apply modern opportunities visual system

## Out of Scope

- Custom user-defined stage ordering
- Workflow state taxonomy changes
- Export/print views

## Questions & Assumptions

**Open Questions:**
- [ ] Should the API provide explicit stage emphasis hints (`active`/`blocked`) or should UI derive them from aging thresholds?

**Assumptions (to be validated):**
- Existing opportunities aggregate endpoints can be extended without schema migration.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged: N/A (read-only view)
- [ ] Tests pass
- [ ] Documentation updated
