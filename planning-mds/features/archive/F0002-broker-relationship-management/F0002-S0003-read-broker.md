# F0002-S0003: Read Broker (Broker 360 View)

**Story ID:** F0002-S0003
**Feature:** F0002 — Broker & MGA Relationship Management
**Title:** View broker details in Broker 360
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution Manager
**I want** to view a broker's full profile in Broker 360
**So that** I can understand the relationship context and current activity

## Context & Background

Broker information is fragmented across tools. A Broker 360 view provides a single source of truth for broker profile data, contacts, and relationship context.

## Acceptance Criteria

- **Given** I have `broker:read` permission and the broker is within my authorization scope
- **When** I open Broker 360 from the Broker List or a deep link
- **Then** I see:
  - Profile header (name, status)
  - Broker profile fields (legal name, license number, state, contact info)
  - Contacts list (if any; empty state if none)
  - Immutable timeline panel (read-only)
  - Note: MGA/program hierarchy links are deferred to a future MGA management feature and are not displayed in MVP

- **Given** I have `broker:read` permission but not `broker:update` permission
- **When** I view Broker 360
- **Then** Edit, Deactivate, and Delete actions are not rendered in the UI and the profile is displayed in read-only mode

- **Given** the broker does not exist or has been deleted
- **When** I navigate to Broker 360
- **Then** I see a "Broker not found" message and a link back to Broker List

- **Given** I do not have permission to read this broker
- **When** I attempt to open Broker 360
- **Then** access is denied with a 403 response and no broker data is returned

- Edge case: broker has no contacts → show empty-state messaging in the Contacts tab ("No contacts added yet.")
- Edge case: broker status is Inactive → display status, keep view read-only, and mask broker/contact email + phone fields in the API response (masking sentinel: fields returned as `null`; frontend displays "Masked")

## Data Requirements

**Required Fields:**
- BrokerId: identifier for the broker to retrieve

**Required Broker Fields (display):**
- LegalName
- LicenseNumber
- State
- Status

**Optional Broker Fields (display if present):**
- Email (masked when Status = Inactive)
- Phone (masked when Status = Inactive)

**Contact Fields (display if present):**
- FullName
- Email (masked when Status = Inactive)
- Phone (masked when Status = Inactive)
- Role

## Role-Based Visibility

**Roles that can read brokers:**
- DistributionUser — scoped read
- DistributionManager — region-scoped read
- RelationshipManager — scoped read
- Underwriter — read-only access to broker context
- ProgramManager — scoped read (program context)
- Admin — unscoped read

**Data Visibility:**
- InternalOnly content is visible to internal users only
- ExternalVisible content: none in MVP

## Non-Functional Expectations

- Performance: Broker 360 loads within p95 < 500ms (excluding auth provider latency)
- Security: ABAC scope enforced on broker reads
- Reliability: Not-found or unauthorized requests return a deterministic response

## Dependencies

**Depends On:**
- F0002-S0001 - Create Broker (broker data exists)
- ActivityTimelineEvent entity (for immutable timeline panel)

**Related Stories:**
- F0002-S0002 - Search Brokers
- F0002-S0007 - View Broker Activity Timeline

## Out of Scope

- External broker portal access
- Advanced broker analytics or scoring
- MGA/broker hierarchy links (deferred to future MGA management feature)

## UI/UX Notes (Optional)

- Screens involved: Broker List, Broker 360
- Primary navigation: Broker List → Broker 360

## Questions & Assumptions

**Assumptions (confirmed):**
- Broker 360 edit/deactivate/delete actions are hidden (not rendered) for users without `broker:update` / `broker:delete` permission — this is now a formal AC above

## Definition of Done

- [x] Acceptance criteria met
- [x] Edge cases handled
- [x] Permissions enforced
- [x] Audit/timeline logged: N/A (read-only)
- [ ] Tests pass
