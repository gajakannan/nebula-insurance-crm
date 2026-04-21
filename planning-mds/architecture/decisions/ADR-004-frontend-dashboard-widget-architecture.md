# ADR-004: Frontend Dashboard Widget Architecture

**Status:** Accepted

**Date:** 2026-02-14

**Deciders:** Architecture Team

**Technical Story:** Phase B — Dashboard F0001 frontend architecture

---

## Context and Problem Statement

The Nebula Dashboard renders 5 independent widgets (Nudge Cards, KPI Metrics, Pipeline Summary, My Tasks, Broker Activity Feed). Each widget fetches data independently (ADR-002). The frontend must handle:

1. **Independent loading states** — each widget shows its own skeleton
2. **Independent error boundaries** — one failed widget must not crash the page
3. **Lazy-loaded popovers** — pipeline mini-cards load on hover/click
4. **Session-scoped dismiss** — nudge card dismissals persist only for the current session
5. **Responsive layout** — 3 breakpoints (desktop > 1200px, tablet 768–1200px, mobile < 768px)

---

## Decision Drivers

- **Resilience:** Widget-level isolation is a Phase A requirement
- **Performance:** Dashboard full render p95 < 2 s; popover load p95 < 300 ms
- **Accessibility:** WCAG 2.1 AA; keyboard navigation; screen reader support
- **Stack Alignment:** React 18 + TypeScript + TanStack Query + Tailwind + shadcn/ui (locked in Section 2)

---

## Decision

### 1. Widget Isolation via React Error Boundaries + TanStack Query

Each widget is wrapped in a `<WidgetErrorBoundary>` component that catches render errors and displays a fallback message. Data fetching uses TanStack Query's built-in error/loading states.

```tsx
function Dashboard() {
  return (
    <DashboardLayout>
      <WidgetErrorBoundary fallback="Unable to load nudges">
        <NudgeCardsWidget />
      </WidgetErrorBoundary>

      <WidgetErrorBoundary fallback="Unable to load metrics">
        <KpiCardsWidget />
      </WidgetErrorBoundary>

      <WidgetErrorBoundary fallback="Unable to load pipeline">
        <PipelineWidget />
      </WidgetErrorBoundary>

      <WidgetErrorBoundary fallback="Unable to load tasks">
        <TasksWidget />
      </WidgetErrorBoundary>

      <WidgetErrorBoundary fallback="Unable to load activity">
        <ActivityFeedWidget />
      </WidgetErrorBoundary>
    </DashboardLayout>
  );
}
```

**Widget-level error display rules (from F0001 spec):**

| Widget | On Query Failure |
|--------|-----------------|
| Nudge Cards | Section not rendered (silent fail) |
| KPI Cards | Individual card shows "—" |
| Pipeline Summary | "Unable to load pipeline data" |
| My Tasks | "Unable to load tasks" |
| Activity Feed | "Unable to load activity feed" |

### 2. Skeleton Loading States

Each widget renders a skeleton placeholder matching its layout shape until data arrives. Skeletons use shadcn/ui `<Skeleton>` component.

```tsx
function KpiCardsWidget() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['dashboard', 'kpis'],
    queryFn: fetchKpis,
  });

  if (isLoading) return <KpiCardsSkeleton />;  // 4 card-shaped skeletons
  if (isError) return <KpiCardsError />;       // Shows "—" per card
  return <KpiCards data={data} />;
}
```

### 3. Pipeline Popover — Lazy-Loaded with `enabled` Flag

Mini-card data for pipeline popovers is fetched only when a user hovers/clicks a status pill. This uses TanStack Query with `enabled: false` until interaction.

```tsx
function PipelinePill({ entityType, status, count }: PillProps) {
  const [isOpen, setIsOpen] = useState(false);

  const { data: items, isLoading } = useQuery({
    queryKey: ['pipeline', entityType, status, 'items'],
    queryFn: () => fetchPipelineItems(entityType, status),
    enabled: isOpen,  // Only fetch when popover is open
    staleTime: 30_000,  // Cache for 30s to avoid re-fetch on re-hover
  });

  return (
    <Popover open={isOpen} onOpenChange={setIsOpen}>
      <PopoverTrigger asChild>
        <button
          aria-label={`${status}: ${count} ${entityType}s`}
          aria-expanded={isOpen}
        >
          <StatusPill label={status} count={count} />
        </button>
      </PopoverTrigger>
      <PopoverContent role="dialog" aria-label={`${status} details`}>
        {isLoading ? <MiniCardsSkeleton /> : <MiniCardsList items={items} />}
      </PopoverContent>
    </Popover>
  );
}
```

### 4. Nudge Card Dismiss — Session-Scoped State

Dismissed nudge card IDs are stored in React state (not persisted). On page refresh, dismissed nudges reappear if the condition still applies (per F0001-S0005 spec).

```tsx
function NudgeCardsWidget() {
  const { data: nudges } = useQuery({
    queryKey: ['dashboard', 'nudges'],
    queryFn: fetchNudges,
  });

  const [dismissedIds, setDismissedIds] = useState<Set<string>>(new Set());

  const visibleNudges = nudges
    ?.filter(n => !dismissedIds.has(nudgeKey(n)))
    .slice(0, 3);

  if (!visibleNudges?.length) return null;  // Section absent when no nudges

  return (
    <section aria-label="Needs Your Attention">
      {visibleNudges.map(nudge => (
        <NudgeCard
          key={nudgeKey(nudge)}
          nudge={nudge}
          onDismiss={() => setDismissedIds(prev =>
            new Set(prev).add(nudgeKey(nudge))
          )}
        />
      ))}
    </section>
  );
}
```

### 5. Dashboard Layout — CSS Grid with Responsive Breakpoints

```tsx
function DashboardLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="space-y-6 p-6">
      {children}
    </div>
  );
}
```

**Grid rules by viewport:**

| Section | Desktop (>1200px) | Tablet (768–1200px) | Mobile (<768px) |
|---------|-------------------|---------------------|-----------------|
| Nudge Cards | 3-col row | 2+1 wrap | 1-col stack |
| KPI Cards | 4-col row | 4-col compressed | 2x2 grid |
| Pipeline Submissions | Full-width horizontal pills | Abbreviated labels | Horizontal scroll |
| Pipeline Renewals | Full-width horizontal pills | Abbreviated labels | Horizontal scroll |
| Tasks + Activity | 2-col (50/50) | 1-col stack | 1-col stack |

Implemented via Tailwind responsive prefixes:
```tsx
<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
  {/* KPI cards */}
</div>
```

### 6. Query Key Conventions

All dashboard queries use a namespaced key structure for easy invalidation:

```tsx
// Invalidate all dashboard data on navigation back to dashboard
queryClient.invalidateQueries({ queryKey: ['dashboard'] });

// Individual widget invalidation
queryClient.invalidateQueries({ queryKey: ['dashboard', 'kpis'] });

// Query keys:
['dashboard', 'nudges']
['dashboard', 'kpis']
['dashboard', 'pipeline']
['dashboard', 'pipeline', entityType, status, 'items']  // lazy
['myTasks']           // shared with Task Center
['brokerActivity']    // shared with Broker 360 timeline
```

---

## Consequences

### Positive

- **True Isolation:** A crash in the pipeline popover does not affect KPI cards or tasks
- **Fast Perceived Load:** Skeletons render instantly; widgets fill in as data arrives
- **Lazy Popovers:** Initial page load fetches only 5 endpoints; popover data loads on demand
- **No State Management Library:** Session-scoped dismiss uses plain React state — no Redux/Zustand needed for dashboard

### Negative

- **Error Boundary Boilerplate:** Each widget needs wrapping. Mitigated by a reusable `<WidgetErrorBoundary>` component.
- **Multiple Loading States:** User may see widgets loading at different rates. Mitigated by fast server response (< 500 ms) and skeleton matching.

### Neutral

- **No SSR for MVP:** Dashboard is client-rendered only. Server-side rendering is a future optimization if initial load latency becomes an issue.

---

## Accessibility Checklist (from F0001 Screen Spec)

- [ ] Tab order: Nudge cards -> KPI cards -> Pipeline pills -> Tasks -> Activity Feed
- [ ] Pipeline pill: Enter/Space opens popover; Escape closes
- [ ] Arrow keys navigate within popover mini-cards
- [ ] Nudge cards: `role="alert"` for overdue, `role="status"` for informational
- [ ] Popover: `role="dialog"` with `aria-labelledby`
- [ ] All status indicators use icons + color (colorblind-safe)
- [ ] Respect `prefers-reduced-motion`

---

## Related ADRs

- ADR-002: Dashboard Data Aggregation (per-widget endpoints)
- ADR-003: Task Entity and Nudge Engine (nudge computation)

---

**Last Updated:** 2026-02-14
