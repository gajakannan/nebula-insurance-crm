# F0002-S0006: Manage Broker Contacts

**Story ID:** F0002-S0006
**Feature:** F0002 — Broker & MGA Relationship Management
**Title:** Create, update, and remove broker contacts
**Priority:** High
**Phase:** MVP

## User Story

**As a** Relationship Manager
**I want** to manage contacts for a broker
**So that** the team has accurate people and communication details

## Context & Background

Broker contacts are essential for submissions, follow-ups, and relationship management. Contact data must be accurate and tied to the correct broker.

## Acceptance Criteria

- **Given** I have `contact:create` permission and the broker is within scope
- **When** I add a contact with valid fields
- **Then** the contact appears in Broker 360 and a timeline event is recorded

- **Given** I have `contact:update` permission
- **When** I edit a contact and save
- **Then** the contact is updated and a timeline event is recorded

- **Given** I have `contact:delete` permission
- **When** I delete a contact
- **Then** the contact is removed from active lists (soft delete) and a timeline event is recorded

- **Given** required fields are missing or invalid
- **When** I submit the contact form
- **Then** I see validation errors and no changes are saved

- **Given** I am not authorized to manage contacts for this broker
- **When** I attempt to create, update, or delete a contact
- **Then** access is denied with a 403 response

- **Given** I attempt to update or delete a contact using a BrokerId that does not match the contact's actual broker
- **When** the request is processed
- **Then** a validation error is returned and no changes are made (cross-broker ownership check applies to create, update, and delete)

- Edge case: parent broker is deactivated → contacts are hidden from active lists (soft cascade from S0005); existing contact data is preserved and becomes accessible again if the broker is reactivated (see F0002-S0008)

## Data Requirements

**Required Fields:**
- BrokerId: broker to associate the contact to
- FullName
- Email
- Phone

**Optional Fields:**
- Role

**Validation Rules:**
- Contact must be linked to a valid BrokerId
- Email must be RFC-compliant
- Phone must be normalized to US format
- Soft delete retains historical timeline events

## Role-Based Visibility

**Roles that can manage contacts:**
- DistributionUser — create/update contacts within scope
- DistributionManager — create/update/delete contacts within region
- RelationshipManager — create/update contacts within scope
- Admin — full access

**Delete is restricted to:**
- DistributionManager and Admin only

**Data Visibility:**
- InternalOnly content; no external access in MVP

## Non-Functional Expectations

- Performance: contact create/update p95 < 500ms (excluding auth provider latency)
- Security: server-side authorization required for all contact mutations
- Reliability: contact changes are atomic and audit logged

## Dependencies

**Depends On:**
- F0002-S0001 - Create Broker
- F0002-S0003 - Read Broker (Broker 360 View)

**Related Stories:**
- F0002-S0007 - View Broker Activity Timeline

## Out of Scope

- Bulk contact import
- External broker self-service contact updates

## Questions & Assumptions

**Assumptions (confirmed):**
- Contact delete is a soft delete (record retained; excluded from active lists)
- Contacts are not independently deactivated when a broker is deactivated; they are hidden via broker-level cascade and restored when the broker is reactivated

## UI/UX Notes

- Screens involved: Broker 360 → Contacts tab
- Layout: Contacts tab displays a list of active contacts; Add Contact button opens a modal form; each row has Edit and Delete (for authorized roles) inline actions
- Contact form fields: FullName (required), Email (required), Phone (required), Role (optional)
- Delete requires confirmation ("Are you sure you want to remove this contact?")
- Unauthorized users: Add/Edit/Delete actions not rendered (hidden, not disabled)

## Definition of Done

- [x] Acceptance criteria met
- [x] Edge cases handled
- [x] Permissions enforced
- [x] Audit/timeline logged for contact changes
- [ ] Tests pass
