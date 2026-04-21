# F0001: Dashboard

**Feature ID:** F0001
**Feature Name:** Dashboard
**Priority:** Critical
**Phase:** MVP

## Feature Statement

**As a** Distribution User, Underwriter, or Relationship Manager
**I want** a unified dashboard view when I log in
**So that** I can immediately see my pipeline status, pending tasks, recent broker activity, and key performance metrics without navigating to multiple screens.

## Business Objective

- **Goal:** Reduce time-to-context for logged-in users by surfacing actionable information on first screen load.
- **Metric:** Percentage of users who navigate to a task or record directly from the dashboard within 30 seconds of login.
- **Baseline:** Users currently rely on spreadsheets and email to track pipeline and tasks (no single view exists).
- **Target:** 80% of daily-active users interact with at least one dashboard widget per session.
- **Measurement:** No analytics instrumentation is defined for MVP. Tracking widget interactions requires a future event-logging integration (e.g., click events on task rows, broker feed items, nudge CTAs). Deferred — add to backlog before Phase B go-live.

## Problem Statement

- **Current State:** Distribution teams piece together pipeline status from multiple tools, emails, and spreadsheets. No single view shows what needs attention right now.
- **Desired State:** A role-aware dashboard that surfaces submissions/renewals pipeline, assigned tasks, recent broker activity, and KPI summaries on login.
- **Impact:** Reduced context-switching, faster response to broker inquiries, fewer missed follow-ups.

## Scope & Boundaries

**In Scope:**
- Nudge cards (dismissible action prompts for overdue tasks, stale submissions, upcoming renewals — up to 3 at top of dashboard)
- Key metrics cards (total active brokers, open submissions, renewal rate, avg turnaround)
- Pipeline summary widget (Sankey-style opportunities flow for submissions and renewals, with interactive node/link details)
- My tasks & reminders widget (tasks assigned to logged-in user, sorted by due date)
- Broker activity feed widget (recent timeline events across broker relationships)
- Role-aware content (widgets show data filtered by the user's authorization scope)

**Out of Scope:**
- Customizable widget layout or drag-and-drop arrangement (Future)
- Advanced analytics, charts, or trend lines beyond simple counts and rates (Non-goal for MVP)
- Dashboard for external broker/MGA users (Non-goal for MVP)
- Real-time push updates / WebSocket live refresh (Future; MVP uses page-load fetch)
- Export or download of dashboard data

## Success Criteria

- Dashboard loads in < 2 seconds (p95) with all five widgets populated.
- Each widget displays accurate data consistent with underlying list views.
- Pipeline status segments display counts and expand on hover/click to show mini-card previews; click-through to filtered submission/renewal lists is deferred until F0006/F0007 (mini-card entity names render as plain text per MVP constraints).
- Clicking a broker-linked task row navigates to Broker 360; click-through for submission/renewal/account-linked tasks is deferred to F0003/F0006/F0007.
- Clicking a broker activity item navigates to the Broker 360 view.
- Empty states render meaningful messages when no data exists for a widget.

## Risks & Assumptions

- **Risk:** Dashboard queries may be slow if pipeline/timeline tables grow large. Mitigation: use materialized counts or indexed queries; enforce pagination on activity feed (max 20 items).
- **Assumption:** Submission and Renewal entities exist with status fields by the time Dashboard is fully wired. Dashboard can render with partial data (e.g., only broker activity) if other modules are not yet implemented.
- **Assumption:** Authorization scope filtering reuses existing Casbin ABAC policies; no new policies are needed beyond entity-level read access.

## Dependencies

- Navigation Shell (authenticated app shell must exist)
- Broker entity and timeline events (for broker activity feed)
- Submission / Renewal entities (for pipeline summary; can degrade gracefully)
- Task entity (for my tasks widget; can degrade gracefully)

## Rollout & Enablement

- Internal team onboarding: ensure Distribution Users, Underwriters, Relationship Managers, and Program Managers are provisioned in authentik with the correct Casbin roles before go-live (see `planning-mds/security/policies/policy.csv`).
- Admin role required to seed initial broker, submission, and task records so dashboard widgets render with real data on first login.
- Verify role-scoped data visibility with at least one test user per role before releasing to the full team.
- Dashboard renders with partial data if Submission/Renewal entities are not yet populated — acceptable for phased rollout.

## Related User Stories

- F0001-S0001 - View Key Metrics Cards
- F0001-S0002 - View Pipeline Summary (Sankey Opportunities)
- F0001-S0003 - View My Tasks and Reminders
- F0001-S0004 - View Broker Activity Feed
- F0001-S0005 - View Nudge Cards
