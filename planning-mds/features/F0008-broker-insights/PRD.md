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

**Out of Scope:**
- Full predictive analytics
- Commission accounting
- Carrier appetite modeling

## Success Criteria

- Managers can review broker performance without manual spreadsheet assembly.
- Insight views support quarterly reviews and relationship prioritization.
- Metrics are consistent with operational source data.

## Risks & Assumptions

- **Risk:** Insights are built before underlying workflow data is trustworthy.
- **Assumption:** Submission, renewal, and policy data will exist before this feature is finalized.
- **Mitigation:** Keep F0008 sequenced after operational workflow foundations.

## Dependencies

- F0006 Submission Intake Workflow
- F0007 Renewal Pipeline
- F0023 Global Search, Saved Views & Operational Reporting

## Architecture & Solution Design

### Solution Components

- Introduce broker insight read models rather than new core transactional aggregates, because this feature is primarily analytical and cross-cutting.
- Add scorecard, trend, and benchmark composition services that assemble broker performance signals from submissions, renewals, policies, and activity history.
- Separate metric computation from dashboard rendering so the insight logic can later support reporting exports and territory rollups.
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
| Introduces: Feature-Local Component | Broker scorecards, benchmark services, and trend projections | PRD only |
| Reuses: Established Component/Pattern | Read-side projections over workflow and activity history for analytical views | PRD only |
| Reuses: Established Component/Pattern | Search and reporting substrate used for scalable broker analytics navigation | [ADR-014](../../architecture/decisions/ADR-014-search-index-and-saved-view-architecture.md) (Proposed) |

## Related User Stories

- To be defined during refinement
