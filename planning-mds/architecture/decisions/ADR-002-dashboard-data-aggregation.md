# ADR-002: Dashboard Data Aggregation Strategy

**Status:** Accepted

**Date:** 2026-02-14

**Deciders:** Architecture Team

**Technical Story:** Phase B — Dashboard-first architecture (F0001)

---

## Context and Problem Statement

The Nebula Dashboard (F0001) displays five widgets that pull data from multiple backend modules:

| Widget | Source Modules | Query Shape |
|--------|---------------|-------------|
| Nudge Cards | Task, Submission, Renewal | Cross-module, prioritized merge |
| KPI Metrics | Broker, Submission, Renewal, WorkflowTransition | Aggregate COUNT / AVG across modules |
| Pipeline Summary (pills) | Submission, Renewal | GROUP BY status with COUNT |
| Pipeline Popover (mini-cards) | Submission or Renewal, WorkflowTransition, UserProfile | Lazy-loaded detail rows |
| My Tasks & Reminders | Task | Single-module, user-scoped |
| Broker Activity Feed | ActivityTimelineEvent, Broker, UserProfile | Single-module with JOINs |

**Key questions:**
1. Should the frontend make parallel calls to individual module endpoints, or should a single backend-for-frontend (BFF) endpoint aggregate the data?
2. How do we ensure a single widget failure does not block the entire dashboard?
3. How do we keep queries within the p95 < 2 s dashboard budget?

---

## Decision Drivers

- **Performance:** p95 < 2 s for full dashboard render (Phase A requirement)
- **Resilience:** Widget-level failure isolation — one failing widget must not block others
- **Simplicity:** Modular monolith; avoid premature microservice patterns
- **ABAC Consistency:** Every query must pass through Casbin scope filtering
- **Frontend DX:** TanStack Query manages caching, loading states, and error boundaries per widget

---

## Considered Options

### Option 1: Single BFF Endpoint (`GET /dashboard`)

A single endpoint returns all widget data in one response.

- Pros: Single HTTP round-trip; server can parallelise internal queries; easy cache key
- Cons: All-or-nothing failure (one slow query blocks everything); large response payload; violates widget-level isolation; hard to cache individual widgets with different staleness

### Option 2: Per-Widget Endpoints (Selected)

Each widget calls its own dedicated endpoint. The frontend fires all calls in parallel via TanStack Query.

- Pros: Widget-level failure isolation; independent caching/staleness; smaller response payloads; natural mapping to TanStack Query's `useQuery` per widget; easier to test and evolve independently
- Cons: More HTTP round-trips (5 parallel calls); slight overhead from repeated auth/ABAC checks

### Option 3: GraphQL

Use a GraphQL layer to let the frontend request exactly the fields it needs per widget.

- Pros: Flexible query composition; single endpoint
- Cons: Adds GraphQL server complexity; team has no GraphQL experience; does not naturally map to the modular monolith; over-engineered for 5 predictable widget shapes

---

## Decision Outcome

**Chosen option: Option 2 — Per-Widget Endpoints**

The dashboard frontend will make **5 parallel API calls** on page load, one per widget:

| Endpoint | Widget | Lazy? |
|----------|--------|-------|
| `GET /dashboard/nudges` | Nudge Cards | No (page load) |
| `GET /dashboard/kpis` | KPI Metrics | No (page load) |
| `GET /dashboard/pipeline` | Pipeline Summary pills | No (page load) |
| `GET /dashboard/pipeline/{entityType}/{status}/items` | Pipeline Popover | Yes (on hover/click) |
| `GET /my/tasks` | My Tasks & Reminders | No (page load) |
| `GET /timeline/events?entityType=Broker&limit=20` | Broker Activity Feed | No (page load) |

The **pipeline popover mini-cards** are the only lazy-loaded call (triggered on user interaction, p95 < 300 ms).

### Why This Works in a Modular Monolith

All endpoints live in the same deployable. "Per-widget endpoints" means separate route handlers and application service methods — not separate services. The overhead of 5 parallel HTTP calls to the same host is negligible (shared connection pool, no network hops).

---

## Consequences

### Positive

- **Resilience:** Each widget renders independently via its own `useQuery`. A failed KPI query shows "—" in KPI cards without blocking the pipeline widget.
- **Cacheability:** TanStack Query can cache each widget's data with different `staleTime` values (e.g., KPIs stale after 60 s, activity feed stale after 30 s).
- **Testability:** Each dashboard endpoint has its own integration test. Widget queries can be load-tested independently.
- **Evolvability:** Adding a new widget means adding one endpoint and one `useQuery` — no coordination with other widgets.

### Negative

- **5 HTTP Requests:** Slightly more overhead than a single call. Mitigated by HTTP/2 multiplexing and shared connection in the modular monolith.
- **Repeated ABAC Checks:** Each endpoint extracts and evaluates user scope independently. Mitigated by caching the user's scope resolution in a request-scoped service.

### Neutral

- **No Pre-Computation for MVP:** KPI counts and pipeline aggregations run live queries against indexed tables. If performance degrades at scale, a future ADR can introduce materialized views or a pre-computation job.

---

## Implementation Notes

### Backend Structure (Modular Monolith)

```
Nebula.Api/
  Endpoints/
    DashboardEndpoints.cs      # Maps /dashboard/* routes
    MyTasksEndpoints.cs        # Maps /my/tasks
    TimelineEndpoints.cs       # Maps /timeline/*

Nebula.Application/
  Dashboard/
    GetKpisQuery.cs            # Aggregates broker/sub/renewal counts
    GetPipelineQuery.cs        # Groups by status, returns counts
    GetPipelineItemsQuery.cs   # Lazy-loaded mini-cards for a status
    GetNudgesQuery.cs          # Cross-module nudge computation
  Tasks/
    GetMyTasksQuery.cs         # User-scoped task list
  Timeline/
    GetTimelineEventsQuery.cs  # Filtered timeline events
```

### Frontend Structure

```tsx
// Each widget has its own useQuery with independent error handling
function Dashboard() {
  return (
    <div>
      <NudgeCardsWidget />   {/* useQuery(['nudges']) */}
      <KpiCardsWidget />     {/* useQuery(['kpis']) */}
      <PipelineWidget />     {/* useQuery(['pipeline']) */}
      <TasksWidget />        {/* useQuery(['myTasks']) */}
      <ActivityFeedWidget /> {/* useQuery(['brokerActivity']) */}
    </div>
  );
}
```

### Performance Budget Allocation

| Call | Server Budget | Notes |
|------|--------------|-------|
| Nudges | 500 ms | 3 parallel sub-queries (tasks, submissions, renewals) |
| KPIs | 500 ms | 4 aggregate queries (can parallelise) |
| Pipeline | 300 ms | 2 GROUP BY queries |
| My Tasks | 200 ms | Single indexed query |
| Activity Feed | 200 ms | Single indexed query |
| **Total (parallel)** | **~500 ms** | All calls fire in parallel; wall-clock is max, not sum |

---

## Related ADRs

- ADR-001: JSON Schema Validation (request/response validation)
- ADR-003: Task Entity and Nudge Engine Design
- ADR-004: Frontend Dashboard Widget Architecture

---

**Last Updated:** 2026-02-14
