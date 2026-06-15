---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0017-S0003 — Assign and maintain producer ownership (effective-dated)

## Story Header

**Story ID:** F0017-S0003
**Feature:** F0017 — Broker/MGA Hierarchy, Producer Ownership & Territory Management
**Title:** Assign and maintain producer ownership (effective-dated)
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution & Marketing Manager
**I want** to assign a producer as the owner of an account or broker relationship with an effective date, and reassign it later without losing history
**So that** accountability is always clear and point-in-time attribution stays accurate as staffing changes

## Context & Background

Producer ownership is who is accountable for a given account/broker
relationship. Per the G1 clarification, ownership is **effective-dated** in MVP:
each assignment has a start (and implied end when superseded), so a reassignment
preserves the prior owner's period rather than overwriting it. This keeps
historical attribution and (later) accurate rollups and commission attribution
(F0025) possible.

## Acceptance Criteria

**Happy Path:**
- **Given** an account/broker relationship with no current owner
- **When** a Distribution & Marketing Manager assigns a producer as owner with an effective-from date and saves
- **Then** an effective-dated ownership record is created, the producer shows as current owner, the assignment persists after reload, and an immutable timeline/audit event records the assignment (actor, old→new owner) per F0017-S0005

**Alternative Flows / Edge Cases:**
- Reassignment → assigning a new owner with a later effective date closes the prior owner's period (sets its effective-to) and starts the new period; both periods remain queryable
- Point-in-time read → querying ownership "as of" a past date returns the owner who was effective then, not the current owner
- Overlapping active ownership for the same scope → rejected with "Ownership already assigned for this period" (HTTP 409 Conflict)
- Backdated assignment earlier than an existing period → rejected or requires explicit correction path (HTTP 422) so history is not silently rewritten
- Effective-from in the wrong order (after an effective-to) → validation error (HTTP 422)

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Account/Broker Detail → Ownership panel → "Assign / Reassign Owner" | Pick producer + effective-from → Save | Enabled for Distribution & Marketing Manager; read-only on archived scopes | New effective-dated ownership row; prior period closed if reassigning | Current owner shown after reload; prior period visible in history; "as of" query returns correct owner | Distribution & Marketing Manager only; HTTP 403 otherwise |

Required checks for mutation stories:
- [x] Render-only behavior cannot satisfy the story.
- [x] The save path has validation (overlap, ordering, backdating) and error behavior specified.
- [x] A successful mutation emits a timeline/audit event (see F0017-S0005).
- [x] Tests prove current owner and point-in-time history persist after reload.

## Data Requirements

**Required Fields:**
- `ownershipId`, `scopeRef` (account/broker relationship being owned), `producerId`, `effectiveFrom`
- `effectiveTo` (nullable; set when superseded)
- Audit metadata: `changedBy`, `changedAt`

**Optional Fields:**
- `assignmentReason` / note

**Validation Rules:**
- No two **active** (open `effectiveTo`) ownership records for the same `scopeRef`.
- `effectiveFrom` < `effectiveTo` when both present.
- Backdating before an existing period requires an explicit correction path, not a silent insert.

## Role-Based Visibility

**Roles that can assign/reassign:**
- Distribution & Marketing Manager.

**Data Visibility:**
- InternalOnly content: ownership records and history are internal.
- Authorization note: ownership-scoped authorization (restricting who sees/owns which scope) is **deferred** (G1). Unauthorized mutation attempts return HTTP 403; this story models data a later authz change consumes.

## Non-Functional Expectations

- Performance: assign/reassign and "as of" reads complete < 1s.
- Reliability: reassignment is transactional — closing the old period and opening the new period either both succeed or both roll back.
- Security: only authorized roles may mutate.

## Dependencies

**Depends On:**
- F0002 — broker/producer records.
- F0017-S0001 — producers exist as hierarchy nodes.

**Related Stories:**
- F0017-S0005 — audits ownership changes.
- F0025 — consumes ownership for commission attribution (out of scope here).

## Business Rules

1. **Single active owner per scope:** at any instant a scope has at most one open ownership period.
2. **History is append-style:** reassignment closes the prior period; periods are never destructively overwritten.
3. **Point-in-time truth:** "owner as of date D" is the period covering D.

## Out of Scope

- Commission splits / revenue attribution (F0025).
- Ownership-scoped access-control enforcement (deferred).
- Bulk reassignment tooling (future).

## UI/UX Notes (Optional)

- Screens involved: Account/Broker Detail (Ownership panel + history list).
- Key interactions: assign owner, view ownership timeline, "as of" date picker.

## Questions & Assumptions

**Open Questions:**
- [ ] None blocking — resolved at G1.

**Assumptions (to be validated):**
- A scope has a single owner dimension in MVP (no co-ownership); splits are F0025.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (overlap, ordering, backdating, point-in-time)
- [ ] Permissions enforced (HTTP 403 on unauthorized mutate)
- [ ] Audit/timeline logged (via F0017-S0005)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated

## Review Provenance

Recorded in F0017 `STATUS.md`. Minimum roles: Quality Engineer, Code Reviewer.
