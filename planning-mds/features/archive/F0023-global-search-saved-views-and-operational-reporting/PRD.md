---
template: feature
version: 1.1
applies_to: product-manager
---

# F0023: Global Search, Saved Views & Operational Reporting

**Feature ID:** F0023
**Feature Name:** Global Search, Saved Views & Operational Reporting
**Priority:** High
**Phase:** CRM Release MVP
**Status:** Planned — Phase A + B approved (plan run `2026-06-19-2f180001`); ready for feature action

## Feature Statement

**As a** CRM operator or manager
**I want** cross-object search, reusable working views, and operational reports
**So that** I can find the right records and manage daily work without rebuilding manual spreadsheets

## Business Objective

- **Goal:** Make Nebula a dependable daily operating surface for finding records, reusing filtered work views, and monitoring backlog, due work, and workflow aging.
- **Primary metrics:**
  - Internal users can find an indexed broker, account, policy, submission, renewal, or task from one global search surface using name/number/title identifiers.
  - At least 80% of repeated operational filter workflows can be saved as personal or team views instead of recreated manually.
  - Managers can review due work, workload by owner, and workflow aging without exporting raw records to spreadsheets for daily triage.
- **Baseline:** Search is spread across entity-specific lists, saved views do not exist as durable CRM artifacts, and operational reporting depends on manual list filtering or external spreadsheets.
- **Target:** Search, saved views, and operational reports are permission-scoped, deep-linkable, and usable from the authenticated CRM shell.

## Target Personas

Persona details are captured in the feature folder:

- [Distribution Operations Manager](./persona-distribution-operations-manager.md)
- [Relationship Manager](./persona-relationship-manager.md)
- [Underwriter](./persona-underwriter.md)

## Problem Statement

- **Current state:** Operators move between broker, account, policy, submission, renewal, and task lists to answer basic questions. Repeated filters are rebuilt by hand. Managers rely on screenshots, ad hoc exports, or manual spreadsheet rollups for daily operating visibility.
- **Desired state:** A user starts from one global search entry, narrows results by known CRM dimensions, opens the source record, saves repeated views, and uses operational reports for backlog, due work, workload, and aging.
- **Impact:** Less time spent hunting for records, fewer missed handoffs, and a shared operational vocabulary for daily management.

## Scope & Boundaries

### In Scope

- Global search across internal CRM records: broker, MGA/program context, account, policy, submission, renewal, and task.
- Search result grouping by object type with permission-scoped snippets, key identifiers, status, owner, and deep links to existing 360/workflow screens.
- Facets and filters for object type, status, owner, due date, region, line of business, broker/account, and workflow stage where the source record already exposes those dimensions.
- Personal saved views for repeated search/list/report filters.
- Team saved views and default views governed by internal manager/admin roles.
- Operational reports for due work, workload by owner/status, workflow aging, and backlog drilldowns.
- Explicit stale/empty/error states so users can tell whether a result set is empty, filtered away, unauthorized, or temporarily unavailable.
- Permission-safe counts and result visibility enforced before data leaves the server.

### Out of Scope

- External broker/MGA self-service search or reporting in this release.
- Free-form report builder, chart designer, SQL-like query tool, or external data warehouse.
- Predictive analytics, AI recommendations, and advanced BI dashboards.
- Cross-corpus document content search. Document metadata may appear only when it is already attached to an in-scope parent record; full document search stays outside F0023.
- Hierarchy-aware access-control enforcement and distribution rollup authorization. F0017 supplies hierarchy/territory data; F0037 owns hierarchy-aware access scoping and rollups.
- Bulk export as a primary workflow. The first release focuses on navigable operational views inside Nebula.

## Success Criteria

- Global search returns permission-scoped grouped results for the in-scope object types and supports keyboard submission from the authenticated shell.
- Search and report result rows navigate to the authoritative source screen for the record.
- Users can create, rename, update, delete, and apply personal saved views without losing filter state after reload.
- Managers/admins can publish team saved views with owner, description, scope, and default status.
- Operational reports show due work, workload by owner/status, and workflow aging with drilldowns to filtered record lists.
- Unauthorized records do not appear in results, counts, snippets, suggestions, saved-view previews, or reports.
- Empty, no-access, stale-data, and system-error states are distinguishable in the UI.

## Dependencies

- F0016 Account 360 & Insured Management - account records and list/detail destinations.
- F0017 Broker/MGA Hierarchy, Producer Ownership & Territory Management - hierarchy, territory, and producer dimensions; F0023 can build core search/reporting in parallel but hierarchy/territory facets depend on F0017 landing.
- F0018 Policy Lifecycle & Policy 360 - policy records and list/detail destinations.
- F0019 Submission Quoting, Proposal & Approval Workflow - quote/bind outcomes, submission workflow history, and backlog status.
- F0006/F0007 Submission and Renewal workflow foundations - workflow state and transition history used by aging reports.
- F0003/F0004 Task Center - assigned task records and user search for ownership filters.

## User Stories

| Story | Title | Scope |
|-------|-------|-------|
| [F0023-S0001](./F0023-S0001-global-search-results.md) | Global search returns grouped CRM results | MVP |
| [F0023-S0002](./F0023-S0002-filter-and-open-search-results.md) | Filter, sort, and open search results | MVP |
| [F0023-S0003](./F0023-S0003-personal-saved-views.md) | Personal saved views | MVP |
| [F0023-S0004](./F0023-S0004-team-saved-views.md) | Team saved views and defaults | MVP |
| [F0023-S0005](./F0023-S0005-daily-operational-workload-report.md) | Daily operational workload report | MVP |
| [F0023-S0006](./F0023-S0006-workflow-aging-and-backlog-report.md) | Workflow aging and backlog drilldowns | MVP |
| [F0023-S0007](./F0023-S0007-permission-safe-search-and-reporting.md) | Permission-safe search and reporting behavior | MVP |

## Screen Responsibilities

### Global Search Overlay

- Entry point from the authenticated application shell.
- Accepts a text query and optional object-type shortcut.
- Shows grouped, permission-scoped results with status, owner, object type, and source-route link.
- Provides empty, no-access, stale, and system-error states.

### Search Results Workspace

- Deep-linkable route for global search criteria.
- Supports object-type filters, facets, sort, pagination, and saved-view actions.
- Lets users open source records without losing the criteria context.

### Saved Views Drawer

- Lists personal views and team views available to the current user.
- Allows create, rename, update, apply, delete for personal views.
- Allows publish/update/default management for team views only for permitted manager/admin roles.

### Operational Reports Workspace

- Shows due work, workload by owner/status, and workflow aging/backlog reports.
- Supports filters shared with saved views where applicable.
- Provides drilldowns from report rows to filtered source record lists.

## Key Workflows

1. **Find a record:** User opens global search, enters a name/number/title, reviews grouped results, and opens the source screen.
2. **Narrow and reuse:** User applies object/status/owner/date filters, saves the criteria as a personal view, reloads, and reapplies the view.
3. **Publish team view:** Manager creates a filtered view for the team, marks it available to a team scope, and sees it listed for eligible users.
4. **Daily triage:** Manager opens operational reports, reviews due work and aging, drills into a filtered list, and assigns follow-up through the owning feature's workflow.
5. **No-leak behavior:** User searches for a record outside their authorization scope and sees no leaked record details, counts, or suggestions.

## Screen Layouts (ASCII)

### Desktop - Global Search Results

```text
+--------------------------------------------------------------------------------+
| Nebula                                      [Global search: acme policy 2026  ] |
+--------------------------------------------------------------------------------+
| Search Results for "acme policy 2026"                         [Save View]      |
| [All] [Brokers] [Accounts] [Policies] [Submissions] [Renewals] [Tasks]          |
| Filters: Owner [Any]  Status [Any]  Due [Any]  Region [Any]  LOB [Any]         |
+-------------------------------+------------------------------------------------+
| Saved Views                   | Grouped Results                                |
| - My open renewals            | Policies (3)                                   |
| - Northeast submissions       |  POL-2026-0142  Acme Manufacturing  Active     |
| - Team: overdue follow-up     |  Broker: Sterling MGA  Owner: Dana Lee         |
|                               |                                                |
| Operational Reports           | Submissions (2)                                |
| - Due work                    |  SUB-10491  Acme Manufacturing  In Review      |
| - Workflow aging              |  Assigned: Robin Patel  Age: 4 days            |
| - Workload by owner           |                                                |
|                               | Renewals (1)                                   |
|                               |  REN-8841  Acme Manufacturing  Outreach        |
+-------------------------------+------------------------------------------------+
```

### Narrow - Search Results

```text
+--------------------------------------+
| [Search acme policy 2026          ]  |
| [All] [Policies] [Subs] [More]       |
| Filters [3]                 Save     |
+--------------------------------------+
| Saved Views                          |
| My open renewals v                   |
+--------------------------------------+
| Policies (3)                         |
| POL-2026-0142                        |
| Acme Manufacturing - Active          |
| Sterling MGA - Dana Lee              |
+--------------------------------------+
| Submissions (2)                      |
| SUB-10491 - In Review - Age 4d       |
+--------------------------------------+
| Renewals (1)                         |
| REN-8841 - Outreach - Due Jun 25     |
+--------------------------------------+
```

### Desktop - Operational Reports

```text
+--------------------------------------------------------------------------------+
| Operational Reports                                      [View: Team overdue v] |
+--------------------------------------------------------------------------------+
| Filters: Team [Northeast] Owner [Any] Status [Open] Due [Next 14 days]          |
+---------------------------+-----------------------------+----------------------+
| Due Work                  | Workload by Owner           | Workflow Aging       |
| Due today: 18             | Dana Lee       42 open      | > 7 days: 13         |
| Due this week: 64         | Robin Patel    37 open      | > 14 days: 5         |
| Past due: 9               | Morgan Smith   25 open      | > 30 days: 1         |
+---------------------------+-----------------------------+----------------------+
| Drilldown: Past due work                                                     |
| Submission SUB-10491  In Review  Owner Robin Patel  Due Jun 17  [Open]       |
| Renewal REN-8841      Outreach   Owner Dana Lee     Due Jun 18  [Open]       |
+--------------------------------------------------------------------------------+
```

## Clarifications And Assumptions

- F0023 is an internal CRM feature in this release. External broker/MGA search and reporting remain out of scope.
- Core search/reporting can proceed in parallel with F0017, but hierarchy/territory-aware facets and reports must degrade until F0017 data is available.
- F0037 owns hierarchy-aware access-control enforcement and distribution rollups. F0023 may display available hierarchy/territory dimensions only under existing authorization boundaries.
- Saved views store reusable criteria and presentation metadata. They do not grant access to records a user could not otherwise see.
- Operational reports are navigable work-management surfaces, not a general-purpose analytics platform.

## Architecture Notes

- Phase B is approved in plan run `2026-06-19-2f180001`.
- [ADR-014 Search Index and Saved View Architecture](../../architecture/decisions/ADR-014-search-index-and-saved-view-architecture.md) governs the search/reporting read model, saved-view persistence, projection freshness, API contracts, and authorization model.
- Search indexing, saved-view persistence, reporting projections, API contracts, schemas, and ontology bindings are architect-owned Phase B outputs now available for feature implementation.
- PM requirements treat raw feature/story artifacts as source of product truth; KG mappings remain retrieval context and were finalized by the Phase B plan run.

## Architecture Traceability

**Taxonomy Reference:** [Feature Architecture Traceability Taxonomy](../../architecture/feature-architecture-traceability-taxonomy.md)

| Classification | Artifact / Decision | ADR |
|----------------|---------------------|-----|
| Introduces: Cross-Cutting Component | Search index, saved-view store, and operational-reporting projections | [ADR-014](../../architecture/decisions/ADR-014-search-index-and-saved-view-architecture.md) |
| Introduces/Standardizes: Cross-Cutting Pattern | Read-optimized operational views over workflow history and backlog metrics | [ADR-014](../../architecture/decisions/ADR-014-search-index-and-saved-view-architecture.md) |
| Extends: Cross-Cutting Component | Search and reporting settings may later be governed through published operational configuration | [ADR-016](../../architecture/decisions/ADR-016-published-operational-configuration-governance.md) |
