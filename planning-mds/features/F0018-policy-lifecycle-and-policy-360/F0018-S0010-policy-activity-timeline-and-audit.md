---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0018-S0010: Policy Activity Timeline and Audit Trail

**Story ID:** F0018-S0010
**Feature:** F0018 — Policy Lifecycle & Policy 360
**Title:** Policy activity timeline and audit trail
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** underwriter, distribution manager, relationship manager, program manager, or admin
**I want** a chronological, immutable activity timeline on every policy that records every create, profile edit, lifecycle transition, endorsement, cancellation, reinstatement, and expiration
**So that** every policy change is auditable end-to-end with actor, timestamp, reason, and event details, and I can answer servicing or compliance questions without cross-referencing multiple tables

## Context & Background

The `ActivityTimelineEvent` pattern (established in F0016 per ADR-011) is reused verbatim on the Policy aggregate. This story owns the Policy-specific event-types catalog, the write contract from every mutating flow, the Timeline rail endpoint on Policy 360, and the retention / immutability guarantees. Every mutating story (S0002, S0003, S0006, S0007, S0008, and the expiration job in PRD § "Expiration Flow") emits timeline events; this story is the single source of truth for the schema and contract.

## Acceptance Criteria

**Happy Path:**
- **Given** a policy has gone through create → issue → endorse → cancel → reinstate → endorse → (future) expire
- **When** the user opens the Timeline rail on Policy 360
- **Then** they see events in descending chronological order: `policy.created`, `policy.issued`, `policy.endorsed`, `policy.cancelled`, `policy.reinstated`, `policy.endorsed`, (later) `policy.expired`

- **Given** any timeline event
- **When** the user inspects it
- **Then** they see: event type, human-readable summary, actor user (name + id), occurredAt, and event-type-specific payload (e.g., endorsement reason, cancellation reason, version number)

- **Given** a write occurs on a policy (any mutating flow)
- **When** the write commits
- **Then** a corresponding `ActivityTimelineEvent` row is written in the same transaction; no event is emitted on write failure

**Alternative Flows / Edge Cases:**
- Unauthorized actor reading timeline → 403 (ABAC on parent policy scope)
- Bulk system events (e.g., daily expiration job) carry a synthetic actor id for `system` (distinguishable in the UI)
- Timeline events are immutable — no PUT / PATCH / DELETE; corrections via compensating events only
- Very long timelines (hundreds of events) paginate (25/page)
- Events referencing deleted users retain the user's display name snapshot for historical display
- Merged / deleted account does NOT alter or delete policy timeline events (audit-first)

**Checklist:**
- [ ] `GET /api/policies/{id}/timeline?page=&pageSize=` paginated, newest first
- [ ] `ActivityTimelineEvent` row schema reuses the existing shape (reference ADR-011) with `policyId` scope
- [ ] Event types (Policy-specific): `policy.created`, `policy.profile_updated`, `policy.issued`, `policy.endorsed`, `policy.cancelled`, `policy.reinstated`, `policy.expired`, `policy.coverage_updated` (fired for Pending-state coverage CRUD), `policy.imported`
- [ ] Every mutating flow writes exactly one timeline event per logical action (idempotent retries do not produce duplicates)
- [ ] Event payload (jsonb) carries event-type-specific fields (e.g., `endorsementReasonCode`, `cancellationReasonCode`, `versionNumber`, `effectiveDate`)
- [ ] Actor fields: `actorUserId` (nullable for system events), `actorDisplayName` (snapshot for historical display)
- [ ] Timeline writes occur inside the same transaction as the mutation; on rollback neither persists
- [ ] Retention: all policy timeline events retained indefinitely for MVP (no pruning); archival strategy is a follow-up
- [ ] ABAC: `policy:read` scope on parent policy applies to timeline read; no separate Casbin action

## Data Requirements

- See ADR-011 for `ActivityTimelineEvent` schema (reused as-is)
- `policyId` scope (nullable on rows scoped to other aggregates; non-null for policy events)
- Event `type` string validated at API layer; Policy-specific event types listed above
- Event `payload` jsonb structured per event type; consumers parse by type

**Validation Rules:**
- Events are append-only; UPDATE / DELETE not exposed
- `occurredAt` defaults to transaction time; not user-settable
- `actorUserId` required for user-initiated events; null only for system-emitted events (with `actorDisplayName='System'`)
- Event type strings are stable; renaming a type is an ADR-level change

## Role-Based Visibility

- Read: any role with `policy:read` in scope on the parent policy
- Write: not user-exposed (events are emitted by the mutating endpoints)

**Data Visibility:** InternalOnly.

## Non-Functional Expectations

- Performance: timeline list p95 ≤ 400 ms; most policies carry ≤ 100 events in MVP
- Reliability: every mutation produces exactly one timeline entry; transactional commit guarantee
- Correctness: timestamps monotonically non-decreasing per policy; actor attribution accurate; no silent drops on event emission failure

## Dependencies

**Depends On:**
- ADR-011 (Workflow State Machines and Transition History) — event schema
- F0018-S0002 (emits `policy.created`, `policy.imported`), S0003 (`policy.profile_updated`, `policy.coverage_updated`), S0006 (`policy.endorsed`), S0007 (`policy.cancelled`), S0008 (`policy.reinstated`), PRD expiration job (`policy.issued`, `policy.expired`)

**Related Stories:**
- F0018-S0004 (Timeline rail rendering)

## Out of Scope

- Timeline search / filter by event type (MVP: chronological list only)
- Cross-policy timeline aggregation (deferred; Account 360 timeline already covers account-scoped events)
- Real-time streaming / server-sent events (MVP: polling on reload)
- Edit history of free-text fields beyond the event payload (profile edits capture a diff summary, not a full field-level change log)
- External export / SIEM forwarding of timeline events (deferred)

## UI/UX Notes

- Timeline rail uses consistent iconography per event type
- System-emitted events (e.g., expiration) display a distinct subtle marker
- Each event is expandable to show the full payload in a dev-friendly panel (JSON)

## Questions & Assumptions

**Assumptions:**
- `ActivityTimelineEvent` schema is already in place from F0016; adding Policy-scoped events requires no schema migration, only new event-type strings
- Retention is "keep indefinitely" for MVP; archival is a follow-up

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (unauthorized, pagination, system events, immutability)
- [ ] Permissions enforced (scope via parent policy)
- [ ] Audit/timeline logged: This story IS the timeline — every other mutation story depends on it
- [ ] Tests pass (including transactional rollback: no event persisted on mutation failure; idempotent retry emits one event)
- [ ] Documentation updated (OpenAPI + policy-specific event-type catalog)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
