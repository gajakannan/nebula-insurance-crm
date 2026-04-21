# F0002-S0008: Reactivate Broker

**Story ID:** F0002-S0008
**Feature:** F0002 — Broker & MGA Relationship Management
**Title:** Reactivate a deactivated broker
**Priority:** Medium
**Phase:** MVP

## User Story

**As a** Distribution Manager or Admin
**I want** to reactivate a deactivated broker record
**So that** a broker that was deactivated in error or has resumed activity can be restored to active workflows

## Context & Background

Broker deactivation (F0002-S0005) is a soft delete — records are retained for audit purposes. Without a reactivation path, erroneous deactivations require manual database intervention. This story provides the controlled, audited reverse of S0005.

## Acceptance Criteria

- **Given** I have `broker:update` permission and the broker is deactivated
- **When** I confirm broker reactivation
- **Then** the broker status is set to Active, the broker reappears in active broker lists and search results, and associated contacts become visible again in Broker 360

- **Given** I am not authorized to reactivate brokers (no `broker:update` permission)
- **When** I attempt to reactivate
- **Then** access is denied with a 403 response

- **Given** a broker is reactivated successfully
- **When** reactivation completes
- **Then** an audit timeline event is stored with actor, timestamp, and broker id (EventType: "BrokerReactivated")

- **Given** I attempt to reactivate a broker that is already Active
- **When** the request is processed
- **Then** a validation error is returned (`already_active`) and no changes are made

- Edge case: broker does not exist → return not found

## Data Requirements

**Required Fields:**
- BrokerId: identifier for the broker to reactivate

**Validation Rules:**
- Only deactivated (soft-deleted) brokers can be reactivated
- Reactivation restores broker Status to Active; contacts become visible again via broker-level cascade (no independent contact state changes needed)

## Role-Based Visibility

**Roles that can reactivate brokers:**
- DistributionManager — region-scoped reactivation
- Admin — unscoped reactivation

**Explicitly not allowed:**
- DistributionUser — cannot reactivate (same reasoning as create: structural record changes require manager authority)
- RelationshipManager — no reactivation permission

**Data Visibility:**
- InternalOnly content only; no external access in MVP

## Non-Functional Expectations

- Performance: reactivation response p95 < 500ms (excluding auth provider latency)
- Security: server-side authorization required; `broker:update` permission enforced
- Reliability: reactivation is idempotent on the broker state (duplicate request returns `already_active` error, not a silent no-op)

## Dependencies

**Depends On:**
- F0002-S0005 - Deactivate Broker (a broker must be deactivated before it can be reactivated)
- F0002-S0003 - Read Broker (Broker 360 View) — reactivation is triggered from the deactivated broker view (Admin only per S0005)

**Related Stories:**
- F0002-S0005 - Deactivate Broker (reverse operation)
- F0002-S0007 - View Broker Activity Timeline (audit trail)

## Out of Scope

- Bulk reactivation
- Automatic reactivation on a schedule

## UI/UX Notes

- Screens involved: Broker 360 (deactivated state, Admin view only per S0005)
- Layout: "Deactivated" banner with a "Reactivate Broker" button visible only to Admin and DistributionManager
- On success: broker status updates to Active, banner is removed, all actions restore to normal
- Non-Admin users do not see the deactivated broker view at all (returns "Broker not found" per S0005)

## Questions & Assumptions

**Assumptions (confirmed):**
- Reactivating a broker restores it to Active status (not Pending); a manager has explicitly chosen to restore the record
- Contact visibility is restored automatically via broker-level cascade; no per-contact action is required

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (already active, not found, unauthorized)
- [ ] Permissions enforced (`broker:update` required)
- [ ] Audit timeline event created (EventType: "BrokerReactivated")
- [ ] Tests pass
