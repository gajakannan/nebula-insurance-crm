# F0001-S0001: View Key Metrics Cards

**Story ID:** F0001-S0001
**Feature:** F0001 — Dashboard
**Title:** View Key Metrics Cards
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution User or Relationship Manager
**I want** to see KPI summary cards on the dashboard
**So that** I can gauge portfolio health at a glance without running manual reports.

## Context & Background

Distribution teams currently have no single view for high-level portfolio metrics. Key indicators like active broker count, open submission volume, and renewal rate are tracked manually in spreadsheets. Surfacing these as cards on the dashboard eliminates context-switching and provides an immediate pulse check on login.

## Acceptance Criteria

**Happy Path:**
- **Given** the user is authenticated and lands on the Dashboard
- **When** the dashboard loads
- **Then** the following KPI cards are displayed:
  - **Active Brokers:** Count of brokers with Status = Active, scoped to user's authorization
  - **Open Submissions:** Count of submissions NOT in terminal states (Bound, Declined, Withdrawn), scoped to user's authorization
  - **Renewal Rate:** Percentage of renewals that reached Bound status out of all renewals that exited the pipeline in the trailing 90 days
  - **Avg Turnaround (days):** Mean calendar days from Submission.CreatedAt to first terminal state transition, trailing 90 days

**Edge Cases:**
- No brokers exist → Active Brokers card displays "0"
- No submissions exist → Open Submissions card displays "0"; Avg Turnaround displays "—"
- No renewals have exited pipeline → Renewal Rate displays "—"
- User has restricted scope (e.g., region-limited) → Counts reflect only authorized entities
- Submission or Renewal entities not yet seeded (e.g., fresh environment or early phased rollout) → affected cards display "0" or "—" as appropriate; dashboard still loads and remaining widgets render normally

**Checklist:**
- [x] Four KPI cards render on dashboard load
- [x] Each card shows a numeric value or "—" when data is insufficient
- [x] Values are consistent with underlying list/search results
- [x] Cards load within the overall dashboard p95 < 2s target
- [x] Cards are read-only (no edit actions)
- [x] Authorization check: all KPI queries are scoped by authenticated user permissions (ABAC)
- [x] Audit/timeline requirement: N/A (read-only view with no mutation)

## Data Requirements

**Required Fields (per card):**
- Label: string (e.g., "Active Brokers")
- Value: number | "—"
- Unit: string (e.g., "count", "%", "days")

**Validation Rules:**
- Counts must be non-negative integers
- Renewal Rate must be between 0–100% or "—"
- Avg Turnaround must be a non-negative number rounded to 1 decimal or "—"

## Role-Based Visibility

**Roles that can view KPI cards:**
- Distribution User — sees metrics scoped to their department/region
- Underwriter — sees metrics scoped to all submissions they can access via Casbin ABAC (not limited to submissions where AssignedToUserId = current user; ABAC policy determines access breadth — confirm with stakeholders if narrower scoping is required)
- Relationship Manager — sees metrics scoped to their broker relationships
- Program Manager — sees metrics scoped to their programs
- Admin — sees all metrics (unscoped)

**Data Visibility:**
- All KPI card data is InternalOnly; not visible to external users.

## Non-Functional Expectations

- Performance: KPI cards must render within the overall dashboard p95 < 2s target
- Security: Backend endpoint must enforce Casbin ABAC scope before aggregating counts
- Reliability: If a KPI query fails, display "—" for that card and log the error; do not block other widgets

## Dependencies

**Depends On:**
- Navigation Shell (authenticated layout)
- Broker entity with Status field (for Active Brokers count)
- Submission entity with CurrentStatus field (for Open Submissions, Avg Turnaround)
- Renewal entity with CurrentStatus field (for Renewal Rate)

**Related Stories:**
- F0001-S0002 — Pipeline Summary (complementary pipeline detail)

## Out of Scope

- Trend indicators (up/down arrows comparing to previous period)
- Sparkline charts within cards
- Configurable time range for trailing metrics (fixed at 90 days for MVP)
- Click-through navigation from cards to filtered lists (deferred to future enhancement)

## UI/UX Notes

- Screens involved: Dashboard
- Layout: Four cards in a horizontal row at the top of the dashboard; responsive wrap to 2x2 grid on narrow viewports
- Each card shows: label, large numeric value, and unit

## Questions & Assumptions

**Assumptions (to be validated):**
- 90-day trailing window is acceptable for Renewal Rate and Avg Turnaround
- "Terminal state" for submissions means Bound, Declined, or Withdrawn
- Active Broker means Broker.Status = "Active" (not soft-deleted, not inactive)

## Definition of Done

- [x] Acceptance criteria met
- [x] Edge cases handled (zero data, restricted scope, query failure)
- [x] Permissions enforced (Casbin ABAC scope filtering)
- [x] Audit/timeline logged: N/A (read-only; no mutations)
- [x] Tests pass (unit test for aggregation logic, integration test for scoped queries)
- [x] Accessible: cards have aria-labels for screen readers
