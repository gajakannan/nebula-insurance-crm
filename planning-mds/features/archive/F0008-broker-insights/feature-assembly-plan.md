# Feature Assembly Plan - F0008: Broker Insights

**Created:** 2026-07-03
**Author:** Architect Agent
**Status:** Draft for G0 validation

## Overview

F0008 adds a read-only Broker Insights workspace backed by a permission-filtered read model. The vertical slice mirrors the F0023 read-side reporting pattern: a projection entity, repository, application service, Minimal API endpoints, React feature slice, and evidence that aggregate values never include unauthorized source rows.

## Build Order

| Step | Scope | Stories | Rationale |
|------|-------|---------|-----------|
| 1 | Backend projection and query contracts | F0008-S0001, F0008-S0005 | Establish typed broker-period-metric facts and query validation before API/UI work. |
| 2 | Backend service and endpoints | F0008-S0001 through F0008-S0005 | Implement scorecards, trends, benchmarks, snapshots, and source-row drilldowns per OpenAPI. |
| 3 | Frontend workspace and route | F0008-S0001 through F0008-S0004 | Render the first usable workspace over the API contracts with filter state and drilldown behavior. |
| 4 | Permission and benchmark tests | F0008-S0003, F0008-S0005 | Prove hidden records do not alter counts, ranks, medians, trend points, source rows, or empty states. |
| 5 | Deployability, review, KG reconciliation, closeout | All | Produce governed evidence and bind as-built code paths. |

## Existing Code (Must Be Modified)

| File | Current State | F0008 Change |
|------|---------------|--------------|
| `engine/src/Nebula.Infrastructure/Persistence/AppDbContext.cs` | DbSets include F0023 `SearchDocuments`, `SavedViews`, and `OperationalReportProjections`. | Add `DbSet<BrokerInsightProjection> BrokerInsightProjections`. |
| `engine/src/Nebula.Infrastructure/DependencyInjection.cs` | Registers F0023 repositories/services and shared authorization. | Register `IBrokerInsightProjectionRepository` and `IBrokerInsightService`. |
| `engine/src/Nebula.Api/Program.cs` | Maps all endpoint groups, ending with F0023 search/saved-view/report endpoints. | Add `app.MapBrokerInsightEndpoints()`. |
| `experience/src/App.tsx` | Protected routes include brokers, search, and operational reports. | Add protected `/broker-insights` route. |
| `planning-mds/architecture/feature-assembly-plan.md` | Umbrella plan references prior feature-local plans. | Add F0008 reference and sequencing summary. |
| `planning-mds/features/F0008-broker-insights/STATUS.md` | Phase B approved, stories planned, required roles set in planning. | Initialize G0 role matrix dates and later append implementation evidence. |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `engine/src/Nebula.Domain/Entities/BrokerInsightProjection.cs` | Domain | Read-side broker metric fact row. |
| `engine/src/Nebula.Application/DTOs/BrokerInsightDtos.cs` | Application | Query and response DTOs mirroring OpenAPI. |
| `engine/src/Nebula.Application/Interfaces/IBrokerInsightProjectionRepository.cs` | Application | Projection query/upsert/count contract. |
| `engine/src/Nebula.Application/Interfaces/IBrokerInsightService.cs` | Application | Service contract consumed by API endpoints. |
| `engine/src/Nebula.Application/Services/BrokerInsightService.cs` | Application | Scorecard, trend, benchmark, and snapshot composition. |
| `engine/src/Nebula.Application/Validators/BrokerInsightQueryValidator.cs` | Application | Period, paging, metric, bucket, and peer-set validation. |
| `engine/src/Nebula.Infrastructure/Repositories/BrokerInsightProjectionRepository.cs` | Infrastructure | EF Core query implementation with visibility-first filtering. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/BrokerInsightProjectionConfiguration.cs` | Infrastructure | EF table, column, index, query filter configuration. |
| `engine/src/Nebula.Infrastructure/Persistence/Migrations/*F0008BrokerInsights*` | Infrastructure | Adds projection table and indexes. |
| `engine/src/Nebula.Api/Endpoints/BrokerInsightEndpoints.cs` | API | Minimal API endpoints under `/broker-insights`. |
| `engine/tests/Nebula.Tests/Unit/BrokerInsights/BrokerInsightServiceTests.cs` | Tests | Metric composition, no-data, denominator, benchmark threshold tests. |
| `engine/tests/Nebula.Tests/Integration/BrokerInsightEndpointTests.cs` | Tests | Endpoint auth, ProblemDetails, and permission-filtering integration tests. |
| `experience/src/features/broker-insights/types.ts` | Frontend | TypeScript response/query types. |
| `experience/src/features/broker-insights/hooks.ts` | Frontend | TanStack Query hooks for scorecards, trends, benchmarks, snapshots. |
| `experience/src/features/broker-insights/components/BrokerInsightsWorkspace.tsx` | Frontend | Main workspace composition. |
| `experience/src/features/broker-insights/components/BrokerInsightCards.tsx` | Frontend | Scorecard metric cards. |
| `experience/src/features/broker-insights/components/BrokerInsightTrendDrawer.tsx` | Frontend | Trend/source-row drilldown. |
| `experience/src/features/broker-insights/components/BrokerInsightBenchmarkPanel.tsx` | Frontend | Authorized benchmark comparison. |
| `experience/src/features/broker-insights/components/BrokerInsightSnapshotPanel.tsx` | Frontend | Review snapshot panel. |
| `experience/src/features/broker-insights/index.ts` | Frontend | Feature exports. |
| `experience/src/pages/BrokerInsightsPage.tsx` | Frontend | Route page wrapper. |
| `experience/src/features/broker-insights/tests/BrokerInsightsWorkspace.test.tsx` | Frontend tests | Component behavior and empty/partial states. |

## Step 1 - Projection, DTOs, and Repository (F0008-S0001, F0008-S0005)

### Entity / DTO / Code

```csharp
// engine/src/Nebula.Domain/Entities/BrokerInsightProjection.cs
namespace Nebula.Domain.Entities;

public class BrokerInsightProjection : BaseEntity
{
    public Guid BrokerId { get; set; }
    public string BrokerName { get; set; } = string.Empty;
    public string MetricKey { get; set; } = string.Empty;
    public string MetricLabel { get; set; } = string.Empty;
    public string MetricFamily { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string? Bucket { get; set; }
    public decimal? Value { get; set; }
    public int Denominator { get; set; }
    public string Unit { get; set; } = "count";
    public decimal? ComparisonValue { get; set; }
    public DateOnly? ComparisonPeriodStart { get; set; }
    public DateOnly? ComparisonPeriodEnd { get; set; }
    public string SourceObjectTypesJson { get; set; } = "[]";
    public int SourceRecordCount { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? ProducerId { get; set; }
    public Guid? TerritoryId { get; set; }
    public string? LineOfBusiness { get; set; }
    public string? Region { get; set; }
    public DateTimeOffset LastSourceUpdatedAt { get; set; }
    public DateTimeOffset ProjectedAt { get; set; }
    public string ProjectionStatus { get; set; } = "Available";
}
```

```csharp
// engine/src/Nebula.Application/DTOs/BrokerInsightDtos.cs
namespace Nebula.Application.DTOs;

public sealed record BrokerInsightScorecardQuery(
    Guid? BrokerId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    Guid? ProducerId,
    Guid? TerritoryId,
    Guid? ProgramId,
    string? LineOfBusiness,
    string? Region,
    int Page,
    int PageSize);

public sealed record BrokerInsightTrendQuery(
    Guid BrokerId,
    string MetricKey,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Bucket,
    int Page,
    int PageSize);

public sealed record BrokerInsightBenchmarkQuery(
    Guid BrokerId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string PeerSet);

public sealed record BrokerInsightSnapshotQuery(Guid BrokerId, DateOnly PeriodStart, DateOnly PeriodEnd);
public sealed record BrokerInsightFilterContextDto(Guid? ProducerId, Guid? TerritoryId, Guid? ProgramId, string? LineOfBusiness, string? Region);
public sealed record BrokerInsightMetricCardDto(string MetricKey, string Label, decimal? Value, decimal? ComparisonValue, string Unit, int Denominator, int SourceRecordCount, string Status, bool DrilldownAvailable, DateTimeOffset LastRefreshedAt);
public sealed record BrokerInsightScorecardDto(Guid BrokerId, string BrokerName, DateOnly PeriodStart, DateOnly PeriodEnd, DateOnly? ComparisonPeriodStart, DateOnly? ComparisonPeriodEnd, BrokerInsightFilterContextDto? Filters, IReadOnlyList<BrokerInsightMetricCardDto> Metrics, bool PartialData, DateTimeOffset GeneratedAt);
public sealed record BrokerInsightTrendPointDto(DateOnly BucketStart, DateOnly BucketEnd, decimal? Value, int Denominator, int SourceRecordCount, string Status);
public sealed record BrokerInsightTrendDto(Guid BrokerId, string MetricKey, string Bucket, DateOnly PeriodStart, DateOnly PeriodEnd, IReadOnlyList<BrokerInsightTrendPointDto> Points, IReadOnlyList<GlobalSearchResultDto> SourceRows, bool PartialData, DateTimeOffset GeneratedAt);
public sealed record BrokerInsightPeerSetDto(string Type, int VisiblePeerCount, int MinimumPeerCount, string Status);
public sealed record BrokerInsightBenchmarkMetricDto(string MetricKey, decimal? BrokerValue, int Denominator, decimal? PeerMedian, int? Rank, decimal? Percentile, decimal? Variance, string Status);
public sealed record BrokerInsightBenchmarkDto(Guid BrokerId, DateOnly PeriodStart, DateOnly PeriodEnd, BrokerInsightPeerSetDto PeerSet, IReadOnlyList<BrokerInsightBenchmarkMetricDto> Metrics, DateTimeOffset GeneratedAt);
public sealed record BrokerInsightSnapshotItemDto(string Label, string Value, int SourceRecordCount);
public sealed record BrokerInsightSnapshotDto(Guid BrokerId, string BrokerName, DateOnly PeriodStart, DateOnly PeriodEnd, IReadOnlyList<BrokerInsightSnapshotItemDto> Highlights, IReadOnlyList<BrokerInsightSnapshotItemDto> Risks, string? ActivitySummary, string? OpportunitySummary, IReadOnlyList<GlobalSearchResultDto> SourceLinks, bool PartialData, DateTimeOffset GeneratedAt);
```

### Logic Flow

`IBrokerInsightProjectionRepository.QueryAsync(query, visibility, ct)` returns only rows passing source visibility before any aggregation.

1. Start from `BrokerInsightProjections.AsNoTracking()`.
2. If `visibility.SeeAll` is false, filter to rows whose `Region` is in the current user's regions or whose source owner/user dimension matches existing projection visibility rules where present.
3. Apply broker, producer, territory, program, LOB, region, period, metric, bucket, paging filters.
4. Return the filtered rows to the service. Aggregation never sees hidden rows.

### Mutation Traceability

N/A - read-only projection and repository. Upsert/backfill is operational seed/refresh behavior, not a user mutation in F0008.

### Casbin Enforcement

- Resource: `broker_insight`, Action: `read`.
- Hydrate attrs: current user roles and source visibility dimensions through `ProjectionVisibilityResolver.For(user)`.
- Enforcement pattern: endpoint checks `AuthzHelper.HasPermissionAsync(authz, user, "broker_insight", "read")` before service call; repository applies visibility predicate before aggregation.

## Step 2 - Broker Insight Service and Endpoints (F0008-S0001 through F0008-S0005)

### Service Methods

```csharp
// engine/src/Nebula.Application/Interfaces/IBrokerInsightService.cs
public interface IBrokerInsightService
{
    Task<PaginatedResult<BrokerInsightScorecardDto>> GetScorecardsAsync(BrokerInsightScorecardQuery query, ICurrentUserService user, CancellationToken ct);
    Task<BrokerInsightTrendDto?> GetTrendAsync(BrokerInsightTrendQuery query, ICurrentUserService user, CancellationToken ct);
    Task<BrokerInsightBenchmarkDto?> GetBenchmarkAsync(BrokerInsightBenchmarkQuery query, ICurrentUserService user, CancellationToken ct);
    Task<BrokerInsightSnapshotDto?> GetSnapshotAsync(BrokerInsightSnapshotQuery query, ICurrentUserService user, CancellationToken ct);
}
```

### Logic Flow

1. Endpoints validate `broker_insight:read`; reject unauthorized users with ProblemDetails `policy_denied`.
2. Validators enforce `periodStart <= periodEnd`, allowed metric keys, allowed buckets, allowed peer sets, page bounds, and max page size 200.
3. Scorecards group filtered projection rows by broker and map seven metric cards: `quoteCount`, `bindCount`, `quoteToBindRate`, `retentionRate`, `openPipelineCount`, `activityCount`, `productionAmount`.
4. Trends group filtered rows by `Bucket`/period and attach source rows mapped from F0023 `GlobalSearchResultDto`.
5. Benchmarks compute visible peer count from filtered broker rows; suppress rank/percentile when visible peers < 5.
6. Snapshot composes top highlights, risks, activity/opportunity summaries, and source links from the same filtered row set.
7. Specific broker endpoints return null when no authorized broker data exists; endpoint maps null to 404 to avoid hidden-record leakage.

### HTTP Responses

| Endpoint | 200 Body | 400 | 401 | 403 | 404 |
|----------|----------|-----|-----|-----|-----|
| `GET /broker-insights/scorecards` | Paginated scorecards | validation error | invalid auth | policy denied | N/A |
| `GET /broker-insights/{brokerId}/trends` | `BrokerInsightTrendDto` | validation error | invalid auth | policy denied | hidden or absent broker |
| `GET /broker-insights/{brokerId}/benchmarks` | `BrokerInsightBenchmarkDto` | validation error | invalid auth | policy denied | hidden or absent broker |
| `GET /broker-insights/{brokerId}/snapshot` | `BrokerInsightSnapshotDto` | validation error | invalid auth | policy denied | hidden or absent broker |

### Mutation Traceability

N/A - all endpoint surfaces are read-only. No broker, submission, renewal, policy, activity, benchmark definition, document, email, or timeline record is created or changed.

## Step 3 - Frontend Broker Insights Workspace (F0008-S0001 through F0008-S0004)

### Frontend Contracts

```ts
// experience/src/features/broker-insights/types.ts
export type BrokerInsightMetricKey =
  | 'quoteCount'
  | 'bindCount'
  | 'quoteToBindRate'
  | 'retentionRate'
  | 'openPipelineCount'
  | 'activityCount'
  | 'productionAmount'

export interface BrokerInsightScorecardParams {
  brokerId?: string
  periodStart: string
  periodEnd: string
  producerId?: string
  territoryId?: string
  programId?: string
  lineOfBusiness?: string
  region?: string
  page?: number
  pageSize?: number
}
```

### UI Flow

1. `/broker-insights` renders `BrokerInsightsPage` inside `DashboardLayout`.
2. Workspace defaults to 90-day window and exposes 30d, 90d, QTD, and YTD controls.
3. Scorecard cards show value, denominator, comparison, metric status, and last refreshed timestamp.
4. Drilldown drawer calls trends endpoint for selected broker/metric and lists authorized source rows.
5. Benchmark panel shows peer-set status, median, variance, and suppressed rank state.
6. Snapshot panel reuses scorecard/trend data where possible and calls the snapshot endpoint for review-ready summary.

### Mutation Traceability

N/A - frontend interactions set filters, open read-only drilldowns, navigate to source routes, and do not mutate state.

## Step 4 - Quality, Security, and Deployability

| Evidence | Required Proof |
|----------|----------------|
| Backend unit tests | Metric card mapping, percentage denominator zero -> `N/A`/null value, benchmark threshold suppression, snapshot summaries. |
| Backend integration tests | 401/403/404/400 behavior, internal role success, external role denied, hidden records omitted from counts and source rows. |
| Frontend tests | Workspace renders cards, empty/partial states, benchmark insufficient-peer copy, drilldown source row navigation target. |
| Security scans | Dependency, secrets, SAST, and DAST artifacts or explicit waivers because `security_sensitive_scope=true`. |
| Deployability | Migration applies, app starts, `/broker-insights/scorecards` smoke returns authenticated response path. |

## Scope Breakdown

| Layer | Required Work | Owner | Status |
|------|----------------|-------|--------|
| Backend (`engine/`) | Projection, repository, service, validators, endpoints, migration, tests | Backend Developer | Planned |
| Frontend (`experience/`) | Workspace, route, hooks, components, tests | Frontend Developer | Planned |
| AI (`neuron/`) | None; F0008 MVP has no AI/LLM/MCP scope | AI Engineer | Not required |
| Quality | Test plan, execution report, coverage report, acceptance mapping | Quality Engineer | Planned |
| DevOps/Runtime | Runtime preflight, migration/deployability check | DevOps | Planned |
| Security | Aggregate leakage review and scan verdict | Security Reviewer | Planned |

## Dependency Order

```text
Step 0 (Architect): feature assembly plan and G0 validation
Step 1 (Backend): projection + DTOs + repository + service contracts
Step 2 (Backend): service logic + endpoints + backend tests
  ---- Backend checkpoint: endpoint tests and unit tests pass ----
Step 3 (Frontend): workspace route + hooks + UI components + component tests
  ---- Frontend checkpoint: lint, build, and feature tests pass ----
Step 4 (QE/DevOps/Security): evidence, scans, deployability, reviews
Step 5 (Architect/PM): G7 KG reconciliation and G8 closeout
```

## Integration Checkpoints

### Backend Checkpoint

- [ ] OpenAPI operations implemented exactly: scorecards, trends, benchmarks, snapshot.
- [ ] `broker_insight:read` enforced before service execution.
- [ ] Source visibility predicate runs before scorecard/trend/benchmark/snapshot aggregation.
- [ ] Benchmark rank and percentile suppressed below five visible peers.

### Frontend Checkpoint

- [ ] `/broker-insights` route is protected.
- [ ] Cards show denominator, period, comparison, status, and refresh timestamp.
- [ ] Drilldown source rows preserve broker/metric/period context and navigate by `targetUrl`.
- [ ] Narrow layout stacks scorecard, trends, benchmarks, snapshot.

### Cross-Story Verification

- [ ] F0008-S0001 scorecard metrics match authorized projection rows.
- [ ] F0008-S0002 trend/source rows reconcile to displayed denominators.
- [ ] F0008-S0003 hidden peers do not change visible peer counts or ranks.
- [ ] F0008-S0004 snapshot omits unauthorized source links.
- [ ] F0008-S0005 all aggregate outputs are computed after authorization filtering.

## Integration Checklist

- [ ] API contract compatibility validated.
- [ ] Frontend contract compatibility validated.
- [ ] AI contract compatibility marked not applicable.
- [ ] Test cases mapped to acceptance criteria.
- [ ] Developer-owned fast-test responsibilities identified by layer.
- [ ] Runtime evidence paths recorded under run `2026-07-03-fd732693`.
- [ ] No `agents/**` framework drift in feature scope.
- [ ] Mutation traceability marked N/A for every read-only story.
- [ ] Run/deploy instructions updated in `GETTING-STARTED.md`.

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Aggregate leakage from hidden rows | High | Visibility-first repository query, endpoint auth, security review, permission tests. | Backend/Security |
| Projection freshness confusion | Medium | Expose `generatedAt`, `lastRefreshedAt`, `ProjectionStatus`, and deployability notes. | Backend/DevOps |
| Peer benchmark inference in small sets | Medium | Enforce minimum visible peer count of 5 before rank/percentile. | Backend/QE |
| F0037 boundary creep | Medium | Do not add hierarchy-aware enforcement or rollup authority; consume existing dimensions only. | Architect/Code Reviewer |

## JSON Serialization Convention

Use ASP.NET Core default camelCase JSON for DTO responses. `DateOnly` query parameters and response fields serialize as ISO `yyyy-MM-dd`; `DateTimeOffset` fields serialize as ISO 8601 date-time with offset. Monetary/percentage values are `decimal?` in C# and `number | null` in TypeScript.

## DI Registration Changes

- Add `services.AddScoped<IBrokerInsightProjectionRepository, BrokerInsightProjectionRepository>();`.
- Add `services.AddScoped<IBrokerInsightService, BrokerInsightService>();`.
- Validators are discovered through `AddValidatorsFromAssemblyContaining<BrokerCreateValidator>()`; place `BrokerInsight*Validator` in the application validators assembly.

## Casbin Policy Sync

Architecture already added `broker_insight:read` to `planning-mds/security/policies/policy.csv`. Implementation must verify the runtime policy source includes the same rows or update the embedded/runtime policy copy if one exists.

## Knowledge-Graph Binding Plan

- Expected code bindings at G7:
  - `engine/src/Nebula.Domain/Entities/BrokerInsightProjection.cs` -> `entity:broker-insight-projection`
  - `engine/src/Nebula.Application/Services/BrokerInsightService.cs` -> `capability:broker-insights`
  - `engine/src/Nebula.Infrastructure/Repositories/BrokerInsightProjectionRepository.cs` -> `entity:broker-insight-projection`
  - `engine/src/Nebula.Api/Endpoints/BrokerInsightEndpoints.cs` -> `endpoint:broker-insight-*`
  - `experience/src/features/broker-insights/**` -> `capability:broker-insights`
- New canonical nodes expected: none; Phase B already introduced F0008 semantic nodes.
