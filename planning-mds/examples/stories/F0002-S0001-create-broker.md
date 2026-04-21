# Story: Create Broker

**Story ID:** F0002-S0001
**Feature:** F0002 - Broker Relationship Management
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
- LicenseNumber must be unique
- Email and Phone must be normalized before persistence

## Role-Based Visibility

**Roles that can create brokers:**
- DistributionManager — create/update broker
- Admin — full access

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

**Open Questions:**
- [ ] Should license uniqueness be global or state-scoped?

**Assumptions (to be validated):**
- MVP treats license number as globally unique

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge case and error scenario tests pass
- [ ] Permission checks enforced server-side
- [ ] Audit timeline event created for successful mutation
- [ ] Unit and integration tests pass
