# F0028-S0003: Underwriter and market contact management

**Story ID:** F0028-S0003
**Feature:** F0028 — Carrier & Market Relationship Management
**Title:** Underwriter and market contact management
**Priority:** High
**Phase:** CRM Release MVP+

## User Story

**As a** relationship manager
**I want** to manage underwriter and market contacts on carrier/market records
**So that** placement teams know who to contact for specific market conversations

## Context & Background

Existing contact concepts support broker/account contexts. F0028 adds market-side contacts linked to carrier/market profiles with role, channel, geography/LOB scope, and active/inactive state.

## Acceptance Criteria

**Happy Path:**
- **Given** an active market profile and an authorized user
- **When** the user adds or updates a market contact
- **Then** the contact appears on the market workspace
- **And** the contact stores name, role/title, communication channel, active state, and optional LOB/geography scope
- **And** the change is captured in the market activity/timeline history

**Alternative Flows / Edge Cases:**
- Missing contact name or communication channel -> rejected with field-level validation.
- Unauthorized actor attempts contact mutation -> `403 Forbidden`.
- Contact marked inactive -> retained in history and hidden from default active-contact view unless the inactive filter is enabled.
- Duplicate active contact for the same profile and same email/phone -> rejected or requires Phase B duplicate resolution contract.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Market Workspace → Contacts → Add contact | Enter contact fields and Save | Enabled for active/monitoring market profiles | Contact created and linked to profile | Reload shows contact in active contact list and timeline | Relationship manager, distribution manager/leader, admin |
| Market Workspace → Contacts → Edit contact | Edit fields or set inactive and Save | Enabled for existing active contacts | Contact updated or marked inactive | Reload shows updated row; inactive row appears when filter enabled | Same manage roles; archived market profiles read-only |

Required checks:
- [ ] Render-only behavior cannot satisfy this story.
- [ ] Save path has validation and error behavior.
- [ ] Successful mutations append audit/timeline events.
- [ ] Tests prove add/edit/inactivate and persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Contact name.
- Contact role/title.
- At least one communication channel: email or phone.
- Active state.

**Optional Fields:**
- LOB scope, geography scope, preferred contact method, notes.

**Validation Rules:**
- Contact name is required.
- Email values use email format when supplied.
- Phone values are stored as entered after basic non-empty validation; phone normalization is Phase B or future scope.

## Role-Based Visibility

**Roles that can manage contacts:**
- Relationship manager, distribution manager, distribution leader, admin.

**Data Visibility:**
- InternalOnly content: contact channels, notes, LOB/geography scope.
- ExternalVisible content: none in F0028.

## Non-Functional Expectations

- Security: unauthorized contact mutations return `403`.
- Reliability: inactive contacts remain historically available for audit and relationship continuity.

## Dependencies

**Depends On:**
- F0028-S0002 — carrier/market profile exists.

**Related Stories:**
- F0028-S0006 — contact changes appear in market activity.

## Business Rules

1. **Historical retention:** inactivating a contact does not delete relationship history.
2. **Market-scoped contact:** a market contact is linked to a carrier/market profile, not to an account or broker record.

## Out of Scope

- Email sending.
- Calendar synchronization.
- External contact portal visibility.

## UI/UX Notes

- Screens involved: Market Relationship Workspace contacts tab/panel.
- Key interactions: add contact, edit contact, mark inactive, filter active/inactive.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Contact delete is not included; inactive state is the MVP removal path.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0028-S0003-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
