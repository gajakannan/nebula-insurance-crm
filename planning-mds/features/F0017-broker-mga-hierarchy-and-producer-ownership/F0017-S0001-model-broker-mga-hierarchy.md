---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0017-S0001 — Model broker/MGA hierarchy (self-referencing, arbitrary depth)

## Story Header

**Story ID:** F0017-S0001
**Feature:** F0017 — Broker/MGA Hierarchy, Producer Ownership & Territory Management
**Title:** Model broker/MGA hierarchy (self-referencing, arbitrary depth)
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution & Marketing Manager
**I want** to place each broker, MGA, sub-broker, and producer under its correct parent in one channel tree
**So that** Nebula reflects how our distribution channels are actually structured and accountable

## Context & Background

F0002 established broker/MGA records as a flat list. This story adds the
parent-child relationship so the real channel structure (an MGA over brokers,
brokers over sub-brokers, sub-brokers over producers) is explicit and reusable
by routing (F0022) and reporting (F0023). Per the G1 clarification the hierarchy
is an **arbitrary-depth, self-referencing tree** (any node may parent any node),
which requires cycle/orphan guards and a cached-ancestry read model.

## Acceptance Criteria

**Happy Path:**
- **Given** an existing distribution node (MGA / broker / sub-broker / producer)
- **When** a Distribution & Marketing Manager sets its parent to another distribution node and saves
- **Then** the parent-child link is stored, the child appears under the parent, the change persists after reload, and the node's cached ancestry (root→node path) is recomputed
- **And** an immutable timeline/audit event records the reparent (actor, old→new parent) per F0017-S0005

**Alternative Flows / Edge Cases:**
- Self-parent (node set as its own parent) → rejected, validation error "A node cannot be its own parent" (HTTP 422)
- Cycle (set an ancestor's parent to one of its descendants) → rejected, "This change would create a cycle" (HTTP 409 Conflict)
- Reparent a node that has children → the whole subtree moves; every descendant's ancestry cache recomputes; no orphaned subtree
- Clear parent (set to null) → node becomes a root; descendants unaffected
- Arbitrary depth → adding a level beyond current maximum depth succeeds (no fixed-tier cap)
- Concurrent reparent of the same node → optimistic-concurrency guard; the stale write is rejected (HTTP 409)

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Broker/MGA Detail → Hierarchy panel → "Set / Change Parent" | Pick parent → Save | Enabled for Distribution & Marketing Manager on active nodes; read-only on archived/retired nodes | `parentId` set; ancestry cache recomputed for node + descendants | Link visible after reload and via ancestors/descendants query | Distribution & Marketing Manager only; blocked (HTTP 403) for other roles; blocked on archived nodes |

Required checks for mutation stories:
- [x] Render-only behavior cannot satisfy the story.
- [x] The save path has validation (self-parent, cycle, missing/inactive parent) and error behavior specified.
- [x] A successful mutation emits a timeline/audit event (see F0017-S0005).
- [x] Tests prove the parent persists after reload / query invalidation.

## Data Requirements

**Required Fields:**
- `nodeId`: distribution node identity
- `parentId`: nullable self-reference to another distribution node
- `nodeType`: one of MGA | Broker | SubBroker | Producer
- Audit metadata: `changedBy`, `changedAt`

**Optional Fields:**
- `cachedAncestryPath` (materialized path) and `depth` for traversal acceleration

**Validation Rules:**
- No self-parent; no cycle; parent must exist and be active.
- `nodeType` ordering is advisory (warn, not block) because arbitrary depth is allowed.
- Cached ancestry must stay consistent with `parentId` after every mutation.

## Role-Based Visibility

**Roles that can set/change parent:**
- Distribution & Marketing Manager — full edit

**Data Visibility:**
- InternalOnly content: full hierarchy is readable by all authenticated internal users in this slice.
- ExternalVisible content: none.
- Authorization note: hierarchy-aware access-control **enforcement is deferred** (G1 decision). The model is built so a later authorization change can scope visibility; today an unauthorized role attempting a mutation receives HTTP 403, but reads are not yet hierarchy-scoped.

## Non-Functional Expectations

- Performance: reparent + ancestry recompute completes < 1s for trees up to 5,000 nodes.
- Security: only authorized roles may mutate; cycle detection is O(depth) via cached ancestry.
- Reliability: optimistic concurrency prevents lost updates on concurrent reparent.

## Dependencies

**Depends On:**
- F0002 — provides the broker/MGA records this hierarchy links.

**Related Stories:**
- F0017-S0002 — reads this model for traversal.
- F0017-S0005 — audits this mutation.

## Business Rules

1. **No cycles:** a node may not be a descendant of itself.
2. **No self-parent.**
3. **Arbitrary depth:** no fixed tier count; `nodeType` ordering is advisory only.
4. **Ancestry consistency:** cached ancestry must equal the resolved `parentId` chain after any change.

## Out of Scope

- Hierarchy-aware access-control enforcement (deferred to a later authorization change).
- Hierarchy-aware rollup reporting (deferred to F0037, built on the F0023 reporting substrate).
- Commission attribution (F0025).

## UI/UX Notes (Optional)

- Screens involved: Broker/MGA Detail (Hierarchy panel).
- Key interactions: choose parent via type-ahead; inline validation for self/cycle.

## Questions & Assumptions

**Open Questions:**
- [ ] None blocking — resolved at the G1 clarification gate.

**Assumptions (to be validated):**
- Cached ancestry / materialized path is the chosen read accelerator; the exact mechanism is finalized in Phase B architecture.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (self-parent, cycle, orphan, depth, concurrency)
- [ ] Permissions enforced (HTTP 403 on unauthorized mutate)
- [ ] Audit/timeline logged (via F0017-S0005)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated

## Review Provenance

Recorded in F0017 `STATUS.md` (`Required Signoff Roles`, `Story Signoff Provenance`). Minimum roles: Quality Engineer, Code Reviewer.
