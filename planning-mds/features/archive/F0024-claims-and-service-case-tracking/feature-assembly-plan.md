# Feature Assembly Plan — F0024: Claims & Service Case Tracking

**Created:** 2026-07-03
**Author:** Architect Agent
**Status:** Drift-reconcile active for feature action run `2026-07-03-72f49d29`

## Overview

F0024 adds CRM-owned service cases for post-bind servicing and claim-support coordination. The implementation creates a `ServiceCase` aggregate, claim-reference child record, communication/task links, append-only service-case transition history, API endpoints, and frontend account/policy/workspace surfaces. Claim details remain reference context only; adjudication, reserves, payments, carrier sync, and external self-service are out of scope.

## 2026-07-03 Drift-Reconcile Addendum

The prior feature run delivered the core service-case slice but left PRD-completion drift. This run must close the following gaps before F0024 can remain archived as complete:

| Gap | Required Reconciliation |
|-----|-------------------------|
| Workspace list is too thin | Add search, owner/account/policy/due/closed filters and show account, policy, owner, due date, priority, status, and claim-reference indicator. |
| Workspace create action is indirect | Add a workspace create modal/drawer with account selection instead of routing users to Accounts. |
| Work-management edits missing in UI | Add detail-page editing for owner, priority, due date, follow-up summary, summary, and description. |
| Communication linking has backend but no UI | Add a detail-page communication-link flow using existing communication records. |
| History is transition-only | Show service-case timeline/audit events for creation, updates, claim-reference updates, communication links, follow-up task creation, and transitions. |
| Validation rules incomplete | Enforce active due date, future date-of-loss rejection, and Waiting transition reason or follow-up. |
| Evidence was accepted with hardening gaps | Add API integration and frontend mutation coverage for the PRD journeys. |

## Drift-Reconcile Build Order

| Step | Scope | Stories | Required Outcome |
|------|-------|---------|------------------|
| 1 | Backend contract and service reconciliation | S0001-S0006 | List/search/filter DTOs support PRD workspace; validations cover required fields and transition rules; timeline payloads include safe changed-field summaries. |
| 2 | Frontend workspace and creation | S0001-S0002 | Workspace supports search/filter/create and renders complete PRD row data. |
| 3 | Frontend detail mutations and history | S0003-S0006 | Detail page supports work-management edit, communication link, claim reference, follow-up task, status transition, and full history display. |
| 4 | Tests, runtime validation, reviews, KG reconciliation | S0001-S0006 | Backend API/service tests, frontend mutation tests, browser smoke, lint/theme/build, security/code/devops signoff, and G7/G8 harness closeout pass. |

## Drift-Reconcile Mutation Traceability

| Screen / Entry Point | User Action | Endpoint | Service Method | Validation / Auth | Audit / Timeline Evidence | Test Expectation |
|----------------------|-------------|----------|----------------|-------------------|---------------------------|------------------|
| Service Cases Workspace | Search/filter/open case | `GET /service-cases` | `ListAsync` | `service_case:read`; query values bounded and normalized | N/A read-only | API + frontend tests prove status/owner/priority/due/account/policy/search/closed filters. |
| Service Cases Workspace | Create case with selected account | `POST /service-cases` | `CreateAsync` | `service_case:create`; account required; due date required; policy account match; owner active | `ServiceCaseCreated` on account and policy when present | API + frontend tests prove save, reload, and workspace visibility. |
| Account/Policy 360 Rail | Create case from context | `POST /service-cases` | `CreateAsync` | Same as workspace with account/policy prefilled | `ServiceCaseCreated` | Existing rail tests expanded for due date and reload persistence. |
| Service Case Detail | Edit owner/priority/due/follow-up | `PATCH /service-cases/{id}` | `UpdateAsync` | `service_case:update`; `service_case:assign` for owner changes; rowVersion; closed read-only | `ServiceCaseUpdated` with changed fields | API + frontend tests prove persisted field edits and history item. |
| Service Case Detail | Transition status | `POST /service-cases/{id}/transition` | `TransitionAsync` | `service_case:transition`; allowed pairs; Waiting requires reason/follow-up; Closing requires resolution summary | `ServiceCaseTransitioned` | API + frontend tests prove invalid attempts fail and valid transitions reload. |
| Service Case Detail | Update claim reference | `PATCH /service-cases/{id}/claim-reference` | `UpdateClaimReferenceAsync` | `service_case:update_claim_reference`; date of loss not future; rowVersion | `ServiceCaseClaimReferenceUpdated` | API + frontend tests prove values persist and future date fails. |
| Service Case Detail | Link communication | `POST /service-cases/{id}/communication-links` | `LinkCommunicationAsync` | `service_case:link_communication` plus `communication_event:read`; duplicate prevention | `ServiceCaseCommunicationLinked` | API + frontend tests prove link appears in detail/history. |
| Service Case Detail | Create follow-up task | `POST /service-cases/{id}/follow-up-task` | `CreateFollowUpTaskAsync` | `service_case:create_follow_up` plus `task:create`; assignee active | `TaskCreated` and `ServiceCaseFollowUpTaskCreated` | API + frontend tests prove task link count/list and history. |

## Knowledge-Graph Binding Plan

No new canonical nodes are expected. This drift-reconcile run extends existing F0024 semantics:
- `entity:service-case`
- `entity:service-case-claim-reference`
- `entity:service-case-transition`
- `workflow:service-case`
- service-case endpoints and policy rules already bound by the prior run

G7 must confirm existing `code-index.yaml` bindings cover the new/updated API, service, repository, frontend feature slice, page, and test files. If new test directories or helper files are added outside existing globs, Architect must bind them at G7.

## Build Order

| Step | Scope | Stories | Rationale |
|------|-------|---------|-----------|
| 1 | Backend data model, repository, service, policy copy | S0001-S0006 | Establish persisted aggregate, authorization, workflow, and timeline source of truth before API/UI. |
| 2 | Backend endpoints, validators, tests | S0001-S0006 | Publish the approved REST surface and prove error/status behavior. |
| 3 | Frontend API hooks, workspace page, detail page, account/policy panels | S0001-S0006 | Build the vertical user workflow after backend contracts exist. |
| 4 | QE, deployability, code/security review, KG reconciliation | S0001-S0006 | Validate persistence, permissions, timeline, UI, migration, and as-built graph bindings. |

## Existing Code (Must Be Modified)

| File | Current State | F0024 Change |
|------|---------------|--------------|
| `engine/src/Nebula.Infrastructure/Persistence/AppDbContext.cs` | DbSets for account, policy, task, communication, timeline, workflow. | Add `ServiceCases`, `ServiceCaseClaimReferences`, `ServiceCaseCommunicationLinks`, `ServiceCaseTaskLinks`, `ServiceCaseTransitions`. |
| `engine/src/Nebula.Infrastructure/DependencyInjection.cs` | Registers repositories and services including `ICommunicationRepository` and `CommunicationService`. | Register `IServiceCaseRepository` and `ServiceCaseService`. |
| `engine/src/Nebula.Api/Program.cs` | Registers app services and maps endpoint modules through `app.MapCommunicationEndpoints()`, `app.MapTaskEndpoints()`, etc. | Add scoped `ServiceCaseService`; call `app.MapServiceCaseEndpoints()`. |
| `engine/src/Nebula.Infrastructure/Authorization/policy.csv` | Embedded Casbin policy loaded by `CasbinAuthorizationService`; currently must mirror planning `policy.csv`. | Copy the new `service_case` rows from `planning-mds/security/policies/policy.csv`. |
| `experience/src/App.tsx` | App route table for dashboard, accounts, policies, tasks, renewals, etc. | Add `/service-cases` and `/service-cases/:serviceCaseId` routes. |
| `experience/src/components/layout/Sidebar.tsx` | Main navigation entries. | Add a Service Cases navigation entry for internal roles only. |
| `experience/src/pages/AccountDetailPage.tsx` | Account 360 tabs/sections include contacts, submissions, renewals, policies, timeline. | Add service-case panel/list and create action. |
| `experience/src/pages/PolicyDetailPage.tsx` | Policy 360 shows policy details, versions, coverages, endorsements, timeline. | Add service-case panel/list and create action scoped to policy/account. |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `engine/src/Nebula.Domain/Entities/ServiceCase.cs` | Domain | Source record for service cases. |
| `engine/src/Nebula.Domain/Entities/ServiceCaseClaimReference.cs` | Domain | Optional 1:1 claim-reference context. |
| `engine/src/Nebula.Domain/Entities/ServiceCaseCommunicationLink.cs` | Domain | Bridge to F0021 `CommunicationEvent`. |
| `engine/src/Nebula.Domain/Entities/ServiceCaseTaskLink.cs` | Domain | Bridge to F0004 `TaskItem`. |
| `engine/src/Nebula.Domain/Entities/ServiceCaseTransition.cs` | Domain | Append-only service-case workflow history. |
| `engine/src/Nebula.Application/DTOs/ServiceCaseDtos.cs` | Application | Request/response/list/query DTOs. |
| `engine/src/Nebula.Application/Interfaces/IServiceCaseRepository.cs` | Application | Repository contract for service-case aggregate and scope checks. |
| `engine/src/Nebula.Application/Services/ServiceCaseService.cs` | Application | Business logic, authorization, workflow, task/communication/timeline integration. |
| `engine/src/Nebula.Application/Validators/ServiceCaseValidators.cs` | Application | FluentValidation request validation. |
| `engine/src/Nebula.Infrastructure/Repositories/ServiceCaseRepository.cs` | Infrastructure | EF repository implementation. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/ServiceCaseConfiguration.cs` | Infrastructure | EF model for `ServiceCase`. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/ServiceCaseClaimReferenceConfiguration.cs` | Infrastructure | EF model for claim reference. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/ServiceCaseCommunicationLinkConfiguration.cs` | Infrastructure | EF model for communication link. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/ServiceCaseTaskLinkConfiguration.cs` | Infrastructure | EF model for task link. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/ServiceCaseTransitionConfiguration.cs` | Infrastructure | EF model for transitions. |
| `engine/src/Nebula.Infrastructure/Persistence/Migrations/*F0024*ServiceCases*.cs` | Infrastructure | EF migration for all new tables/indexes. |
| `engine/src/Nebula.Api/Endpoints/ServiceCaseEndpoints.cs` | API | REST endpoints. |
| `engine/tests/Nebula.Tests/Unit/ServiceCaseServiceTests.cs` | Tests | Workflow, validation, and authorization unit tests. |
| `engine/tests/Nebula.Tests/Integration/ServiceCaseEndpointTests.cs` | Tests | API integration and persistence/reload tests. |
| `experience/src/features/service-cases/api/serviceCasesApi.ts` | Frontend | Fetch/mutate API client. |
| `experience/src/features/service-cases/hooks/useServiceCases.ts` | Frontend | TanStack Query hooks. |
| `experience/src/features/service-cases/types.ts` | Frontend | TypeScript contracts. |
| `experience/src/features/service-cases/components/ServiceCaseStatusBadge.tsx` | Frontend | Status display. |
| `experience/src/features/service-cases/components/ServiceCasePriorityBadge.tsx` | Frontend | Priority display. |
| `experience/src/features/service-cases/components/ServiceCaseCreateModal.tsx` | Frontend | Create form from account/policy context. |
| `experience/src/features/service-cases/components/ServiceCaseListPanel.tsx` | Frontend | Context/workspace list. |
| `experience/src/features/service-cases/components/ServiceCaseDetailPanel.tsx` | Frontend | Detail sections and mutation controls. |
| `experience/src/pages/ServiceCasesPage.tsx` | Frontend | Workspace list. |
| `experience/src/pages/ServiceCaseDetailPage.tsx` | Frontend | Detail route. |
| `experience/src/features/service-cases/components/__tests__/ServiceCaseCreateModal.test.tsx` | Tests | Form and validation tests. |
| `experience/src/pages/tests/ServiceCasesPage.integration.test.tsx` | Tests | Workspace UI integration. |
| `experience/src/pages/tests/ServiceCaseDetailPage.integration.test.tsx` | Tests | Detail mutation/reload integration. |

---

## Step 1 — Backend Aggregate And Persistence (S0001-S0006)

### Entity / DTO / Code

```csharp
// engine/src/Nebula.Domain/Entities/ServiceCase.cs
namespace Nebula.Domain.Entities;

public class ServiceCase : BaseEntity
{
    public string CaseNumber { get; set; } = default!;
    public Guid AccountId { get; set; }
    public Guid? PolicyId { get; set; }
    public string Summary { get; set; } = default!;
    public string? Description { get; set; }
    public string Type { get; set; } = default!;
    public string Status { get; set; } = "Intake";
    public string Priority { get; set; } = "Medium";
    public Guid OwnerUserId { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? FollowUpSummary { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ResolutionSummary { get; set; }
    public Account Account { get; set; } = default!;
    public Policy? Policy { get; set; }
    public ServiceCaseClaimReference? ClaimReference { get; set; }
    public ICollection<ServiceCaseCommunicationLink> CommunicationLinks { get; set; } = [];
    public ICollection<ServiceCaseTaskLink> TaskLinks { get; set; } = [];
    public ICollection<ServiceCaseTransition> Transitions { get; set; } = [];
}

// engine/src/Nebula.Domain/Entities/ServiceCaseClaimReference.cs
namespace Nebula.Domain.Entities;

public class ServiceCaseClaimReference : BaseEntity
{
    public Guid ServiceCaseId { get; set; }
    public string? CarrierClaimNumber { get; set; }
    public DateOnly? DateOfLoss { get; set; }
    public string? ClaimantDisplayName { get; set; }
    public string? LossSummary { get; set; }
    public string? CarrierContactReference { get; set; }
    public ServiceCase ServiceCase { get; set; } = default!;
}

// engine/src/Nebula.Domain/Entities/ServiceCaseCommunicationLink.cs
namespace Nebula.Domain.Entities;

public class ServiceCaseCommunicationLink : BaseEntity
{
    public Guid ServiceCaseId { get; set; }
    public Guid CommunicationEventId { get; set; }
    public string LinkType { get; set; } = "Context";
    public ServiceCase ServiceCase { get; set; } = default!;
    public CommunicationEvent CommunicationEvent { get; set; } = default!;
}

// engine/src/Nebula.Domain/Entities/ServiceCaseTaskLink.cs
namespace Nebula.Domain.Entities;

public class ServiceCaseTaskLink : BaseEntity
{
    public Guid ServiceCaseId { get; set; }
    public Guid TaskId { get; set; }
    public string Relationship { get; set; } = "FollowUp";
    public ServiceCase ServiceCase { get; set; } = default!;
    public TaskItem Task { get; set; } = default!;
}

// engine/src/Nebula.Domain/Entities/ServiceCaseTransition.cs
namespace Nebula.Domain.Entities;

public class ServiceCaseTransition : BaseEntity
{
    public Guid ServiceCaseId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = default!;
    public Guid ActorUserId { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? ReasonCode { get; set; }
    public string? Note { get; set; }
    public ServiceCase ServiceCase { get; set; } = default!;
}
```

```csharp
// engine/src/Nebula.Application/DTOs/ServiceCaseDtos.cs
namespace Nebula.Application.DTOs;

public record ServiceCaseClaimReferenceDto(
    string? CarrierClaimNumber,
    DateOnly? DateOfLoss,
    string? ClaimantDisplayName,
    string? LossSummary,
    string? CarrierContactReference,
    Guid? UpdatedByUserId,
    DateTime? UpdatedAt);

public record ServiceCaseCommunicationLinkDto(
    Guid CommunicationEventId,
    string LinkType,
    Guid CreatedByUserId,
    DateTime CreatedAt);

public record ServiceCaseTaskLinkDto(
    Guid TaskId,
    string Relationship,
    Guid CreatedByUserId,
    DateTime CreatedAt);

public record ServiceCaseTransitionDto(
    string? FromStatus,
    string ToStatus,
    Guid ActorUserId,
    DateTime OccurredAt,
    string? ReasonCode,
    string? Note);

public record ServiceCaseDto(
    Guid Id,
    string CaseNumber,
    Guid AccountId,
    Guid? PolicyId,
    string Summary,
    string? Description,
    string Type,
    string Status,
    string Priority,
    Guid OwnerUserId,
    DateOnly? DueDate,
    string? FollowUpSummary,
    ServiceCaseClaimReferenceDto? ClaimReference,
    IReadOnlyList<ServiceCaseCommunicationLinkDto> CommunicationLinks,
    IReadOnlyList<ServiceCaseTaskLinkDto> TaskLinks,
    IReadOnlyList<ServiceCaseTransitionDto> Transitions,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    string? ResolutionSummary,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    uint RowVersion);

public record ServiceCaseCreateRequestDto(
    Guid AccountId,
    Guid? PolicyId,
    string Summary,
    string? Description,
    string Type,
    string Priority,
    Guid OwnerUserId,
    DateOnly? DueDate,
    string? FollowUpSummary,
    ServiceCaseClaimReferenceUpdateRequestDto? ClaimReference);

public record ServiceCaseUpdateRequestDto(
    string? Summary,
    string? Description,
    string? Priority,
    Guid? OwnerUserId,
    DateOnly? DueDate,
    string? FollowUpSummary,
    string? ResolutionSummary,
    uint? RowVersion);

public record ServiceCaseTransitionRequestDto(
    string ToStatus,
    string? ReasonCode,
    string? Note,
    string? ResolutionSummary,
    uint? RowVersion);

public record ServiceCaseClaimReferenceUpdateRequestDto(
    string? CarrierClaimNumber,
    DateOnly? DateOfLoss,
    string? ClaimantDisplayName,
    string? LossSummary,
    string? CarrierContactReference,
    uint? RowVersion);

public record ServiceCaseCommunicationLinkRequestDto(
    Guid CommunicationEventId,
    string? LinkType,
    uint? RowVersion);

public record ServiceCaseFollowUpTaskRequestDto(
    string Title,
    string? Description,
    Guid AssignedToUserId,
    DateOnly? DueDate,
    string? Priority,
    uint? RowVersion);

public record ServiceCaseListQuery(
    Guid? AccountId,
    Guid? PolicyId,
    Guid? OwnerUserId,
    string? Status,
    string? Priority,
    DateOnly? DueBefore,
    int Page,
    int PageSize);

public record ServiceCaseListResponseDto(
    IReadOnlyList<ServiceCaseDto> Data,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
```

```csharp
// engine/src/Nebula.Application/Interfaces/IServiceCaseRepository.cs
using Nebula.Application.Common;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IServiceCaseRepository
{
    Task AddAsync(ServiceCase serviceCase, CancellationToken ct = default);
    Task<ServiceCase?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<PaginatedResult<ServiceCase>> ListAsync(ServiceCaseListQuery query, CancellationToken ct = default);
    Task<bool> AccountExistsAsync(Guid accountId, CancellationToken ct = default);
    Task<bool> PolicyBelongsToAccountAsync(Guid policyId, Guid accountId, CancellationToken ct = default);
    Task<bool> CommunicationExistsAsync(Guid communicationEventId, CancellationToken ct = default);
    Task<string> NextCaseNumberAsync(CancellationToken ct = default);
}
```

### Logic Flow

`ServiceCaseService.CreateAsync(ServiceCaseCreateRequestDto dto, ICurrentUserService user, CancellationToken ct)`:

1. Authorize `service_case:create`; reject `forbidden`.
2. Validate account exists; reject `account_not_found`.
3. If `PolicyId` is present, validate policy belongs to account; reject `policy_not_found` or `policy_account_mismatch`.
4. Validate owner: non-manager roles can only self-own; DistributionManager/Admin can assign active internal users.
5. Generate `CaseNumber` from repository (`SC-{yyyy}-{sequence:000000}`).
6. Create `ServiceCase` with `Status = "Intake"` and optional `ServiceCaseClaimReference`.
7. Append initial `ServiceCaseTransition` with `FromStatus = null`, `ToStatus = "Intake"`.
8. Emit `ServiceCaseCreated` timeline event to Account and Policy when present.
9. `unitOfWork.CommitAsync(ct)`.
10. Return `ServiceCaseDto`.

### Mutation Traceability

| Screen / Entry Point | User Action | Endpoint | Service Method | Entity / Carrier | Authorization | Concurrency | Validation Failure | Audit / Timeline | Test Expectation |
|----------------------|-------------|----------|----------------|------------------|---------------|-------------|--------------------|------------------|------------------|
| Account detail / Policy detail | Create service case | `POST /service-cases` | `CreateAsync` | `ServiceCase`, optional `ServiceCaseClaimReference` | `service_case:create` + account/policy read | N/A create | 400 validation, 403, 404, 409 policy-account mismatch | `ServiceCaseCreated`, initial `ServiceCaseTransition` | Create, reload account/policy panel, detail opens with same data. |

### Casbin Enforcement

- Resource: `service_case`, Action: `create`
- Hydrate attrs: `subjectId = user.UserId`, `accountId`, `policyId`, `ownerUserId`
- Policy condition: role row in `policy.csv`; service method enforces account/policy scope and owner assignment rule.
- Enforcement pattern: loop `user.Roles`; allow if any role authorizes the action.

### Timeline Event

- EventType: `ServiceCaseCreated`
- EntityType: `Account` and optional `Policy`; EntityId: `AccountId` / `PolicyId`
- EventDescription: `$"Service case {case.CaseNumber} created"`
- ExternalDescription: `null`
- EventPayloadJson: `ServiceCaseCreated`

### HTTP Responses

| Status | Body | Condition |
|--------|------|-----------|
| 201 Created | `ServiceCaseDto` | Success |
| 400 | ProblemDetails (`validation_error`) | Schema/FluentValidation failure |
| 403 | ProblemDetails (`policy_denied`) | Casbin deny or owner assignment deny |
| 404 | ProblemDetails (`account_not_found` / `policy_not_found`) | Missing context |
| 409 | ProblemDetails (`policy_account_mismatch`) | Policy does not belong to account |

---

## Step 2 — Service-Case Read, Update, Transition, Claim, Link, Follow-Up APIs (S0002-S0006)

### New Files

| File | Layer |
|------|-------|
| `engine/src/Nebula.Api/Endpoints/ServiceCaseEndpoints.cs` | API |
| `engine/src/Nebula.Application/Validators/ServiceCaseValidators.cs` | Application |

### Service Methods

```csharp
public class ServiceCaseService(
    IServiceCaseRepository serviceCaseRepo,
    ICommunicationRepository communicationRepo,
    ITaskRepository taskRepo,
    ITimelineRepository timelineRepo,
    IUserProfileRepository userProfileRepo,
    IUnitOfWork unitOfWork,
    IAuthorizationService authz)
{
    public Task<(ServiceCaseDto? Dto, string? ErrorCode)> CreateAsync(ServiceCaseCreateRequestDto dto, ICurrentUserService user, CancellationToken ct = default);
    public Task<(ServiceCaseListResponseDto? Dto, string? ErrorCode)> ListAsync(ServiceCaseListQuery query, ICurrentUserService user, CancellationToken ct = default);
    public Task<(ServiceCaseDto? Dto, string? ErrorCode)> GetByIdAsync(Guid id, ICurrentUserService user, CancellationToken ct = default);
    public Task<(ServiceCaseDto? Dto, string? ErrorCode)> UpdateAsync(Guid id, ServiceCaseUpdateRequestDto dto, ICurrentUserService user, CancellationToken ct = default);
    public Task<(ServiceCaseDto? Dto, string? ErrorCode)> TransitionAsync(Guid id, ServiceCaseTransitionRequestDto dto, ICurrentUserService user, CancellationToken ct = default);
    public Task<(ServiceCaseDto? Dto, string? ErrorCode)> UpdateClaimReferenceAsync(Guid id, ServiceCaseClaimReferenceUpdateRequestDto dto, ICurrentUserService user, CancellationToken ct = default);
    public Task<(ServiceCaseDto? Dto, string? ErrorCode)> LinkCommunicationAsync(Guid id, ServiceCaseCommunicationLinkRequestDto dto, ICurrentUserService user, CancellationToken ct = default);
    public Task<(TaskDto? Dto, string? ErrorCode)> CreateFollowUpTaskAsync(Guid id, ServiceCaseFollowUpTaskRequestDto dto, ICurrentUserService user, CancellationToken ct = default);
}
```

### Logic Flow

`ListAsync`:
1. Authorize `service_case:read`.
2. Normalize page/pageSize (`page >= 1`, `pageSize <= 100`).
3. Query by account/policy/owner/status/priority/dueBefore.
4. Repository excludes `IsDeleted` and includes claim reference, links, and transitions.
5. Return paged DTOs sorted by due date asc nulls last, priority weight, created desc.

`UpdateAsync`:
1. Load service case with details or return `not_found`.
2. Authorize `service_case:update`; if `OwnerUserId` changes, require `service_case:assign`.
3. Reject `Status == "Closed"` with `closed_service_case`.
4. Enforce `RowVersion` when provided; stale returns `stale_row_version`.
5. Apply mutable fields only; trim strings; validate owner active when changed.
6. Emit `ServiceCaseUpdated`; commit and return DTO.

`TransitionAsync`:
1. Load service case with transitions or return `not_found`.
2. Authorize `service_case:transition`.
3. Reject closed records.
4. Validate allowed transition:
   - `Intake -> InProgress|Waiting`
   - `InProgress -> Waiting|Resolved`
   - `Waiting -> InProgress|Resolved`
   - `Resolved -> Closed`
5. Require `ResolutionSummary` when `ToStatus == "Resolved"` and no existing resolution summary.
6. Append `ServiceCaseTransition`; set `ResolvedAt`/`ClosedAt` as applicable.
7. Emit `ServiceCaseTransitioned`; commit and return DTO.

`UpdateClaimReferenceAsync`:
1. Load service case or return `not_found`.
2. Authorize `service_case:update_claim_reference`.
3. Reject closed records and stale row version.
4. Upsert `ServiceCaseClaimReference`; never write reserve/payment/adjudication fields.
5. Emit `ServiceCaseClaimReferenceUpdated` with safe metadata only.

`LinkCommunicationAsync`:
1. Load service case or return `not_found`.
2. Authorize `service_case:link_communication` and `communication_event:read`.
3. Reject closed records and duplicate `CommunicationEventId`.
4. Validate communication exists.
5. Add `ServiceCaseCommunicationLink`; emit `ServiceCaseCommunicationLinked`.

`CreateFollowUpTaskAsync`:
1. Load service case or return `not_found`.
2. Authorize `service_case:create_follow_up` and `task:create`.
3. Reject closed records and stale row version.
4. Enforce same assignment rule as Task Center / Communication follow-up.
5. Create `TaskItem` with `LinkedEntityType = "ServiceCase"` and `LinkedEntityId = serviceCase.Id`.
6. Add `ServiceCaseTaskLink`; emit `TaskCreated` and `ServiceCaseFollowUpTaskCreated`.

### Endpoint Table

| Endpoint | Request | Response | Service Method | Main Auth |
|----------|---------|----------|----------------|-----------|
| `POST /service-cases` | `ServiceCaseCreateRequestDto` | `ServiceCaseDto` | `CreateAsync` | `service_case:create` |
| `GET /service-cases` | query params | `ServiceCaseListResponseDto` | `ListAsync` | `service_case:read` |
| `GET /service-cases/{serviceCaseId}` | route id | `ServiceCaseDto` | `GetByIdAsync` | `service_case:read` |
| `PATCH /service-cases/{serviceCaseId}` | `ServiceCaseUpdateRequestDto` | `ServiceCaseDto` | `UpdateAsync` | `service_case:update`, maybe `assign` |
| `POST /service-cases/{serviceCaseId}/transition` | `ServiceCaseTransitionRequestDto` | `ServiceCaseDto` | `TransitionAsync` | `service_case:transition` |
| `PATCH /service-cases/{serviceCaseId}/claim-reference` | `ServiceCaseClaimReferenceUpdateRequestDto` | `ServiceCaseDto` | `UpdateClaimReferenceAsync` | `service_case:update_claim_reference` |
| `POST /service-cases/{serviceCaseId}/communication-links` | `ServiceCaseCommunicationLinkRequestDto` | `ServiceCaseDto` | `LinkCommunicationAsync` | `service_case:link_communication` + `communication_event:read` |
| `POST /service-cases/{serviceCaseId}/follow-up-task` | `ServiceCaseFollowUpTaskRequestDto` | `TaskDto` | `CreateFollowUpTaskAsync` | `service_case:create_follow_up` + `task:create` |

### Casbin Enforcement

- Resource/action pairs: `service_case:create/read/update/assign/transition/update_claim_reference/link_communication/create_follow_up`
- Extra checks:
  - `communication_event:read` for communication links.
  - `task:create` for follow-up tasks.
  - Account/policy read scope before returning or mutating any case.
  - External roles receive no service-case policy rows.

### HTTP Responses

| Status | Body | Condition |
|--------|------|-----------|
| 200 OK | `ServiceCaseDto` / `ServiceCaseListResponseDto` | Read/update/transition/link success |
| 201 Created | `ServiceCaseDto` / `TaskDto` | Create service case or follow-up task success |
| 400 | ProblemDetails (`validation_error`) | Request validation failure |
| 403 | ProblemDetails (`policy_denied`) | Casbin/scope deny |
| 404 | ProblemDetails (`not_found`) | Case/account/policy/communication not found |
| 409 | ProblemDetails (`invalid_transition`, `closed_service_case`, `duplicate_link`, `policy_account_mismatch`) | Business conflict |
| 412 | ProblemDetails (`stale_row_version`) | Optimistic concurrency failure |

---

## Step 3 — Frontend Workspace, Context Panels, And Detail UX (S0001-S0006)

### Modified Files

| File | Change |
|------|--------|
| `experience/src/App.tsx` | Add service-case list/detail routes. |
| `experience/src/components/layout/Sidebar.tsx` | Add Service Cases navigation item. |
| `experience/src/pages/AccountDetailPage.tsx` | Add `ServiceCaseListPanel accountId={account.id}` and create modal. |
| `experience/src/pages/PolicyDetailPage.tsx` | Add `ServiceCaseListPanel accountId={policy.accountId} policyId={policy.id}` and create modal. |

### New Frontend Contracts

```ts
// experience/src/features/service-cases/types.ts
export type ServiceCaseStatus = "Intake" | "InProgress" | "Waiting" | "Resolved" | "Closed";
export type ServiceCasePriority = "Low" | "Medium" | "High" | "Urgent";
export type ServiceCaseType = "ServiceRequest" | "ClaimSupport" | "DocumentationSupport" | "BillingInquiry" | "Other";

export interface ServiceCaseClaimReference {
  carrierClaimNumber: string | null;
  dateOfLoss: string | null;
  claimantDisplayName: string | null;
  lossSummary: string | null;
  carrierContactReference: string | null;
  updatedByUserId: string | null;
  updatedAt: string | null;
}

export interface ServiceCase {
  id: string;
  caseNumber: string;
  accountId: string;
  policyId: string | null;
  summary: string;
  description: string | null;
  type: ServiceCaseType;
  status: ServiceCaseStatus;
  priority: ServiceCasePriority;
  ownerUserId: string;
  dueDate: string | null;
  followUpSummary: string | null;
  claimReference: ServiceCaseClaimReference | null;
  resolvedAt: string | null;
  closedAt: string | null;
  resolutionSummary: string | null;
  rowVersion: number;
}
```

### Frontend Logic Flow

1. `serviceCasesApi.ts` wraps all endpoints and normalizes ProblemDetails.
2. `useServiceCases(query)` lists workspace/context cases.
3. `useServiceCase(id)` loads detail.
4. Mutations invalidate `["service-cases"]`, `["service-case", id]`, account detail, policy detail, and timeline queries.
5. Forms use React Hook Form + AJV schemas from `planning-mds/schemas/service-case*.schema.json` generated/consumed through existing contract workflow.
6. Closed cases disable edit, claim-reference update, link, follow-up, and transition controls.
7. Do not place instructional text blocks in the app; controls should be self-evident with labels/tooltips.

### Mutation Traceability

| Screen / Entry Point | User Action | Endpoint | Service Method | Entity / Carrier | Authorization | Concurrency | Validation Failure | Audit / Timeline | Test Expectation |
|----------------------|-------------|----------|----------------|------------------|---------------|-------------|--------------------|------------------|------------------|
| Account/policy context panel | Create | `POST /service-cases` | `CreateAsync` | `ServiceCase` | `service_case:create` | N/A | Inline validation + ProblemDetails | `ServiceCaseCreated` | Modal closes, list refreshes, reload persists. |
| Workspace/detail | Edit owner/priority/due/follow-up | `PATCH /service-cases/{id}` | `UpdateAsync` | `ServiceCase` | `service_case:update/assign` | `rowVersion` | 403/409/412 displayed | `ServiceCaseUpdated` | Mutation persists after reload. |
| Detail | Transition status | `POST /service-cases/{id}/transition` | `TransitionAsync` | `ServiceCaseTransition` | `service_case:transition` | `rowVersion` | Invalid transition shown | `ServiceCaseTransitioned` | Status/history updated after reload. |
| Detail claim section | Save claim reference | `PATCH /service-cases/{id}/claim-reference` | `UpdateClaimReferenceAsync` | `ServiceCaseClaimReference` | `service_case:update_claim_reference` | `rowVersion` | ProblemDetails shown | `ServiceCaseClaimReferenceUpdated` | Claim metadata persists; timeline does not expose loss narrative. |
| Detail communication section | Link communication | `POST /service-cases/{id}/communication-links` | `LinkCommunicationAsync` | `ServiceCaseCommunicationLink` | `service_case:link_communication` | `rowVersion` | Duplicate link shown | `ServiceCaseCommunicationLinked` | Link visible after reload. |
| Detail follow-up section | Create follow-up | `POST /service-cases/{id}/follow-up-task` | `CreateFollowUpTaskAsync` | `TaskItem`, `ServiceCaseTaskLink` | `service_case:create_follow_up`, `task:create` | `rowVersion` | Invalid/inactive assignee shown | `TaskCreated`, `ServiceCaseFollowUpTaskCreated` | Task link visible; task appears in Task Center. |

### UI Guardrails

- Use feature-local placement under `experience/src/features/service-cases/**`.
- Use existing shadcn/ui primitives and semantic tokens; no raw palette classes for app UI surfaces/text.
- Use compact operational layout: lists, filters, status/priority badges, detail sections. No marketing-style hero.
- Verify light/dark theme for `ServiceCasesPage`, `ServiceCaseDetailPage`, account panel, and policy panel.

---

## Scope Breakdown

| Layer | Required Work | Owner | Status |
|------|----------------|-------|--------|
| Backend (`engine/`) | Entities, DTOs, validators, repository, service, endpoint module, migration, policy embedded copy, tests. | Backend Developer | Pending |
| Frontend (`experience/`) | API hooks, workspace/detail pages, context panels, forms, routing/nav, component tests. | Frontend Developer | Pending |
| AI (`neuron/`) | None; no AI scope. | N/A | Not required |
| Quality | Test plan, unit/integration/UI/E2E coverage, reload and permission evidence. | Quality Engineer | Pending |
| DevOps/Runtime | Runtime preflight, migration/deployability check, local compose readiness. | DevOps | Pending |
| Security | Review service-case ABAC, external denial, claim-reference data exposure, timeline payload safety. | Security Reviewer | Pending |

## Dependency Order

```text
Step 0 (Architect):   G0 assembly plan + evidence initialization
Step 1 (Backend):     aggregate, repository, service, migration, policy copy
Step 2 (Backend):     endpoints, validators, backend tests
  ---- Backend checkpoint: dotnet build + service/endpoint tests pass ----
Step 3 (Frontend):    service-case API hooks, pages, panels, forms, tests
  ---- Frontend checkpoint: lint/build/tests + theme smoke pass ----
Step 4 (QE/DevOps):   runtime validation, coverage, deployability, reviews
Step 5 (G7/G8):       KG reconciliation and PM closeout
```

## Integration Checkpoints

### After Backend

- [ ] `POST /service-cases` creates ServiceCase, initial transition, optional claim reference, account/policy timeline events.
- [ ] All mutation endpoints reject closed cases.
- [ ] Invalid transition matrix returns 409 `invalid_transition`.
- [ ] External roles are denied by absence of `service_case` policy rows.
- [ ] Embedded `Nebula.Infrastructure/Authorization/policy.csv` matches planning policy rows.

### After Frontend

- [ ] Account and policy context panels list and create service cases.
- [ ] Workspace filters by owner/status/priority/due/account/policy.
- [ ] Detail page supports update, transition, claim reference, communication link, follow-up task.
- [ ] Closed cases render read-only controls.
- [ ] Light and dark theme smoke checks pass.

### Cross-Story Verification

- [ ] Full lifecycle: create -> assign/update -> claim reference -> link communication -> create follow-up -> transition Intake/InProgress/Waiting/Resolved/Closed.
- [ ] All Casbin policies enforced for six internal roles and external roles denied.
- [ ] Timeline events are ordered and do not expose sensitive loss narrative text.
- [ ] ProblemDetails format matches existing endpoint helper conventions.
- [ ] Reload after every mutation proves persistence.

## Integration Checklist

- [ ] API contract compatibility validated against `api-schema-deltas.md`.
- [ ] Frontend contract compatibility validated against `planning-mds/schemas/service-case*.schema.json`.
- [ ] AI contract compatibility: N/A.
- [ ] Test cases mapped to all six story acceptance criteria.
- [ ] Developer-owned fast-test responsibilities identified by backend/frontend layer.
- [ ] Required runtime evidence artifacts identified under run folder `2026-07-03-ba011af8`.
- [ ] Framework vs solution boundary reviewed; no `agents/**` changes in feature scope.
- [ ] Mutation traceability tables completed for every create/update/manage/transition/link path.
- [ ] Render-only tests are not used to close mutation stories.
- [ ] Run/deploy instructions updated in `GETTING-STARTED.md` after implementation.

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Claim-reference fields may leak sensitive narrative into timeline or external contexts. | High | Timeline payload stores only carrier claim number/date metadata; Security review required. | Security Reviewer |
| New persisted aggregate requires migration and policy deployment sync. | Medium | DevOps deployability check must include migration and embedded policy resource verification. | DevOps |
| Service-case links cross communication/task ownership boundaries. | Medium | Keep CommunicationEvent and TaskItem as source records; ServiceCase stores bridge links only. | Backend Developer |
| Existing account/policy pages are already dense. | Medium | Use compact panels and feature-local components; no page-level layout redesign outside F0024. | Frontend Developer |

## JSON Serialization Convention

- API DTOs use .NET JSON camelCase serialization.
- `DateOnly` serializes as `yyyy-MM-dd`; `DateTime` serializes as ISO 8601 UTC.
- `RowVersion` remains `uint` in DTOs and TypeScript `number`.
- JSON schemas use Draft-07 nullable type arrays; OpenAPI 3.0 deltas use `nullable: true` when promoted into `nebula-api.yaml`.

## DI Registration Changes

- `engine/src/Nebula.Infrastructure/DependencyInjection.cs`
  - Add `services.AddScoped<IServiceCaseRepository, ServiceCaseRepository>();`
  - Add `services.AddScoped<ServiceCaseService>();`
- `engine/src/Nebula.Api/Program.cs`
  - Add `builder.Services.AddScoped<ServiceCaseService>();` only if service registrations remain API-owned for peer services.
  - Add `app.MapServiceCaseEndpoints();`

## Casbin Policy Sync

Copy the `service_case` rows already added to `planning-mds/security/policies/policy.csv` into `engine/src/Nebula.Infrastructure/Authorization/policy.csv`, then verify the embedded resource is included by `Nebula.Infrastructure.csproj`. Do not add BrokerUser or ExternalUser `service_case` rows.

## Knowledge-Graph Binding Plan

G0 predicted semantic/as-built delta:

- Add runtime code-index binding for `entity:service-case` after implementation:
  - `engine/src/Nebula.Domain/Entities/ServiceCase*.cs`
  - `engine/src/Nebula.Application/DTOs/ServiceCase*.cs`
  - `engine/src/Nebula.Application/Interfaces/IServiceCaseRepository.cs`
  - `engine/src/Nebula.Application/Services/ServiceCaseService.cs`
  - `engine/src/Nebula.Application/Validators/ServiceCase*.cs`
  - `engine/src/Nebula.Infrastructure/Repositories/ServiceCaseRepository.cs`
  - `engine/src/Nebula.Infrastructure/Persistence/Configurations/ServiceCase*.cs`
  - `engine/src/Nebula.Infrastructure/Persistence/Migrations/*F0024*ServiceCase*.cs`
  - `engine/src/Nebula.Api/Endpoints/ServiceCaseEndpoints.cs`
  - `engine/tests/Nebula.Tests/**/ServiceCase*.cs`
  - `experience/src/features/service-cases/**`
  - `experience/src/pages/ServiceCase*.tsx`
  - `experience/src/pages/tests/ServiceCase*.test.tsx`
- Existing canonical nodes from Phase B remain authoritative: `entity:service-case`, `entity:service-case-claim-reference`, `entity:service-case-transition`, `workflow:service-case`, F0024 endpoint nodes, F0024 schema nodes, and F0024 policy rules.
- G7 must reconcile actual file globs against this prediction and run symbol/drift validation.
