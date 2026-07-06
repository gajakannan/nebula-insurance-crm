---
template: feature
version: 1.1
applies_to: product-manager
---

# F0022: Work Queues, Assignment Rules & Coverage Management

**Feature ID:** F0022
**Feature Name:** Work Queues, Assignment Rules & Coverage Management
**Priority:** High
**Phase:** CRM Release MVP

## Feature Statement

**As a** manager or operations lead
**I want** work queues, assignment rules, and backup coverage controls
**So that** submissions, renewals, and tasks are routed consistently and work continues when people are overloaded or unavailable

## Business Objective

- **Goal:** Give Nebula an operational routing layer rather than only personal work views.
- **Metric:** Assignment latency, queue aging, rebalance frequency, and coverage continuity.
- **Baseline:** Work assignment depends too heavily on manual coordination.
- **Target:** Work can be routed, monitored, and redirected using explicit operational rules.

## Problem Statement

- **Current State:** Task assignment alone does not solve queue ownership, backup coverage, or workload balancing.
- **Desired State:** Queues and routing rules manage who gets work and how backup coverage behaves.
- **Impact:** Less operational friction and better continuity across submissions and renewals.

## Scope & Boundaries

**In Scope:**
- Named work queues
- Assignment and routing rules
- Reassignment and workload balancing
- Backup coverage and out-of-office continuity
- Minimal manager/admin controls required to create, edit, and operate queues, rules, memberships, coverage, and rebalancing
- Manager-visible unassigned fallback queue for no-match routing outcomes
- Routing and reassignment audit events for every queue, rule, assignment, coverage, and rebalance mutation

**Out of Scope:**
- Centralized configuration governance, publish/rollback flows, and cross-module admin console consolidation owned by F0032
- Full workforce management
- Predictive staffing
- Advanced AI routing

**Boundary Guardrail with F0032:**
- F0022 owns the durable queue/routing foundation: queue records, assignment rules, memberships, coverage rules, rule evaluation, queue worklists, reassignment, and minimal manager controls.
- F0032 later centralizes governed configuration over queue/rule settings and other operational domains; it must not be a prerequisite for queues to function.
- F0022 should avoid throwaway seed-only configuration or UI patterns that would force F0032 to rebuild the queue/rule model.

## Success Criteria

- Managers can see and control how work is routed and covered.
- Queue aging and backlog become operationally visible.
- Teams can maintain continuity during absence or overload.
- The queue/routing feature is usable with its local controls before F0032 centralizes configuration governance.
- Routing outcomes are explainable by rule version, match reason, selected queue, selected assignee, and fallback path.

## Risks & Assumptions

- **Risk:** Queue and rule design becomes too complex before core workflows are stable.
- **Assumption:** Simple, explicit rules provide strong value before advanced automation is needed.
- **Mitigation:** Start with constrained queue types and clear rule precedence.
 
## Dependencies

- F0004 Task Center UI + Manager Assignment
- F0006 Submission Intake Workflow
- F0007 Renewal Pipeline
- F0017 Broker/MGA Hierarchy, Producer Ownership & Territory Management

## Business Rules

1. **Initial routed work types:** F0022 routes tasks, submissions, and renewals in the first release.
2. **Rule precedence:** Routing evaluates in this order: explicit manual assignment override, coverage/out-of-office rule, territory/ownership rule, workload balancing, fallback queue.
3. **No-match fallback:** Work that does not match an active rule is placed in a manager-visible `Unassigned Operations Queue`; it is not randomly assigned.
4. **Coverage activation:** Backup coverage activates only from explicit out-of-office or coverage windows. Inactivity alone does not activate coverage.
5. **Manual control preservation:** Authorized managers can reassign work after routing. Manual reassignment records the override reason and becomes the current assignment.
6. **Auditability:** Queue, rule, membership, coverage, routing, reassignment, rebalance, and fallback events must produce audit/timeline evidence.

## Architecture & Solution Design

### Solution Components

- Introduce a queue management module with explicit queue definitions, assignment-rule evaluation, rebalance actions, and coverage configuration services.
- Add a routing engine that determines queue placement and assignee outcomes from deterministic business rules instead of implicit manager judgment alone.
- Provide workload and backlog projections that surface queue aging, pending work, rebalance pressure, and coverage exceptions.
- Ship durable queue/rule objects and service boundaries that F0032 can later govern without replacing the foundation.
- Keep advanced AI routing out of the first architecture so the operating model remains transparent and debuggable.

### Data & Workflow Design

- Model queues, queue membership, routing rules, coverage rules, and assignment outcomes as separate concepts so rule administration and execution history remain explainable.
- Make routing decisions auditable by storing rule version, matched conditions, selected queue, and selected assignee on assignment events.
- Represent out-of-office and backup coverage as explicit configuration, not ad hoc user preferences embedded in unrelated profile data.
- Preserve deterministic rule precedence and fallback order so submissions, renewals, and tasks are routed consistently under retry or replay conditions.

### API & Integration Design

- Expose queue administration, queue worklist, reassignment, rebalance, and coverage-management endpoints with consistent resource semantics.
- Consume events or state changes from submissions, renewals, and tasks to trigger routing decisions rather than hard-coding module-specific entry points everywhere.
- Keep routing execution behind an application-service boundary so the same rules can be invoked synchronously for user actions or asynchronously for background rebalancing.
- Allow F0032 to become the governed administrative control surface for queue rules later without requiring this feature to redesign its execution model or data contracts.

### Security & Operational Considerations

- Restrict queue and reassignment powers by manager or admin authorization because routing actions can materially change workload ownership and data visibility.
- Instrument rule evaluation latency, queue aging, backlog size, rebalance frequency, and no-match exceptions as core operational metrics.
- Design assignment operations to be idempotent enough to survive repeated upstream events without duplicate queue entries or churn.
- Plan indexing and partitioning around queue status, owner, due date, and work type because queues will become hot operational datasets.

## Architecture Traceability

**Taxonomy Reference:** [Feature Architecture Traceability Taxonomy](../../architecture/feature-architecture-traceability-taxonomy.md)

| Classification | Artifact / Decision | ADR |
|----------------|---------------------|-----|
| Introduces: Cross-Cutting Component | Queue management, routing, reassignment, and coverage engine | [ADR-013](../../architecture/decisions/ADR-013-operational-routing-and-queue-engine.md) (Accepted) |
| Introduces/Standardizes: Cross-Cutting Pattern | Deterministic rule evaluation and auditable assignment decisions | [ADR-013](../../architecture/decisions/ADR-013-operational-routing-and-queue-engine.md) (Accepted) |
| Extends: Cross-Cutting Component | Runtime rule administration is expected to flow through published operational configuration | [ADR-016](../../architecture/decisions/ADR-016-published-operational-configuration-governance.md) (Proposed) |

## Architecture Deliverables

| Artifact | Purpose |
|----------|---------|
| [ADR-013](../../architecture/decisions/ADR-013-operational-routing-and-queue-engine.md) | Accepted routing/queue engine decision and F0022 execution contract |
| [data-model.md](../../architecture/data-model.md#18-work-queues-assignment-rules-and-coverage-f0022) | WorkQueue, AssignmentRule, CoverageWindow, QueueWorkItem, and RoutingDecisionLog entities |
| [nebula-api.yaml](../../api/nebula-api.yaml) | WorkQueues OpenAPI endpoints for queue/rule/coverage/worklist/reassignment/rebalance/audit |
| [authorization-matrix.md](../../security/authorization-matrix.md#26c-work-queues-and-operational-routing-f0022) | Queue read/manage/assign role decisions and source-record ABAC constraints |
| [schemas/](../../schemas/) | Shared JSON Schemas for F0022 queue/routing payloads |

## Related User Stories

| Story | Title | Priority | Summary |
|-------|-------|----------|---------|
| [F0022-S0001](./F0022-S0001-manage-work-queues-and-memberships.md) | Manage work queues and memberships | Critical | Create and maintain queue definitions and membership before centralized F0032 governance exists. |
| [F0022-S0002](./F0022-S0002-define-assignment-rules-and-precedence.md) | Define assignment rules and precedence | Critical | Configure active routing rules with deterministic precedence and no-match fallback. |
| [F0022-S0003](./F0022-S0003-route-work-from-tasks-submissions-and-renewals.md) | Route work from tasks, submissions, and renewals | Critical | Apply queue/rule routing to the initial work types and record explainable outcomes. |
| [F0022-S0004](./F0022-S0004-manage-backup-coverage-windows.md) | Manage backup coverage windows | High | Configure explicit coverage windows and route eligible work to backups. |
| [F0022-S0005](./F0022-S0005-queue-worklists-and-aging-visibility.md) | Queue worklists and aging visibility | High | Show queue backlog, aging, no-match items, and owner workload from routed work. |
| [F0022-S0006](./F0022-S0006-reassign-and-rebalance-queued-work.md) | Reassign and rebalance queued work | High | Let managers override, reassign, and rebalance work with audit evidence. |
| [F0022-S0007](./F0022-S0007-routing-audit-permissions-and-exceptions.md) | Routing audit, permissions, and exceptions | High | Prove routing/reassignment visibility is permission-safe and operationally traceable. |

## Screen Layouts (ASCII)

### Desktop — Queue Operations Workspace

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│ Work Queues                                      [New Queue] [Coverage]      │
├───────────────┬───────────────────────────────┬──────────────────────────────┤
│ Queues        │ Queue Worklist                │ Routing / Coverage Detail    │
│ - Submission  │ Filter: type/status/age/owner │ Selected queue: Submission   │
│ - Renewal     │                               │ Members: 8 active           │
│ - Task        │ #  Work item   Age   Owner    │ Rules: 4 active             │
│ - Unassigned  │ 1  SUB-1042    2d    Pending  │ Coverage: 1 active window   │
│               │ 2  REN-2201    5d    A. Patel │ Actions: Edit, Rebalance    │
└───────────────┴───────────────────────────────┴──────────────────────────────┘
```

### Narrow — Queue Operations Workspace

```text
┌─────────────────────────────┐
│ Work Queues      [+]        │
├─────────────────────────────┤
│ Queue selector              │
│ Submission Queue            │
├─────────────────────────────┤
│ Backlog 24 · Overdue 3      │
├─────────────────────────────┤
│ SUB-1042 · 2d · Unassigned  │
│ REN-2201 · 5d · A. Patel    │
├─────────────────────────────┤
│ Tabs: Work · Rules · Cover  │
└─────────────────────────────┘
```
