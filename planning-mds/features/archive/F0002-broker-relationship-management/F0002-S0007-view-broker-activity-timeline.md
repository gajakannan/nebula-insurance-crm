# F0002-S0007: View Broker Activity Timeline

**Story ID:** F0002-S0007
**Feature:** F0002 — Broker & MGA Relationship Management
**Title:** View broker activity timeline in Broker 360
**Priority:** High
**Phase:** MVP

## User Story

**As a** Relationship Manager or Distribution Manager
**I want** to view a broker's activity timeline
**So that** I can see a complete, chronological history of broker-related events

## Context & Background

The dashboard feed provides recent broker events, but relationship work requires a full broker timeline within Broker 360 for auditability and context.

## Acceptance Criteria

- **Given** I have `timeline:read` permission and the broker is within my authorization scope
- **When** I open the timeline panel in Broker 360
- **Then** I see a chronological list of broker-related ActivityTimelineEvents for that broker, newest first

- **Given** timeline events exist
- **When** they are displayed
- **Then** each item shows event description, actor display name, and timestamp

- **Given** no timeline events exist for this broker
- **When** I open the timeline panel
- **Then** I see an empty-state message

- **Given** I am not authorized to view the broker
- **When** I attempt to view the timeline
- **Then** access is denied with a 403 response

- Edge case: actor no longer exists → display actor as "Unknown User"

## Data Requirements

**Required Fields (per timeline item):**
- EventType
- EventDescription
- ActorDisplayName
- OccurredAt
- EntityId (Broker ID)

**Pagination:**
- Default page size: 50 events per request
- Client requests additional pages via `page` parameter; response includes `totalCount` and `totalPages`
- UI renders a "Load more" control or page navigation when more events exist beyond the first page
- Infinite scroll is not required for MVP; standard pagination is sufficient

**Validation Rules:**
- Events must be scoped to the requested broker
- Events are immutable and read-only

## Role-Based Visibility

**Roles that can view broker timelines:**
- DistributionUser — scoped read
- DistributionManager — region-scoped read
- RelationshipManager — scoped read
- Underwriter — read-only access to broker context
- ProgramManager — scoped read
- Admin — unscoped read

**Data Visibility:**
- InternalOnly timeline events only; no external access in MVP

## Non-Functional Expectations

- Performance: timeline panel initial page (50 events) renders within p95 < 500ms (consistent with all other F0002 story targets)
- Security: ABAC scope enforced on timeline reads
- Reliability: timeline read failures do not corrupt broker data

## Dependencies

**Depends On:**
- ActivityTimelineEvent entity
- F0002-S0003 - Read Broker (Broker 360 View)

**Related Stories:**
- F0001-S0004 - View Broker Activity Feed

## Out of Scope

- Real-time push updates
- Timeline edit/delete
- Cross-entity timeline aggregation beyond broker context

## Questions & Assumptions

**Assumptions (to be validated):**
- Timeline events include broker and contact mutations for this broker

## Definition of Done

- [x] Acceptance criteria met
- [x] Edge cases handled
- [x] Permissions enforced
- [x] Audit/timeline logged: N/A (read-only)
- [ ] Tests pass
