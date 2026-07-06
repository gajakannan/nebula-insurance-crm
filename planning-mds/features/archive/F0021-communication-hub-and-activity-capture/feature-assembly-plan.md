# Feature Assembly Plan — F0021: Communication Hub & Activity Capture

**Created:** 2026-07-01
**Author:** Architect Agent
**Status:** Approved for implementation after G0 validation
**Feature Run:** `2026-07-01-9cee64f0`

## Overview

F0021 adds structured internal communication capture as a durable CRM source record, then projects communication activity into existing timeline surfaces. The vertical slice introduces backend communication entities/services/endpoints, frontend contextual communication panels on approved record pages, follow-up task linkage, correction/redaction audit behavior, and QE/security evidence.

## Build Order

| Step | Scope | Stories | Rationale |
|------|-------|---------|-----------|
| 1 | Backend communication source model and repository | F0021-S0001, S0003, S0005 | Source records, links, participants, and corrections must exist before API/UI work can persist data. |
| 2 | Backend service, authorization, timeline, and task integration | F0021-S0001, S0003, S0004, S0005 | Mutations need one transaction, linked-entity checks, task creation, and timeline evidence. |
| 3 | API endpoints and OpenAPI/schema alignment | F0021-S0001 through S0005 | UI needs stable contracts and ProblemDetails behavior. |
| 4 | Frontend communication feature slice and contextual panels | F0021-S0001 through S0005 | Reuse existing page patterns while adding capture/history/follow-up/correction affordances. |
| 5 | QE, code review, security review, signoff, KG reconciliation, PM closeout | All stories | Harness gates require evidence before closeout. |

## Existing Code (Must Be Modified)

| File | Current State | F0021 Change |
|------|---------------|--------------|
| `engine/src/Nebula.Infrastructure/Persistence/AppDbContext.cs` | DbSets exist for Task, Timeline, Broker, Account, Submission, Policy, Renewal; no communication DbSets. | Add DbSets for `CommunicationEvent`, `CommunicationLink`, `CommunicationParticipant`, `CommunicationCorrection`, and optional `CommunicationFollowUpTaskLink`. |
| `engine/src/Nebula.Infrastructure/DependencyInjection.cs` | Registers task, timeline, account, broker, submission, policy, renewal repositories/services. | Register `ICommunicationRepository` and `CommunicationService`. |
| `engine/src/Nebula.Api/Program.cs` | Maps existing endpoint groups through extension methods. | Add `app.MapCommunicationEndpoints();`. |
| `engine/src/Nebula.Application/Services/TaskService.cs` | Creates tasks with one `LinkedEntityType/LinkedEntityId` and emits `TaskCreated`. | Add service method or overload to create a follow-up task from communication without changing existing task semantics. |
| `engine/src/Nebula.Application/DTOs/TaskCreateRequestDto.cs` | Task create payload has title, description, priority, due date, assignee, linked entity. | Add optional `SourceCommunicationId` only if backend implementation chooses task-side back-reference; otherwise persist link in communication table. |
| `engine/src/Nebula.Domain/Entities/TaskItem.cs` | Task entity has linked business entity but no communication back-reference. | Add nullable `SourceCommunicationId` only if task-side back-reference is selected; preferred MVP is a separate communication follow-up link table. |
| `engine/src/Nebula.Application/Services/TimelineService.cs` | Lists timeline projections and maps display DTOs. | No ownership change; communication service emits `ActivityTimelineEvent` records through `ITimelineRepository`. |
| `engine/src/Nebula.Infrastructure/Repositories/TimelineRepository.cs` | Supports list/add of ActivityTimelineEvent. | Reuse `AddEventAsync`; no new timeline repository shape required. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/ActivityTimelineEventConfiguration.cs` | Configures append-only timeline table. | No mutation behavior added; verify indexes support communication projections. |
| `planning-mds/api/nebula-api.yaml` | Does not yet include `/communications` contracts. | Add approved F0021 endpoint contracts from `api-schema-deltas.md`. |
| `planning-mds/schemas/activity-event-payloads.schema.json` | Contains event payload registry and F0021 communication event definitions from Phase B. | Keep aligned with implementation event payloads. |
| `planning-mds/security/policies/policy.csv` | Contains F0021 `communication_event:*` policies from Phase B. | Copy/update runtime policy source if embedded/runtime copy differs. |
| `planning-mds/security/authorization-matrix.md` | Contains F0021 authorization deltas. | Keep final implementation behavior consistent. |
| `experience/src/mocks/handlers.ts` | MSW handlers cover existing CRM APIs. | Add communication list/detail/create/follow-up/correction handlers and fixtures. |
| `experience/src/pages/BrokerDetailPage.tsx` | Tabs are `Profile`, `Contacts`, `Timeline`. | Add `Communications` tab/panel or integrated section using feature slice. |
| `experience/src/pages/AccountDetailPage.tsx` | Tabs are `Overview`, `Contacts`, `Documents`, `Activity`. | Add `Communications` tab/panel. |
| `experience/src/pages/SubmissionDetailPage.tsx` | Shows documents and activity timeline cards. | Add communication panel near documents/activity context. |
| `experience/src/pages/PolicyDetailPage.tsx` | Shows policy detail, documents, timeline query. | Add communication panel. |
| `experience/src/pages/RenewalDetailPage.tsx` | Shows renewal detail and timeline section. | Add communication panel if renewal is enabled by endpoint scope. |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `engine/src/Nebula.Domain/Entities/CommunicationEvent.cs` | Domain | Source communication record. |
| `engine/src/Nebula.Domain/Entities/CommunicationLink.cs` | Domain | Business record link rows. |
| `engine/src/Nebula.Domain/Entities/CommunicationParticipant.cs` | Domain | Structured participant metadata. |
| `engine/src/Nebula.Domain/Entities/CommunicationCorrection.cs` | Domain | Append-only correction/redaction audit. |
| `engine/src/Nebula.Domain/Entities/CommunicationFollowUpTaskLink.cs` | Domain | Optional link table between communication and task. |
| `engine/src/Nebula.Application/DTOs/CommunicationDtos.cs` | Application | Request/response DTOs. |
| `engine/src/Nebula.Application/Interfaces/ICommunicationRepository.cs` | Application | Repository contract. |
| `engine/src/Nebula.Application/Services/CommunicationService.cs` | Application | Authorization, validation orchestration, persistence, timeline/task integration. |
| `engine/src/Nebula.Application/Validators/CommunicationValidators.cs` | Application | FluentValidation rules. |
| `engine/src/Nebula.Infrastructure/Repositories/CommunicationRepository.cs` | Infrastructure | EF repository implementation. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/CommunicationEventConfiguration.cs` | Infrastructure | EF mapping and indexes. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/CommunicationLinkConfiguration.cs` | Infrastructure | Link constraints and indexes. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/CommunicationParticipantConfiguration.cs` | Infrastructure | Participant mapping. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/CommunicationCorrectionConfiguration.cs` | Infrastructure | Correction audit mapping. |
| `engine/src/Nebula.Api/Endpoints/CommunicationEndpoints.cs` | API | `/communications` endpoints. |
| `engine/tests/Nebula.Tests/Unit/CommunicationServiceTests.cs` | Backend tests | Service validation, auth, redaction, timeline behavior. |
| `engine/tests/Nebula.Tests/Integration/CommunicationEndpointTests.cs` | Backend tests | API and persistence integration. |
| `experience/src/features/communications/types.ts` | Frontend | Communication API types. |
| `experience/src/features/communications/hooks/useCommunicationHistory.ts` | Frontend | Contextual history query. |
| `experience/src/features/communications/hooks/useCommunicationMutations.ts` | Frontend | Create, follow-up, correction, redaction mutations. |
| `experience/src/features/communications/components/CommunicationPanel.tsx` | Frontend | Contextual panel composition. |
| `experience/src/features/communications/components/AddCommunicationDrawer.tsx` | Frontend | Capture form and optional follow-up fields. |
| `experience/src/features/communications/components/CommunicationDetailDrawer.tsx` | Frontend | Detail/correction/redaction/follow-up actions. |
| `experience/src/features/communications/components/CommunicationHistoryList.tsx` | Frontend | Paginated list rendering. |
| `experience/src/features/communications/components/RelatedRecordPicker.tsx` | Frontend | Approved related record selector. |
| `experience/src/features/communications/components/ParticipantEditor.tsx` | Frontend | Participant metadata editor. |
| `experience/src/features/communications/index.ts` | Frontend | Slice exports. |
| `experience/src/features/communications/__tests__/CommunicationPanel.test.tsx` | Frontend tests | Panel state and reload behavior. |
| `experience/src/pages/tests/CommunicationDetailPages.integration.test.tsx` | Frontend tests | Contextual page integration. |

---

## Step 1 — Communication Source Model (F0021-S0001, F0021-S0003, F0021-S0005)

### New Files

| File | Layer |
|------|-------|
| `engine/src/Nebula.Domain/Entities/CommunicationEvent.cs` | Domain |
| `engine/src/Nebula.Domain/Entities/CommunicationLink.cs` | Domain |
| `engine/src/Nebula.Domain/Entities/CommunicationParticipant.cs` | Domain |
| `engine/src/Nebula.Domain/Entities/CommunicationCorrection.cs` | Domain |
| `engine/src/Nebula.Domain/Entities/CommunicationFollowUpTaskLink.cs` | Domain |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/Communication*.cs` | Infrastructure |

### Modified Files

| File | Change |
|------|--------|
| `engine/src/Nebula.Infrastructure/Persistence/AppDbContext.cs` | Add communication DbSets. |
| `engine/src/Nebula.Infrastructure/Persistence/Migrations/*F0021*Communication*.cs` | Add migration for source records, links, participants, corrections, follow-up links, indexes, and FK constraints where feasible. |

### Entity / DTO / Code

```csharp
// engine/src/Nebula.Domain/Entities/CommunicationEvent.cs
namespace Nebula.Domain.Entities;

public class CommunicationEvent : BaseEntity
{
    public string Type { get; set; } = default!; // Note | Call | Meeting | EmailReference
    public string? Direction { get; set; } // Inbound | Outbound | Internal | null
    public string Summary { get; set; } = default!;
    public string? Body { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Visibility { get; set; } = "InternalOnly";
    public string? EmailProvider { get; set; }
    public string? EmailMessageId { get; set; }
    public string? EmailSubject { get; set; }
    public DateTime? EmailSentAt { get; set; }
    public DateTime? RedactedAt { get; set; }
    public Guid? RedactedByUserId { get; set; }
    public string? RedactionReason { get; set; }
    public ICollection<CommunicationLink> Links { get; set; } = [];
    public ICollection<CommunicationParticipant> Participants { get; set; } = [];
    public ICollection<CommunicationCorrection> Corrections { get; set; } = [];
    public ICollection<CommunicationFollowUpTaskLink> FollowUpTaskLinks { get; set; } = [];
}

// engine/src/Nebula.Domain/Entities/CommunicationLink.cs
namespace Nebula.Domain.Entities;

public class CommunicationLink : BaseEntity
{
    public Guid CommunicationEventId { get; set; }
    public CommunicationEvent CommunicationEvent { get; set; } = default!;
    public string EntityType { get; set; } = default!; // Broker | Account | Submission | Policy | Renewal | Task
    public Guid EntityId { get; set; }
    public bool IsPrimary { get; set; }
}

// engine/src/Nebula.Domain/Entities/CommunicationParticipant.cs
namespace Nebula.Domain.Entities;

public class CommunicationParticipant : BaseEntity
{
    public Guid CommunicationEventId { get; set; }
    public CommunicationEvent CommunicationEvent { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string? Email { get; set; }
    public string ParticipantType { get; set; } = default!; // InternalUser | BrokerContact | ExternalContact | Other
    public string? Role { get; set; }
    public string? LinkedEntityType { get; set; } // User | Broker | Contact | null
    public Guid? LinkedEntityId { get; set; }
}

// engine/src/Nebula.Domain/Entities/CommunicationCorrection.cs
namespace Nebula.Domain.Entities;

public class CommunicationCorrection : BaseEntity
{
    public Guid CommunicationEventId { get; set; }
    public CommunicationEvent CommunicationEvent { get; set; } = default!;
    public string Action { get; set; } = default!; // Correct | Redact
    public string Reason { get; set; } = default!;
    public string? PreviousSummary { get; set; }
    public string? PreviousBody { get; set; }
    public string? NewSummary { get; set; }
    public string? NewBody { get; set; }
}

// engine/src/Nebula.Domain/Entities/CommunicationFollowUpTaskLink.cs
namespace Nebula.Domain.Entities;

public class CommunicationFollowUpTaskLink : BaseEntity
{
    public Guid CommunicationEventId { get; set; }
    public CommunicationEvent CommunicationEvent { get; set; } = default!;
    public Guid TaskId { get; set; }
}
```

### Logic Flow

```
Migration + EF configuration → communication tables and indexes
```

1. Create `CommunicationEvents` with audit columns inherited from `BaseEntity`, free-text fields, email metadata, redaction metadata, and row version.
2. Create `CommunicationLinks` with `(CommunicationEventId, EntityType, EntityId)` unique index and a filtered/validated `IsPrimary` constraint in code plus migration SQL where supported.
3. Create `CommunicationParticipants` with bounded text lengths.
4. Create `CommunicationCorrections` as append-only audit rows.
5. Create `CommunicationFollowUpTaskLinks` with unique `(CommunicationEventId, TaskId)`.

### Mutation Traceability

N/A — model-only step.

### Casbin Enforcement

N/A — model-only step.

### Timeline Event

N/A — model-only step.

### HTTP Responses

N/A — model-only step.

---

## Step 2 — Communication Service And Repository (F0021-S0001 through F0021-S0005)

### New Files

| File | Layer |
|------|-------|
| `engine/src/Nebula.Application/DTOs/CommunicationDtos.cs` | Application |
| `engine/src/Nebula.Application/Interfaces/ICommunicationRepository.cs` | Application |
| `engine/src/Nebula.Application/Services/CommunicationService.cs` | Application |
| `engine/src/Nebula.Application/Validators/CommunicationValidators.cs` | Application |
| `engine/src/Nebula.Infrastructure/Repositories/CommunicationRepository.cs` | Infrastructure |

### Modified Files

| File | Change |
|------|--------|
| `engine/src/Nebula.Application/Services/TaskService.cs` | Add follow-up creation helper or call existing `CreateAsync` then record communication link. |
| `engine/src/Nebula.Infrastructure/DependencyInjection.cs` | Register repository and service. |

### Entity / DTO / Code

```csharp
// engine/src/Nebula.Application/DTOs/CommunicationDtos.cs
namespace Nebula.Application.DTOs;

public record CommunicationLinkDto(string EntityType, Guid EntityId, bool IsPrimary, string? Label = null);
public record CommunicationParticipantDto(string DisplayName, string? Email, string ParticipantType, string? Role, string? LinkedEntityType, Guid? LinkedEntityId);
public record CommunicationEmailReferenceDto(string? Provider, string? MessageId, string? Subject, DateTime? SentAt);

public record CommunicationEventDto(
    Guid Id,
    string Type,
    string? Direction,
    string Summary,
    string? Body,
    DateTime OccurredAt,
    string Visibility,
    CommunicationEmailReferenceDto? EmailReference,
    IReadOnlyList<CommunicationParticipantDto> Participants,
    IReadOnlyList<CommunicationLinkDto> Links,
    IReadOnlyList<Guid> FollowUpTaskIds,
    bool IsRedacted,
    DateTime? RedactedAt,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    uint RowVersion);

public record CommunicationEventCreateRequestDto(
    string Type,
    string? Direction,
    string Summary,
    string? Body,
    DateTime OccurredAt,
    CommunicationEmailReferenceDto? EmailReference,
    IReadOnlyList<CommunicationParticipantDto> Participants,
    IReadOnlyList<CommunicationLinkDto> Links,
    CommunicationEventFollowUpRequestDto? FollowUp);

public record CommunicationEventCorrectionRequestDto(
    string Action,
    string Reason,
    string? Summary,
    string? Body,
    IReadOnlyList<CommunicationLinkDto>? Links,
    IReadOnlyList<CommunicationParticipantDto>? Participants);

public record CommunicationEventFollowUpRequestDto(
    string Title,
    string? Description,
    string? Priority,
    DateTime? DueDate,
    Guid AssignedToUserId,
    string LinkedEntityType,
    Guid LinkedEntityId);

public record CommunicationHistoryQuery(string EntityType, Guid EntityId, int Page, int PageSize);
public record CommunicationHistoryResponseDto(IReadOnlyList<CommunicationEventDto> Data, int Page, int PageSize, int TotalCount, int TotalPages);
```

```csharp
// engine/src/Nebula.Application/Interfaces/ICommunicationRepository.cs
using Nebula.Application.Common;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface ICommunicationRepository
{
    Task AddAsync(CommunicationEvent communication, CancellationToken ct = default);
    Task<CommunicationEvent?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<PaginatedResult<CommunicationEvent>> ListByEntityAsync(string entityType, Guid entityId, int page, int pageSize, CancellationToken ct = default);
    Task<bool> LinkedEntityExistsAsync(string entityType, Guid entityId, CancellationToken ct = default);
    Task<string?> ResolveLinkedEntityNameAsync(string entityType, Guid entityId, CancellationToken ct = default);
}
```

```csharp
// engine/src/Nebula.Application/Services/CommunicationService.cs
public class CommunicationService(
    ICommunicationRepository communicationRepo,
    ITaskRepository taskRepo,
    ITimelineRepository timelineRepo,
    IUnitOfWork unitOfWork,
    IAuthorizationService authz,
    TaskService taskService)
{
    public Task<(CommunicationEventDto? Dto, string? ErrorCode)> CreateAsync(CommunicationEventCreateRequestDto dto, ICurrentUserService user, CancellationToken ct = default);
    public Task<(CommunicationHistoryResponseDto? Dto, string? ErrorCode)> ListAsync(CommunicationHistoryQuery query, ICurrentUserService user, CancellationToken ct = default);
    public Task<(CommunicationEventDto? Dto, string? ErrorCode)> GetByIdAsync(Guid id, ICurrentUserService user, CancellationToken ct = default);
    public Task<(TaskDto? Dto, string? ErrorCode)> CreateFollowUpTaskAsync(Guid communicationId, CommunicationEventFollowUpRequestDto dto, ICurrentUserService user, CancellationToken ct = default);
    public Task<(CommunicationEventDto? Dto, string? ErrorCode)> CorrectOrRedactAsync(Guid communicationId, CommunicationEventCorrectionRequestDto dto, ICurrentUserService user, CancellationToken ct = default);
}
```

### Logic Flow

```
CreateAsync(dto, user) → returns (CommunicationEventDto?, errorCode?)
```

1. Validate exactly one primary link, supported `Type`, supported linked entity types, and no duplicate links.
2. For each role, authorize `communication_event:create`; if no role allows, return `forbidden`.
3. For every link, verify entity exists and caller can read linked entity. Use existing linked-entity authorization rules/service helpers; if entity exists but caller lacks access, return `forbidden`; if missing, return `not_found`.
4. Create `CommunicationEvent`, `CommunicationLink`, and `CommunicationParticipant` rows with `CreatedByUserId`, `CreatedAt`, and `UpdatedAt`.
5. Add `CommunicationCaptured` timeline projection for every linked entity, with no body content in payload.
6. If `FollowUp` is present, call task creation with existing task rules, add `CommunicationFollowUpTaskLink`, and add `CommunicationFollowUpTaskCreated` timeline projection. If task creation fails, rollback the whole transaction by not committing.
7. `unitOfWork.CommitAsync(ct)`.
8. Return mapped DTO.

```
ListAsync(query, user) → returns CommunicationHistoryResponseDto
```

1. Validate entity type, `page >= 1`, `pageSize <= 100`.
2. Authorize `communication_event:read` and linked entity read access.
3. Query communications linked to the entity, sorted by `OccurredAt desc, CreatedAt desc`.
4. Map redacted records with `Summary = "[Redacted]"` and `Body = null`.
5. Return paginated response.

```
CorrectOrRedactAsync(id, dto, user) → returns CommunicationEventDto
```

1. Load communication with links; return `not_found` for missing or inaccessible records.
2. If `Action == Correct`, authorize `communication_event:correct`; require reason and at least one corrected field/link/participant delta.
3. If `Action == Redact`, authorize `communication_event:redact`; require reason; set `RedactedAt`, `RedactedByUserId`, `RedactionReason`, replace user-facing summary/body display state, and preserve metadata.
4. Append `CommunicationCorrection` row.
5. Emit `CommunicationCorrected` or `CommunicationRedacted` timeline event for linked entities.
6. Commit and return mapped DTO.

### Mutation Traceability

| Screen / Entry Point | User Action | Endpoint | Service Method | Entity / Carrier | Authorization | Concurrency | Validation Failure | Audit / Timeline | Test Expectation |
|----------------------|-------------|----------|----------------|------------------|---------------|-------------|--------------------|------------------|------------------|
| Contextual communication panel | Save Add Communication form | `POST /communications` | `CommunicationService.CreateAsync` | `CommunicationEvent`, `CommunicationLink`, `CommunicationParticipant` | `communication_event:create` plus linked entity read | Create only; no If-Match | 400 validation, 403 forbidden, 404 missing linked entity | `CommunicationCaptured` per linked entity | API integration proves reload list contains event and timeline projection exists. |
| Add Communication drawer | Save communication with follow-up | `POST /communications` | `CommunicationService.CreateAsync` then `TaskService.CreateAsync` | Communication + Task + link row | `communication_event:create`, `communication_event:create_follow_up`, `task:create` | Atomic create; no partial follow-up commit | 400 task validation, 403 forbidden, rollback communication if follow-up fails | `CommunicationCaptured`, `TaskCreated`, `CommunicationFollowUpTaskCreated` | Integration proves task and communication link both exist after reload. |
| Communication detail | Create follow-up after save | `POST /communications/{id}/follow-up-task` | `CommunicationService.CreateFollowUpTaskAsync` | `TaskItem`, `CommunicationFollowUpTaskLink` | `communication_event:read`, `communication_event:create_follow_up`, `task:create` | Existing communication row version not required; duplicate task link prevented | 400 validation, 403 forbidden, 404 communication missing | `TaskCreated`, `CommunicationFollowUpTaskCreated` | API and UI tests prove chip/status appears after reload. |
| Communication detail | Correct content/metadata | `POST /communications/{id}/corrections` | `CommunicationService.CorrectOrRedactAsync` | `CommunicationCorrection`, current display fields | `communication_event:correct` | Optional rowVersion if implementation adds optimistic concurrency | 400 missing reason/no delta, 403 forbidden, 404 inaccessible | `CommunicationCorrected` | Tests prove corrected value persists and correction audit exists. |
| Communication detail | Redact content | `POST /communications/{id}/corrections` | `CommunicationService.CorrectOrRedactAsync` | `CommunicationCorrection`, redaction fields | `communication_event:redact` | Optional rowVersion if implementation adds optimistic concurrency | 400 missing reason, 403 non-admin, 404 inaccessible | `CommunicationRedacted` | Security test proves summary/body hidden in list/detail/timeline payloads. |

### Casbin Enforcement

- Resource/actions: `communication_event:create/read/link/correct/redact/create_follow_up`.
- Hydrate attrs: `subjectId = user.UserId`, `linkedEntityType`, `linkedEntityId`, and role-specific linked entity scope attributes where existing services expose them.
- Policy condition: `true` in `policy.csv`, with linked-entity scope enforced in query/service layer.
- Enforcement pattern: loop through `user.Roles`, allow when any role authorizes action and linked entity read access passes.

### Timeline Event

- `CommunicationCaptured`: emitted for every linked record. Payload includes `communicationId`, `communicationType`, `summary`, `primaryEntityType`, `primaryEntityId`. No body content.
- `CommunicationCorrected`: emitted for correction. Payload includes `communicationId`, `reason`.
- `CommunicationRedacted`: emitted for redaction. Payload includes `communicationId`, `reason`.
- `CommunicationFollowUpTaskCreated`: emitted on selected task or primary entity. Payload includes `communicationId`, `taskId`, `taskTitle`.

### HTTP Responses

| Status | Body | Condition |
|--------|------|-----------|
| 200 OK | `CommunicationEventDto` or `TaskDto` | Detail/correction/follow-up success |
| 201 Created | `CommunicationEventDto` | Communication create success |
| 400 | ProblemDetails (`validation_error`) | Schema or business validation failure |
| 403 | ProblemDetails (`policy_denied`) | Casbin or linked-entity deny |
| 404 | ProblemDetails (`not_found`) | Communication or linked entity missing/inaccessible |
| 409 | ProblemDetails (`duplicate_link`) | Duplicate related link |
| 422 | ProblemDetails (`invalid_assignee` / `inactive_assignee`) | Follow-up task assignee failure |

---

## Step 3 — API Contracts And Endpoints (F0021-S0001 through F0021-S0005)

### New Files

| File | Layer |
|------|-------|
| `engine/src/Nebula.Api/Endpoints/CommunicationEndpoints.cs` | API |

### Modified Files

| File | Change |
|------|--------|
| `engine/src/Nebula.Api/Program.cs` | Register communication endpoints. |
| `planning-mds/api/nebula-api.yaml` | Add `/communications` contracts. |
| `planning-mds/schemas/communication-event*.schema.json` | Keep schema deltas aligned with DTOs. |

### Entity / DTO / Code

```csharp
// engine/src/Nebula.Api/Endpoints/CommunicationEndpoints.cs
public static class CommunicationEndpoints
{
    public static IEndpointRouteBuilder MapCommunicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/communications")
            .WithTags("Communications")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        group.MapGet("/", ListCommunications);
        group.MapPost("/", CreateCommunication);
        group.MapGet("/{communicationId:guid}", GetCommunicationById);
        group.MapPost("/{communicationId:guid}/follow-up-task", CreateFollowUpTask);
        group.MapPost("/{communicationId:guid}/corrections", CorrectOrRedactCommunication);
        return app;
    }
}
```

### Logic Flow

1. `ListCommunications` accepts `entityType`, `entityId`, `page`, `pageSize`; calls service and maps errors to ProblemDetails.
2. `CreateCommunication` validates with FluentValidation, calls service, returns `201 Created`.
3. `GetCommunicationById` returns detail or 404.
4. `CreateFollowUpTask` validates follow-up DTO, returns created task.
5. `CorrectOrRedactCommunication` validates correction/redaction DTO, returns updated communication.

### Mutation Traceability

Covered in Step 2.

### Casbin Enforcement

Endpoint layer performs coarse role checks only if needed; service owns authoritative authorization and linked-entity scope.

### Timeline Event

Endpoint layer does not emit timeline events; service owns emission.

### HTTP Responses

Use response table in Step 2 and existing `ProblemDetailsHelper` helpers.

---

## Step 4 — Frontend Communication Slice (F0021-S0001 through F0021-S0005)

### New Files

| File | Layer |
|------|-------|
| `experience/src/features/communications/types.ts` | Frontend |
| `experience/src/features/communications/hooks/useCommunicationHistory.ts` | Frontend |
| `experience/src/features/communications/hooks/useCommunicationMutations.ts` | Frontend |
| `experience/src/features/communications/components/CommunicationPanel.tsx` | Frontend |
| `experience/src/features/communications/components/AddCommunicationDrawer.tsx` | Frontend |
| `experience/src/features/communications/components/CommunicationDetailDrawer.tsx` | Frontend |
| `experience/src/features/communications/components/CommunicationHistoryList.tsx` | Frontend |
| `experience/src/features/communications/components/RelatedRecordPicker.tsx` | Frontend |
| `experience/src/features/communications/components/ParticipantEditor.tsx` | Frontend |
| `experience/src/features/communications/index.ts` | Frontend |

### Modified Files

| File | Change |
|------|--------|
| `experience/src/pages/BrokerDetailPage.tsx` | Add Communications tab/panel. |
| `experience/src/pages/AccountDetailPage.tsx` | Add Communications tab/panel. |
| `experience/src/pages/SubmissionDetailPage.tsx` | Add Communication panel card. |
| `experience/src/pages/PolicyDetailPage.tsx` | Add Communication panel card. |
| `experience/src/pages/RenewalDetailPage.tsx` | Add Communication panel card if renewal endpoint remains in scope. |
| `experience/src/mocks/handlers.ts` | Add MSW handlers. |
| `experience/src/mocks/data.ts` or `experience/src/mocks/communications.ts` | Add fixtures and in-memory mutation helpers. |

### Entity / DTO / Code

```ts
// experience/src/features/communications/types.ts
export type CommunicationType = 'Note' | 'Call' | 'Meeting' | 'EmailReference';
export type CommunicationDirection = 'Inbound' | 'Outbound' | 'Internal' | null;
export type CommunicationLinkEntityType = 'Broker' | 'Account' | 'Submission' | 'Policy' | 'Renewal' | 'Task';

export interface CommunicationLinkDto {
  entityType: CommunicationLinkEntityType;
  entityId: string;
  isPrimary: boolean;
  label?: string | null;
}

export interface CommunicationParticipantDto {
  displayName: string;
  email?: string | null;
  participantType: 'InternalUser' | 'BrokerContact' | 'ExternalContact' | 'Other';
  role?: string | null;
  linkedEntityType?: 'User' | 'Broker' | 'Contact' | null;
  linkedEntityId?: string | null;
}

export interface CommunicationEventDto {
  id: string;
  type: CommunicationType;
  direction?: CommunicationDirection;
  summary: string;
  body?: string | null;
  occurredAt: string;
  visibility: 'InternalOnly';
  participants: CommunicationParticipantDto[];
  links: CommunicationLinkDto[];
  followUpTaskIds: string[];
  isRedacted: boolean;
  redactedAt?: string | null;
  createdByUserId: string;
  createdAt: string;
  updatedAt?: string | null;
  rowVersion: number;
}
```

### Logic Flow

```
useCreateCommunication() → POST /communications
```

1. Validate required summary/occurredAt/type/primary link client-side.
2. Submit DTO to API.
3. On success invalidate `['communications', entityType, entityId]`, `['timeline']`, and task queries when follow-up was created.
4. Close drawer and show saved communication in list after refetch.

```
CommunicationPanel({ entityType, entityId })
```

1. Query `GET /communications?entityType=&entityId=`.
2. Render empty state with Add Communication action when user has internal role.
3. Render redacted indicator for redacted records.
4. Open detail drawer for correction/redaction/follow-up actions.

### Mutation Traceability

| Screen / Entry Point | User Action | Endpoint | Service Method | Entity / Carrier | Authorization | Concurrency | Validation Failure | Audit / Timeline | Test Expectation |
|----------------------|-------------|----------|----------------|------------------|---------------|-------------|--------------------|------------------|------------------|
| Broker/account/submission/policy/renewal detail | Add Communication | `POST /communications` | `CommunicationService.CreateAsync` | Communication source rows | `communication_event:create` | Create only | Inline required-field errors; API 400 surfaced | `CommunicationCaptured` | Frontend integration proves form save, refetch, and list item display. |
| Communication detail | Create follow-up | `POST /communications/{id}/follow-up-task` | `CommunicationService.CreateFollowUpTaskAsync` | Task + link row | `communication_event:create_follow_up`, `task:create` | Create only | Inline task validation; API 422 surfaced | `CommunicationFollowUpTaskCreated` | Test proves task chip/status appears and task query invalidates. |
| Communication detail | Correct | `POST /communications/{id}/corrections` | `CommunicationService.CorrectOrRedactAsync` | Correction audit row | `communication_event:correct` | Use rowVersion if API implements it | Reason required | `CommunicationCorrected` | Test proves corrected summary after refetch. |
| Communication detail | Redact | `POST /communications/{id}/corrections` | `CommunicationService.CorrectOrRedactAsync` | Redaction fields + correction row | `communication_event:redact` | Use rowVersion if API implements it | Reason required; forbidden for non-admin | `CommunicationRedacted` | Test proves body/summary redacted after refetch. |

### Casbin Enforcement

Frontend gates actions by role for ergonomics only. API remains authoritative.

### Timeline Event

Frontend renders timeline effects through existing timeline sections after invalidation/refetch.

### HTTP Responses

Frontend handles 400/403/404/409/422 with existing API error patterns and inline form errors where possible.

---

## Scope Breakdown

| Layer | Required Work | Owner | Status |
|------|----------------|-------|--------|
| Backend (`engine/`) | Communication entities, repository, service, validators, endpoints, migration, task/timeline integration, tests. | Backend Developer | Pending G1 |
| Frontend (`experience/`) | Communication slice, panels/drawers, page integration, MSW fixtures, tests. | Frontend Developer | Pending G1 |
| AI (`neuron/`) | None. | N/A | Out of scope |
| Quality | Acceptance mapping, backend/frontend integration tests, evidence reports. | Quality Engineer | Pending implementation |
| Security | Redaction, authorization, no body content in timeline payloads, external-role denial, security scans/verdict. | Security Reviewer | Pending G3 |
| DevOps/Runtime | Runtime preflight only unless migrations/config create deployability risk. | DevOps optional | Pending G1 |

## Dependency Order

```
Step 0 (Architect):   G0 assembly plan and evidence validation
Step 1 (Backend):     entities, EF configuration, repository
Step 2 (Backend):     service, validators, timeline/task integration
Step 3 (Backend):     endpoints, OpenAPI alignment, backend tests
  ---- Backend checkpoint: API tests prove create/list/detail/follow-up/correction/redaction ----
Step 4 (Frontend):    communication slice, contextual panels, MSW handlers, frontend tests
  ---- Frontend checkpoint: page tests prove save/refetch/redaction/follow-up states ----
Step 5 (QE/Security): test plan, execution, coverage, security review, signoff
```

## Integration Checkpoints

### After Backend Service

- [ ] `POST /communications` creates communication, links, participants, and timeline projections atomically.
- [ ] Inaccessible related links return forbidden and create no data.
- [ ] Follow-up task failure rolls back communication create when submitted atomically.
- [ ] Redaction hides summary/body from normal list/detail/timeline read models.

### After Frontend Panel

- [ ] Add Communication form saves from at least broker and submission detail contexts.
- [ ] Communication list refetches and shows persisted record after reload/query invalidation.
- [ ] Follow-up task chip/status appears after task creation.
- [ ] Non-admin user cannot see redaction action; Admin redaction result shows redacted indicator.

### Cross-Story Verification

- [ ] Full lifecycle: capture communication → view history → add related records/participants → create follow-up → correct/redact.
- [ ] All communication policies enforced for internal roles and BrokerUser/ExternalUser denied.
- [ ] Timeline events for communication lifecycle are correct and contain no body content.
- [ ] ProblemDetails format consistent with existing endpoints.
- [ ] Security evidence confirms redacted free text does not leak through list/detail/timeline/mock surfaces.

## Integration Checklist

- [ ] API contract compatibility validated.
- [ ] Frontend contract compatibility validated.
- [ ] AI contract compatibility N/A.
- [ ] Test cases mapped to acceptance criteria.
- [ ] Developer-owned fast-test responsibilities identified by layer.
- [ ] Required runtime evidence artifacts identified in feature run folder.
- [ ] Framework vs solution boundary reviewed.
- [ ] Mutation traceability tables completed.
- [ ] Render-only tests are not used to close mutation stories.
- [ ] Run/deploy instructions updated if migration changes startup requirements.

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Linked-entity authorization helpers are inconsistent by entity type. | High | Centralize checks in `CommunicationService` with explicit per-entity helper methods and tests. | Backend Developer |
| Task follow-up cannot be represented without losing primary business record context. | Medium | Prefer `CommunicationFollowUpTaskLink`; keep `TaskItem.LinkedEntityType/Id` pointed at the business record. | Architect / Backend Developer |
| Redacted content leaks through cached frontend state or timeline payload. | High | Never embed body in timeline payload; refetch after redaction; add security tests. | Security Reviewer / Frontend Developer |
| OpenAPI and JSON schemas drift from DTO implementation. | Medium | Update planning schemas/OpenAPI and run schema/contract checks during G6. | Backend Developer / QE |

## JSON Serialization Convention

Use existing API camelCase JSON behavior. Date/time fields are UTC ISO-8601 strings. `rowVersion` follows existing DTO convention as a number when available. Email reference metadata is nullable and does not contain raw email body.

## DI Registration Changes

- Add `services.AddScoped<ICommunicationRepository, CommunicationRepository>();`
- Add `services.AddScoped<Nebula.Application.Services.CommunicationService>();`
- Add FluentValidation registrations if the project does not auto-discover validators.

## Casbin Policy Sync

F0021 policy rows already exist in `planning-mds/security/policies/policy.csv`. During implementation, verify the runtime policy source consumed by `CasbinAuthorizationService` includes the same `communication_event` rows.

## Knowledge-Graph Binding Plan

Expected G7 code-index additions:

- `entity:communication-event`
  - backend domain: `engine/src/Nebula.Domain/Entities/Communication*.cs`
  - application: `engine/src/Nebula.Application/DTOs/Communication*.cs`, `engine/src/Nebula.Application/Interfaces/ICommunicationRepository.cs`, `engine/src/Nebula.Application/Services/CommunicationService.cs`, `engine/src/Nebula.Application/Validators/Communication*.cs`
  - infrastructure: `engine/src/Nebula.Infrastructure/Repositories/CommunicationRepository.cs`, `engine/src/Nebula.Infrastructure/Persistence/Configurations/Communication*.cs`, `engine/src/Nebula.Infrastructure/Persistence/Migrations/*F0021*Communication*.cs`
  - api: `engine/src/Nebula.Api/Endpoints/CommunicationEndpoints.cs`
  - tests: `engine/tests/Nebula.Tests/**/Communication*.cs`
  - frontend: `experience/src/features/communications/**`

Expected canonical nodes already introduced in Phase B: `entity:communication-event`, `entity:communication-link`, `entity:communication-participant`, communication endpoints, communication schemas, ADR-029, and `communication_event:*` policy rules. G7 reconciles as-built paths and adjusts only if implementation diverges from this plan.
