# F0002-S0004: Update Broker

**Story ID:** F0002-S0004
**Feature:** F0002 — Broker & MGA Relationship Management
**Title:** Update broker profile information
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution Manager
**I want** to update broker profile details
**So that** the broker record stays accurate and current

## Context & Background

Broker profiles change over time. The CRM must support updating core broker fields and record each change in the audit timeline.

## Acceptance Criteria

- **Given** I have `broker:update` permission and the broker is within my authorization scope
- **When** I edit broker fields and save
- **Then** the broker record is updated and I remain on Broker 360

- **Given** required fields are missing or invalid
- **When** I submit the update
- **Then** I see field-level validation errors and the record is not updated

- **Given** I attempt to set a license number that already exists
- **When** I submit the update
- **Then** I receive a deterministic conflict error and the record is not updated

- **Given** I attempt to change the broker's license number
- **When** I submit the update
- **Then** I receive a validation error and the record is not updated

- **Given** I am not authorized to update this broker
- **When** I attempt to update
- **Then** access is denied with a 403 response

- **Given** an update completes successfully
- **When** the change is persisted
- **Then** a broker update timeline event is stored with actor, timestamp, and changed fields

- **Given** two users attempt to update the same broker concurrently and the second request carries a stale version token
- **When** the second update is submitted (via `If-Match` header with an outdated ETag)
- **Then** a 409 Conflict response is returned with error code `concurrency_conflict`; the second user must re-fetch the broker and reapply their changes

- **Given** I update the broker's Status to Inactive
- **When** the update is persisted
- **Then** the broker is hidden from active search results, and broker/contact Email and Phone fields are masked in all subsequent API responses (sentinel: `null`; see S0003 masking spec); a timeline event records the status change

- Edge case: broker does not exist or has been deactivated → return not found

## Data Requirements

**Required Fields:**
- BrokerId: identifier for the broker to update
- If-Match header: ETag value from the most recent GET response (used for optimistic concurrency; request rejected with 409 if value is stale)

**Updatable Fields:**
- LegalName
- State
- Email
- Phone
- Status

**Validation Rules:**
- LicenseNumber must remain unique
- LicenseNumber is immutable after creation
- Field formats must match create-broker validation rules

## Role-Based Visibility

**Roles that can update brokers:**
- DistributionUser — scoped update
- DistributionManager — region-scoped update
- RelationshipManager — scoped update
- Admin — unscoped update

**Data Visibility:**
- InternalOnly content: audit metadata and internal notes
- ExternalVisible content: none in MVP

## Non-Functional Expectations

- Performance: update response p95 < 500ms (excluding auth provider latency)
- Security: server-side authorization required for all update paths
- Reliability: partial updates must not leave the record in an inconsistent state

## Dependencies

**Depends On:**
- F0002-S0001 - Create Broker
- F0002-S0003 - Read Broker (Broker 360 View)

**Related Stories:**
- F0002-S0007 - View Broker Activity Timeline

## Out of Scope

- Bulk broker updates
- Automated enrichment from external data sources

## Questions & Assumptions

**Assumptions (confirmed):**
- Status updates (Active/Inactive/Pending) are allowed via broker update; changing to Inactive triggers PII masking and search exclusion as documented in the AC above
- LicenseNumber is not included in the edit form — it is displayed read-only in Broker 360 to prevent update attempts at the UI layer

## Definition of Done

- [x] Acceptance criteria met
- [x] Edge cases handled
- [x] Permissions enforced
- [x] Audit/timeline logged for updates
- [ ] Tests pass
