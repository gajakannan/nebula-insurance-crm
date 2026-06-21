# Feature Assembly Plan - F0023: Global Search, Saved Views & Operational Reporting

**Created:** 2026-06-19
**Author:** Architect Agent
**Status:** Approved for feature action (Phase B plan run `2026-06-19-2f180001`)

## Overview

F0023 adds a read-side SearchReporting module to the modular monolith. It creates permission-filtered global search, personal/team saved views, and operational workload/aging reports without introducing external search infrastructure. The module owns read-model/projection records and saved-view persistence; source modules remain authoritative for record details and authorization semantics.

## Build Order

| Step | Scope | Stories | Rationale |
|------|-------|---------|-----------|
| 1 | Data model + migration + backfill command | S0001, S0005, S0006, S0007 | Search and reports need derived records before API/UI can be implemented. |
| 2 | Global search API + projection service | S0001, S0002, S0007 | Establish permission-filtered search rows, facets, snippets, and source navigation. |
| 3 | Saved-view persistence/API/audit | S0003, S0004, S0007 | Saved views are the only F0023 mutation surface and require concurrency/audit. |
| 4 | Operational report APIs | S0005, S0006, S0007 | Reports reuse projections and search-style drilldown rows. |
| 5 | Frontend search/report workspaces and drawer | S0001-S0006 | UI consumes stable APIs after backend contracts are available. |
| 6 | Security/QE hardening | S0007 | Cross-object visibility tests must close the feature. |

## Existing Code (Must Be Modified)

| File | Current State | F0023 Change |
|------|---------------|--------------|
| `engine/src/Nebula.Api/Endpoints/DashboardEndpoints.cs` | Dashboard read endpoints only. | No direct edit expected; use as per-endpoint read pattern reference. |
| `engine/src/Nebula.Api/Endpoints/TaskEndpoints.cs` | CRUD and `If-Match` parsing pattern. | Use same ProblemDetails and concurrency style for saved views. |
| `engine/src/Nebula.Application/Common/PaginatedResult.cs` | Shared pagination wrapper. | Reuse for search results and saved-view lists. |
| `engine/src/Nebula.Infrastructure/Authorization/policy.csv` | Embedded runtime Casbin policy. | Copy F0023 policy rows from `planning-mds/security/policies/policy.csv`. |
| `experience/src/components/layout/TopBar.tsx` | Authenticated shell controls. | Add global search trigger and keyboard-accessible overlay entry. |
| `experience/src/App.tsx` | Route registry. | Add search/report workspace routes. |
| `experience/src/services/api.ts` | Existing API client methods. | Add search, saved-view, and operational-report client methods. |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `engine/src/Nebula.Domain/Entities/SearchDocument.cs` | Domain | Search read-model row. |
| `engine/src/Nebula.Domain/Entities/SavedView.cs` | Domain | Saved criteria record. |
| `engine/src/Nebula.Domain/Entities/SavedViewAuditEvent.cs` | Domain | Immutable saved-view mutation audit. |
| `engine/src/Nebula.Domain/Entities/OperationalReportProjection.cs` | Domain | Report fact/projection row. |
| `engine/src/Nebula.Application/DTOs/SearchDtos.cs` | Application | Search query/result DTOs. |
| `engine/src/Nebula.Application/DTOs/SavedViewDtos.cs` | Application | Saved-view request/response DTOs. |
| `engine/src/Nebula.Application/DTOs/OperationalReportDtos.cs` | Application | Workload and aging report DTOs. |
| `engine/src/Nebula.Application/Services/SearchService.cs` | Application | Permission-filtered search orchestration. |
| `engine/src/Nebula.Application/Services/SavedViewService.cs` | Application | Saved-view mutation and audit workflow. |
| `engine/src/Nebula.Application/Services/OperationalReportService.cs` | Application | Report aggregation and drilldown service. |
| `engine/src/Nebula.Application/Services/SearchProjectionService.cs` | Application | Projection refresh/backfill coordination. |
| `engine/src/Nebula.Application/Interfaces/ISearchDocumentRepository.cs` | Application | Search persistence abstraction. |
| `engine/src/Nebula.Application/Interfaces/ISavedViewRepository.cs` | Application | Saved-view persistence abstraction. |
| `engine/src/Nebula.Application/Interfaces/IOperationalReportProjectionRepository.cs` | Application | Report projection abstraction. |
| `engine/src/Nebula.Application/Validators/GlobalSearchQueryValidator.cs` | Application | Search query validation. |
| `engine/src/Nebula.Application/Validators/SavedViewUpsertRequestValidator.cs` | Application | Saved-view validation. |
| `engine/src/Nebula.Application/Validators/OperationalReportQueryValidator.cs` | Application | Report query validation. |
| `engine/src/Nebula.Infrastructure/Repositories/SearchDocumentRepository.cs` | Infrastructure | EF search query implementation. |
| `engine/src/Nebula.Infrastructure/Repositories/SavedViewRepository.cs` | Infrastructure | EF saved-view repository. |
| `engine/src/Nebula.Infrastructure/Repositories/OperationalReportProjectionRepository.cs` | Infrastructure | EF report projection queries. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/SearchDocumentConfiguration.cs` | Infrastructure | EF table/index config. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/SavedViewConfiguration.cs` | Infrastructure | EF table/index config. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/SavedViewAuditEventConfiguration.cs` | Infrastructure | EF audit config. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/OperationalReportProjectionConfiguration.cs` | Infrastructure | EF table/index config. |
| `engine/src/Nebula.Api/Endpoints/SearchEndpoints.cs` | API | `GET /search-results`. |
| `engine/src/Nebula.Api/Endpoints/SavedViewEndpoints.cs` | API | `/saved-views` CRUD/default endpoints. |
| `engine/src/Nebula.Api/Endpoints/OperationalReportEndpoints.cs` | API | `/operational-reports/*` endpoints. |
| `experience/src/pages/SearchResultsPage.tsx` | Frontend | Search results workspace. |
| `experience/src/pages/OperationalReportsPage.tsx` | Frontend | Workload and aging reports workspace. |
| `experience/src/features/search/**` | Frontend | Search overlay, filters, result list, saved-view drawer. |
| `experience/src/features/reports/**` | Frontend | Report filters, summary tables, drilldowns. |

## Domain Entity Contracts

```csharp
// engine/src/Nebula.Domain/Entities/SearchDocument.cs
public sealed class SearchDocument : BaseEntity
{
    public Guid Id { get; set; }
    public required string ObjectType { get; set; }
    public Guid ObjectId { get; set; }
    public required string TargetUrl { get; set; }
    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Status { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string? OwnerDisplayName { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? BrokerId { get; set; }
    public Guid? PolicyId { get; set; }
    public Guid? SubmissionId { get; set; }
    public Guid? RenewalId { get; set; }
    public Guid? TaskId { get; set; }
    public string? LineOfBusiness { get; set; }
    public string? Region { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? TerritoryId { get; set; }
    public required string SearchText { get; set; }
    public string MatchedFieldHintsJson { get; set; } = "[]";
    public DateTimeOffset SourceUpdatedAt { get; set; }
    public DateTimeOffset IndexedAt { get; set; }
    public string? LastProjectionError { get; set; }
}

// engine/src/Nebula.Domain/Entities/SavedView.cs
public sealed class SavedView : BaseEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public string? Description { get; set; }
    public required string ViewType { get; set; } // Search | WorkloadReport | WorkflowAgingReport
    public required string Visibility { get; set; } // Personal | Team
    public Guid OwnerUserId { get; set; }
    public string? TeamScopeType { get; set; } // Role | Region | Program | Territory
    public string? TeamScopeKey { get; set; }
    public required string CriteriaJson { get; set; }
    public string SortJson { get; set; } = "{}";
    public bool IsDefault { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public Guid? LastEditedByUserId { get; set; }
    public uint RowVersion { get; set; }
}

// engine/src/Nebula.Domain/Entities/SavedViewAuditEvent.cs
public sealed class SavedViewAuditEvent
{
    public Guid Id { get; set; }
    public Guid SavedViewId { get; set; }
    public required string EventType { get; set; } // Created | Updated | DefaultChanged | Archived
    public Guid ActorUserId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
}

// engine/src/Nebula.Domain/Entities/OperationalReportProjection.cs
public sealed class OperationalReportProjection : BaseEntity
{
    public Guid Id { get; set; }
    public required string SourceObjectType { get; set; } // Submission | Renewal | Policy | Task
    public Guid SourceObjectId { get; set; }
    public required string TargetUrl { get; set; }
    public string? WorkflowType { get; set; }
    public string? CurrentStatus { get; set; }
    public DateTimeOffset? StatusEnteredAt { get; set; }
    public int? DaysInStatus { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string? OwnerDisplayName { get; set; }
    public DateOnly? DueDate { get; set; }
    public bool IsDueToday { get; set; }
    public bool IsOverdue { get; set; }
    public string? AgeBand { get; set; } // OnTrack | ApproachingSla | Overdue
    public Guid? AccountId { get; set; }
    public Guid? BrokerId { get; set; }
    public Guid? PolicyId { get; set; }
    public string? LineOfBusiness { get; set; }
    public string? Region { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? TerritoryId { get; set; }
    public DateTimeOffset LastSourceUpdatedAt { get; set; }
    public DateTimeOffset ProjectedAt { get; set; }
}
```

## DTO Contracts

```csharp
public sealed record GlobalSearchQuery(
    string Q,
    IReadOnlyList<string> ObjectTypes,
    string? Status,
    Guid? OwnerUserId,
    string? Region,
    string? LineOfBusiness,
    string Sort,
    int Page,
    int PageSize);

public sealed record GlobalSearchResultDto(
    string ObjectType,
    Guid ObjectId,
    string Title,
    string? Subtitle,
    string? Status,
    Guid? OwnerUserId,
    string? OwnerDisplayName,
    string? LineOfBusiness,
    string? Region,
    IReadOnlyList<string> MatchedFields,
    string? Snippet,
    string TargetUrl,
    decimal Score,
    DateTimeOffset LastUpdatedAt,
    DateTimeOffset IndexedAt);

public sealed record GlobalSearchResponseDto(
    IReadOnlyList<GlobalSearchResultDto> Data,
    GlobalSearchFacetsDto Facets,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    string? QueryEcho,
    DateTimeOffset GeneratedAt);

public sealed record SavedViewUpsertRequestDto(
    string Name,
    string? Description,
    string ViewType,
    string Visibility,
    string? TeamScopeType,
    string? TeamScopeKey,
    JsonDocument Criteria,
    bool IsDefault);

public sealed record SavedViewDto(
    Guid Id,
    string Name,
    string? Description,
    string ViewType,
    string Visibility,
    string? TeamScopeType,
    string? TeamScopeKey,
    JsonDocument Criteria,
    Guid OwnerUserId,
    string? OwnerDisplayName,
    Guid? LastEditedByUserId,
    string? LastEditedByDisplayName,
    bool IsDefault,
    DateTimeOffset? ArchivedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string RowVersion);
```

## Service Signatures

```csharp
public interface ISearchService
{
    Task<GlobalSearchResponseDto> SearchAsync(GlobalSearchQuery query, ICurrentUserService user, CancellationToken ct);
}

public interface ISavedViewService
{
    Task<PaginatedResult<SavedViewDto>> ListAsync(SavedViewListQuery query, ICurrentUserService user, CancellationToken ct);
    Task<SavedViewDto?> GetAsync(Guid savedViewId, ICurrentUserService user, CancellationToken ct);
    Task<(SavedViewDto? Result, string? Error)> CreateAsync(SavedViewUpsertRequestDto request, ICurrentUserService user, CancellationToken ct);
    Task<(SavedViewDto? Result, string? Error)> UpdateAsync(Guid savedViewId, SavedViewUpsertRequestDto request, uint expectedRowVersion, ICurrentUserService user, CancellationToken ct);
    Task<string?> ArchiveAsync(Guid savedViewId, uint expectedRowVersion, ICurrentUserService user, CancellationToken ct);
    Task<(SavedViewDto? Result, string? Error)> SetDefaultAsync(Guid savedViewId, uint expectedRowVersion, ICurrentUserService user, CancellationToken ct);
}

public interface IOperationalReportService
{
    Task<OperationalWorkloadReportDto> GetWorkloadAsync(OperationalReportQuery query, ICurrentUserService user, CancellationToken ct);
    Task<WorkflowAgingReportDto> GetWorkflowAgingAsync(OperationalReportQuery query, ICurrentUserService user, CancellationToken ct);
}
```

## Step 1 - Data Model, Migration, and Projection Backfill (S0001, S0005, S0006, S0007)

### Logic Flow

1. Add entity classes and EF configurations.
2. Create migration `F0023_SearchSavedViewsOperationalReporting`.
3. Create indexes from ADR-014/data-model §10.
4. Add `SearchProjectionService.BackfillAsync(DateTimeOffset startedAt, CancellationToken ct)`.
5. Backfill from existing Broker/MGA/Program, Account, Policy, Submission, Renewal, and Task records.
6. Use retryable refresh units keyed by `(ObjectType, ObjectId)`.

### Casbin Enforcement

- Resource/action: `global_search:read`, `operational_report:read`
- Query layer must apply source visibility filters before count/facet/row materialization.

### Mutation Traceability

N/A - migration/backfill is not a user mutation. Operational audit is through command logs and projection metrics.

## Step 2 - Global Search API (S0001, S0002, S0007)

### Endpoint

`GET /search-results`

### Logic Flow

1. Validate `q` length >= 2 after trim, bounded filters, and pagination.
2. Enforce `global_search:read`.
3. Build source-visibility predicate for current user.
4. Query `SearchDocuments` using full-text search and filter indexes.
5. Compute facets/counts after source-visibility filtering.
6. Return `GlobalSearchResponseDto`.

### HTTP Responses

| Status | Body | Condition |
|--------|------|-----------|
| 200 | `GlobalSearchResponse` | Success |
| 400 | ProblemDetails (`search_query_too_short` or `validation_error`) | Invalid query/filter |
| 401 | ProblemDetails | Missing/invalid authentication |
| 403 | ProblemDetails (`policy_denied`) | External user or no global search permission |

## Step 3 - Saved Views API and Audit (S0003, S0004, S0007)

### Endpoints

- `GET /saved-views`
- `POST /saved-views`
- `GET /saved-views/{savedViewId}`
- `PATCH /saved-views/{savedViewId}`
- `DELETE /saved-views/{savedViewId}`
- `PUT /saved-views/{savedViewId}/default`

### Logic Flow

1. Validate name, view type, visibility, team scope, and criteria JSON.
2. Enforce `saved_view:read`, `saved_view:manage`, or `saved_view:default`.
3. For personal mutations, require `OwnerUserId == currentUser.UserId`.
4. For team mutations, require DistributionManager, ProgramManager, or Admin and validate administered `teamScopeType/teamScopeKey`.
5. For updates/default/archive, parse `If-Match` and compare `RowVersion`.
6. Persist `SavedView` mutation and append `SavedViewAuditEvent` in one transaction.
7. Return updated DTO or 204 archive response.

### Mutation Traceability

| Screen / Entry Point | User Action | Endpoint | Service Method | Entity / Carrier | Authorization | Concurrency | Validation Failure | Audit / Timeline | Test Expectation |
|----------------------|-------------|----------|----------------|------------------|---------------|-------------|--------------------|------------------|------------------|
| Search Results Workspace -> Save View | Save personal view | `POST /saved-views` | `SavedViewService.CreateAsync` | `SavedView.CriteriaJson` | `saved_view:manage` with owner match | N/A create | 400 `validation_error`, 409 `saved_view_duplicate_name` | `SavedViewAuditEvent.Created` | Integration + E2E reload shows view persists and applies criteria |
| Saved Views Drawer -> Rename/Update | Rename or update criteria | `PATCH /saved-views/{savedViewId}` | `SavedViewService.UpdateAsync` | `SavedView.Name`, `CriteriaJson`, `SortJson` | owner or team admin | `If-Match`, 412 `precondition_failed` | 400/422/409 as applicable | `SavedViewAuditEvent.Updated` | Integration + E2E prove persisted update after query invalidation |
| Saved Views Drawer -> Delete | Archive/delete | `DELETE /saved-views/{savedViewId}` | `SavedViewService.ArchiveAsync` | `SavedView.ArchivedAt` | owner or team admin | `If-Match`, 412 `precondition_failed` | 403 `saved_view_scope_denied`, 404 hidden/not found | `SavedViewAuditEvent.Archived` | View disappears after reload; audit row exists |
| Saved Views Drawer -> Publish to Team | Publish team view | `POST /saved-views` | `SavedViewService.CreateAsync` | `SavedView.TeamScopeType/Key` | manager/admin administered scope | N/A create | 400 `saved_view_scope_required`, 403 `saved_view_scope_denied`, 409 duplicate | `SavedViewAuditEvent.Created` | Eligible team user can see/apply after reload; non-eligible cannot infer it |
| Saved Views Drawer -> Team Default | Mark default | `PUT /saved-views/{savedViewId}/default` | `SavedViewService.SetDefaultAsync` | `SavedView.IsDefault` | manager/admin administered scope | `If-Match`, 412 `precondition_failed` | 403/409 as applicable | `SavedViewAuditEvent.DefaultChanged` | Prior default cleared and new default loads for eligible team |

### HTTP Responses

| Status | Body | Condition |
|--------|------|-----------|
| 200 | `SavedView` | Detail/update/default success |
| 201 | `SavedView` | Create success |
| 204 | Empty | Archive success |
| 400 | ProblemDetails (`validation_error`, `saved_view_scope_required`) | Invalid request |
| 403 | ProblemDetails (`policy_denied`, `saved_view_scope_denied`) | Unauthorized owner/team scope |
| 404 | ProblemDetails (`not_found`) | Missing or hidden saved view |
| 409 | ProblemDetails (`saved_view_duplicate_name`) | Duplicate active saved view |
| 412 | ProblemDetails (`precondition_failed`) | Stale `If-Match` |
| 422 | ProblemDetails (`saved_view_criteria_invalid`) | Unsupported criteria |

## Step 4 - Operational Reports API (S0005, S0006, S0007)

### Endpoints

- `GET /operational-reports/workload`
- `GET /operational-reports/workflow-aging`

### Logic Flow

1. Validate bounded filters and `asOf`.
2. Enforce `operational_report:read`.
3. Apply source visibility filters to `OperationalReportProjection`.
4. Aggregate due-today, overdue, owner, status, aging, and backlog rows.
5. Shape drilldowns as `GlobalSearchResultDto`.
6. Return report with `generatedAt`.

### HTTP Responses

| Status | Body | Condition |
|--------|------|-----------|
| 200 | `OperationalWorkloadReport` or `WorkflowAgingReport` | Success |
| 400 | ProblemDetails (`validation_error`) | Invalid filters |
| 401 | ProblemDetails | Missing/invalid authentication |
| 403 | ProblemDetails (`policy_denied`) | External user or no report permission |

## Step 5 - Frontend Workspaces (S0001-S0006)

### Required Routes and Components

| File | Purpose |
|------|---------|
| `experience/src/features/search/SearchOverlay.tsx` | Global shell search input and recent query launcher. |
| `experience/src/features/search/SearchResultsList.tsx` | Grouped/paginated result rows and source navigation. |
| `experience/src/features/search/SearchFilters.tsx` | Object type, owner, status, region, and LOB filters. |
| `experience/src/features/search/SavedViewsDrawer.tsx` | Personal/team saved-view apply/manage/default UI. |
| `experience/src/pages/SearchResultsPage.tsx` | Deep-linkable search workspace. |
| `experience/src/pages/OperationalReportsPage.tsx` | Workload and aging reports workspace. |

### UI Rules

- Search overlay lives in the authenticated shell and navigates to `/search`.
- Saved view drawer is reusable on search and report workspaces.
- Delete/archive requires confirmation.
- Team mutation controls render only for manager/admin roles, but backend authorization is authoritative.
- No hidden-record counts or hidden-scope metadata appears in UI copy.

## Scope Breakdown

| Layer | Required Work | Owner | Status |
|------|----------------|-------|--------|
| Backend (`engine/`) | Entities, migration, repositories, services, endpoints, validators, policy copy | Backend Developer | Planned |
| Frontend (`experience/`) | Search overlay, workspace, filters, drawer, report workspace, API client | Frontend Developer | Planned |
| AI (`neuron/`) | Not in scope | N/A | N/A |
| Quality | Unit, integration, E2E, permission matrix tests, projection backfill checks | QE | Required |
| Security | Cross-object authorization, metadata leakage, saved-view scope review | Security Reviewer | Required |
| DevOps/Runtime | Backfill command/projection lag observability; no new external service | DevOps | Required |

## Integration Checkpoints

### After Backend Search

- [ ] `GET /search-results` returns only authorized rows.
- [ ] Facet and total counts equal authorized rows only.
- [ ] External roles receive 403.

### After Saved Views

- [ ] Owner-only personal mutations pass and cross-owner mutations fail.
- [ ] Manager/admin team mutations validate administered team scope.
- [ ] Duplicate names and stale row versions return deterministic ProblemDetails.
- [ ] Every successful mutation writes `SavedViewAuditEvent`.

### After Reports

- [ ] Workload and aging report counts are computed after authorization filtering.
- [ ] Drilldown rows navigate to authorized source records only.
- [ ] No hidden-source count is returned.

### Cross-Story Verification

- [ ] Full flow: search -> filter -> save personal view -> reload -> apply -> open source record.
- [ ] Full flow: manager publishes team workload default -> eligible user sees default -> ineligible user cannot infer it.
- [ ] All F0023 Casbin resources enforced.
- [ ] Projection backfill and refresh lag logged/metriced.
- [ ] ProblemDetails format consistent with existing endpoints.

## JSON Serialization Convention

All request/response fields are camelCase. Saved-view `criteria` is structured JSON and must be validated server-side; do not treat it as opaque display text. `rowVersion` is serialized as a string in saved-view responses and supplied through `If-Match` for updates/default/archive.

## DI Registration Changes

Register:

- `ISearchService -> SearchService`
- `ISavedViewService -> SavedViewService`
- `IOperationalReportService -> OperationalReportService`
- `ISearchProjectionService -> SearchProjectionService`
- `ISearchDocumentRepository -> SearchDocumentRepository`
- `ISavedViewRepository -> SavedViewRepository`
- `IOperationalReportProjectionRepository -> OperationalReportProjectionRepository`

Register endpoint maps:

- `SearchEndpoints.Map(app)`
- `SavedViewEndpoints.Map(app)`
- `OperationalReportEndpoints.Map(app)`

## Casbin Policy Sync

Copy the F0023 rows from `planning-mds/security/policies/policy.csv` into `engine/src/Nebula.Infrastructure/Authorization/policy.csv` during implementation. The architecture plan updates only planning/security policy artifacts; runtime embedded policy sync belongs to the feature implementation.

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Cross-object search leaks hidden record existence through counts/facets | Critical | Compute all counts/facets after source authorization filters; add role matrix tests. | Backend + Security |
| Projection lag confuses operational reports | Medium | Expose `generatedAt`/`indexedAt`, metric projection lag, and keep source detail authoritative. | Backend + DevOps |
| Team saved-view scope model drifts from auth context | High | Use `teamScopeType/teamScopeKey` only from current authorization context; reject unsupported scopes. | Backend + Security |
| External search engine scope creep | Medium | F0023 MVP uses PostgreSQL full-text search only. | Architect |

## Knowledge-Graph Binding Plan

> Predicted semantic-graph delta for the `G7` architect reconciliation to diff the as-built source against. The F0023 capability/entity/endpoint nodes already exist in `canonical-nodes.yaml` / `code-index.yaml` (added in Phase B plan run `2026-06-19-2f180001`); their `sources` currently point to planning docs only. No **new** canonical nodes are expected — this feature implements existing planned nodes. `G7` is expected to **add code-path globs** to the existing node bindings.

**Expected `code-index.yaml` glob/file bindings to add at G7 (code paths, stable across the closeout archive move):**

| Node | Anticipated code binding |
|------|--------------------------|
| `capability:global-search` | `engine/src/Nebula.Application/Services/SearchService.cs`, `engine/src/Nebula.Application/Services/SearchProjectionService.cs`, `engine/src/Nebula.Api/Endpoints/SearchEndpoints.cs`, `experience/src/features/search/**` |
| `capability:saved-views` | `engine/src/Nebula.Application/Services/SavedViewService.cs`, `engine/src/Nebula.Api/Endpoints/SavedViewEndpoints.cs`, `experience/src/features/search/**` (saved-view drawer) |
| `capability:operational-reporting` | `engine/src/Nebula.Application/Services/OperationalReportService.cs`, `engine/src/Nebula.Api/Endpoints/OperationalReportEndpoints.cs`, `experience/src/features/reports/**` |
| `entity:search-document` | `engine/src/Nebula.Domain/Entities/SearchDocument.cs`, `engine/src/Nebula.Infrastructure/Repositories/SearchDocumentRepository.cs`, `engine/src/Nebula.Infrastructure/Persistence/Configurations/SearchDocumentConfiguration.cs` |
| `entity:saved-view` | `engine/src/Nebula.Domain/Entities/SavedView.cs`, `engine/src/Nebula.Infrastructure/Repositories/SavedViewRepository.cs`, `engine/src/Nebula.Infrastructure/Persistence/Configurations/SavedViewConfiguration.cs` |
| `entity:saved-view-audit-event` | `engine/src/Nebula.Domain/Entities/SavedViewAuditEvent.cs`, `engine/src/Nebula.Infrastructure/Persistence/Configurations/SavedViewAuditEventConfiguration.cs` |
| `entity:operational-report-projection` | `engine/src/Nebula.Domain/Entities/OperationalReportProjection.cs`, `engine/src/Nebula.Infrastructure/Repositories/OperationalReportProjectionRepository.cs`, `engine/src/Nebula.Infrastructure/Persistence/Configurations/OperationalReportProjectionConfiguration.cs` |
| `endpoint:global-search-results` | `engine/src/Nebula.Api/Endpoints/SearchEndpoints.cs` |
| `endpoint:saved-view-*` | `engine/src/Nebula.Api/Endpoints/SavedViewEndpoints.cs` |
| `endpoint:operational-workload-report`, `endpoint:workflow-aging-report` | `engine/src/Nebula.Api/Endpoints/OperationalReportEndpoints.cs` |

**New canonical nodes expected:** None. F0023 reuses the existing Phase B planned nodes; `G7` affirms them and binds code.
