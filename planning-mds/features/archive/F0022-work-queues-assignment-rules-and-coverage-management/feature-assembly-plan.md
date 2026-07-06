# Feature Assembly Plan — F0022: Work Queues, Assignment Rules & Coverage Management

**Created:** 2026-07-03
**Author:** Architect Agent
**Status:** Draft

## Overview

F0022 adds a durable operations routing subsystem for named work queues, assignment rules, coverage windows, queue worklists, manual reassignment, rebalancing, and routing audit evidence. Implementation must follow accepted ADR-013 and preserve source-work ownership: tasks, submissions, and renewals keep their lifecycle rules, while `OperationsRoutingService` owns queue placement and calls source-specific assignment ports when reassignment changes the underlying owner.

## Build Order

| Step | Scope | Stories | Rationale |
|------|-------|---------|-----------|
| 1 | Backend domain, DTOs, repository contracts, EF configuration, migration, seed fallback queue | S0001, S0002, S0004, S0007 | Durable queue/rule/coverage/work-item storage must exist before services and UI can operate. |
| 2 | `OperationsRoutingService`, validators, source adapters, authorization checks, timeline/audit events | S0001-S0007 | Central deterministic routing and mutation behavior must be implemented once behind application boundaries. |
| 3 | Minimal API endpoints under `/work-queues`, `/assignment-rules`, `/coverage-windows`, `/queue-work-items`, `/routing-events` | S0001-S0007 | Frontend and integration tests consume the accepted WorkQueues OpenAPI contract. |
| 4 | Frontend work-queues feature slice and page route | S0001, S0002, S0004, S0005, S0006, S0007 | Managers need local controls before F0032 centralizes governance. |
| 5 | QE tests, security review, deployability evidence, KG reconciliation | S0001-S0007 | Closing requires integration evidence, security signoff, and as-built graph bindings. |

## Existing Code (Must Be Modified)

| File | Current State | F0022 Change |
|------|---------------|--------------|
| `engine/src/Nebula.Infrastructure/Persistence/AppDbContext.cs` | Existing `DbSet` and `OnModelCreating` registrations for task/submission/renewal/configuration entities | **Expand** — add `DbSet<WorkQueue>`, `WorkQueueMember`, `AssignmentRule`, `CoverageWindow`, `QueueWorkItem`, `RoutingDecisionLog`; apply configurations. |
| `engine/src/Nebula.Infrastructure/DependencyInjection.cs` | Registers repositories and application services; no operations routing services | **Expand** — register `IQueueAssignmentRepository`, source adapters, `IDistributionOwnershipResolver`, `OperationsRoutingService`. |
| `engine/src/Nebula.Api/Program.cs` | Registers existing services, validators, and endpoint groups | **Expand** — register operations routing service and map `app.MapWorkQueueEndpoints()`. |
| `engine/src/Nebula.Application/Services/TaskService.cs` | Owns task assignment, update, status, and timeline semantics | **Expand narrowly** — expose a source assignment port method or adapter target that F0022 can call without bypassing task authorization/timeline behavior. |
| `engine/src/Nebula.Application/Services/SubmissionService.cs` | Owns submission assignment and workflow transitions | **Expand narrowly** — expose source assignment port method or adapter target for routing assignment updates. |
| `engine/src/Nebula.Application/Services/RenewalService.cs` | Owns renewal assignment and workflow transitions | **Expand narrowly** — expose source assignment port method or adapter target for routing assignment updates. |
| `engine/src/Nebula.Application/Services/ProducerOwnershipService.cs` and F0017 repositories | Resolve producer ownership/territory context | **Read through adapter** — `DistributionOwnershipResolver` uses existing ownership/territory APIs rather than duplicating F0017 rules. |
| `engine/src/Nebula.Api/Helpers/ProblemDetailsHelper.cs` | Central ProblemDetails helpers and error codes | **Expand** — add helpers for F0022 error codes in `architecture/error-codes.md`. |
| `experience/src/services/api.ts` | Generic authenticated `api.get/post/put/delete` client | **Reuse** — no client rewrite; work-queues hooks call this client. |
| `experience/src/components/layout/Sidebar.tsx` | Primary navigation; no Work Queues route | **Expand** — add manager/admin-visible Work Queues nav item. |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `engine/src/Nebula.Domain/Entities/WorkQueue.cs` | Domain | Queue definition aggregate root. |
| `engine/src/Nebula.Domain/Entities/WorkQueueMember.cs` | Domain | Effective-dated queue membership. |
| `engine/src/Nebula.Domain/Entities/AssignmentRule.cs` | Domain | Versioned deterministic routing rule. |
| `engine/src/Nebula.Domain/Entities/CoverageWindow.cs` | Domain | Explicit out-of-office/backup coverage interval. |
| `engine/src/Nebula.Domain/Entities/QueueWorkItem.cs` | Domain | Active/historical queue placement for task/submission/renewal source items. |
| `engine/src/Nebula.Domain/Entities/RoutingDecisionLog.cs` | Domain | Append-only routing/reassignment/rebalance/fallback evidence. |
| `engine/src/Nebula.Application/DTOs/WorkQueueDtos.cs` | Application | Queue, rule, coverage, work-item, routing-event request/response records. |
| `engine/src/Nebula.Application/Interfaces/IQueueAssignmentRepository.cs` | Application | Queue/rule/coverage/work-item persistence boundary. |
| `engine/src/Nebula.Application/Interfaces/IRoutingSourceAdapters.cs` | Application | `ITaskRoutingSource`, `ISubmissionRoutingSource`, `IRenewalRoutingSource`, `IDistributionOwnershipResolver`. |
| `engine/src/Nebula.Application/Services/OperationsRoutingService.cs` | Application | Routing, queue management, reassignment, rebalance, and audit orchestration. |
| `engine/src/Nebula.Application/Validators/WorkQueueValidators.cs` | Application | FluentValidation for queue/rule/coverage/reassignment/rebalance requests. |
| `engine/src/Nebula.Infrastructure/Repositories/QueueAssignmentRepository.cs` | Infrastructure | EF Core implementation of queue repository. |
| `engine/src/Nebula.Infrastructure/Services/RoutingSourceAdapters.cs` | Infrastructure | Source summary and assignment adapter implementations. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/WorkQueueConfiguration.cs` | Infrastructure | Queue table, unique name/workType, fallback constraint. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/WorkQueueMemberConfiguration.cs` | Infrastructure | Membership table and active membership indexes. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/AssignmentRuleConfiguration.cs` | Infrastructure | Rule version and precedence indexes. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/CoverageWindowConfiguration.cs` | Infrastructure | Coverage interval indexes and relationships. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/QueueWorkItemConfiguration.cs` | Infrastructure | Idempotency and active queue item indexes. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/RoutingDecisionLogConfiguration.cs` | Infrastructure | Decision log query indexes. |
| `engine/src/Nebula.Infrastructure/Persistence/Migrations/{timestamp}_F0022_WorkQueuesRouting.cs` | Infrastructure | Database migration and seed data. |
| `engine/src/Nebula.Api/Endpoints/WorkQueueEndpoints.cs` | API | Minimal API endpoint group matching `planning-mds/api/nebula-api.yaml`. |
| `engine/tests/Nebula.Tests/Unit/OperationsRoutingServiceTests.cs` | Tests | Rule precedence, fallback, idempotency, coverage, reassignment. |
| `engine/tests/Nebula.Tests/Integration/WorkQueueEndpointTests.cs` | Tests | API contract, authz, persistence, and ProblemDetails behavior. |
| `experience/src/features/work-queues/types.ts` | Frontend | TypeScript DTOs matching OpenAPI schemas. |
| `experience/src/features/work-queues/hooks.ts` | Frontend | React Query hooks and mutation invalidation. |
| `experience/src/features/work-queues/components/QueueListPanel.tsx` | Frontend | Queue selector and summary counts. |
| `experience/src/features/work-queues/components/QueueWorklist.tsx` | Frontend | Queue work item list with aging and assignment state. |
| `experience/src/features/work-queues/components/QueueDetailPanel.tsx` | Frontend | Members, rules, coverage, and audit tabs. |
| `experience/src/features/work-queues/components/QueueEditorModal.tsx` | Frontend | Queue create/update form. |
| `experience/src/features/work-queues/components/AssignmentRuleEditor.tsx` | Frontend | Rule create/update form. |
| `experience/src/features/work-queues/components/CoverageWindowEditor.tsx` | Frontend | Coverage create/update form. |
| `experience/src/features/work-queues/components/ReassignWorkItemModal.tsx` | Frontend | Manual reassignment workflow. |
| `experience/src/pages/WorkQueuesPage.tsx` | Frontend | Manager/admin queue operations workspace. |
| `experience/src/pages/tests/WorkQueuesPage.integration.test.tsx` | Tests | Frontend page and mutation behavior. |

## Step 1 — Durable Queue Model and Persistence (S0001, S0002, S0004, S0007)

### Entity / DTO / Code

```csharp
// engine/src/Nebula.Domain/Entities/WorkQueue.cs
namespace Nebula.Domain.Entities;

public class WorkQueue : BaseEntity
{
    public string Name { get; set; } = default!;
    public string WorkType { get; set; } = default!; // Task | Submission | Renewal | Mixed
    public string Status { get; set; } = "Active"; // Active | Inactive
    public bool IsFallback { get; set; }
    public string? Description { get; set; }
    public List<WorkQueueMember> Members { get; set; } = [];
    public List<AssignmentRule> AssignmentRules { get; set; } = [];
    public List<CoverageWindow> CoverageWindows { get; set; } = [];
}

public class WorkQueueMember : BaseEntity
{
    public Guid WorkQueueId { get; set; }
    public WorkQueue WorkQueue { get; set; } = default!;
    public Guid UserProfileId { get; set; }
    public UserProfile UserProfile { get; set; } = default!;
    public string Role { get; set; } = "Member"; // Manager | Member | Backup
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class AssignmentRule : BaseEntity
{
    public Guid WorkQueueId { get; set; }
    public WorkQueue WorkQueue { get; set; } = default!;
    public string RuleType { get; set; } = default!; // ManualOverride | Coverage | TerritoryOwnership | WorkloadBalance | Fallback
    public int Precedence { get; set; }
    public int Version { get; set; }
    public string Status { get; set; } = "Draft"; // Draft | Active | Superseded | Inactive
    public string ConditionsJson { get; set; } = "{}";
    public string OutcomeJson { get; set; } = "{}";
    public DateTime? ActivatedAt { get; set; }
    public Guid? ActivatedByUserId { get; set; }
}

public class CoverageWindow : BaseEntity
{
    public Guid CoveredUserId { get; set; }
    public UserProfile CoveredUser { get; set; } = default!;
    public Guid BackupUserId { get; set; }
    public UserProfile BackupUser { get; set; } = default!;
    public Guid? WorkQueueId { get; set; }
    public WorkQueue? WorkQueue { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Status { get; set; } = "Scheduled"; // Scheduled | Active | Expired | Cancelled
    public string? Reason { get; set; }
}

public class QueueWorkItem : BaseEntity
{
    public Guid WorkQueueId { get; set; }
    public WorkQueue WorkQueue { get; set; } = default!;
    public string SourceType { get; set; } = default!; // Task | Submission | Renewal
    public Guid SourceId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public UserProfile? AssignedToUser { get; set; }
    public string QueueStatus { get; set; } = "Open"; // Open | Assigned | InProgress | Closed
    public DateTime RoutedAt { get; set; }
    public string? RuleVersion { get; set; }
    public string? MatchReason { get; set; }
    public string IdempotencyKey { get; set; } = default!;
}

public class RoutingDecisionLog : BaseEntity
{
    public Guid? QueueWorkItemId { get; set; }
    public QueueWorkItem? QueueWorkItem { get; set; }
    public string SourceType { get; set; } = default!;
    public Guid SourceId { get; set; }
    public string Outcome { get; set; } = default!; // Routed | Fallback | Reassigned | Rebalanced | Skipped
    public string ReasonCode { get; set; } = default!;
    public Guid? ActorUserId { get; set; }
    public DateTime OccurredAt { get; set; }
    public string DecisionPayloadJson { get; set; } = "{}";
}
```

```csharp
// engine/src/Nebula.Application/DTOs/WorkQueueDtos.cs
namespace Nebula.Application.DTOs;

public sealed record WorkQueueDto(
    Guid Id, string Name, string WorkType, string Status, bool IsFallback,
    string? Description, int ActiveMemberCount, int OpenItemCount, uint RowVersion);

public sealed record WorkQueueUpsertRequestDto(
    string Name, string WorkType, string Status, string? Description,
    IReadOnlyList<QueueMemberUpsertRequestDto> Members);

public sealed record QueueMemberUpsertRequestDto(
    Guid UserProfileId, string Role, DateTime EffectiveFrom, DateTime? EffectiveTo);

public sealed record AssignmentRuleDto(
    Guid Id, Guid WorkQueueId, string RuleType, int Precedence, int Version,
    string Status, string ConditionsJson, string OutcomeJson, uint RowVersion);

public sealed record AssignmentRuleUpsertRequestDto(
    Guid WorkQueueId, string RuleType, int Precedence, string Status,
    string ConditionsJson, string OutcomeJson);

public sealed record CoverageWindowDto(
    Guid Id, Guid CoveredUserId, Guid BackupUserId, Guid? WorkQueueId,
    DateTime StartsAt, DateTime EndsAt, string Status, string? Reason, uint RowVersion);

public sealed record CoverageWindowUpsertRequestDto(
    Guid CoveredUserId, Guid BackupUserId, Guid? WorkQueueId,
    DateTime StartsAt, DateTime EndsAt, string Status, string? Reason);

public sealed record QueueWorkItemDto(
    Guid Id, Guid WorkQueueId, string SourceType, Guid SourceId,
    Guid? AssignedToUserId, string QueueStatus, DateTime RoutedAt,
    string? RuleVersion, string? MatchReason, uint RowVersion);

public sealed record QueueReassignmentRequestDto(
    Guid AssignedToUserId, string Reason);

public sealed record QueueRebalanceRequestDto(
    string Strategy, int? MaxItems, string Reason);

public sealed record RoutingEventDto(
    Guid Id, Guid? QueueWorkItemId, string SourceType, Guid SourceId,
    string Outcome, string ReasonCode, Guid? ActorUserId, DateTime OccurredAt,
    string DecisionPayloadJson);
```

### Logic Flow

`QueueAssignmentRepository` must provide exact methods:

1. `ListQueuesAsync(workType, status, ct)` returns queues with member and open-item counts.
2. `GetQueueForUpdateAsync(queueId, ct)` loads queue, members, rules, and coverage windows.
3. `QueueNameExistsAsync(name, workType, excludingId, ct)` enforces uniqueness.
4. `HasOpenItemsAsync(queueId, ct)` blocks deactivation while work remains open.
5. `GetActiveRulesAsync(sourceType, ct)` returns active rules ordered by `Precedence`.
6. `GetActiveCoverageAsync(userId, sourceType, now, ct)` returns explicit active windows only.
7. `GetActiveQueueItemAsync(sourceType, sourceId, ct)` and `GetByIdForUpdateAsync(queueItemId, ct)` support idempotency and reassignment.
8. `AddRoutingDecisionAsync(log, ct)` appends evidence; never update decision rows.

### Migration SQL

- Unique index: `WorkQueues(Name, WorkType)` where `IsDeleted = false`.
- Partial unique index: exactly one active fallback queue per `WorkType` where `IsFallback = true AND Status = 'Active' AND IsDeleted = false`.
- Unique index: `QueueWorkItems(SourceType, SourceId, IdempotencyKey)` where `IsDeleted = false`.
- Partial unique index: one active queue item per source where `QueueStatus IN ('Open','Assigned','InProgress') AND IsDeleted = false`.
- GIST/exclusion-style overlap protection is preferred for `CoverageWindows` by `(CoveredUserId, WorkQueueId, StartsAt, EndsAt)` if provider support is available; otherwise enforce overlap in repository/service and add btree indexes on covered user and interval endpoints.
- Seed `Unassigned Operations Queue` for `Task`, `Submission`, and `Renewal`.

## Step 2 — Operations Routing Application Service (S0001-S0007)

### Code Signatures

```csharp
// engine/src/Nebula.Application/Interfaces/IQueueAssignmentRepository.cs
public interface IQueueAssignmentRepository
{
    Task<(IReadOnlyList<WorkQueue> Queues, int TotalCount)> ListQueuesAsync(string? workType, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<WorkQueue?> GetQueueAsync(Guid queueId, CancellationToken ct = default);
    Task<WorkQueue?> GetQueueForUpdateAsync(Guid queueId, CancellationToken ct = default);
    Task<bool> QueueNameExistsAsync(string name, string workType, Guid? excludingQueueId, CancellationToken ct = default);
    Task<bool> HasOpenItemsAsync(Guid queueId, CancellationToken ct = default);
    Task AddQueueAsync(WorkQueue queue, CancellationToken ct = default);
    Task<IReadOnlyList<AssignmentRule>> ListActiveRulesAsync(string sourceType, CancellationToken ct = default);
    Task<AssignmentRule?> GetRuleForUpdateAsync(Guid ruleId, CancellationToken ct = default);
    Task AddRuleAsync(AssignmentRule rule, CancellationToken ct = default);
    Task<IReadOnlyList<CoverageWindow>> ListActiveCoverageAsync(Guid coveredUserId, string sourceType, DateTime now, CancellationToken ct = default);
    Task<CoverageWindow?> GetCoverageForUpdateAsync(Guid coverageWindowId, CancellationToken ct = default);
    Task AddCoverageWindowAsync(CoverageWindow window, CancellationToken ct = default);
    Task<QueueWorkItem?> GetActiveQueueItemAsync(string sourceType, Guid sourceId, CancellationToken ct = default);
    Task<QueueWorkItem?> GetQueueItemForUpdateAsync(Guid queueItemId, CancellationToken ct = default);
    Task AddQueueItemAsync(QueueWorkItem item, CancellationToken ct = default);
    Task<(IReadOnlyList<QueueWorkItem> Items, int TotalCount)> ListQueueItemsAsync(Guid queueId, string? status, int page, int pageSize, CancellationToken ct = default);
    Task AddRoutingDecisionAsync(RoutingDecisionLog decision, CancellationToken ct = default);
    Task<(IReadOnlyList<RoutingDecisionLog> Events, int TotalCount)> ListRoutingEventsAsync(string? sourceType, Guid? sourceId, Guid? queueItemId, int page, int pageSize, CancellationToken ct = default);
}

public interface IRoutingSource
{
    Task<RoutingSourceSummary?> GetSummaryAsync(Guid sourceId, CancellationToken ct = default);
    Task<(bool Success, string? ErrorCode)> AssignAsync(Guid sourceId, Guid assignedToUserId, string reason, ICurrentUserService user, CancellationToken ct = default);
}

public interface ITaskRoutingSource : IRoutingSource {}
public interface ISubmissionRoutingSource : IRoutingSource {}
public interface IRenewalRoutingSource : IRoutingSource {}

public sealed record RoutingSourceSummary(
    string SourceType, Guid SourceId, Guid? CurrentAssigneeId, Guid? BrokerId,
    Guid? AccountId, Guid? ProgramId, string? Region, string? LineOfBusiness,
    DateTime? DueOrEffectiveDate);

public interface IDistributionOwnershipResolver
{
    Task<Guid?> ResolveOwnerAsync(RoutingSourceSummary source, CancellationToken ct = default);
}
```

```csharp
// engine/src/Nebula.Application/Services/OperationsRoutingService.cs
public sealed class OperationsRoutingService(
    IQueueAssignmentRepository queueRepo,
    ITaskRoutingSource taskSource,
    ISubmissionRoutingSource submissionSource,
    IRenewalRoutingSource renewalSource,
    IDistributionOwnershipResolver ownershipResolver,
    ITimelineRepository timelineRepo,
    IAuthorizationService authz,
    IUnitOfWork unitOfWork,
    ILogger<OperationsRoutingService> logger)
{
    public Task<(IReadOnlyList<WorkQueueDto> Items, int TotalCount)> ListQueuesAsync(string? workType, string? status, int page, int pageSize, ICurrentUserService user, CancellationToken ct = default);
    public Task<(WorkQueueDto? Dto, string? ErrorCode)> UpsertQueueAsync(Guid? queueId, WorkQueueUpsertRequestDto dto, uint? rowVersion, ICurrentUserService user, CancellationToken ct = default);
    public Task<(AssignmentRuleDto? Dto, string? ErrorCode)> UpsertRuleAsync(Guid? ruleId, AssignmentRuleUpsertRequestDto dto, uint? rowVersion, ICurrentUserService user, CancellationToken ct = default);
    public Task<(CoverageWindowDto? Dto, string? ErrorCode)> UpsertCoverageWindowAsync(Guid? coverageWindowId, CoverageWindowUpsertRequestDto dto, uint? rowVersion, ICurrentUserService user, CancellationToken ct = default);
    public Task<(QueueWorkItemDto? Dto, string? ErrorCode)> RouteAsync(string sourceType, Guid sourceId, ICurrentUserService user, CancellationToken ct = default);
    public Task<(IReadOnlyList<QueueWorkItemDto> Items, int TotalCount)> ListQueueItemsAsync(Guid queueId, string? status, int page, int pageSize, ICurrentUserService user, CancellationToken ct = default);
    public Task<(QueueWorkItemDto? Dto, string? ErrorCode)> ReassignAsync(Guid queueItemId, QueueReassignmentRequestDto dto, uint rowVersion, ICurrentUserService user, CancellationToken ct = default);
    public Task<(IReadOnlyList<QueueWorkItemDto>? Items, string? ErrorCode)> RebalanceAsync(Guid queueId, QueueRebalanceRequestDto dto, ICurrentUserService user, CancellationToken ct = default);
    public Task<(IReadOnlyList<RoutingEventDto> Items, int TotalCount)> ListRoutingEventsAsync(string? sourceType, Guid? sourceId, Guid? queueItemId, int page, int pageSize, ICurrentUserService user, CancellationToken ct = default);
}
```

### Routing Logic Flow

`RouteAsync(sourceType, sourceId)`:

1. Authorize caller for `queue:assign` or internal routing actor role; external users must never route work.
2. Resolve source summary through `ITaskRoutingSource`, `ISubmissionRoutingSource`, or `IRenewalRoutingSource`; return `not_found` when absent or unauthorized.
3. Build idempotency key as `{SourceType}:{SourceId}:{ActiveRuleSetVersionHash}`.
4. If an active `QueueWorkItem` already exists for the same source and idempotency key, append `RoutingDecisionLog` with `Outcome=Skipped`, `ReasonCode=duplicate_route_event`, return existing item.
5. Evaluate active rules in ADR-013 order: manual override, coverage, territory/ownership, workload balancing, fallback.
6. Coverage only matches explicit active `CoverageWindow` rows where `StartsAt <= now < EndsAt`.
7. Territory/ownership delegates to `IDistributionOwnershipResolver`; no copied F0017 rule logic.
8. If no rule matches, choose the active `Unassigned Operations Queue` for the source work type and set `ReasonCode=no_rule_match_fallback`.
9. Create/update one active `QueueWorkItem` and append `RoutingDecisionLog`.
10. Emit `ActivityTimelineEvent` on the source entity and queue item with `InternalOnly` broker visibility.
11. Commit once through `IUnitOfWork`.

### Mutation Traceability

| Screen / Entry Point | User Action | Endpoint | Service Method | Entity / Carrier | Authorization | Concurrency | Validation Failure | Audit / Timeline | Test Expectation |
|----------------------|-------------|----------|----------------|------------------|---------------|-------------|--------------------|------------------|------------------|
| Work Queues -> New Queue | Create queue + members | `POST /work-queues` | `UpsertQueueAsync(null, dto, null)` | `WorkQueue`, `WorkQueueMember` | `queue:manage` | N/A create | `400 queue_name_required`, `409 assignment_rule_conflict` for active-without-members | `WorkQueueCreated`, `QueueMembershipChanged` | Integration test reloads list and detail after mutation. |
| Work Queues -> Queue Detail | Edit queue | `PUT /work-queues/{queueId}` | `UpsertQueueAsync(queueId, dto, rowVersion)` | `WorkQueue`, `WorkQueueMember` | `queue:manage` | `If-Match` required; stale -> `409 concurrency_conflict` | `409 queue_inactive` when deactivation has open work | `WorkQueueUpdated`, `QueueMembershipChanged` | Update test proves rowVersion conflict and persisted member change. |
| Rules Tab | Create/update rule | `POST /assignment-rules`, `PUT /assignment-rules/{ruleId}` | `UpsertRuleAsync` | `AssignmentRule` | `queue:manage` | `If-Match` on update | `400 assignment_rule_invalid`, `409 assignment_rule_conflict` | `AssignmentRuleVersioned` | Rule order and active/superseded behavior covered. |
| Coverage Tab | Create/update coverage | `POST /coverage-windows`, `PUT /coverage-windows/{coverageWindowId}` | `UpsertCoverageWindowAsync` | `CoverageWindow` | `queue:manage` | `If-Match` on update | `400 coverage_window_invalid`, `409 coverage_window_overlap` | `CoverageWindowScheduled/Updated` | Overlap and explicit-window-only routing covered. |
| Source lifecycle hooks | Route source work | service call from task/submission/renewal flow | `RouteAsync` | `QueueWorkItem`, `RoutingDecisionLog` | internal or `queue:assign` | idempotency key | `routing_no_match` only if fallback queue missing | `WorkRouted`, `RoutingFallbackApplied` | Duplicate route event returns existing queue item. |
| Queue Worklist | Reassign item | `PUT /queue-work-items/{queueItemId}/assignment` | `ReassignAsync` | `QueueWorkItem` + source assignment port | `queue:assign` | `If-Match` required | `409 queue_item_closed`, `400 invalid_assignee` | `QueueWorkItemReassigned` | Source assignment and queue item assignment match after reload. |
| Queue Detail | Rebalance | `POST /work-queues/{queueId}/rebalance` | `RebalanceAsync` | `QueueWorkItem`, `RoutingDecisionLog` | `queue:assign` | service-level per-item rowVersion check | `400 assignment_rule_invalid` | `QueueRebalanced` | Test verifies max items and decision logs. |

## Step 3 — API Endpoints (S0001-S0007)

### Endpoint Registration

```csharp
// engine/src/Nebula.Api/Endpoints/WorkQueueEndpoints.cs
public static class WorkQueueEndpoints
{
    public static IEndpointRouteBuilder MapWorkQueueEndpoints(this IEndpointRouteBuilder app)
    {
        var queues = app.MapGroup("/work-queues")
            .WithTags("WorkQueues")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        queues.MapGet("/", ListQueues);
        queues.MapPost("/", CreateQueue);
        queues.MapGet("/{queueId:guid}", GetQueue);
        queues.MapPut("/{queueId:guid}", UpdateQueue);
        queues.MapPut("/{queueId:guid}/members", UpdateMembers);
        queues.MapGet("/{queueId:guid}/items", ListQueueItems);
        queues.MapPost("/{queueId:guid}/rebalance", RebalanceQueue);

        var rules = app.MapGroup("/assignment-rules").WithTags("WorkQueues").RequireAuthorization().RequireRateLimiting("authenticated");
        rules.MapGet("/", ListRules);
        rules.MapPost("/", CreateRule);
        rules.MapPut("/{ruleId:guid}", UpdateRule);

        var coverage = app.MapGroup("/coverage-windows").WithTags("WorkQueues").RequireAuthorization().RequireRateLimiting("authenticated");
        coverage.MapGet("/", ListCoverage);
        coverage.MapPost("/", CreateCoverage);
        coverage.MapPut("/{coverageWindowId:guid}", UpdateCoverage);

        app.MapPut("/queue-work-items/{queueItemId:guid}/assignment", ReassignQueueItem)
            .WithTags("WorkQueues").RequireAuthorization().RequireRateLimiting("authenticated");

        app.MapGet("/routing-events", ListRoutingEvents)
            .WithTags("WorkQueues").RequireAuthorization().RequireRateLimiting("authenticated");

        return app;
    }
}
```

### HTTP Responses

| Endpoint | Success | Required Errors |
|----------|---------|-----------------|
| `GET /work-queues` | `200 WorkQueue[]` | `400 validation_error`, `403 policy_denied` |
| `POST /work-queues` | `201 WorkQueue` | `400 validation_error`, `403 policy_denied`, `409 assignment_rule_conflict` |
| `PUT /work-queues/{queueId}` | `200 WorkQueue` | `400 validation_error`, `403 policy_denied`, `404 queue_not_found`, `409 concurrency_conflict`, `409 queue_inactive` |
| `PUT /work-queues/{queueId}/members` | `200 WorkQueue` | `400 validation_error`, `403 policy_denied`, `404 queue_not_found`, `409 concurrency_conflict` |
| `GET /work-queues/{queueId}/items` | `200 QueueWorkItem[]` | `400 validation_error`, `403 policy_denied`, `404 queue_not_found` |
| `POST /assignment-rules` | `201 AssignmentRule` | `400 assignment_rule_invalid`, `403 policy_denied`, `404 queue_not_found`, `409 assignment_rule_conflict` |
| `PUT /assignment-rules/{ruleId}` | `200 AssignmentRule` | `400 assignment_rule_invalid`, `403 policy_denied`, `404 not_found`, `409 concurrency_conflict` |
| `POST /coverage-windows` | `201 CoverageWindow` | `400 coverage_window_invalid`, `403 policy_denied`, `409 coverage_window_overlap` |
| `PUT /coverage-windows/{coverageWindowId}` | `200 CoverageWindow` | `400 coverage_window_invalid`, `403 policy_denied`, `404 not_found`, `409 coverage_window_overlap` |
| `PUT /queue-work-items/{queueItemId}/assignment` | `200 QueueWorkItem` | `400 invalid_assignee`, `403 policy_denied`, `404 not_found`, `409 queue_item_closed`, `409 concurrency_conflict` |
| `POST /work-queues/{queueId}/rebalance` | `201 QueueWorkItem[]` | `400 validation_error`, `403 policy_denied`, `404 queue_not_found` |
| `GET /routing-events` | `200 RoutingEvent[]` | `400 validation_error`, `403 policy_denied` |

### Casbin Enforcement

- Read surfaces: resource `queue`, action `read`; hydrate `subjectId`, `queueId`, `sourceType`, `sourceId`, `workType`, `region`, `programId`, `brokerId` where available.
- Manage surfaces: resource `queue`, action `manage`; only `DistributionManager` and `Admin` pass by policy; `ProgramManager` remains read-only in `policy.csv`.
- Assignment/rebalance surfaces: resource `queue`, action `assign`; only `DistributionManager` and `Admin` pass by policy.
- Source ABAC intersection: after queue authorization, route/reassign must still respect source-record visibility through source adapter read/assign methods.

## Step 4 — Frontend Queue Operations Workspace (S0001, S0002, S0004, S0005, S0006, S0007)

### Frontend Contracts

```ts
// experience/src/features/work-queues/types.ts
export type WorkType = 'Task' | 'Submission' | 'Renewal' | 'Mixed'
export type QueueStatus = 'Active' | 'Inactive'
export type QueueItemStatus = 'Open' | 'Assigned' | 'InProgress' | 'Closed'

export interface WorkQueueDto {
  id: string
  name: string
  workType: WorkType
  status: QueueStatus
  isFallback: boolean
  description: string | null
  activeMemberCount: number
  openItemCount: number
  rowVersion: number
}

export interface QueueWorkItemDto {
  id: string
  workQueueId: string
  sourceType: 'Task' | 'Submission' | 'Renewal'
  sourceId: string
  assignedToUserId: string | null
  queueStatus: QueueItemStatus
  routedAt: string
  ruleVersion: string | null
  matchReason: string | null
  rowVersion: number
}
```

### UI Flow

1. `WorkQueuesPage` uses `DashboardLayout title="Work Queues"` and a dense operations layout: queue selector, queue worklist, detail tabs.
2. Navigation appears only for roles that can read queues: DistributionUser, DistributionManager, ProgramManager, Underwriter, RelationshipManager, Admin.
3. Create/edit/reassign buttons render only for manager/admin roles; server-side policy remains authoritative.
4. Hooks:
   - `useWorkQueues(filters)` -> `GET /work-queues`
   - `useQueueItems(queueId, filters)` -> `GET /work-queues/{queueId}/items`
   - `useUpsertQueue()` -> invalidates `['work-queues']`
   - `useUpsertAssignmentRule()` -> invalidates `['work-queues', queueId]`
   - `useUpsertCoverageWindow()` -> invalidates queue detail and routing events
   - `useReassignQueueItem()` and `useRebalanceQueue()` -> invalidates queue items, source detail, and routing events
5. UI must use existing components (`Modal`, `Tabs`, `Badge`, `TextInput`, `Select`) and lucide icons. Do not add a marketing-style landing page.

### Frontend Mutation Traceability

| Screen / Entry Point | User Action | Endpoint | Client Hook | Validation / Error UX | Reload Evidence |
|----------------------|-------------|----------|-------------|------------------------|-----------------|
| Work Queues page | New Queue | `POST /work-queues` | `useUpsertQueue` | Inline field errors from ProblemDetails; toast/alert for conflict | Queue list invalidates and displays new queue. |
| Queue detail | Edit queue/members | `PUT /work-queues/{queueId}` or `/members` | `useUpsertQueue` | Stale rowVersion conflict prompts reload | Detail refetch shows rowVersion increment. |
| Rules tab | Save rule | `POST/PUT /assignment-rules` | `useUpsertAssignmentRule` | Rule conflict shown near precedence/status fields | Rules list refetch shows version/status. |
| Coverage tab | Save window | `POST/PUT /coverage-windows` | `useUpsertCoverageWindow` | Overlap error shown with date fields | Coverage tab refetch shows explicit window. |
| Worklist row | Reassign | `PUT /queue-work-items/{id}/assignment` | `useReassignQueueItem` | Closed/stale item errors keep modal open | Worklist and source detail refetch. |
| Queue detail | Rebalance | `POST /work-queues/{queueId}/rebalance` | `useRebalanceQueue` | Strategy/max item validation | Worklist and audit tab refetch. |

## Scope Breakdown

| Layer | Required Work | Owner | Status |
|------|----------------|-------|--------|
| Backend (`engine/`) | Entities, DTOs, validators, repository, service, adapters, endpoints, migration, seed data | Backend Developer | Planned |
| Frontend (`experience/`) | Work queues page, hooks, components, route/nav, tests | Frontend Developer | Planned |
| AI (`neuron/`) | None; advanced AI routing is out of scope | AI Engineer | Not required |
| Quality | Unit, integration, frontend, and contract test coverage | Quality Engineer | Planned |
| DevOps/Runtime | Runtime preflight, migration/deployability check, no hosted job expected | DevOps / Feature Orchestrator | Planned |

## Dependency Order

```text
Step 0 (Architect):   this assembly plan + G0 validation
Step 1 (Backend):     durable model, repository, migration, seed fallback queues
Step 2 (Backend):     operations routing service, source adapters, validators, endpoint group
  ---- Backend checkpoint: unit + integration tests cover routing, fallback, coverage, authz, idempotency ----
Step 3 (Frontend):    work-queues feature slice, route/nav, mutation flows
  ---- Frontend checkpoint: page integration tests cover list/detail/create/edit/reassign error states ----
Step 4 (QE):          evidence reports, coverage, deployability and security scan coordination
Step 5 (Reviews):     code review + security review, signoff, candidate evidence, KG reconciliation, PM closeout
```

## Integration Checkpoints

### After Backend Model

- [ ] EF migration creates all six F0022 tables, indexes, row versions, relationships, and fallback seed queues.
- [ ] `QueueWorkItem` idempotency prevents duplicate active queue entries per `(SourceType, SourceId, RuleSetVersion)`.
- [ ] Active queue deactivation fails with open items.

### After Routing Service

- [ ] Rule precedence exactly matches ADR-013.
- [ ] Explicit coverage windows are the only coverage trigger.
- [ ] Fallback queue receives no-match work.
- [ ] Reassignment updates both `QueueWorkItem.AssignedToUserId` and source assignment via source port.
- [ ] Every mutation appends `RoutingDecisionLog` and source timeline evidence.

### After API + Frontend

- [ ] OpenAPI WorkQueues paths are implemented with matching status codes.
- [ ] Manager/admin can create queues, rules, coverage windows, reassign, and rebalance.
- [ ] Read-only roles can view allowed queues but cannot mutate.
- [ ] External users and broker users cannot access queue operations.
- [ ] Work Queues page is responsive and does not overlap text at mobile widths.

### Cross-Story Verification

- [ ] S0001 queue/member management persists and emits audit.
- [ ] S0002 rule creation and precedence affect routing outcomes.
- [ ] S0003 tasks, submissions, and renewals route to queues.
- [ ] S0004 backup coverage routes eligible work only inside explicit windows.
- [ ] S0005 worklist aging and no-match visibility are visible.
- [ ] S0006 reassignment and rebalance persist and audit.
- [ ] S0007 permissions and routing events prove access-safe traceability.

## Integration Checklist

- [ ] API contract compatibility validated.
- [ ] Frontend contract compatibility validated.
- [ ] AI contract compatibility marked not in scope.
- [ ] Test cases mapped to all seven stories.
- [ ] Developer-owned fast-test responsibilities identified by layer.
- [ ] Runtime evidence artifacts identified under run `2026-07-03-b9f40b31`.
- [ ] Framework vs solution boundary reviewed; no `agents/**` changes are needed.
- [ ] Mutation traceability tables completed.
- [ ] Render-only tests are not used to close mutation stories.
- [ ] Run/deploy instructions updated in `GETTING-STARTED.md`.

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Source assignment ports can bypass existing authorization if implemented too directly | High | Adapters must call source service methods or preserve their validation/timeline semantics. | Backend Developer |
| Queue worklist may expose source records beyond source ABAC | High | Queue read checks must intersect with source visibility before returning items. | Backend Developer / Security Reviewer |
| Coverage overlap enforcement may require provider-specific SQL | Medium | Prefer service-level overlap guard plus btree indexes if PostgreSQL exclusion constraint is too invasive. | Backend Developer |
| Frontend scope can sprawl into F0032 admin governance | Medium | Keep local manager controls only; do not add publish/rollback governance. | Frontend Developer |

## JSON Serialization Convention

- Backend DTOs use C# PascalCase records; ASP.NET JSON policy emits camelCase to frontend.
- `ConditionsJson`, `OutcomeJson`, and `DecisionPayloadJson` store JSON strings in the database but API responses may expose parsed JSON only if endpoint handlers explicitly map it. Keep the initial API shape aligned to `planning-mds/schemas/*.schema.json`.
- Date/time fields are UTC ISO 8601 strings in API responses. UI should not assume local timezone for routing decisions.

## DI Registration Changes

- `services.AddScoped<IQueueAssignmentRepository, QueueAssignmentRepository>();`
- `services.AddScoped<ITaskRoutingSource, TaskRoutingSource>();`
- `services.AddScoped<ISubmissionRoutingSource, SubmissionRoutingSource>();`
- `services.AddScoped<IRenewalRoutingSource, RenewalRoutingSource>();`
- `services.AddScoped<IDistributionOwnershipResolver, DistributionOwnershipResolver>();`
- `services.AddScoped<OperationsRoutingService>();`
- `app.MapWorkQueueEndpoints();`

## Casbin Policy Sync

`planning-mds/security/policies/policy.csv` already contains queue policy rows. Implementation must ensure the runtime policy source used by `CasbinAuthorizationService` contains the same `queue, read/manage/assign` rules. If runtime policy is embedded or copied elsewhere, update that copy in the same backend slice and cite it in evidence.
