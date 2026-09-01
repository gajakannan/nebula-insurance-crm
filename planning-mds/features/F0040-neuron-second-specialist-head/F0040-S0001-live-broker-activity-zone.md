---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0040-S0001 — Live Broker Activity Day-at-a-Glance Zone

## Story Header

**Story ID:** F0040-S0001
**Feature:** F0040 — Neuron Second Specialist Head (Broker Activity)
**Title:** Live Broker activity Day-at-a-Glance zone with Broker 360 drill-through
**Priority:** High
**Phase:** MVP

## User Story

**As a** Relationship Manager or Distribution user
**I want** the Broker activity zone to show the newest broker events I am allowed to see
**So that** I can stay aware of relationship changes and open the relevant broker without leaving my current workflow

## Context & Background

F0038 registered Broker activity as a visible, inert zone. F0040 activates that exact slot using the already-delivered F0001-S0004 broker-feed behavior: newest 20 authorized broker events, stored descriptions, actor/time context, and Broker 360 click-through. The story is read-only and must not create a parallel definition of "broker activity."

## Acceptance Criteria

**Happy Path:**
- **Given** an eligible authenticated user opens Neuron's Day at a Glance
- **When** the Broker activity head completes its read
- **Then** the `broker_activity` zone has status `content`, carries a registered Broker activity component, and is labelled `LIVE` rather than `not yet active`
- **And** the component shows at most 20 `ActivityTimelineEvent` records with `EntityType = "Broker"` that the engine authorizes for that user
- **And** items are ordered by `OccurredAt` descending
- **And** every item shows event type, stored event description, broker name, actor display name, and relative timestamp

**Click-Through:**
- **Given** a rendered event has a broker identity
- **When** the user selects its broker name
- **Then** the existing Broker 360 route for that broker opens

**Alternative Flows / Edge Cases:**
- No authorized broker events exist → return zone status `empty` and show `No recent broker activity.`; do not render a blank list.
- Actor profile no longer resolves → show `Unknown User` while retaining the event.
- The engine read fails or times out → return zone status `error`, show `Unable to load broker activity.` with a retry affordance, and leave every other zone rendered.
- The session is expired or invalid → show the existing authentication-required state and no broker event data.
- Authorization excludes a broker → omit every event for that broker; do not reveal counts, names, descriptions, or identifiers for filtered records.
- Multiple events target the same broker → render each event separately; do not group them.

## Interaction Contract

N/A — read-only story. Selecting a broker navigates to the existing Broker 360 read surface and performs no CRM mutation.

## Data Requirements

**Required Fields:**
- `eventType`: existing broker event type.
- `eventDescription`: stored human-readable description created with the source event.
- `entityId` and `entityName`: Broker identity and name for navigation.
- `actorDisplayName`: actor name, with `Unknown User` fallback.
- `occurredAt`: valid occurrence timestamp used for descending order and relative time.

**Optional Fields:**
- Existing event payload fields may pass through the engine contract only when the registered component schema requires them; they are not displayed or used to broaden scope in this story.

**Validation Rules:**
- `EntityType` equals `Broker`.
- Maximum result size is 20.
- Results are ordered newest first.
- The engine applies existing Casbin ABAC before Neuron receives records.
- Only a registry-known component with schema-valid props renders.

## Role-Based Visibility

**Roles that can view:**
- `DistributionUser` — events for brokers in existing scope.
- `DistributionManager` — events in existing region/hierarchy scope.
- `RelationshipManager` — events for brokers managed or otherwise visible under existing policy.
- `ProgramManager` — events for brokers within authorized programs.
- `Underwriter` — events for brokers linked to accessible work.
- `Admin` — existing administrative scope.
- `BrokerUser` / external user — no access.

**Data Visibility:**
- InternalOnly content: all rendered broker activity and navigation context.
- ExternalVisible content: none.

## Non-Functional Expectations

- Performance: the zone reaches `content`, `empty`, or `error` within p95 < 2 seconds.
- Security: the user token is forwarded to the engine; Neuron neither recreates nor broadens broker authorization.
- Reliability: an unavailable Broker activity head cannot blank, delay, or change the result of another zone.
- Accessibility: the zone has an accessible name, status is conveyed without color alone, and every Broker 360 link has a usable text label.

## Dependencies

**Depends On:**
- F0038-S0002 — Day-at-a-Glance zone dispatch and registered-component envelope.
- F0038-S0004 — existing Broker activity stub identity and inactive fallback.
- F0001-S0004 — broker-feed business contract.
- F0002-S0007 — Broker 360 timeline and authorization behavior.

**Related Stories:**
- F0040-S0002 — exposes the same live read through a direct conversation request.
- F0040-S0003 — hardens two-head dispatch and failure isolation.

## Business Rules

1. **Existing feed semantics win:** F0001-S0004 defines the result limit, ordering, fields, supported roles, and edge behavior.
2. **Stored descriptions only:** Neuron renders the existing `EventDescription`; it does not generate or reinterpret event facts.
3. **Read-only:** the zone exposes no create, edit, delete, assign, follow-up, or approve action.
4. **Assembly only:** Broker activity does not read, score, merge, or reorder Renewals content.

## Out of Scope

- Filters, search, pagination, load-more, grouping, or live push updates.
- Broker summaries, recommendations, next-best-action, or follow-up drafting.
- Any broker/contact/timeline mutation.

## UI/UX Notes

- Screens involved: Neuron panel — Day-at-a-Glance `broker_activity` slot; existing Broker 360 destination.
- Key interactions: open glance → scan newest events → select broker name → open Broker 360; retry only when the zone is in `error`.

## Questions & Assumptions

**Open Questions:** None — G1 selected `broker_activity`, and F0001-S0004 supplies the existing product rules.

**Assumptions (validated from current product artifacts):**
- Broker timeline events, authorized feed reads, and Broker 360 navigation are already available.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Empty, unknown-actor, auth, timeout, and authorization-filter edge cases handled
- [ ] Existing engine authorization enforced with no Python-side substitute
- [ ] Audit/timeline logged — N/A; this story reads existing append-only events and performs no mutation
- [ ] Tests cover ordering, 20-item cap, fields, click-through, all role scopes, safe component validation, and zone failure isolation
- [ ] Accessibility and p95 < 2 second target validated
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
