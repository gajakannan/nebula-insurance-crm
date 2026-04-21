# F0002-S0001: Create Broker

**Story ID:** F0002-S0001
**Feature:** F0002 — Broker & MGA Relationship Management
**Title:** Create a new broker record
**Priority:** Critical
**Phase:** MVP

## User Story

**As a** Distribution Manager
**I want** to create a broker record with validated profile fields
**So that** the team can start managing submissions and relationship activity for that broker

## Context & Background

Broker data is currently managed in spreadsheets, which causes inconsistent records and limited traceability.
This story establishes the first auditable broker lifecycle event in the CRM.

## Acceptance Criteria

- **Given** I am an authorized internal user with `broker:create` permission
- **When** I submit a valid create-broker form
- **Then** a broker record is created and I am redirected to Broker 360

- **Given** required fields are missing or invalid
- **When** I submit the form
- **Then** I see field-level validation errors and the record is not created

- **Given** I am not authorized to create brokers
- **When** I call the create endpoint or open the create screen
- **Then** access is denied with a 403 response

- **Given** a broker was successfully created
- **When** creation completes
- **Then** an audit timeline event is stored with actor, timestamp, and broker id

- Edge case: duplicate broker license number returns a deterministic conflict error

## Data Requirements

**Required Fields:**
- LegalName: 1-255 chars
- LicenseNumber: unique, 1-50 chars
- State: valid US state code

**Optional Fields:**
- Email: RFC-compliant email format
- Phone: normalized US format

**Validation Rules:**
- LicenseNumber must be globally unique (MVP)
- Email and Phone must be normalized before persistence

## Role-Based Visibility

**Roles that can create brokers:**
- DistributionManager — create broker
- Admin — full access

Note: DistributionUser can update existing broker records (see S0004) but cannot create new ones. This is intentional — broker creation introduces a new record into the system and requires manager-level authority to prevent duplicate or erroneous entries. DistributionUser access is limited to maintaining accuracy of records already created by a manager.

**Data Visibility:**
- InternalOnly content: audit metadata and internal notes
- ExternalVisible content: none in MVP

## Non-Functional Expectations

- Performance: create response p95 < 500ms (excluding auth provider latency)
- Security: server-side authorization required on all create paths
- Reliability: duplicate submission does not create duplicate brokers

## Dependencies

**Depends On:**
- AuthZ policy for `broker:create`
- Broker persistence model and repository

**Related Stories:**
- F0002-S0002 - Search brokers by name/license

## Out of Scope

- Bulk broker import
- Contact management
- Broker hierarchy management

## Questions & Assumptions

**Assumptions (confirmed):**
- New broker records default to Status = "Pending" until reviewed and set to Active by a manager
- The create form lives on a dedicated Create Broker screen (not a modal) navigated to from the Broker List screen

## UI/UX Notes

- Screens involved: Broker List → Create Broker (dedicated screen) → Broker 360 (on success)
- Layout: Full-page form with field groups (Identity: LegalName, LicenseNumber, State; Contact: Email, Phone)
- On success: redirect to Broker 360 for the newly created broker
- On validation error: inline field-level messages; form remains open
- Unauthorized users: Create Broker option not rendered in the UI (hidden, not just disabled)

## Definition of Done

- [x] Acceptance criteria met
- [x] Edge case and error scenario tests pass
- [x] Permission checks enforced server-side
- [x] Audit timeline event created for successful mutation
- [ ] Unit and integration tests pass
