# F0002-S0005: Delete Broker

**Story ID:** F0002-S0005
**Feature:** F0002 — Broker & MGA Relationship Management
**Title:** Deactivate (soft delete) a broker
**Priority:** Medium
**Phase:** MVP

## User Story

**As a** Distribution User
**I want** to deactivate a broker record
**So that** inactive or erroneous records are removed from active workflows

## Context & Background

Brokers occasionally need to be removed from active use due to errors, inactivity, or compliance issues. Deactivation should be reversible (soft delete) and fully audited.

## Acceptance Criteria

- **Given** I have `broker:delete` permission
- **When** I confirm broker deactivation
- **Then** the broker is deactivated (soft deleted) and no longer appears in active broker lists or search results

- **Given** a broker has been deactivated
- **When** a non-Admin user attempts to access Broker 360 via direct URL
- **Then** they see a "Broker not found" message and a link back to Broker List

- **Given** a broker has been deactivated
- **When** an Admin user navigates to Broker 360 via direct URL
- **Then** the Admin can view the broker profile in a read-only deactivated state (for audit and recovery purposes); a "Deactivated" banner is displayed; all actions except Reactivate (see F0002-S0008) are hidden

- **Given** I am not authorized to deactivate brokers
- **When** I attempt to deactivate
- **Then** access is denied with a 403 response

- **Given** a broker is deactivated successfully
- **When** deactivation completes
- **Then** an audit timeline event is stored with actor, timestamp, and broker id; associated contacts are also hidden from active views (soft cascade — contacts are not independently deactivated but are excluded from list/search while the broker is deactivated)

- **Given** the broker has active submissions or renewals
- **When** I attempt to deactivate the broker
- **Then** the request is rejected with a deterministic conflict error (`active_dependencies_exist`) and the broker remains active

- Edge case: deactivating a broker that does not exist → return not found

## Data Requirements

**Required Fields:**
- BrokerId: identifier for the broker to delete

**Validation Rules:**
- Deactivation is a soft delete (record retained, flagged as deactivated); hard delete is not supported
- Broker cannot be deactivated while active submissions or renewals exist
- Associated contacts are hidden from active lists while broker is deactivated (soft cascade); they are not permanently deleted

## Role-Based Visibility

**Roles that can delete brokers:**
- DistributionUser — scoped delete
- DistributionManager — region-scoped delete
- Admin — unscoped delete
  
**Explicitly not allowed in MVP:**
- RelationshipManager — no delete permission

**Data Visibility:**
- InternalOnly content only; no external access in MVP

## Non-Functional Expectations

- Security: server-side authorization required for delete paths
- Reliability: delete is idempotent and audit logged

## Dependencies

**Depends On:**
- F0002-S0001 - Create Broker
- F0002-S0003 - Read Broker (Broker 360 View)

**Related Stories:**
- F0002-S0007 - View Broker Activity Timeline
- F0002-S0008 - Reactivate Broker (the reverse operation)

## Out of Scope

- Hard delete (permanent removal)
- Bulk deactivation

## Questions & Assumptions

**Assumptions (confirmed):**
- Deactivation hides brokers from list/search but retains audit history and all timeline events
- "Deactivate" is the user-facing term throughout the UI (not "delete"); the Casbin policy permission is `broker:delete` for historical reasons

## Definition of Done

- [x] Acceptance criteria met
- [x] Edge cases handled
- [x] Permissions enforced
- [x] Audit/timeline logged for deletion
- [ ] Tests pass
