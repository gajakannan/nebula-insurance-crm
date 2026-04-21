---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0006-S0003: Submission Detail View with Intake Context

**Story ID:** F0006-S0003
**Feature:** F0006 — Submission Intake Workflow
**Title:** Submission detail view with intake context
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution user or underwriter
**I want** to view full submission details including linked account, broker, program, completeness status, and activity timeline
**So that** I can assess the submission's readiness for the next workflow step without switching screens or systems

## Context & Background

The submission detail view is the primary workspace for intake triage. Distribution users come here to review a submission's context, edit mutable intake fields as more information arrives, check completeness, log follow-up, assign an underwriter, and advance the submission through intake states. Underwriters come here to review the submission they have been assigned, see what outreach has happened, and confirm completeness before F0019 takes over for the review/quote phase. This view must render linked entity context (account, broker, program) and the completeness projection without requiring navigation away from the page.

## Acceptance Criteria

**Happy Path:**
- **Given** a distribution user with read access to a submission
- **When** they navigate to the submission detail view (e.g., from pipeline list row click)
- **Then** the following sections are displayed:
  - **Header:** Submission status badge, account name (linked), broker name (linked), LOB, effective date, assigned user, created date
  - **Submission Fields:** ProgramId (if linked), PremiumEstimate, Description, ExpirationDate
  - **Edit Action:** An explicit "Edit Intake Details" action that opens a form or dialog for mutable submission fields
  - **Completeness Panel:** Shows checklist of required fields and document categories with pass/fail indicators per item
  - **Activity Timeline:** Chronological list of ActivityTimelineEvents for this submission (most recent first)
  - **Action Bar:** Available transition buttons based on current state and user role

**Linked Entity Context:**
- **Given** the submission has an AccountId and BrokerId
- **When** the detail view loads
- **Then** account name, region, and industry are shown inline; broker name and license number are shown inline; clicking either navigates to the respective 360 view

**Happy Path — Edit Intake Details:**
- **Given** a distribution user with update access to a submission in their scope
- **When** they open the "Edit Intake Details" action, update mutable fields such as ProgramId, LineOfBusiness, PremiumEstimate, Description, or ExpirationDate, and save
- **Then** the submission detail reloads with updated values, completeness is re-evaluated, and a `SubmissionUpdated` timeline event is recorded when any field actually changed

**Completeness Panel:**
- **Given** a submission in Triaging state
- **When** the completeness panel renders
- **Then** each required field shows green check (populated) or red indicator (missing); each required document category shows count of linked documents or "Missing" indicator

**Alternative Flows / Edge Cases:**
- Submission not found, soft-deleted, or hidden by entity-scope visibility rules → HTTP 404 error page
- User lacks route/action-level `submission:read` permission → HTTP 403 error page
- Deleted/merged account fallback on linked detail views is deferred from F0006 closeout and owned by F0016
- No timeline events yet due to legacy or malformed seeded data → empty timeline section with message "No activity recorded yet"
- F0020 not available → document completeness section shows "Document management not yet configured" placeholder

**Checklist:**
- [ ] Header displays: status badge, account name (linked), broker name (linked), LOB, effective date, assigned user, created date/by
- [ ] Submission fields section: program, premium estimate, description, expiration date
- [ ] Edit action allows mutable intake fields to be updated from the detail workspace via a dedicated form or dialog
- [ ] Completeness panel: required-field checklist + document-category checklist (F0020-dependent with graceful fallback)
- [ ] Activity timeline section: chronological events, most recent first, with actor, timestamp, and event description
- [ ] Action bar: transition buttons rendered per current state and user role (F0006-S0004)
- [ ] Assignment display: shows current assigned user with edit action (F0006-S0006)
- [ ] Stale indicator: visual flag if submission exceeds stale threshold (F0006-S0008)
- [ ] ABAC enforced: user can only view submissions within their scope
- [ ] Responsive layout for desktop and tablet

## Data Requirements

**Required Fields (detail response):**
- All Submission entity fields
- `accountName`, `accountRegion`, `accountIndustry`: Denormalized from Account
- `brokerName`, `brokerLicenseNumber`: Denormalized from Broker
- `programName`: Denormalized from Program (nullable)
- `assignedToDisplayName`: Denormalized from UserProfile
- `rowVersion`: Current optimistic-concurrency token for follow-up update / assignment / transition actions
- `completeness`: Object with field-level and document-level pass/fail status
- Timeline data is loaded separately from `GET /submissions/{id}/timeline` so the detail response does not embed a paginated timeline payload

**Validation Rules:**
- SubmissionId must be a valid uuid
- User must have ABAC read permission for this submission
- Edit actions require ABAC update permission for the submission

## Role-Based Visibility

**Roles that can view:**
- Distribution User — Submissions within assigned scope
- Distribution Manager — Submissions within region scope
- Underwriter — Submissions assigned to them
- Relationship Manager — Submissions for own accounts/brokers (read-only)
- Program Manager — Submissions within own programs (read-only)
- Admin — All submissions

**Roles that can edit mutable intake fields from the detail view:**
- Distribution User — Submissions within assigned scope
- Distribution Manager — Submissions within region scope
- Admin — All submissions
- Underwriter, Relationship Manager, Program Manager — Read-only in F0006 scope

**Data Visibility:**
- InternalOnly: All submission detail data is internal-only in MVP
- ExternalVisible: None

## Non-Functional Expectations

- Performance: Detail view loads in < 2s including completeness projection and timeline (first page)
- Security: ABAC-scoped read; no unauthorized submission data leaks
- Reliability: Timeline paginates independently; account-lifecycle fallback behavior is explicitly deferred to F0016 rather than being implicitly assumed in F0006

## Dependencies

**Depends On:**
- F0006-S0001 — Navigation from pipeline list
- F0006-S0002 — Submissions must exist

**Related Stories:**
- F0006-S0004 — Action bar transition buttons
- F0006-S0005 — Completeness panel data
- F0006-S0006 — Assignment display and edit
- F0006-S0007 — Activity timeline section
- F0006-S0008 — Stale indicator

## Business Rules

1. **ABAC Scope Enforcement:** The detail view is gated by Casbin ABAC policy (policy.csv §2.3). Route/action-level denial of `submission:read` returns HTTP 403. If the caller has read capability in general but the specific submission is outside resolved visibility scope, the API may cloak that outcome as HTTP 404.
2. **Edit Produces Audit Trail:** Every successful field edit through the detail view's edit action atomically appends a `SubmissionUpdated` ActivityTimelineEvent recording the changed fields, actor, and timestamp. No-op edits (no fields actually changed) do not produce a timeline event.
3. **Optimistic Concurrency on Edit:** Field edits enforce optimistic concurrency via `rowVersion` + `If-Match`. If another user modified the submission since the detail view loaded, the edit returns HTTP 412 with `code=precondition_failed`.
4. **LOB Validation on Edit:** LineOfBusiness, when edited, must match a value in the known LOB set (ADR-009). Invalid values return HTTP 400.
5. **InternalOnly Data:** All submission detail data is internal-only in MVP. No ExternalVisible fields.
6. **Lifecycle Boundary:** Deleted or merged account fallback behavior is not part of F0006 closeout. If that requirement is needed, it is owned by F0016 account lifecycle work. Broker-side deleted-entity fallback is also outside F0006 closeout; active broker deletion remains constrained by broker dependency rules.

## Out of Scope

- Free-form inline editing of individual fields without an explicit edit action (use a dedicated edit form or dialog instead)
- Document upload directly from detail view (F0020 handles document management)
- Communication or email integration from detail view (F0021)
- Deleted or merged linked-entity fallback behavior beyond active-record assumptions
- Printing or PDF export of submission detail

## UI/UX Notes

- Screens involved: Submission Detail
- Key interactions: Status badge with color coding; linked entity names as clickable links to 360 views; an explicit edit action for mutable intake fields; completeness panel as a collapsible checklist; timeline as a scrollable, paginated feed; action bar with contextual transition buttons
- Layout: Header at top, two-column body (left: submission fields + completeness; right: timeline), action bar pinned at bottom or header

## Questions & Assumptions

**Open Questions:**
- None

**Assumptions (to be validated):**
- Timeline events are paginated independently (default 20 per page, load-more pattern)
- Completeness panel is read-only — it reflects state, while field edits happen through the dedicated edit action and document changes happen in F0020

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (ABAC-scoped read)
- [ ] Audit/timeline logged for any successful field edits (`SubmissionUpdated`)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0006-S0003-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
