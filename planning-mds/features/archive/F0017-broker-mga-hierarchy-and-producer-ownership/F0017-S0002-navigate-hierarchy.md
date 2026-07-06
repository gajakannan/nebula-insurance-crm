---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0017-S0002 — Navigate and traverse the distribution hierarchy

## Story Header

**Story ID:** F0017-S0002
**Feature:** F0017 — Broker/MGA Hierarchy, Producer Ownership & Territory Management
**Title:** Navigate and traverse the distribution hierarchy
**Priority:** High
**Phase:** MVP

## User Story

**As a** Broker Relationship Coordinator
**I want** to view the channel tree and drill down from an MGA to its brokers, sub-brokers, and producers
**So that** I can understand reporting lines and find the right relationship quickly

## Context & Background

Once the hierarchy is modeled (F0017-S0001), users need to read it. This story
delivers the read surface: ancestors (path to root), direct children, and the
full descendant subtree, with drill-down navigation. It is read-only — no
mutation. Traversal performance matters because the tree is arbitrary depth.

## Acceptance Criteria

**Happy Path:**
- **Given** a distribution node with children
- **When** a Broker Relationship Coordinator opens its Hierarchy view
- **Then** they see the node's ancestors (breadcrumb to root) and its direct children, and can expand any child to drill into deeper levels

**Alternative Flows / Edge Cases:**
- Leaf node (a producer with no children) → shows an empty-children state, not an error
- Root node (no parent) → ancestors breadcrumb shows just the node
- Deep subtree → expanding loads descendants lazily; the view remains responsive
- Node not found / no read access → HTTP 404 / 403 surfaced as an inline message, not a blank screen

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A — read-only story. No mutation; no editable state.

## Data Requirements

**Required Fields (read/query inputs and outputs):**
- Query input: `nodeId`, optional `depth` for subtree expansion
- Output per node: `nodeId`, `nodeType`, display name, `parentId`, child count

**Optional Fields:**
- `cachedAncestryPath` to render the breadcrumb without repeated lookups

**Validation Rules:**
- `depth` is bounded (e.g., default 2 levels per expansion) to protect response size.

## Role-Based Visibility

**Roles that can view:**
- Broker Relationship Coordinator, Distribution & Marketing Manager, and other authenticated internal users.

**Data Visibility:**
- InternalOnly content: the full tree is readable by authenticated internal users in this slice.
- ExternalVisible content: none.
- Authorization note: hierarchy-scoped read authorization is **deferred** (G1 decision); a later authorization change will restrict which subtrees a user can see. Unauthorized/unauthenticated access returns HTTP 403/401.

## Non-Functional Expectations

- Performance: ancestors + first level of children render in < 1s for trees up to 5,000 nodes; lazy expansion keeps each request bounded.
- Reliability: not-found and forbidden states are handled with inline messaging.

## Dependencies

**Depends On:**
- F0017-S0001 — provides the hierarchy model and cached ancestry.

**Related Stories:**
- F0017-S0003, F0017-S0004 — ownership/territory context shown alongside the node.

## Business Rules

1. **Bounded expansion:** the descendant query returns a bounded depth per request to keep responses paginated/lazy.
2. **Consistent identifiers:** drill-down uses the same node identifiers as create/assign flows so cross-feature linking (F0022/F0023) is stable.

## Out of Scope

- Editing the hierarchy (F0017-S0001 owns mutation).
- Hierarchy-aware rollup metrics (deferred to F0037, built on the F0023 reporting substrate).

## UI/UX Notes (Optional)

- Screens involved: Broker/MGA Detail (Hierarchy view), with breadcrumb + expandable tree.
- Key interactions: expand/collapse, breadcrumb navigation, drill-down to a child's detail.

## Questions & Assumptions

**Open Questions:**
- [ ] None blocking — resolved at G1.

**Assumptions (to be validated):**
- Lazy, depth-bounded expansion is acceptable for MVP rather than loading the entire subtree at once.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (leaf, root, deep subtree, not-found/forbidden)
- [ ] Permissions enforced (HTTP 401/403)
- [ ] Audit/timeline logged (N/A — read-only)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated

## Review Provenance

Recorded in F0017 `STATUS.md`. Minimum roles: Quality Engineer, Code Reviewer.
