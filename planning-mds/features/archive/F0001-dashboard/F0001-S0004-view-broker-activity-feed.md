# F0001-S0004: View Broker Activity Feed

**Story ID:** F0001-S0004
**Feature:** F0001 — Dashboard
**Title:** View Broker Activity Feed
**Priority:** High
**Phase:** MVP

## User Story

**As a** Relationship Manager or Distribution User
**I want** to see recent broker-related activity on the dashboard
**So that** I can stay informed about relationship changes, new brokers, and recent interactions without checking each broker individually.

## Context & Background

Broker relationships are the lifeblood of commercial P&C distribution. Currently, staying informed about broker activity requires visiting individual broker records or relying on colleagues to share updates. A centralized activity feed on the dashboard provides ambient awareness of the most recent broker events, enabling proactive relationship management.

## Acceptance Criteria

**Happy Path:**
- **Given** the user is authenticated and on the Dashboard
- **When** the dashboard loads
- **Then** the Broker Activity Feed widget displays:
  - The 20 most recent ActivityTimelineEvents where EntityType = "Broker", scoped to user's authorization
  - Each feed item shows: event description, broker name, actor display name, and relative timestamp (e.g., "2 hours ago")
  - Items are sorted by OccurredAt descending (most recent first)

**Click-Through:**
- **Given** the user clicks a feed item
- **When** the click is processed
- **Then** the user is navigated to the Broker 360 view for that broker

**Edge Cases:**
- No broker activity exists → Display: "No recent broker activity."
- User's authorization scope limits visible brokers → Feed shows only events for authorized brokers
- Actor no longer exists (deactivated user) → Display actor as "Unknown User"
- Multiple events for the same broker in quick succession → Display each as a separate feed item (no grouping in MVP)

**Checklist:**
- [x] Feed shows up to 20 most recent broker timeline events
- [x] Events are scoped to user's authorization
- [x] Each item shows: event description, broker name, actor, relative timestamp
- [x] Sorted by OccurredAt descending
- [x] Click navigates to Broker 360
- [x] Empty state shows "No recent broker activity."
- [x] Widget loads within the overall dashboard p95 < 2s target

## Data Requirements

**Required Fields (per feed item):**
- EventType: string (e.g., "BrokerCreated", "BrokerUpdated", "ContactAdded")
- EventDescription: string (pre-rendered at write time and stored on the event record; templates defined in [activity-event-payloads.schema.json](../../schemas/activity-event-payloads.schema.json) — feed query reads the stored string, no query-time resolution needed)
- BrokerName: string (resolved from EntityId)
- ActorDisplayName: string (resolved from ActorUserId via UserProfile)
- OccurredAt: datetime
- EntityId: uuid (Broker ID, for navigation)

**Validation Rules:**
- Only events where EntityType = "Broker" are included
- Maximum 20 items per load
- OccurredAt must be a valid datetime

## Role-Based Visibility

**Roles that can view Broker Activity Feed:**
- Distribution User — sees events for brokers within their authorization scope
- Relationship Manager — sees events for brokers they manage
- Program Manager — sees events for brokers within their programs
- Underwriter — sees events for brokers linked to their accessible submissions
- Admin — sees all broker events (unscoped)

**Data Visibility:**
- Only InternalOnly timeline events are shown. Events flagged as BrokerVisible are included (they are a subset of internal events). No external-user-only content exists in MVP.

## Non-Functional Expectations

- Performance: Widget must render within the overall dashboard p95 < 2s target; query against ActivityTimelineEvent must use an index on (EntityType, OccurredAt DESC)
- Security: Backend must enforce Casbin ABAC scope to filter events by authorized broker IDs
- Reliability: If query fails, display "Unable to load activity feed" and log the error; do not block other widgets

## Dependencies

**Depends On:**
- ActivityTimelineEvent entity (append-only timeline table)
- Broker entity (for resolving broker name from EntityId)
- UserProfile entity (for resolving actor display name from ActorUserId)
- Broker 360 screen (for click-through navigation)

**Related Stories:**
- F0002 — Broker & MGA Relationship Management (source of broker events)
- F0001-S0001 — KPI Cards (complementary dashboard widget)
- F0001-S0007 (F0002) — View Broker Activity Timeline (full timeline on Broker 360)

## Out of Scope

- Filtering or searching within the feed widget
- Pagination or "load more" within the widget (fixed at 20 items)
- Grouping events by broker or by time period
- Activity for non-broker entities (accounts, submissions, renewals)
- Real-time push updates (MVP uses page-load fetch)

## UI/UX Notes

- Screens involved: Dashboard
- Layout: Vertical feed/list, typically positioned as the rightmost or bottom widget
- Each feed item: icon (based on EventType), description text, broker name as link, actor name, relative timestamp
- Compact design; no expandable detail within the widget

## Questions & Assumptions

**Assumptions (confirmed):**
- EventDescription is pre-rendered at write time and stored alongside EventPayloadJson. Description templates are defined in [activity-event-payloads.schema.json](../../schemas/activity-event-payloads.schema.json). The feed query reads the stored string — no query-time template resolution needed.
- Relative timestamps (e.g., "2 hours ago") are computed client-side from OccurredAt
- 20 items is sufficient for the feed widget; users wanting full history use Broker 360 timeline
- Contact events (ContactCreated, ContactUpdated, ContactDeleted) use EntityType="Broker" so they appear in the broker activity feed. The Contact's ID is in the event payload, not the EntityId field.

## Definition of Done

- [x] Acceptance criteria met
- [x] Edge cases handled (empty feed, restricted scope, unknown actor, query failure)
- [x] Permissions enforced (Casbin ABAC scope on broker entity)
- [x] Audit/timeline logged: N/A (read-only)
- [x] Tests pass (unit test for event rendering, integration test for scoped query)
- [x] Click-through to Broker 360 works
- [x] Accessible: feed items use semantic list markup with proper ARIA
