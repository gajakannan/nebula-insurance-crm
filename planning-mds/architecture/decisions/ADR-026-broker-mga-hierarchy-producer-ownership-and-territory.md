---
template: adr
version: 1.1
applies_to: architect
---

# ADR-026: Broker/MGA Hierarchy, Producer Ownership & Territory

## Status

- [ ] Proposed
- [x] Accepted (operator G5 Phase B approval — plan run `2026-06-06-5fb353e9`, 2026-06-06)
- [ ] Superseded
- [ ] Rejected

## Context

F0017 makes the commercial-P&C distribution structure explicit in Nebula:
broker/MGA hierarchy, producer ownership, and territory management. F0002
already delivered flat broker/MGA + contact records; F0017 adds the structural
relationships on top. The operator resolved four scoping decisions at the plan
G1 clarification gate (plan run `2026-06-06-5fb353e9`):

1. **MVP** = hierarchy modeling + producer ownership + territory management (hierarchy-aware **rollup reporting deferred to F0037**).
2. **Hierarchy** = **arbitrary-depth, self-referencing tree** (any node may parent any node).
3. **Ownership & territory** = **effective-dated** (point-in-time history preserved).
4. **Authorization** = structural/reporting-only; hierarchy-aware **access-control enforcement deferred to F0037**. F0017 models the data a later authz change consumes; all structural changes are audited.

Nebula's platform invariant applies: it **records status/facts, it does not
compute economics** — commission/splits/revenue stay with F0025. F0017 is the
structural model only.

## Decision Drivers

- Reflect real MGA → broker → sub-broker → producer structure without a fixed tier cap.
- Preserve historically accurate attribution as staffing/territories change.
- Keep the first slice shippable (defer enforcement + rollups to F0037).
- Reuse existing platform patterns (audit/timeline, ProblemDetails, ABAC scaffolding, clean architecture) per `SOLUTION-PATTERNS.md`.
- Provide stable identifiers + effective-dated reads that F0022 (routing), F0023 (reporting), F0008 (insights), and F0037 (enforcement + rollups) can consume without recomputing structure.

## Decision

1. **Self-referencing hierarchy.** Model the distribution tree as a nullable
   `parent_id` self-reference across distribution nodes (MGA, broker,
   sub-broker, producer). `node_type` ordering is **advisory** (not enforced) so
   arbitrary depth is allowed. Integrity rules: **no self-parent, no cycles, no
   orphaned subtree on reparent**; cycle detection is O(depth).

2. **Cached ancestry read model.** Maintain a denormalized **materialized
   ancestry path** (root→node) per node, recomputed transactionally on every
   reparent (node + all descendants). Reads (ancestors breadcrumb, descendant
   subtree, drill-down) use the cache; expansion is **depth-bounded/lazy** to
   keep responses paginated. This keeps traversal off recursive CTE hot paths
   for the common reads while the tree remains arbitrary depth.

3. **Effective-dated relationships.** Producer ownership and territory
   assignment are modeled as **effective-dated relationship rows**
   (`effective_from`, nullable `effective_to`). Reassignment **closes the prior
   period** (sets `effective_to`) and **opens a new period** in one transaction;
   rows are never destructively overwritten. Invariants: at most one **open**
   (`effective_to IS NULL`) period per scope; `effective_from < effective_to`;
   backdating before an existing period requires an explicit correction path
   (not a silent insert). "As of `D`" reads return the period covering `D`.

4. **Territory overlap rule.** A member may not hold two **conflicting active**
   territory assignments for the same period; conflicts are rejected (HTTP 409).
   Territory names are unique within the active set. Nested territories are out
   of scope.

5. **Audit via the existing timeline.** Every successful structural mutation
   (reparent, owner assign/reassign, territory create/assign) emits an
   **immutable** `entity:activity-timeline-event` (actor, timestamp, old→new),
   committed atomically with the mutation (reuse F0002's pattern; no new audit
   subsystem). Rejected mutations emit no event. Bulk reparent emits one event
   per affected node under a shared correlation id.

6. **Authorization posture.** Mutations require an authorized role (HTTP 403
   otherwise); **read scoping by hierarchy/territory/ownership is NOT enforced
   in F0017** — the tree and effective-dated records are readable by
   authenticated internal users. Enforcement is **F0037**. F0017 defines only
   standard role-based mutation guards, without recursive ABAC predicates:
   `distribution_node:update`, `producer_ownership:assign`, `territory:create`,
   and `territory:assign`. The feature action must enforce these resource/action
   names consistently in endpoint, service, and policy tests.

7. **API and schema surface.** RESTful paths follow the local no-`/api` route
   convention from `api-design-guide.md`. The concrete OpenAPI contract is
   authored in `planning-mds/api/nebula-api.yaml`, backed by shared JSON Schemas
   in `planning-mds/schemas/`:
   - `PUT /distribution-nodes/{nodeId}/parent` — set/clear parent (self-parent 422, cycle 409, stale rowVersion 412, timeline event on success)
   - `GET /distribution-nodes/{nodeId}/ancestors` and `GET /distribution-nodes/{nodeId}/descendants?depth=` — cached-ancestry traversal and lazy descendant expansion
   - `POST /producer-ownership` and `GET /producer-ownership?scopeType=&scopeId=&asOf=` — effective-dated ownership for account or broker-relationship scopes
   - `POST /territories`, `GET /territories/{territoryId}/members?asOf=`, `POST /territories/{territoryId}/members`, and `GET /territory-assignments?memberType=&memberId=&asOf=` — territory definition, assignment, and point-in-time lookup
   - Timeline reuses the existing `TimelineEvent` schema and timeline surfaces; F0017 mutations emit immutable events atomically rather than exposing direct user-authored audit writes.

## Options Considered

1. **Self-referencing tree + cached ancestry (CHOSEN).**
2. **Fixed-tier columns (mga_id/broker_id/producer_id).**
3. **Closure table (explicit ancestor/descendant rows).**
4. **Current-state-only relationships (no effective dating).**

## Pros / Cons

**Option 1 — Self-ref + cached ancestry**
- ✅ Arbitrary depth (operator requirement); single relationship concept; cache keeps reads fast.
- ❌ Reparent must recompute descendant caches; needs cycle/orphan guards.

**Option 2 — Fixed tiers**
- ✅ Simplest queries.
- ❌ Violates the arbitrary-depth requirement; rigid to channel reorganizations.

**Option 3 — Closure table**
- ✅ Fast ancestor/descendant set queries at any depth.
- ❌ O(n·depth) write amplification and more moving parts than MVP needs; revisit in F0037 if rollups demand it.

**Option 4 — Current-state only**
- ✅ Least storage/query complexity.
- ❌ Loses point-in-time attribution (operator chose effective-dated); would corrupt later rollups/commission attribution.

## Consequences

- **Development:** new entities (`producer`, `territory`, `producer-ownership`, `territory-assignment`) + a self-reference and ancestry cache on distribution nodes; transactional reparent and reassignment; overlap/cycle validators; timeline emission on each mutation.
- **Operations:** monitor reparent/recompute cost on deep trees; consider async ancestry recompute or a closure table in F0037 if hot.
- **Downstream:** F0022/F0023/F0008/F0037 consume stable node ids + effective-dated reads; they must not recompute structure.
- **Risks & mitigations:** deep-tree recompute cost → cached path + bounded lazy reads, async path open for F0037; accidental history rewrite → backdating correction path + append-style periods.

## Security & Compliance Notes (If Applicable)

- F0017 introduces **no** hierarchy-aware read enforcement; that is F0037 and is where the security-sensitive scope (recursive visibility) lands. Because enforcement is deferred, Security Reviewer is **not forced** for F0017 (recorded in F0017 STATUS.md).
- All structural changes are audited (immutable timeline), preserving an attribution trail for future access-boundary and economics decisions.

## References

- F0017 PRD + stories F0017-S0001…S0005 (`planning-mds/features/F0017-broker-mga-hierarchy-and-producer-ownership/`)
- F0017 OpenAPI + JSON Schemas (`planning-mds/api/nebula-api.yaml`, `planning-mds/schemas/distribution-node*.schema.json`, `producer-ownership*.schema.json`, `territory*.schema.json`)
- F0037 (deferred enforcement + rollups), F0002 (broker/MGA + timeline pattern), F0025 (economics, out of scope)
- ADR-008 (Casbin), ADR-011 (transition history / append-only), ADR-013 (routing consumes hierarchy/territory), ADR-014-search (reporting substrate)
- `planning-mds/architecture/SOLUTION-PATTERNS.md`, `planning-mds/architecture/data-model.md` §9

## Follow-up Actions

- [ ] Feature action G0 must turn the OpenAPI/schema surface above into `feature-assembly-plan.md` endpoint/service/file tables without changing route names or payload semantics unless an ADR amendment is approved.
- [ ] F0037 to design hierarchy-aware read enforcement + rollup projections over these effective-dated records.
- [ ] Re-evaluate closure-table vs cached-path if rollup query cost in F0037 warrants it.
