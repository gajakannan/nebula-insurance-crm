---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0016-S0006: Account Relationships — Broker of Record, Producer, Territory

**Story ID:** F0016-S0006
**Feature:** F0016 — Account 360 & Insured Management
**Title:** Account relationships (broker of record, producer, territory) with audited history
**Priority:** High
**Phase:** CRM Release MVP

## User Story

**As a** distribution manager
**I want** to assign and change an account's broker of record, primary producer, and territory with an audited history
**So that** ABAC scoping stays correct as books shift and I can prove when and by whom an account changed hands

## Context & Background

Broker-of-record, primary producer, and territory assignments drive ABAC scope for the account and all of its related submissions / renewals. Changes must be auditable and gated behind manager-level authority. This story covers the mutation endpoints and the `AccountRelationshipHistory` append-only log.

## Acceptance Criteria

**Happy Path:**
- **Given** a distribution manager with `account:update` in scope
- **When** they change the broker of record on an account
- **Then** the account row updates, a `AccountRelationshipHistory` row is written with previous value, new value, effectiveAt, actorUserId, and a timeline event `account.broker_of_record_changed` is appended

- **Given** a distribution manager changes primary producer
- **When** the change commits
- **Then** a relationship-history row (`RelationshipType=PrimaryProducer`) is appended and a `account.primary_producer_changed` timeline event is recorded

- **Given** a distribution manager changes territory
- **When** the change commits
- **Then** a relationship-history row (`RelationshipType=Territory`) is appended and a `account.territory_changed` timeline event is recorded

**Alternative Flows / Edge Cases:**
- New broker of record references an inactive / non-existent broker → 400
- New primary producer references a user without any distribution role → 400
- Territory change that moves the account outside the current user's scope → allowed for managers and admins; the actor retains read access via "last-acted-on" window of 24 hours (tracked via timeline, not a new access grant)
- Distribution User attempts any of these mutations → 403

**Checklist:**
- [ ] `PATCH /api/accounts/{id}` accepts `brokerOfRecordId`, `primaryProducerUserId`, `territoryCode` changes
- [ ] Each change writes one `AccountRelationshipHistory` row per changed relationship
- [ ] Each change appends one timeline event per relationship
- [ ] Combined patch that changes more than one relationship yields one history row per relationship and one timeline event per relationship
- [ ] Relationship history is read-only, append-only; no update / delete endpoint
- [ ] `GET /api/accounts/{id}/relationship-history?page=&pageSize=` paginated, manager+admin only

## Data Requirements

**AccountRelationshipHistory fields:**
- `id`, `accountId`, `relationshipType` (enum: `BrokerOfRecord`, `PrimaryProducer`, `Territory`), `previousValue` (string / nullable), `newValue` (string / nullable), `effectiveAt` (timestamp), `actorUserId`, `notes` (nullable)

**Validation Rules:**
- `brokerOfRecordId` must reference an active broker
- `primaryProducerUserId` must reference a user with a distribution-family role
- `territoryCode` free-form string in MVP; F0017 will introduce validation

## Role-Based Visibility

- Mutations: Distribution Manager (territory), Admin
- Read relationship history: Distribution Manager (own territory), Admin

## Non-Functional Expectations

- Performance: relationship mutation p95 ≤ 500 ms; history list p95 ≤ 300 ms
- Security: ABAC `account:update` (with `manager+` gate for relationship columns); history read is manager-only
- Reliability: append-only history; one history row per changed relationship per mutation

## Dependencies

**Depends On:**
- F0016-S0003 (profile edit path hosts these fields)

**Related Stories:**
- F0016-S0010 (timeline)

## Out of Scope

- Territory hierarchy / rule-based territory auto-assignment (deferred to F0017)
- Effective-dated future assignments (MVP records effective-at = now)
- Multi-producer splits (deferred to F0025)

## UI/UX Notes

- Relationship changes surfaced on the profile card with an inline "Change" action
- History drawer on Account 360 shows the full relationship-history log (manager+)

## Questions & Assumptions

**Assumptions:**
- `AccountRelationshipHistory` is separate from `ActivityTimelineEvent` (the timeline is the user-facing narrative; the history table is the machine-readable system-of-record for ABAC)

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged: Yes
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
