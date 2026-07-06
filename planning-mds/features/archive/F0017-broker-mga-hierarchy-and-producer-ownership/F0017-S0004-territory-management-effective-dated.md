---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0017-S0004 — Define and manage territories with effective-dated assignment

## Story Header

**Story ID:** F0017-S0004
**Feature:** F0017 — Broker/MGA Hierarchy, Producer Ownership & Territory Management
**Title:** Define and manage territories with effective-dated assignment
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution & Marketing Manager
**I want** to define territories and assign brokers and producers to them with effective dates, while the system prevents conflicting overlaps
**So that** regional accountability is unambiguous and stays historically accurate as territories are reorganized

## Context & Background

Territories group distribution by region/segment for accountability and (later)
reporting. Per the G1 clarification, territory assignment is **effective-dated**
and must **reject conflicting overlaps** so a broker/producer is not
simultaneously claimed by two territories for the same period in a way that
would corrupt downstream routing (F0022) and reporting (F0023).

## Acceptance Criteria

**Happy Path:**
- **Given** a defined territory
- **When** a Distribution & Marketing Manager assigns a broker or producer to it with an effective-from date and saves
- **Then** an effective-dated territory assignment is created, the member appears under the territory, it persists after reload, and an immutable timeline/audit event records the assignment (actor, old→new territory) per F0017-S0005

**Alternative Flows / Edge Cases:**
- Create territory → name + region/segment criteria saved; duplicate territory name rejected (HTTP 409)
- Overlapping assignment → assigning a member already actively assigned to a conflicting territory for the same period is rejected with "Member already assigned to a conflicting territory for this period" (HTTP 409)
- Reassignment → assigning to a new territory with a later effective date closes the prior assignment period; both remain queryable
- Point-in-time read → "territory of member as of date D" returns the assignment effective then
- Effective date ordering invalid (`effectiveFrom` after `effectiveTo`) → validation error (HTTP 422)
- Empty territory (no members) → valid state, shown as empty, not an error

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Territory admin → "Create Territory" / Territory Detail → "Assign Member" | Enter criteria / pick member + effective-from → Save | Enabled for Distribution & Marketing Manager; read-only on archived territories | Territory created or effective-dated assignment row added; prior period closed on reassignment | Territory + members visible after reload; "as of" query returns correct assignment | Distribution & Marketing Manager only; HTTP 403 otherwise |

Required checks for mutation stories:
- [x] Render-only behavior cannot satisfy the story.
- [x] The save path has validation (duplicate name, overlap, ordering) and error behavior specified.
- [x] A successful mutation emits a timeline/audit event (see F0017-S0005).
- [x] Tests prove assignment and point-in-time history persist after reload.

## Data Requirements

**Required Fields:**
- Territory: `territoryId`, `name`, region/segment criteria
- Assignment: `assignmentId`, `territoryId`, `memberRef` (broker/producer), `effectiveFrom`, `effectiveTo` (nullable)
- Audit metadata: `changedBy`, `changedAt`

**Optional Fields:**
- Territory `description`, parent territory (future hierarchy of territories — out of scope)

**Validation Rules:**
- Territory `name` unique.
- No conflicting **active** overlapping assignment for the same member/period.
- `effectiveFrom` < `effectiveTo` when both present.

## Role-Based Visibility

**Roles that can create/assign:**
- Distribution & Marketing Manager.

**Data Visibility:**
- InternalOnly content: territories and assignments are internal.
- Authorization note: territory-scoped authorization (who can see/manage which territory) is **deferred** (G1). Unauthorized mutation attempts return HTTP 403; the model is built for a later authz change to consume.

## Non-Functional Expectations

- Performance: create/assign and "as of" reads complete < 1s.
- Reliability: reassignment is transactional (close prior + open new succeed or roll back together).
- Security: only authorized roles may mutate.

## Dependencies

**Depends On:**
- F0002 — broker/producer records.
- F0017-S0001 — members exist as hierarchy nodes.

**Related Stories:**
- F0017-S0005 — audits territory changes.
- F0022, F0023 — consume territory as a routing/reporting dimension (those modules do not recompute it).

## Business Rules

1. **No conflicting overlap:** a member cannot hold two conflicting active territory assignments for the same period.
2. **Effective-dated history:** reassignment closes the prior period rather than overwriting.
3. **Unique territory name** within the active set.

## Out of Scope

- Hierarchy of territories (nested territories) — future.
- Territory-based access-control enforcement (deferred).
- Territory rollup metrics (deferred to F0037, built on the F0023 reporting substrate).

## UI/UX Notes (Optional)

- Screens involved: Territory admin list, Territory Detail (members + history).
- Key interactions: create territory, assign member with effective date, "as of" date picker.

## Questions & Assumptions

**Open Questions:**
- [ ] None blocking — resolved at G1.

**Assumptions (to be validated):**
- "Conflicting" overlap in MVP means same member + overlapping active period; richer geographic overlap rules are deferred.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (duplicate, overlap, ordering, point-in-time, empty)
- [ ] Permissions enforced (HTTP 403 on unauthorized mutate)
- [ ] Audit/timeline logged (via F0017-S0005)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated

## Review Provenance

Recorded in F0017 `STATUS.md`. Minimum roles: Quality Engineer, Code Reviewer.
