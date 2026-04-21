---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0016-S0005: Account-Scoped Contacts Management

**Story ID:** F0016-S0005
**Feature:** F0016 — Account 360 & Insured Management
**Title:** Account-scoped contacts (lightweight CRUD)
**Priority:** High
**Phase:** CRM Release MVP

## User Story

**As a** distribution user, distribution manager, or relationship manager
**I want** to maintain a lightweight list of contacts (people) on each account with one designated primary
**So that** I know who to call, email, or send documents to on the insured side without relying on external address books

## Context & Background

F0016 owns a lightweight account-scoped contact collection for MVP. This intentionally avoids shipping a full cross-feature Contact module; generalization to a shared Contact aggregate is deferred to F0021 / a future refactor. At most one contact per account can be primary.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated user in scope for the account
- **When** they create a contact with required fields
- **Then** the contact is persisted, scoped to the account, and returned with an id

- **Given** an existing contact on the account
- **When** the user sets it as primary
- **Then** exactly one primary contact exists on the account (previous primary is unset atomically)

- **Given** a user deletes a contact
- **When** the delete commits
- **Then** the contact row is soft-deleted and a timeline event `account.contact_removed` is appended to the account

**Alternative Flows / Edge Cases:**
- Setting `IsPrimary=true` while another contact is primary → the previous primary is cleared in the same transaction
- Creating a contact on a Merged account → 409 `invalid_state`; caller should use the survivor
- Creating a contact on a Deleted account → 410 Gone
- Email / phone uniqueness: NOT enforced in MVP (people may share phone / email within a household or group)
- Contact count per account is not hard-capped, but the rail paginates at 25/page

**Checklist:**
- [ ] `GET /api/accounts/{id}/contacts?page=&pageSize=`
- [ ] `POST /api/accounts/{id}/contacts` creates a contact
- [ ] `PATCH /api/accounts/{id}/contacts/{contactId}` updates a contact with If-Match
- [ ] `DELETE /api/accounts/{id}/contacts/{contactId}` soft-deletes
- [ ] Primary flag uniqueness enforced at service layer (transactional clear + set)
- [ ] Timeline events: `account.contact_added`, `account.contact_updated`, `account.contact_removed`, `account.primary_contact_set`
- [ ] RowVersion / If-Match on update + delete

## Data Requirements

**AccountContact fields:**
- `id`, `accountId` (FK), `fullName` (required), `role`, `email`, `phone`, `isPrimary` (bool), audit fields, `rowVersion`

**Validation Rules:**
- `fullName` required, ≤ 200
- `email` format validated if provided
- `phone` format light-validated (length / permitted chars) if provided

## Role-Based Visibility

- CRUD allowed for Distribution User (scope), Distribution Manager (territory), Relationship Manager (on managed brokers' accounts), Admin
- Underwriter: read-only via Account 360 rail

## Non-Functional Expectations

- Performance: contact list p95 ≤ 300 ms; mutations p95 ≤ 400 ms
- Security: ABAC `account:contact:manage`
- Reliability: atomic primary-flag flip

## Dependencies

**Depends On:**
- F0016-S0002 (account exists)

**Related Stories:**
- F0016-S0004 (rail renders contacts)

## Out of Scope

- Cross-account contact search / global address book (deferred)
- Contact avatar / rich fields (Future)
- Merge / dedup of contacts across accounts

## UI/UX Notes

- Contacts rail displays primary contact on top with a badge
- Quick-add popover from Account 360
- Edit / delete actions inline per row

## Questions & Assumptions

**Assumptions:**
- Contacts do not need ABAC scoping independent of their account (if user can see the account, they can see the contacts)

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (primary flip, merged/deleted parent)
- [ ] Permissions enforced (`account:contact:manage`)
- [ ] Audit/timeline logged: Yes (contact events)
- [ ] Tests pass
- [ ] Documentation updated (OpenAPI)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
