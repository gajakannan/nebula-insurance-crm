# Feature Assembly Plan - F0037: Hierarchy-Aware Access Scoping & Distribution Rollups

**Created:** 2026-07-06
**Author:** Architect Agent
**Status:** Draft - pending G5 approval
**Plan Run:** `2026-07-06-6e3851ab`

> This is the Phase B implementation execution plan. It authorizes no code by itself; the later `feature` action owns implementation and feature evidence package creation after G5 approval.

## Overview

F0037 replaces the current broad `ProjectionVisibility` shape with a hierarchy-aware distribution scope resolved from role, hierarchy, territory, producer ownership, source-record visibility, and `asOf`. Existing F0023/F0008 projection repositories remain the reporting substrate and keep the predicate-first invariant: hidden rows are filtered before rows, counts, facets, suggestions, drilldowns, insights, or rollups are materialized.

No new external search/reporting infrastructure is approved. MVP rollups are query-time over existing `SearchDocument`, `OperationalReportProjection`, and `BrokerInsightProjection` rows plus F0017 hierarchy/territory/ownership context.

## Build Order

| Step | Scope | Stories | Rationale |
|------|-------|---------|-----------|
| 1 | Distribution scope resolver | F0037-S0001 | Centralizes role/hierarchy/territory/producer/as-of logic before any consumer changes. |
| 2 | Predicate-first projection visibility | F0037-S0002, F0037-S0003 | Extends existing repositories while preserving no-leak rows/counts/facets/drilldowns. |
| 3 | Distribution rollup API and service | F0037-S0004 | Adds the new read contract after shared scope is available. |
| 4 | Report/search UI filters and states | F0037-S0005 | Frontend consumes stable contracts and no-leak state semantics. |
| 5 | Security, reconciliation, and evidence | F0037-S0006 | Proves policy parity and scoped totals before closeout. |

## Existing Code To Modify

| File | Current State | F0037 Change |
|------|---------------|--------------|
| `engine/src/Nebula.Application/DTOs/SearchDtos.cs` | `GlobalSearchQuery` has 8 parameters; `ProjectionVisibility` is `SeeAll/Regions/UserId`. | Expand query filters with `rootNodeId`, `territoryId`, `producerUserId`, `asOf`. Replace `ProjectionVisibility` with distribution-scope-aware fields. |
| `engine/src/Nebula.Application/DTOs/OperationalReportDtos.cs` | `OperationalReportQuery` has 6 parameters and two report response records. | Expand query filters and add `DistributionRollupQuery`, `DistributionRollupReportDto`, `DistributionRollupRowDto`, `DistributionRollupMetricSetDto`, `DistributionScopeEchoDto`. |
| `engine/src/Nebula.Application/DTOs/BrokerInsightDtos.cs` | Broker insight queries filter by broker/producer/territory/program/LOB/region. | Use the shared distribution scope internally; do not add public F0037 query params except where already present. |
| `engine/src/Nebula.Application/Services/ProjectionVisibilityResolver.cs` | Static role resolver grants see-all to Admin, ProgramManager, DistributionManager. | Replace with async `DistributionScopeService`; remove manager see-all behavior. |
| `engine/src/Nebula.Application/Services/SearchService.cs` | Calls static `ProjectionVisibilityResolver.For(user)`. | Resolve distribution scope using `GlobalSearchQuery.AsOf` and pass it to repository. |
| `engine/src/Nebula.Application/Services/OperationalReportService.cs` | Aggregates rows returned by `IOperationalReportProjectionRepository.QueryAsync`. | Resolve scope for workload/aging; add `GetDistributionRollupsAsync`. |
| `engine/src/Nebula.Application/Services/BrokerInsightService.cs` | Uses static visibility resolver and region-only broker insight scoping. | Resolve scope using `PeriodEnd` as `asOf`; filter broker insight rows by scope before metrics/peer sets. |
| `engine/src/Nebula.Infrastructure/Repositories/SearchDocumentRepository.cs` | Applies `OwnerUserId == user` or region scope before facets/counts. | Apply distribution scope predicates for broker, territory, producer/owner, region, and empty-scope fail-closed behavior. |
| `engine/src/Nebula.Infrastructure/Repositories/OperationalReportProjectionRepository.cs` | Applies owner/region predicate before report filters. | Apply distribution scope predicates and add rollup row query support if needed. |
| `engine/src/Nebula.Infrastructure/Repositories/BrokerInsightProjectionRepository.cs` | Applies region predicate before metric filters. | Apply distribution scope predicates for broker, territory, producer, program, region, and fail-closed empty scope. |
| `engine/src/Nebula.Api/Endpoints/SearchEndpoints.cs` | Binds F0023 search query params. | Bind F0037 `rootNodeId`, `territoryId`, `producerUserId`, and `asOf`. |
| `engine/src/Nebula.Api/Endpoints/OperationalReportEndpoints.cs` | Maps workload and workflow-aging. | Bind F0037 filters and map `GET /operational-reports/distribution-rollups`. |
| `engine/src/Nebula.Api/Program.cs` | Registers F0017/F0023/F0008 services. | Register `IDistributionScopeService`, `IDistributionScopeRepository`, and `IDistributionRollupService` if split from `IOperationalReportService`. |
| `experience/src/features/search/*` | Search filters include object/status/region/LOB/sort. | Add approved hierarchy/territory/producer/as-of filters and no-leak empty state copy. |
| `experience/src/features/reports/*` | Reports include basic controls and workload/aging views. | Add distribution rollup types, hook, view, filters, panels, drilldowns, and distinct empty/no-access/stale/error states. |
| `experience/src/features/broker-insights/*` | Insights consume existing F0008 contracts. | Preserve UI but verify scoped/filtered-away/no-access behavior for F0037. |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `engine/src/Nebula.Application/Interfaces/IDistributionScopeService.cs` | Application | Scope resolver contract consumed by search, reports, insights, and read scoping. |
| `engine/src/Nebula.Application/Interfaces/IDistributionScopeRepository.cs` | Application | Query contract for F0017 hierarchy/territory/ownership scope materialization. |
| `engine/src/Nebula.Application/Services/DistributionScopeService.cs` | Application | Resolves fail-closed `ProjectionVisibility` from user, requested filters, and `asOf`. |
| `engine/src/Nebula.Infrastructure/Repositories/DistributionScopeRepository.cs` | Infrastructure | EF Core queries over DistributionNodes, ProducerOwnership, TerritoryAssignments, and projection dimensions. |
| `engine/src/Nebula.Application/Validators/DistributionRollupQueryValidator.cs` | Application | Validates grouping, metric family, and filter shape. |
| `experience/src/features/reports/components/DistributionRollupReportView.tsx` | Frontend | Rollup table/panel view with scoped drilldowns and no-leak states. |
| `experience/src/features/reports/components/DistributionRollupPanels.tsx` | Frontend | Metric summary panels for production/workflow/activity totals. |
| `experience/src/features/reports/tests/DistributionRollupReportView.test.tsx` | Frontend test | Filter, drilldown, no-access, empty, stale, and accessibility coverage. |

## Step 1 - Distribution Scope Resolver (F0037-S0001)

### Entity / DTO / Code

```csharp
// engine/src/Nebula.Application/DTOs/SearchDtos.cs
public sealed record ProjectionVisibility(
    bool SeeAll,
    Guid UserId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Regions,
    IReadOnlySet<Guid> DistributionNodeIds,
    IReadOnlySet<Guid> BrokerIds,
    IReadOnlySet<Guid> TerritoryIds,
    IReadOnlySet<Guid> ProducerUserIds,
    DateOnly AsOf,
    bool HasScope,
    IReadOnlyList<string> ExplanationCodes);

public sealed record DistributionScopeRequest(
    Guid? RootNodeId,
    Guid? TerritoryId,
    Guid? ProducerUserId,
    DateOnly? AsOf);
```

```csharp
// engine/src/Nebula.Application/Interfaces/IDistributionScopeService.cs
public interface IDistributionScopeService
{
    Task<ProjectionVisibility> ResolveAsync(
        DistributionScopeRequest request,
        ICurrentUserService user,
        CancellationToken ct);
}

// engine/src/Nebula.Application/Interfaces/IDistributionScopeRepository.cs
public interface IDistributionScopeRepository
{
    Task<IReadOnlySet<Guid>> ListSubtreeNodeIdsAsync(Guid rootNodeId, CancellationToken ct);
    Task<IReadOnlySet<Guid>> ListBrokerIdsForNodesAsync(IReadOnlySet<Guid> nodeIds, CancellationToken ct);
    Task<IReadOnlySet<Guid>> ListBrokerIdsForTerritoryAsync(Guid territoryId, DateOnly asOf, CancellationToken ct);
    Task<IReadOnlySet<Guid>> ListProducerScopeIdsAsync(Guid producerUserId, DateOnly asOf, CancellationToken ct);
}
```

### Logic Flow

`DistributionScopeService.ResolveAsync(request, user, ct) -> ProjectionVisibility`

1. Normalize `asOf = request.AsOf ?? DateOnly.FromDateTime(DateTime.UtcNow)`.
2. If `user.Roles` contains `BrokerUser` or `ExternalUser`, return `HasScope=false` with `external_denied`.
3. If `Admin`, return `SeeAll=true`, preserving optional requested filters as narrowing filters after validation.
4. Resolve requested `rootNodeId` to descendant node ids and broker ids; if requested root is outside the user's managed scope, return empty scope.
5. Resolve requested/current territory ids and producer ids using effective-dated F0017 data.
6. Intersect role-derived scope with requested filters. Empty intersection is valid and fail-closed.
7. Return explanation codes such as `admin_full_scope`, `manager_subtree_scope`, `territory_scope`, `producer_scope`, `external_denied`, or `empty_scope`.

### Mutation Traceability

N/A - read-only.

### Casbin Enforcement

- The resolver does not replace endpoint-level Casbin gates.
- Consuming endpoints continue checking their resource/action first:
  - `global_search:read`
  - `operational_report:read`
  - `broker_insight:read`
  - `distribution_node:read`
  - `producer_ownership:read`
  - `territory:read`
  - `distribution_rollup:read`

### HTTP Responses

| Status | Body | Condition |
|--------|------|-----------|
| 200 | Empty authorized result shape | Scope resolves to empty. |
| 400 | ProblemDetails (`validation_error`) | Invalid date or malformed filter. |
| 403 | ProblemDetails (`policy_denied`) | Endpoint resource/action denied before scope resolution. |
| 404 | ProblemDetails (`not_found`) | Direct hidden record access after broad resource authorization. |

## Step 2 - Predicate-First Search, Report, Insight, and Direct Read Scoping (F0037-S0002, F0037-S0003)

### Code Signatures

```csharp
// engine/src/Nebula.Application/DTOs/SearchDtos.cs
public sealed record GlobalSearchQuery(
    string Q,
    IReadOnlyList<string> ObjectTypes,
    string? Status,
    Guid? OwnerUserId,
    Guid? RootNodeId,
    Guid? TerritoryId,
    Guid? ProducerUserId,
    DateOnly? AsOf,
    string? Region,
    string? LineOfBusiness,
    string Sort,
    int Page,
    int PageSize);

// engine/src/Nebula.Application/DTOs/OperationalReportDtos.cs
public sealed record OperationalReportQuery(
    string? Region,
    string? LineOfBusiness,
    Guid? OwnerUserId,
    Guid? RootNodeId,
    Guid? TerritoryId,
    Guid? ProducerUserId,
    string? WorkflowType,
    DateOnly? AsOf,
    int DrilldownLimit);
```

### Repository Predicate Rules

1. `HasScope=false` returns no rows.
2. `SeeAll=true` skips role scope but still applies requested filters.
3. Broker scope filters `BrokerId` on SearchDocument, OperationalReportProjection, and BrokerInsightProjection.
4. Territory scope filters `TerritoryId`.
5. Producer scope filters `OwnerUserId` for search/report rows and `ProducerId` where broker-insight projections use producer dimensions.
6. Region fallback remains only as an additional narrowing predicate; it must not grant visibility beyond distribution scope.
7. Counts, facets, suggestions, peer sets, medians, ranks, drilldowns, and rollups are computed from the already-filtered queryable.

### Direct Read Rule

Distribution-node, producer-ownership, territory, broker, and source-detail reads must call the resolver or a shared predicate helper before returning detail. If the record exists but is outside scope, return `ProblemDetailsHelper.NotFound()` or the existing 404 helper; do not return hidden-count or scope diagnostics to the caller.

### Mutation Traceability

N/A - read-only.

### Tests Required

- Admin full scope.
- DistributionManager subtree scope.
- ProgramManager program/hierarchy scope.
- RelationshipManager producer/territory scope.
- Sibling branch exclusion.
- External denial.
- Saved-view reapplication by executing user.
- Counts/facets/suggestions/drilldowns computed after filtering.

## Step 3 - Distribution Rollup API and Service (F0037-S0004)

### API Contract

`GET /operational-reports/distribution-rollups`

Query parameters:

| Parameter | Type | Required | Notes |
|-----------|------|----------|-------|
| `groupBy` | `Hierarchy | Territory | Producer` | Yes | Determines rollup row grouping. |
| `metricFamily` | `Production | Workflow | Activity` | Yes | Selects metric family. |
| `asOf` | date | No | Defaults to current business date. |
| `rootNodeId` | uuid | No | Must be inside resolved scope. |
| `territoryId` | uuid | No | Must be inside resolved scope. |
| `producerUserId` | uuid | No | Must be inside resolved scope. |

### Code Signatures

```csharp
public sealed record DistributionRollupQuery(
    string GroupBy,
    string MetricFamily,
    DateOnly? AsOf,
    Guid? RootNodeId,
    Guid? TerritoryId,
    Guid? ProducerUserId);

public sealed record DistributionScopeEchoDto(
    Guid? RootNodeId,
    Guid? TerritoryId,
    Guid? ProducerUserId);

public sealed record DistributionRollupMetricSetDto(
    int RecordCount,
    int ProductionCount,
    int WorkflowOpen,
    int WorkflowOverdue,
    int ActivityCount);

public sealed record DistributionRollupRowDto(
    string GroupKey,
    string GroupLabel,
    string GroupType,
    DistributionRollupMetricSetDto Metrics,
    string? DrilldownUrl,
    string? UnavailableReason);

public sealed record DistributionRollupReportDto(
    string GroupBy,
    string MetricFamily,
    DateOnly AsOf,
    DateTimeOffset GeneratedAt,
    DistributionScopeEchoDto? Scope,
    DistributionRollupMetricSetDto Totals,
    IReadOnlyList<DistributionRollupRowDto> Rows);
```

```csharp
public interface IOperationalReportService
{
    Task<OperationalWorkloadReportDto> GetWorkloadAsync(OperationalReportQuery query, ICurrentUserService user, CancellationToken ct);
    Task<WorkflowAgingReportDto> GetWorkflowAgingAsync(OperationalReportQuery query, ICurrentUserService user, CancellationToken ct);
    Task<DistributionRollupReportDto> GetDistributionRollupsAsync(DistributionRollupQuery query, ICurrentUserService user, CancellationToken ct);
}
```

### Logic Flow

`OperationalReportService.GetDistributionRollupsAsync(query, user, ct)`

1. Endpoint verifies `distribution_rollup:read`.
2. Validate `groupBy`, `metricFamily`, ids, and date.
3. Resolve distribution scope with `DistributionScopeService`.
4. If `HasScope=false`, return empty `Rows=[]` and zero totals.
5. For `Workflow`, query `OperationalReportProjectionRepository.QueryAsync` with scoped `OperationalReportQuery`.
6. For `Production` and `Activity`, query `BrokerInsightProjectionRepository.QueryAsync` with scoped `BrokerInsightProjectionQuery`; if the metric family cannot be derived from existing projections, return rows with `unavailableReason` rather than fabricated totals.
7. Group by hierarchy node, territory, or producer.
8. Compute totals from visible rows only.
9. Create drilldown URLs that preserve `rootNodeId`, `territoryId`, `producerUserId`, `asOf`, and grouping filters. Drilldown targets revalidate scope.
10. Return `DistributionRollupReportDto`.

### Mutation Traceability

N/A - read-only.

### Casbin Enforcement

- Resource: `distribution_rollup`, Action: `read`
- Allowed roles: Admin, DistributionManager, ProgramManager, RelationshipManager
- External roles: denied by absence of policy row
- Enforcement location: `OperationalReportEndpoints.DistributionRollups`

### HTTP Responses

| Status | Body | Condition |
|--------|------|-----------|
| 200 | `DistributionRollupReportDto` | Success or empty authorized scope. |
| 400 | ProblemDetails (`validation_error`) | Invalid grouping, metric family, date, or filter id format. |
| 401 | ProblemDetails | Missing/invalid auth token. |
| 403 | ProblemDetails (`policy_denied`) | Missing `distribution_rollup:read`. |

## Step 4 - Frontend Filters, Panels, Drilldowns, and States (F0037-S0005)

### TypeScript Contracts

```ts
export type DistributionRollupGroupBy = 'Hierarchy' | 'Territory' | 'Producer';
export type DistributionRollupMetricFamily = 'Production' | 'Workflow' | 'Activity';

export interface DistributionRollupMetricSet {
  recordCount: number;
  productionCount: number;
  workflowOpen: number;
  workflowOverdue: number;
  activityCount: number;
}

export interface DistributionRollupRow {
  groupKey: string;
  groupLabel: string;
  groupType: DistributionRollupGroupBy;
  metrics: DistributionRollupMetricSet;
  drilldownUrl: string | null;
  unavailableReason: string | null;
}

export interface DistributionRollupReport {
  groupBy: DistributionRollupGroupBy;
  metricFamily: DistributionRollupMetricFamily;
  asOf: string;
  generatedAt: string;
  scope: { rootNodeId: string | null; territoryId: string | null; producerUserId: string | null } | null;
  totals: DistributionRollupMetricSet;
  rows: DistributionRollupRow[];
}
```

### UI Rules

1. Extend `experience/src/features/reports/types.ts` and `hooks.ts`.
2. Add rollup controls to existing reports workspace without replacing workload/aging views.
3. Use semantic theme tokens only; no raw palette classes for app UI text/surfaces/borders.
4. States must be distinct:
   - empty in-scope result
   - filters removed all visible rows
   - no access
   - stale/unavailable metric
   - system error
5. Do not show hidden record counts in option labels, empty copy, disabled labels, or errors.
6. Drilldowns use the backend-provided URL and revalidate through search/report endpoints.

### Tests Required

- `DistributionRollupReportView.test.tsx` covers filter changes, empty/no-access/stale/error states, drilldown URL use, and keyboard navigation.
- Existing `BrokerInsightsWorkspace.test.tsx`, search tests, and report tests add no-leak regression cases.
- Run `pnpm --dir experience lint:theme` during feature implementation if UI classes change.

## Step 5 - Security Evidence and Reconciliation (F0037-S0006)

### Required Evidence

- Backend integration tests:
  - Admin full scope.
  - Manager subtree.
  - Producer/territory.
  - Sibling exclusion.
  - BrokerUser/ExternalUser denial.
  - Direct hidden detail returns no-leak 404.
  - Rollup totals reconcile to scoped source rows.
- Repository/service tests proving predicates run before counts, facets, drilldowns, peer sets, and aggregates.
- Frontend tests for filters, rollup panels, no-leak states, saved-view reapplication, and accessibility.
- Security review is mandatory.
- Code review and QE signoff are mandatory.
- DevOps signoff is not required unless implementation introduces materialized rollup jobs, background recomputation, new runtime config, or deployment changes.

## Scope Breakdown

| Layer | Required Work | Owner | Status |
|------|----------------|-------|--------|
| Backend (`engine/`) | Scope resolver, projection predicate extension, rollup service/endpoint, validators, integration/unit tests. | Backend Developer | Planned |
| Frontend (`experience/`) | Report filters, rollup panels/table, drilldowns, no-leak states, UI tests. | Frontend Developer | Planned |
| AI (`neuron/`) | None. | N/A | Out of scope |
| Quality | Backend/frontend/security reconciliation tests and evidence. | Quality Engineer | Planned |
| DevOps/Runtime | None expected for query-time MVP. Revisit if materialization is introduced. | DevOps | Conditional |
| Security | Policy/no-leak review. | Security Reviewer | Required |

## Dependency Order

```text
Step 0 (Architect):   G5 approval for this plan
Step 1 (Backend):     Distribution scope resolver and repository
Step 2 (Backend):     Search/report/insight predicate extension
Step 3 (Backend):     Distribution rollup API/service/contracts
  ---- Backend checkpoint: policy, no-leak, and rollup reconciliation tests pass ----
Step 4 (Frontend):    Rollup controls, panels, drilldowns, and states
  ---- Frontend checkpoint: UI behavior/a11y/theme tests pass ----
Step 5 (QE/Security): Cross-surface validation, security review, signoff
```

## Integration Checkpoints

### After Step 1

- [ ] Empty scope fails closed.
- [ ] External roles are denied.
- [ ] `asOf` effective dating is deterministic.

### After Step 2

- [ ] Search counts/facets/results exclude hidden rows.
- [ ] Operational report counts/drilldowns exclude hidden rows.
- [ ] Broker insights peer sets/ranks/trends exclude hidden rows.
- [ ] Saved views recompute executing-user scope.

### After Step 3

- [ ] `GET /operational-reports/distribution-rollups` returns schema-valid response.
- [ ] Rollup totals reconcile to scoped source rows.
- [ ] Drilldown URLs preserve scope filters and revalidate on target.

### Cross-Story Verification

- [ ] Full lifecycle: resolve scope -> run search/report/insight/rollup -> drill into visible rows -> hidden sibling omitted.
- [ ] All Casbin policies enforced: global_search, operational_report, broker_insight, saved_view, distribution_node, producer_ownership, territory, distribution_rollup.
- [ ] ProblemDetails format consistent with API guide.
- [ ] No feature evidence package exists until the future `feature` action.

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| User-to-hierarchy authority source is thinner than role/region claims. | High | MVP resolver uses role, existing regions, territory, producer ownership, and requested filters; implementation must add tests documenting any authority fallback and fail closed. | Architect + Backend |
| Production/activity metrics may be unavailable from current projections. | Medium | Use BrokerInsightProjection where available; return `unavailableReason` instead of fabricated values. | Backend |
| Hidden-count leaks through UI option labels. | High | Filter options must be scoped; tests assert no hidden totals in empty/no-access states. | Frontend + Security |
| Existing OpenAPI report schemas predate current C# DTO shape. | Medium | F0037 new rollup schema is self-contained; feature action should reconcile old F0023 schema drift only if touched by implementation. | Architect + Backend |

## JSON Serialization Convention

- JSON uses camelCase.
- Dates use ISO `yyyy-MM-dd`.
- Date-times use ISO 8601 UTC offsets.
- `Guid?` nullable fields serialize as `null`, not omitted.
- ProblemDetails follow `planning-mds/architecture/api-guidelines-profile.md`.

## DI Registration Changes

- Add `services.AddScoped<IDistributionScopeService, DistributionScopeService>();`
- Add `services.AddScoped<IDistributionScopeRepository, DistributionScopeRepository>();`
- If `DistributionRollupService` is separated from `OperationalReportService`, register `IDistributionRollupService`.

## Casbin Policy Sync

`planning-mds/security/policies/policy.csv` adds `distribution_rollup:read`. During implementation, copy policy changes to any embedded/runtime policy resource if the runtime loads a packaged copy.

## Knowledge-Graph Binding Plan

- New canonical nodes:
  - `capability:distribution-rollup-reporting`
  - `endpoint:distribution-rollup-report`
  - `schema:distribution-rollup-report`
  - `policy_rule:distribution-rollup-read`
- New planning schema:
  - `planning-mds/schemas/distribution-rollup-report.schema.json`
- Code-index future implementation bindings must be added during feature implementation after files exist.
