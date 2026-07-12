---
template: feature
version: 1.1
applies_to: product-manager
---

# F0037: Hierarchy-Aware Access Scoping & Distribution Rollups

**Feature ID:** F0037
**Feature Name:** Hierarchy-Aware Access Scoping & Distribution Rollups
**Priority:** High
**Phase:** CRM Release MVP+
**Plan Run:** `2026-07-06-6e3851ab`
**Feature Mode:** Existing
**Status:** Phase B architecture drafted - pending G5 approval

## Feature Statement

**As a** distribution leader or compliance owner
**I want** access visibility and production reporting to honor the broker/MGA hierarchy, territories, producer ownership, user role, and as-of date
**So that** users see only the channel data they are entitled to see and leaders can trust rolled-up production, workflow, and activity metrics.

## Business Objective

- **Goal:** Convert the structural distribution model from F0017 and the projection/reporting substrate from F0023 into enforced visibility and hierarchy-aware rollup insight.
- **Metric:** Search, report, broker-insight, drilldown, and rollup results reconcile to the same scoped source rows for the same user and `asOf` date.
- **Baseline:** F0017 created hierarchy, producer ownership, territory assignment, and audit structures without enforcing access. F0023 created global search, saved views, and operational reports with projection visibility that does not yet resolve full distribution hierarchy scope.
- **Target:** Admins have full scope; internal users receive hierarchy/territory/producer constrained scope; BrokerUser and ExternalUser remain denied unless a later gate explicitly changes scope.

## Problem Statement

- **Current State:** The CRM has durable hierarchy/territory/ownership data and projection-based reporting, but visibility is still broad for several internal roles and rollups do not reflect the distribution tree.
- **Desired State:** Every affected read path resolves a single current-user distribution scope before rows, counts, facets, suggestions, drilldowns, and aggregations are materialized.
- **Impact:** Confidentiality improves across channel boundaries, and distribution leaders get production/workflow/activity rollups that match their actual hierarchy and territory responsibility.

## G1 Clarifications Locked for Phase A

1. **Feature shape:** F0037 is a full feature, not a spike. Access scoping and distribution rollup reporting both stay in scope.
2. **Role scope:** Admin has full scope. ProgramManager and DistributionManager are scoped by configured hierarchy/program/territory authority. RelationshipManager, DistributionUser, Underwriter, and ServiceUser are scoped by ownership, assignment, territory, and existing record permissions. BrokerUser and ExternalUser are denied for F0037-owned internal rollup/search/report visibility unless Phase B explicitly carves out a safer external contract.
3. **Hidden-record behavior:** Hidden records are omitted before response materialization from rows, counts, facets, suggestions, drilldowns, and rollups. Direct access to a hidden record returns no-leak not found behavior, preferably 404, while authentication or broad resource-policy failure can still use the platform's existing unauthorized/forbidden behavior.
4. **Rollup semantics:** Production, workflow, and activity rollups must reconcile to the same scoped projection rows used by operational reports and broker insights. Rollup rows must expose totals, grouping key, `asOf`, generated timestamp, and drilldown links that preserve the same scope.
5. **As-of date:** `asOf` defaults to the operator's current business date and applies to effective-dated hierarchy, territory, and producer ownership relationships.
6. **Substrate:** MVP uses existing F0017 structure plus F0023 projections. No new external search/reporting infrastructure is in Phase A scope. Materialized rollup jobs are deferred unless Phase B proves they are required.

## Scope & Boundaries

**In Scope:**
- Current-user distribution-scope resolution from hierarchy, territory, producer ownership, role, and `asOf` date.
- Hierarchy-aware read scoping for distribution structure and source-record visibility.
- Scope enforcement for global search, saved views, broker insights, operational reports, suggestions, counts, facets, and drilldowns.
- Distribution rollup reporting for production, workflow, and activity grouped by hierarchy node, territory, and producer where data exists.
- UI filters, rollup panels, drilldowns, and no-leak empty/no-access/stale/error states.
- Authorization matrix and policy parity for F0037 resource/action names.
- Security evidence, reconciliation tests, and review signoff for access-control behavior.

**Out of Scope:**
- The structural broker/MGA hierarchy, producer ownership, and territory model itself; that remains owned by F0017.
- Commission, producer split, or revenue economics; those remain owned by F0025.
- External broker collaboration portal experiences; those remain owned by F0029.
- Replacing F0023 global search, saved views, or operational reports with a new reporting substrate.
- Free-form BI, ad hoc custom metric builders, and new external search/reporting infrastructure.
- Background materialized rollup jobs unless Phase B accepts a specific operational need.

## Personas

- **Distribution Operations Manager:** Needs subtree, territory, and producer rollups to manage production, workload, and activity without seeing unrelated channels.
- **Relationship Manager:** Needs producer/territory scoped search and drilldown access for assigned broker relationships only.
- **Program Manager:** Needs program-aware hierarchy visibility and rollups for owned programs without leaking sibling programs.
- **Admin / Security Compliance Owner:** Needs full-scope verification, policy parity, audit evidence, and reconciliation proof.

## Success Criteria

- A single distribution-scope service can explain which hierarchy nodes, territories, producers, and source records are visible for a user at an `asOf` date.
- Search, saved views, broker insights, operational reports, facets, suggestions, drilldowns, and rollups apply the scope before materializing response data.
- Hidden records do not affect visible counts, totals, facets, suggestions, drilldown links, or empty-state copy.
- Direct access to a hidden record returns no-leak not-found behavior unless the request fails authentication or broad resource authorization first.
- Rollup totals reconcile to scoped operational-report projection rows for Admin, manager subtree, producer/territory, sibling exclusion, and external-denial scenarios.
- Authorization documentation and policy fixtures include F0037 resource/action names and deny BrokerUser/ExternalUser by default.

## Backend Direction

- Introduce a distribution-scope application service that resolves visible broker, distribution-node, territory, and producer scope for the current user.
- Extend the existing projection visibility flow instead of creating a separate reporting substrate.
- Apply visibility predicates before counts, facets, suggestions, drilldowns, and aggregations are computed.
- Keep no-leak behavior consistent between list/search/report surfaces and direct-detail access.
- Use F0017 effective-dated hierarchy, producer ownership, and territory assignments with the request `asOf` date.

## Public API / Schema Direction

- Extend approved search and operational report query contracts with hierarchy filters such as `rootNodeId`, `territoryId`, `producerUserId`, and `asOf` where Phase B confirms compatibility.
- Add a distribution rollup response contract with grouped rows, totals, grouping metadata, `asOf`, generated timestamp, and scope-preserving drilldown links.
- Update `authorization-matrix.md` and `policy.csv` with F0037 resource/action names.
- Keep BrokerUser and ExternalUser denied unless a later gate explicitly approves an external-safe scope.

## Frontend Direction

- Reuse F0023 operational reports, saved-view, and search UI patterns.
- Add distribution-scope filters and rollup views without replacing existing operational reports.
- Show distinct empty, filtered-away, no-access, stale-data, and system-error states without exposing hidden record counts.
- Preserve scope filters when drilling from rollups into search/report result sets.

## Screen / Workflow Expectations

### Distribution Rollups - Desktop

```text
Reports
  Filters: [As of] [Root node] [Territory] [Producer] [Metric family]
  Summary: Production | Workflow | Activity totals
  Grouping: Hierarchy | Territory | Producer
  Rows: group name, visible totals, trend/context, drilldown action
  Drilldown: opens scoped operational report/search result with the same filters
```

### Distribution Rollups - Narrow View

```text
Reports
  Filter sheet
  Metric tabs
  Rollup list rows
  Drilldown action per row
```

## Story Breakdown

| Story | Title | Priority |
|-------|-------|----------|
| F0037-S0001 | Resolve current user distribution scope | Critical |
| F0037-S0002 | Enforce hierarchy-aware read scoping | Critical |
| F0037-S0003 | Apply visibility to search, saved views, insights, and reports | Critical |
| F0037-S0004 | Add distribution rollup reporting | High |
| F0037-S0005 | Add rollup filters, panels, drilldowns, and no-leak states | High |
| F0037-S0006 | Add security evidence and reconciliation checks | High |

## Dependencies

- F0017 Broker/MGA Hierarchy, Producer Ownership & Territory Management - structural model, effective dating, and audit.
- F0023 Global Search, Saved Views & Operational Reporting - projection visibility, search/reporting UI, saved-view substrate.
- F0008 Broker Insights - broker insight projections and permission-safe broker analytics behavior.
- Authorization/policy foundation - Casbin ABAC policy and permission matrix.

## Risks & Mitigations

- **Recursive scope can be expensive:** Phase B must define traversal/caching strategy and performance tests for deep trees.
- **Count leaks are easy to miss:** Stories require tests for rows, counts, facets, suggestions, drilldowns, and rollups.
- **As-of semantics can drift by surface:** One scope service should own date interpretation, with contract tests across search/report/rollup paths.
- **Rollup definitions may exceed available projection data:** Missing metrics should be explicitly unavailable rather than fabricated.

## Architecture & Solution Design

Phase B keeps F0037 inside the existing CRM modular monolith. It introduces a shared distribution-scope resolver and extends F0023/F0008 projection visibility so rows are filtered before counts, facets, suggestions, drilldowns, peer sets, metrics, and rollups are materialized. MVP rollups are query-time over existing `OperationalReportProjection` and `BrokerInsightProjection` rows plus F0017 hierarchy/territory/producer ownership context.

Accepted Phase B deltas pending G5 approval:
- New endpoint: `GET /operational-reports/distribution-rollups`.
- New schema: `planning-mds/schemas/distribution-rollup-report.schema.json`.
- New policy rule: `distribution_rollup:read`.
- New canonical KG nodes: `capability:distribution-rollup-reporting`, `endpoint:distribution-rollup-report`, `schema:distribution-rollup-report`, `policy_rule:distribution-rollup-read`.
- Existing search/report query contracts gain `rootNodeId`, `territoryId`, `producerUserId`, and `asOf` where applicable.

## Architecture Traceability

**Execution Plan:** [feature-assembly-plan.md](./feature-assembly-plan.md)

**Governing ADRs:** ADR-014 Search Index and Saved View Architecture, ADR-026 Broker/MGA Hierarchy Producer Ownership and Territory, ADR-031 Broker Insights Read Models and Permission-Safe Analytics, ADR-016 Published Operational Configuration Governance.

**Ontology Bindings:** `planning-mds/knowledge-graph/canonical-nodes.yaml`, `feature-mappings.yaml`, and `code-index.yaml` updated during plan run `2026-07-06-6e3851ab`.
