---
template: feature
version: 1.1
applies_to: product-manager
---

# F0008: Broker Insights

**Feature ID:** F0008
**Feature Name:** Broker Insights
**Priority:** High
**Phase:** MVP
**Planning State:** Phase A refined 2026-07-03 in plan run `2026-07-03-4b9ca863`

## Feature Statement

**As a** distribution manager or relationship manager
**I want** broker scorecards and trend views
**So that** I can focus on high-value relationships and improve quote, bind, and retention outcomes

## Business Objective

- **Goal:** Turn broker activity and production data into actionable relationship insight.
- **Metric:** Insight adoption, broker review preparation time, and quality of relationship targeting.
- **Baseline:** Broker performance analysis is manual, stale, and fragmented.
- **Target:** Nebula surfaces consistent broker metrics and trends in one place.

## Problem Statement

- **Current State:** Users cannot quickly evaluate broker quality, production, or pipeline contribution.
- **Desired State:** Broker-level insights summarize performance, activity, and opportunity trends.
- **Impact:** Better prioritization, more informed relationship strategy, and less spreadsheet work.

## Scope & Boundaries

**In Scope:**
- Broker scorecards and trend views
- Quote, bind, retention, and production metrics
- Pipeline and activity summaries by broker
- Time-window comparisons and benchmark views
- Broker insight report pack and read models that consume F0023 reporting/search substrate and F0017 hierarchy/producer dimensions

**Out of Scope:**
- Full predictive analytics
- Commission accounting
- Carrier appetite modeling
- Replacement of F0023's general reporting substrate or F0017's hierarchy/ownership model

## Success Criteria

- Managers can review broker performance without manual spreadsheet assembly.
- Insight views support quarterly reviews and relationship prioritization.
- Metrics are consistent with operational source data.
- Broker insights can segment and roll up by hierarchy, producer ownership, and territory once F0017 is available.
- Counts, comparisons, drilldowns, and empty states expose only records already authorized for the current user.
- The first implementation slice can ship without replacing F0037 hierarchy-aware access enforcement or distribution rollup scope.

## Risks & Assumptions

- **Risk:** Insights are built before underlying workflow data is trustworthy.
- **Assumption:** Submission, renewal, policy, hierarchy, producer ownership, and reporting substrate data will exist before this feature is finalized.
- **Mitigation:** Keep F0008 sequenced after operational workflow foundations.

## Dependencies

- F0006 Submission Intake Workflow
- F0007 Renewal Pipeline
- F0017 Broker/MGA Hierarchy, Producer Ownership & Territory Management
- F0019 Submission Quoting, Proposal & Approval Workflow
- F0023 Global Search, Saved Views & Operational Reporting

F0008 should remain a separate broker insight/report-pack feature, but it should land after F0023 provides the reporting substrate and F0017 provides hierarchy, producer ownership, and territory dimensions. F0019 quote/bind outcomes are needed for reliable quote-to-bind metrics.

## Phase A Refinement

### MVP Slice

F0008 MVP is a read-only Broker Insights workspace for internal distribution and relationship teams. It adds broker scorecards, trend drilldowns, peer benchmarks, review-ready snapshots, and permission-safe behavior over existing operational data. It does not create a new analytics platform, new distribution access model, or new write workflow.

### Personas

| Persona | Primary Job | F0008 Need |
|---------|-------------|------------|
| Distribution Manager | Prioritize broker relationships and production review effort | Compare broker performance, spot weak quote/bind or retention movement, and open source records for follow-up. |
| Broker Relationship Manager | Prepare broker review conversations | See broker-specific trends, recent activity, open opportunities, and evidence behind metrics. |
| Program Manager | Review MGA/program contribution | Compare broker or MGA performance within authorized program and territory context without manual spreadsheet assembly. |

### Screen Responsibilities

| Screen / Surface | Responsibility |
|------------------|----------------|
| Broker Insights Workspace | Entry point for broker scorecards, time window filters, benchmark set selection, and review snapshot access. |
| Broker Scorecard Panel | Shows broker-level quote, bind, retention, pipeline, activity, and production summary metrics with provenance. |
| Trend Drilldown Drawer | Shows period-over-period trend lines and the source record list behind a selected metric. |
| Benchmark Comparison View | Compares visible brokers or broker groups within an authorized peer set. |
| Review Snapshot View | Presents a read-only, review-ready summary that can be used in quarterly broker reviews. |

### Business Rules

1. F0008 metrics are computed from existing source records: submissions, quote/proposal outcomes, bind handoffs, policies, renewals, activity timeline events, F0017 hierarchy/producer/territory dimensions, and F0023 reporting projections.
2. Broker insight counts and benchmarks are computed after authorization filtering. Unauthorized records are indistinguishable from non-matches in counts, trends, peer sets, and drilldowns.
3. Time windows use inclusive calendar ranges in the user's locale and must show the selected period and comparison period.
4. Metric cards must show denominator counts and last refresh timestamp so managers can explain the score.
5. F0037 remains the owner of hierarchy-aware access enforcement and distribution rollups. F0008 may consume existing authorized data and F0017 dimensions, but it must not implement or redefine F0037 scope.

## Screen Layouts (ASCII)

### Desktop

```text
+--------------------------------------------------------------------------------+
| Broker Insights                                           [30d][90d][QTD][YTD] |
+--------------------------------------------------------------------------------+
| Filters: Broker/MGA [__________]  Producer [____]  Territory [____]  Peer Set |
+--------------------------+--------------------------+--------------------------+
| Broker Scorecard         | Quote / Bind Trend       | Retention / Production   |
| Quote count              | line chart + delta       | retention %, premium     |
| Bind count               | denominator + refresh    | denominator + refresh    |
| Quote-to-bind rate       | [Open source records]    | [Open source records]    |
+--------------------------+--------------------------+--------------------------+
| Activity & Pipeline Summary                                                     |
| Recent activity | Open submissions | Renewals due | Bound / declined outcomes |
+--------------------------------------------------------------------------------+
| Benchmarks                                                                      |
| Visible peer set only | rank | percentile | variance | drilldown               |
+--------------------------------------------------------------------------------+
| Review Snapshot                                                                 |
| Highlights | risks | source links | last refreshed                                  |
+--------------------------------------------------------------------------------+
```

### Narrow

```text
+--------------------------------------+
| Broker Insights              [90d v] |
+--------------------------------------+
| Broker/MGA [______________]          |
| Producer [____] Territory [____]     |
+--------------------------------------+
| Scorecard                            |
| quote | bind | q2b | retention       |
| denominator + refresh                |
+--------------------------------------+
| Trend                                |
| selected metric chart                |
| [Open source records]                |
+--------------------------------------+
| Benchmarks                           |
| visible peer set only                |
+--------------------------------------+
| Review Snapshot                      |
+--------------------------------------+
```

## Architecture & Solution Design

### Solution Components

- Introduce broker insight read models rather than new core transactional aggregates, because this feature is primarily analytical and cross-cutting.
- Add scorecard, trend, and benchmark composition services that assemble broker performance signals from submissions, renewals, policies, and activity history.
- Separate metric computation from dashboard rendering so the insight logic can later support reporting exports and territory rollups.
- Reuse F0023 search/reporting and F0017 hierarchy dimensions rather than creating a parallel analytics foundation.
- Treat benchmark logic as configurable business rules, not hard-coded UI formulas.

### Data & Workflow Design

- Build derived projections for quote rate, bind rate, retention, production, and activity intensity using immutable workflow and timeline history as source data.
- Define time-window snapshots or materialized views so trend analysis does not rely on expensive live aggregation against transactional tables.
- Respect hierarchy and producer ownership dimensions from F0017 when producing broker-level rollups and comparative benchmarks.
- Capture metric provenance, refresh timestamp, and denominator counts so users can understand how a score was calculated.

### API & Integration Design

- Expose broker insight endpoints optimized for read access, filtering, and drill-down into underlying records rather than mutation-heavy interactions.
- Reuse F0023 saved views and reporting infrastructure for filtering, sorting, and export patterns instead of inventing a second analytics contract.
- Keep predictive scoring, ML models, and carrier appetite recommendations out of the initial architecture to avoid premature analytical coupling.
- Support navigation from insight cards into broker, account, submission, and renewal detail surfaces with stable deep-link parameters.

### Security & Operational Considerations

- Enforce row-level visibility based on broker hierarchy, territory, and user scope so comparative metrics never leak inaccessible broker data.
- Define refresh cadence and caching policy explicitly because analytical views can tolerate slightly stale data in exchange for predictable performance.
- Instrument slow aggregation paths and projection refresh jobs because broker insights will compete with transactional workloads if left unmanaged.
- Ensure benchmark outputs are auditable enough to explain visible scores during manager review or producer disputes.

## Architecture Traceability

**Taxonomy Reference:** [Feature Architecture Traceability Taxonomy](../../architecture/feature-architecture-traceability-taxonomy.md)

| Classification | Artifact / Decision | ADR |
|----------------|---------------------|-----|
| Introduces: Feature-Local Component | Broker scorecards, benchmark services, and trend projections | [ADR-029](../../architecture/decisions/ADR-029-broker-insights-read-models.md) |
| Introduces: Feature-Local Component | `BrokerInsightProjection` read model and `broker_insight:read` authorization gate | [ADR-029](../../architecture/decisions/ADR-029-broker-insights-read-models.md) |
| Reuses: Established Component/Pattern | Read-side projections over workflow and activity history for analytical views | PRD only |
| Reuses: Established Component/Pattern | Search and reporting substrate used for scalable broker analytics navigation | [ADR-014](../../architecture/decisions/ADR-014-search-index-and-saved-view-architecture.md) (Proposed) |
| Reuses: Established Component/Pattern | Broker/MGA hierarchy, producer ownership, and territory dimensions | [ADR-026](../../architecture/decisions/ADR-026-broker-mga-hierarchy-producer-ownership-and-territory.md) |

## Phase B Architecture Contract

- API contract: `planning-mds/api/nebula-api.yaml` adds `BrokerInsights` read-only endpoints.
- JSON Schemas: `planning-mds/schemas/broker-insight-scorecard.schema.json`, `broker-insight-trend.schema.json`, `broker-insight-benchmark.schema.json`, and `broker-insight-snapshot.schema.json`.
- Data model: `planning-mds/architecture/data-model.md` §12 defines `BrokerInsightProjection`.
- Authorization: `planning-mds/security/authorization-matrix.md` §2.10c and `planning-mds/security/policies/policy.csv` define `broker_insight:read`.
- Boundary: F0008 does not implement F0037 hierarchy-aware access enforcement or distribution rollups.

## Related User Stories

| Story | Title | Status |
|-------|-------|--------|
| [F0008-S0001](./F0008-S0001-broker-scorecard-overview.md) | Broker scorecard overview | Planned |
| [F0008-S0002](./F0008-S0002-trend-drilldown-source-records.md) | Trend drilldown and source record navigation | Planned |
| [F0008-S0003](./F0008-S0003-authorized-benchmark-comparison.md) | Authorized benchmark comparison | Planned |
| [F0008-S0004](./F0008-S0004-review-snapshot.md) | Broker review snapshot | Planned |
| [F0008-S0005](./F0008-S0005-permission-safe-insights.md) | Permission-safe broker insight behavior | Planned |

Refinement confirms F0023, F0017, and F0019 are complete enough for a read-only F0008 MVP. F0037 remains a separate dependency for hierarchy-aware access enforcement and distribution rollups beyond the first F0008 slice.
