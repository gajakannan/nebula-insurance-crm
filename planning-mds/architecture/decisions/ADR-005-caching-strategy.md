# ADR-005: Caching Strategy (In-Memory First)

## Status

- [ ] Proposed
- [x] Accepted
- [ ] Superseded
- [ ] Rejected

## Context

The implementation phase will introduce repeated reads for reference data, dashboard queries, and ABAC scope resolution. We need a caching strategy that is simple, safe, and consistent across services, while keeping the framework lightweight.

## Decision Drivers

- Keep runtime complexity low for the initial implementation.
- Prefer in-memory caching for small, low-churn datasets.
- Ensure multi-instance consistency when required.
- Avoid caching sensitive, user-specific data without explicit scoping.

## Decision

Adopt an **in-memory-first caching strategy** for small, low-churn datasets and request-scoped computations. Use **external cache (Redis)** only when cross-instance consistency or scale demands it.

Default pattern is **cache-aside**. **Write-through** is used only for reference data managed by admin operations where read-after-write is required. **Write-behind** is not used in the MVP.

## Options Considered

1. **In-memory only (MemoryCache)**
2. **External cache only (Redis)**
3. **Hybrid: in-memory first, external when needed**

## Pros / Cons

**Option 1: In-memory only**
- ✅ Lowest complexity and operational overhead
- ❌ Not shared across instances; can cause cache misses at scale

**Option 2: External only**
- ✅ Shared across instances; consistent cache behavior
- ❌ Higher operational complexity; network dependency and latency

**Option 3: Hybrid (chosen)**
- ✅ Simple by default, scales when necessary
- ✅ Allows per-use-case decisions
- ❌ Requires clear criteria and discipline

## Caching Patterns

**Cache-aside (default)**
- Read from cache; on miss, fetch from DB and populate cache.
- Use for dashboard aggregates, reference data, and computed view models.

**Write-through (selective)**
- On write, update DB and cache in the same request.
- Use for admin-managed reference data that must be immediately consistent.

**Write-behind**
- Not used in MVP (risk of inconsistency).

## Criteria For External Cache

Use Redis when **any** of these are true:
- Multi-instance deployments require shared cache state.
- Cached payloads exceed in-memory size thresholds.
- Cache entry reuse across users or services is high.
- Cache invalidation must be centrally coordinated.

## Criteria For In-Memory Cache

Use MemoryCache when **all** are true:
- Data is small and changes infrequently (reference data).
- Cache scope is per-request or per-process (ABAC scope resolution).
- Consistency across instances is not required for correctness.

## Security & Compliance Notes

- Do not cache secrets or raw PII without encryption and explicit approval.
- Include tenant/subject identifiers in cache keys for scoped data.
- Apply TTLs to all cached entries; avoid unbounded caches.

## References

- planning-mds/architecture/decisions/ADR-002-dashboard-data-aggregation.md
- planning-mds/architecture/data-model.md

## Follow-up Actions

- [x] Update SOLUTION-PATTERNS.md with caching guidelines.
- [ ] Add runtime cache configuration defaults during implementation.
- [ ] Add cache invalidation tests for reference data updates.
