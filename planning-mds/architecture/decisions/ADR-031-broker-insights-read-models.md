---
template: adr
version: 1.1
applies_to: architect
---

# ADR-031: Broker Insights Read Models and Permission-Safe Analytics

## Status

- [ ] Proposed
- [x] Accepted (operator G5 Phase B approval — plan run `2026-07-03-4b9ca863`, 2026-07-03)
- [ ] Superseded
- [ ] Rejected

## Context

F0008 adds broker scorecards, trend drilldowns, authorized benchmarks, and review snapshots for internal distribution and relationship teams. It depends on completed operational foundations:

- F0017 provides broker/MGA hierarchy, producer ownership, and territory dimensions, but explicitly defers hierarchy-aware access enforcement and distribution rollups to F0037.
- F0019 records downstream quote, approval, bind, decline, withdraw, and archive outcomes.
- F0023 provides permission-safe search/reporting projections and source-record drilldown behavior.

The architecture must expose useful broker insight views without replacing source aggregates, bypassing source-record authorization, or introducing predictive/economic analytics.

## Decision Drivers

- Keep the first F0008 slice read-only and explainable.
- Reuse F0023 projection and query-layer authorization patterns.
- Preserve F0037 ownership of hierarchy-aware enforcement and distribution rollups.
- Make every metric auditable through source type, denominator, refresh timestamp, and drilldown links.
- Avoid a separate analytics platform for MVP.

## Decision

### 1. BrokerInsightProjection read model

Introduce a derived read-side projection named `BrokerInsightProjection`. It is not a transactional source of truth and never owns business workflow state. Source modules remain authoritative.

Projection rows are broker-period-metric facts with source dimensions:

| Field | Purpose |
|-------|---------|
| `BrokerId`, `BrokerName` | insight target |
| `MetricKey`, `MetricLabel`, `MetricFamily` | metric identity |
| `PeriodStart`, `PeriodEnd`, `Bucket` | selected period or trend bucket |
| `Value`, `Denominator`, `Unit` | explainable metric value |
| `ComparisonValue`, `ComparisonPeriodStart`, `ComparisonPeriodEnd` | period-over-period comparison |
| `SourceObjectTypes` | contributing source types |
| `SourceRecordCount` | authorized denominator support |
| `ProgramId`, `ProducerId`, `TerritoryId`, `LineOfBusiness`, `Region` | F0017/F0023 dimensions |
| `ProjectedAt`, `LastSourceUpdatedAt`, `ProjectionStatus` | freshness and partial-data state |

The projection can be implemented as a materialized table or view over F0023's `OperationalReportProjection`, `SearchDocument`, workflow history, quote/bind records, policy/renewal records, and activity timeline events. Phase C chooses the concrete persistence path, but the API contract is stable.

### 2. Query-time authorization before aggregation output

F0008 adds Casbin resource/action `broker_insight:read`. It gates access to Broker Insights endpoints for internal roles only. After the resource check, query-layer filters apply the same source-record visibility model used by F0023 before any rows, metrics, counts, ranks, medians, trend points, peer sets, or source links leave the server.

External `BrokerUser` and `ExternalUser` have no F0008 policy lines.

Unauthorized records are indistinguishable from non-matches. Hidden brokers do not contribute to peer counts, ranks, medians, or denominator values.

### 3. API surfaces

F0008 adds read-only REST endpoints to `planning-mds/api/nebula-api.yaml`:

- `GET /broker-insights/scorecards`
- `GET /broker-insights/{brokerId}/trends`
- `GET /broker-insights/{brokerId}/benchmarks`
- `GET /broker-insights/{brokerId}/snapshot`

All endpoints:

- require authentication;
- require `broker_insight:read`;
- return `401`, `403`, and `400` ProblemDetails where applicable;
- return `404` when a specific broker is absent or intentionally hidden by visibility policy;
- expose `generatedAt`, `periodStart`, `periodEnd`, denominator counts, and source availability.

Drilldown source rows use the existing `GlobalSearchResult` shape so navigation behavior remains aligned with F0023.

### 4. Benchmark guardrail

Rank and percentile outputs require a minimum visible peer count. Phase B sets the default threshold at **5 visible peers**. If fewer visible peers match the selected criteria, the API suppresses rank/percentile and returns an insufficient-peer-data state.

The threshold prevents small-group inference while preserving median/summary behavior where permitted by Phase C implementation constraints.

### 5. No F0037 replacement

F0008 may consume broker hierarchy, producer, territory, and program dimensions already available through F0017/F0023. It does not introduce recursive visibility predicates, hierarchy-aware access enforcement, territory rollup authority, or distribution rollup projections. Those stay with F0037.

## Options Considered

1. **Reuse F0023 projections + BrokerInsightProjection (chosen).**
2. **Live aggregate directly from transactional tables on every request.**
3. **Build a separate analytics warehouse or external BI service.**
4. **Wait for F0037 before any F0008 slice.**

## Pros / Cons

**Option 1 - Reuse F0023 projections + BrokerInsightProjection**
- ✅ Consistent permission-safe drilldowns and source navigation.
- ✅ Bounded read performance with visible freshness metadata.
- ✅ Shippable before F0037 while preserving F0037 ownership.
- ❌ Requires projection refresh hooks and metric reconciliation tests.

**Option 2 - Live transactional aggregation**
- ✅ No new projection storage.
- ❌ Harder to guarantee permission-safe counts and likely too slow for benchmarks/trends.

**Option 3 - External analytics**
- ✅ Scales later.
- ❌ Adds infrastructure and governance beyond MVP.

**Option 4 - Wait for F0037**
- ✅ Simplifies later rollup behavior.
- ❌ Blocks broker scorecards even though a read-only, authorized first slice is now possible.

## Consequences

- F0008 becomes a read-side reporting feature with no source-record mutations.
- Security Reviewer is required because aggregate analytics can leak hidden-record existence if implemented incorrectly.
- DevOps is required if Phase C implements scheduled refresh/backfill jobs for BrokerInsightProjection.
- Metrics must be testable against source rows and authorization fixtures.

## References

- F0008 PRD and stories (`planning-mds/features/F0008-broker-insights/`)
- ADR-014 Search Index, Saved Views, and Operational Reporting Projections
- ADR-026 Broker/MGA Hierarchy, Producer Ownership & Territory
- F0037 planned boundary for hierarchy-aware enforcement and distribution rollups
- `planning-mds/api/nebula-api.yaml`
- `planning-mds/schemas/broker-insight-*.schema.json`

## Follow-up Actions

- Feature action must define the exact projection refresh strategy, backfill command, and code path placement in `feature-assembly-plan.md`.
- Feature action must include test fixtures proving hidden records do not alter counts, ranks, trend points, or empty states.
- Revisit benchmark threshold and rollup behavior in F0037 if hierarchy-aware reporting is promoted.
