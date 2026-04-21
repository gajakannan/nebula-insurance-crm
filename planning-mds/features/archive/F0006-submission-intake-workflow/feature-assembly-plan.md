# Feature Assembly Plan — F0006: Submission Intake Workflow

**Created:** 2026-03-27
**Author:** Architect Agent
**Status:** Approved

## Overview

F0006 builds the new business submission intake workflow — the primary entry point for all new business into Nebula CRM. The existing Submission entity, service, endpoints, DTOs, validators, and repository require significant expansion. The entity needs two new columns plus a nullability fix. The service needs Create, Update, List, Assign, and CompletenessEvaluation methods. Four new API endpoints are required; three existing endpoints need enhancement. The WorkflowStateMachine submission transitions must be aligned to the F0006 + F0019 state model.

This architect pass resolves four design drifts before implementation starts:

1. State-changing submission endpoints (`PUT /submissions/{id}`, `PUT /submissions/{id}/assignment`, `POST /submissions/{id}/transitions`) must use the platform `If-Match` / `rowVersion` precondition contract and return HTTP 412 `precondition_failed` on stale versions.
2. The submission detail response must expose `rowVersion` for those follow-up mutations, while the submission timeline remains a separately paged read via `GET /submissions/{id}/timeline`.
3. Completeness cannot remain a pure function of `Submission` alone because it must validate that the current assignee has the Underwriter role and it must integrate with an F0020 document-presence adapter when that feature is available.
4. The F0020 soft dependency must be represented as a null-object adapter in Application/Infrastructure rather than as hardcoded "always unavailable" logic inside `SubmissionService`.

## Build Order

| Step | Scope | Stories | Rationale |
|------|-------|---------|-----------|
| 1 | Migration: Add missing Submission columns, seed intake statuses and SLA thresholds | S0001–S0008 | Foundation — entity must be complete before service layer |
| 2 | Domain entity + catalog updates | S0001–S0008 | Submission.cs, OpportunityStatusCatalog, WorkflowStateMachine alignment |
| 3 | DTOs (rewritten + new) | S0001–S0007 | Create, detail response, list-item, assignment, list query, completeness DTOs |
| 4 | Validators (rewritten + new) | S0002, S0003, S0004, S0006 | Create, update, transition guard, assignment validators |
| 5 | Repository expansion | S0001–S0007 | AddAsync, ListAsync (paginated+filtered), GetByIdWithIncludesAsync |
| 6 | Service rewrite | S0001–S0008 | Create, Update, List, Transition (enhanced), Assign, Completeness, Stale |
| 7 | API endpoints (7 routes) | S0001–S0007 | Full SubmissionEndpoints rewrite with Casbin enforcement |
| 8 | ProblemDetailsHelper extensions | S0002, S0004, S0006 | New error helpers for F0006-specific codes |

## Existing Code (Must Be Modified)

| File | Current State | F0006 Change |
|------|---------------|--------------|
| `Nebula.Domain/Entities/Submission.cs` | 8 fields, 3 nav props | **Expand** — add Description, ExpirationDate; make PremiumEstimate nullable; add UserProfile nav prop |
| `Nebula.Application/DTOs/SubmissionDto.cs` | 13-param record | **Rewrite** — 25+ fields including denormalized names, completeness, availableTransitions, isStale |
| `Nebula.Application/DTOs/SubmissionCreateDto.cs` | 8-param record (server-set fields mixed in) | **Rewrite** — 8-param matching schema (remove CurrentStatus, AssignedToUserId; add Description, ExpirationDate; make PremiumEstimate nullable) |
| `Nebula.Application/DTOs/SubmissionUpdateDto.cs` | 6-param record | **Rewrite** — 6-param (remove CurrentStatus, AssignedToUserId; add Description, ExpirationDate) |
| `Nebula.Application/Validators/SubmissionCreateValidator.cs` | Basic field validation | **Rewrite** — Remove server-set field rules; add LOB validation, premium >= 0 |
| `Nebula.Application/Services/SubmissionService.cs` | GetById, GetTransitions, Transition | **Rewrite** — Add Create, Update, List, Assign, Completeness; rewrite Transition with completeness guards + role gates |
| `Nebula.Application/Interfaces/ISubmissionRepository.cs` | 2 methods | **Expand** — Add AddAsync, ListAsync, GetByIdWithIncludesAsync |
| `Nebula.Infrastructure/Repositories/SubmissionRepository.cs` | 2 methods | **Expand** — Implement new interface methods |
| `Nebula.Application/Interfaces/IReferenceDataRepository.cs` | List-only reference-data reads | **Expand** — Add `GetAccountByIdAsync` and `GetProgramByIdAsync` for point validation/lookups |
| `Nebula.Infrastructure/Repositories/ReferenceDataRepository.cs` | Cached list reads | **Expand** — Add point lookups without forcing SubmissionService to load full reference-data lists |
| `Nebula.Infrastructure/Persistence/Configurations/SubmissionConfiguration.cs` | Missing columns | **Expand** — Add Description, ExpirationDate columns; add AccountId/BrokerId indexes |
| `Nebula.Api/Endpoints/SubmissionEndpoints.cs` | 3 routes | **Rewrite** — 7 routes with Casbin enforcement |
| `Nebula.Application/Services/WorkflowStateMachine.cs` | Old 11-state submission map with WaitingOnDocuments | **Rewrite** — Align to F0006 intake + F0019 downstream states |
| `Nebula.Domain/Workflow/OpportunityStatusCatalog.cs` | 17 submission statuses | **Review** — Align with schema enum (10 states for F0006+F0019) |
| `Nebula.Api/Helpers/ProblemDetailsHelper.cs` | Missing F0006 error helpers | **Expand** — Add RegionMismatch, InvalidAccount, InvalidBroker, InvalidProgram, InvalidLob, InvalidAssignee (submission), MissingTransitionPrerequisite, PreconditionFailed |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `Nebula.Application/DTOs/SubmissionListItemDto.cs` | Application | Pipeline list item with computed isStale, denormalized names |
| `Nebula.Application/DTOs/SubmissionAssignmentRequestDto.cs` | Application | Assignment request |
| `Nebula.Application/DTOs/SubmissionListQuery.cs` | Application | List query parameters object |
| `Nebula.Application/DTOs/SubmissionCompletenessDto.cs` | Application | Completeness evaluation result |
| `Nebula.Application/DTOs/SubmissionFieldCheckDto.cs` | Application | Per-field completeness check |
| `Nebula.Application/DTOs/SubmissionDocumentCheckDto.cs` | Application | Per-document-category completeness check |
| `Nebula.Application/Validators/SubmissionAssignmentValidator.cs` | Application | Assignment request validation |
| `Nebula.Application/Interfaces/ISubmissionDocumentChecklistReader.cs` | Application | Soft-dependency adapter for F0020-backed document presence checks |
| `Nebula.Infrastructure/Services/UnavailableSubmissionDocumentChecklistReader.cs` | Infrastructure | Default null-object implementation while F0020 is not deployed |

---

## Step 1 — Migration: Expand Submissions Table

### Modified Files

| File | Change |
|------|--------|
| `Nebula.Infrastructure/Persistence/Configurations/SubmissionConfiguration.cs` | Add Description, ExpirationDate columns; new indexes |

### New Files

| File | Layer |
|------|-------|
| `Nebula.Infrastructure/Persistence/Migrations/*_F0006_SubmissionIntakeColumns.cs` | Infrastructure |

### Migration Changes

```
ADD columns:
  - Description          varchar(2000) NULL
  - ExpirationDate       date NULL

ALTER columns:
  - PremiumEstimate      decimal(18,2) NOT NULL → decimal(18,2) NULL

ADD indexes:
  - IX_Submissions_AccountId                    (AccountId)
  - IX_Submissions_BrokerId                     (BrokerId)
  - IX_Submissions_EffectiveDate                (EffectiveDate)
  - IX_Submissions_AssignedToUserId             (AssignedToUserId)  -- already exists as compound; add standalone
```

### Seed Data

Seed `WorkflowSlaThreshold` entries for submission intake states (if not already present). Reuse the existing table shape; F0006 does not add a per-LOB column to this table in MVP:

```sql
INSERT INTO "WorkflowSlaThresholds" ("Id", "EntityType", "Status", "WarningDays", "TargetDays")
VALUES
  (gen_random_uuid(), 'submission', 'Received',        1, 2),
  (gen_random_uuid(), 'submission', 'Triaging',        1, 2),
  (gen_random_uuid(), 'submission', 'WaitingOnBroker', 2, 3);
-- ReadyForUWReview: not stale in F0006 scope because intake ownership ends there
```

Seed 5–8 sample submissions across intake states for dev/test, linked to existing dev accounts and brokers.

### EF Core Configuration Update

```csharp
// Add to SubmissionConfiguration.Configure():
builder.Property(e => e.Description).HasMaxLength(2000);
builder.Property(e => e.ExpirationDate);
builder.Property(e => e.PremiumEstimate).HasPrecision(18, 2);  // remove .IsRequired()

builder.HasIndex(e => e.AccountId).HasDatabaseName("IX_Submissions_AccountId");
builder.HasIndex(e => e.BrokerId).HasDatabaseName("IX_Submissions_BrokerId");
builder.HasIndex(e => e.EffectiveDate).HasDatabaseName("IX_Submissions_EffectiveDate");
```

### Migration Command

```bash
cd engine/src/Nebula.Infrastructure
dotnet ef migrations add F0006_SubmissionIntakeColumns -s ../Nebula.Api
```

---

## Step 2 — Domain Entity + Catalog Updates

### Modified Files

| File | Change |
|------|--------|
| `Nebula.Domain/Entities/Submission.cs` | Add fields, nav prop |
| `Nebula.Domain/Workflow/OpportunityStatusCatalog.cs` | Align submission statuses to F0006+F0019 |
| `Nebula.Application/Services/WorkflowStateMachine.cs` | Rewrite submission transition map |

### Entity Update

```csharp
// Nebula.Domain/Entities/Submission.cs
namespace Nebula.Domain.Entities;

public class Submission : BaseEntity
{
    public Guid AccountId { get; set; }
    public Guid BrokerId { get; set; }
    public Guid? ProgramId { get; set; }
    public string? LineOfBusiness { get; set; }
    public string CurrentStatus { get; set; } = "Received";
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }       // NEW
    public decimal? PremiumEstimate { get; set; }        // CHANGED: nullable
    public string? Description { get; set; }             // NEW
    public Guid AssignedToUserId { get; set; }

    // Navigation
    public Account Account { get; set; } = default!;
    public Broker Broker { get; set; } = default!;
    public Program? Program { get; set; }
    public UserProfile AssignedToUser { get; set; } = default!;  // NEW
}
```

### OpportunityStatusCatalog — Submission Statuses

Align to the 10-state model (F0006 intake + F0019 downstream):

```csharp
public static readonly IReadOnlyList<WorkflowStatusDefinition> SubmissionStatuses =
[
    // F0006 intake states
    new("Received",         "Received",            "Initial state when submission is created",               false, 1,  "intake"),
    new("Triaging",         "Triaging",            "Initial triage and data validation",                     false, 2,  "triage"),
    new("WaitingOnBroker",  "Waiting on Broker",   "Awaiting additional information from broker",            false, 3,  "waiting"),
    new("ReadyForUWReview", "Ready for UW Review", "All data received, queued for underwriter",              false, 4,  "review"),
    // F0019 downstream states
    new("InReview",         "In Review",           "Under active underwriter review",                        false, 5,  "review"),
    new("Quoted",           "Quoted",              "Quote issued, awaiting broker response",                  false, 6,  "decision"),
    new("BindRequested",    "Bind Requested",      "Broker accepted quote, bind in progress",                false, 7,  "decision"),
    new("Bound",            "Bound",               "Policy bound and issued",                                true,  8,  "won"),
    new("Declined",         "Declined",            "Submission declined by underwriter",                     true,  9,  "lost"),
    new("Withdrawn",        "Withdrawn",           "Broker or insured withdrew submission",                  true,  10, "lost"),
];
```

### WorkflowStateMachine — Submission Transitions

```csharp
private static readonly Dictionary<string, HashSet<string>> SubmissionTransitions = new()
{
    // F0006 intake transitions
    ["Received"]        = ["Triaging"],
    ["Triaging"]        = ["WaitingOnBroker", "ReadyForUWReview"],
    ["WaitingOnBroker"] = ["ReadyForUWReview"],
    // F0019 downstream transitions (placeholder — F0019 will expand)
    ["ReadyForUWReview"] = ["InReview"],
    ["InReview"]         = ["Quoted", "Declined"],
    ["Quoted"]           = ["BindRequested", "Declined", "Withdrawn"],
    ["BindRequested"]    = ["Bound", "Withdrawn"],
};
```

### ReferenceSubmissionStatus Re-seed

The migration should re-seed to match the 10-state model:

```sql
DELETE FROM "ReferenceSubmissionStatuses";
INSERT INTO "ReferenceSubmissionStatuses" ("Code", "DisplayName", "Description", "IsTerminal", "DisplayOrder", "ColorGroup")
VALUES
  ('Received',         'Received',            'Initial state when submission is created',     false, 1,  'intake'),
  ('Triaging',         'Triaging',            'Initial triage and data validation',           false, 2,  'triage'),
  ('WaitingOnBroker',  'Waiting on Broker',   'Awaiting additional information from broker',  false, 3,  'waiting'),
  ('ReadyForUWReview', 'Ready for UW Review', 'All data received, queued for underwriter',   false, 4,  'review'),
  ('InReview',         'In Review',           'Under active underwriter review',              false, 5,  'review'),
  ('Quoted',           'Quoted',              'Quote issued, awaiting broker response',       false, 6,  'decision'),
  ('BindRequested',    'Bind Requested',      'Broker accepted quote, bind in progress',      false, 7,  'decision'),
  ('Bound',            'Bound',               'Policy bound and issued',                      true,  8,  'won'),
  ('Declined',         'Declined',            'Submission declined by underwriter',           true,  9,  'lost'),
  ('Withdrawn',        'Withdrawn',           'Broker or insured withdrew submission',        true,  10, 'lost');
```

> **Note:** Existing submission records with old statuses (WaitingOnDocuments, QuotePreparation, RequoteRequested, Binding, NotQuoted, Lost, Expired) must be mapped to new statuses in the migration Up(). If no production submissions exist yet, a simple DELETE + re-INSERT suffices.

---

## Step 3 — DTOs (Rewritten + New)

### SubmissionCreateDto (Rewrite)

```csharp
// Nebula.Application/DTOs/SubmissionCreateDto.cs
namespace Nebula.Application.DTOs;

public record SubmissionCreateDto(
    Guid AccountId,
    Guid BrokerId,
    DateTime EffectiveDate,
    Guid? ProgramId = null,
    string? LineOfBusiness = null,
    decimal? PremiumEstimate = null,
    DateTime? ExpirationDate = null,
    string? Description = null);
```

### SubmissionDto (Rewrite — Detail Response)

```csharp
// Nebula.Application/DTOs/SubmissionDto.cs
namespace Nebula.Application.DTOs;

public record SubmissionDto(
    Guid Id,
    Guid AccountId,
    Guid BrokerId,
    Guid? ProgramId,
    string? LineOfBusiness,
    string CurrentStatus,
    DateTime EffectiveDate,
    DateTime? ExpirationDate,
    decimal? PremiumEstimate,
    string? Description,
    Guid AssignedToUserId,
    // Denormalized display names
    string AccountName,
    string? AccountRegion,
    string? AccountIndustry,
    string BrokerName,
    string? BrokerLicenseNumber,
    string? ProgramName,
    string? AssignedToDisplayName,
    // Computed fields
    bool IsStale,
    SubmissionCompletenessDto Completeness,
    IReadOnlyList<string> AvailableTransitions,
    string RowVersion,
    // Audit
    DateTime CreatedAt,
    Guid CreatedByUserId,
    DateTime UpdatedAt,
    Guid UpdatedByUserId);
```

### SubmissionUpdateDto (Rewrite)

```csharp
// Nebula.Application/DTOs/SubmissionUpdateDto.cs
namespace Nebula.Application.DTOs;

public record SubmissionUpdateDto(
    Guid? ProgramId,
    string? LineOfBusiness,
    DateTime? EffectiveDate,
    DateTime? ExpirationDate,
    decimal? PremiumEstimate,
    string? Description);
```

### SubmissionListItemDto (New)

```csharp
// Nebula.Application/DTOs/SubmissionListItemDto.cs
namespace Nebula.Application.DTOs;

public record SubmissionListItemDto(
    Guid Id,
    string AccountName,
    string BrokerName,
    string? LineOfBusiness,
    string CurrentStatus,
    DateTime EffectiveDate,
    Guid AssignedToUserId,
    string? AssignedToDisplayName,
    DateTime CreatedAt,
    bool IsStale);
```

### SubmissionAssignmentRequestDto (New)

```csharp
// Nebula.Application/DTOs/SubmissionAssignmentRequestDto.cs
namespace Nebula.Application.DTOs;

public record SubmissionAssignmentRequestDto(Guid AssignedToUserId);
```

### SubmissionListQuery (New)

```csharp
// Nebula.Application/DTOs/SubmissionListQuery.cs
namespace Nebula.Application.DTOs;

public record SubmissionListQuery(
    string? Status = null,
    Guid? BrokerId = null,
    Guid? AccountId = null,
    string? LineOfBusiness = null,
    Guid? AssignedToUserId = null,
    bool? Stale = null,
    string Sort = "createdAt",
    string SortDir = "desc",
    int Page = 1,
    int PageSize = 25);
```

### SubmissionCompletenessDto (New)

```csharp
// Nebula.Application/DTOs/SubmissionCompletenessDto.cs
namespace Nebula.Application.DTOs;

public record SubmissionCompletenessDto(
    bool IsComplete,
    IReadOnlyList<SubmissionFieldCheckDto> FieldChecks,
    IReadOnlyList<SubmissionDocumentCheckDto> DocumentChecks,
    IReadOnlyList<string> MissingItems);

public record SubmissionFieldCheckDto(string Field, bool Required, string Status);
public record SubmissionDocumentCheckDto(string Category, bool Required, string Status);
```

---

## Step 4 — Validators (Rewritten + New)

### SubmissionCreateValidator (Rewrite)

```csharp
// Nebula.Application/Validators/SubmissionCreateValidator.cs
namespace Nebula.Application.Validators;

public class SubmissionCreateValidator : AbstractValidator<SubmissionCreateDto>
{
    public SubmissionCreateValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.BrokerId).NotEmpty();
        RuleFor(x => x.EffectiveDate).NotEmpty();
        RuleFor(x => x.PremiumEstimate)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PremiumEstimate.HasValue);
        RuleFor(x => x.LineOfBusiness)
            .Must(LineOfBusinessValidation.IsValid)
            .WithMessage(LineOfBusinessValidation.ErrorMessage)
            .When(x => x.LineOfBusiness != null);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description != null);
    }
}
```

### SubmissionUpdateValidator (Rewrite)

```csharp
// Nebula.Application/Validators/SubmissionUpdateValidator.cs
namespace Nebula.Application.Validators;

public class SubmissionUpdateValidator : AbstractValidator<SubmissionUpdateDto>
{
    public SubmissionUpdateValidator()
    {
        RuleFor(x => x.PremiumEstimate)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PremiumEstimate.HasValue);
        RuleFor(x => x.LineOfBusiness)
            .Must(LineOfBusinessValidation.IsValid)
            .WithMessage(LineOfBusinessValidation.ErrorMessage)
            .When(x => x.LineOfBusiness != null);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description != null);
    }
}
```

### SubmissionAssignmentValidator (New)

```csharp
// Nebula.Application/Validators/SubmissionAssignmentValidator.cs
namespace Nebula.Application.Validators;

public class SubmissionAssignmentValidator : AbstractValidator<SubmissionAssignmentRequestDto>
{
    public SubmissionAssignmentValidator()
    {
        RuleFor(x => x.AssignedToUserId).NotEmpty();
    }
}
```

---

## Step 5 — Repository Expansion

### ISubmissionRepository (Expand)

```csharp
// Nebula.Application/Interfaces/ISubmissionRepository.cs
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface ISubmissionRepository
{
    Task<Submission?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Submission?> GetByIdWithIncludesAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Submission submission, CancellationToken ct = default);
    Task UpdateAsync(Submission submission, CancellationToken ct = default);
    Task<PaginatedResult<Submission>> ListAsync(SubmissionListQuery query, CancellationToken ct = default);
}
```

### IReferenceDataRepository (Expand)

```csharp
public interface IReferenceDataRepository
{
    Task<IReadOnlyList<Account>> GetAccountsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MGA>> GetMgasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Program>> GetProgramsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ReferenceSubmissionStatus>> GetSubmissionStatusesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ReferenceRenewalStatus>> GetRenewalStatusesAsync(CancellationToken ct = default);
    Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken ct = default);   // NEW
    Task<Program?> GetProgramByIdAsync(Guid id, CancellationToken ct = default);   // NEW
}
```

### SubmissionRepository (Expand)

```csharp
// Key implementation notes for SubmissionRepository:

// GetByIdWithIncludesAsync — eager-load Account, Broker, Program, AssignedToUser:
await db.Submissions
    .Include(s => s.Account)
    .Include(s => s.Broker)
    .Include(s => s.Program)
    .Include(s => s.AssignedToUser)
    .FirstOrDefaultAsync(s => s.Id == id, ct);

// AddAsync:
await db.Submissions.AddAsync(submission, ct);
// Note: SaveChangesAsync called by service (unit of work pattern)

// ListAsync — build IQueryable with filters:
// 1. Status filter: split comma-separated, WHERE CurrentStatus IN (...)
// 2. BrokerId, AccountId, AssignedToUserId: exact match
// 3. LineOfBusiness: exact match
// 4. Stale: computed at query time (see Step 6 stale logic)
// 5. Sort: switch on sort field, apply OrderBy/OrderByDescending
// 6. Include Account, Broker, AssignedToUser for denormalized names
// 7. Apply Skip/Take for pagination
// 8. Return PaginatedResult<Submission>
```

### ISubmissionDocumentChecklistReader (New)

```csharp
public interface ISubmissionDocumentChecklistReader
{
    Task<IReadOnlyList<SubmissionDocumentCheckDto>> GetChecklistAsync(Guid submissionId, CancellationToken ct = default);
}
```

Default implementation while F0020 is unavailable:

```csharp
public sealed class UnavailableSubmissionDocumentChecklistReader : ISubmissionDocumentChecklistReader
{
    public Task<IReadOnlyList<SubmissionDocumentCheckDto>> GetChecklistAsync(Guid submissionId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SubmissionDocumentCheckDto>>(
        [
            new("Application", true, "unavailable"),
            new("Supporting Document", true, "unavailable"),
        ]);
}
```

---

## Step 6 — Service Rewrite

### SubmissionService Constructor

```csharp
public class SubmissionService(
    ISubmissionRepository submissionRepo,
    IWorkflowTransitionRepository transitionRepo,
    ITimelineRepository timelineRepo,
    IBrokerRepository brokerRepo,
    IReferenceDataRepository referenceDataRepo,
    IUserProfileRepository userProfileRepo,
    ISubmissionDocumentChecklistReader submissionDocumentChecklistReader,
    IUnitOfWork unitOfWork)
```

### 6a — CreateAsync (S0002)

```
CreateAsync(SubmissionCreateDto dto, ICurrentUserService user, CancellationToken ct) → SubmissionDto
```

1. Validate Account exists and is not soft-deleted via `referenceDataRepo.GetAccountByIdAsync(dto.AccountId, ct)` → error `invalid_account` (400)
2. Validate Broker exists, not soft-deleted, status=Active → error `invalid_broker` (400)
3. Region alignment: load Account.Region, load BrokerRegions for BrokerId, check Account.Region ∈ BrokerRegions → error `region_mismatch` (400)
4. If ProgramId provided: validate Program exists and is not soft-deleted via `referenceDataRepo.GetProgramByIdAsync(dto.ProgramId.Value, ct)` → error `invalid_program` (400)
5. If LineOfBusiness provided: validate against known LOB set → error `invalid_lob` (400)
6. Compute ExpirationDate: if null, default to EffectiveDate + 12 months
7. Create Submission entity:
   - `CurrentStatus = "Received"`
   - `AssignedToUserId = user.UserId` (creator is initial owner)
   - `CreatedAt = DateTime.UtcNow`, `CreatedByUserId = user.UserId`
   - `UpdatedAt = now`, `UpdatedByUserId = user.UserId`
8. `submissionRepo.AddAsync(submission, ct)`
9. Append WorkflowTransition: `WorkflowType="Submission"`, `FromState=null`, `ToState="Received"`, `ActorUserId=user.UserId`, `OccurredAt=now`
10. Append ActivityTimelineEvent: `EntityType="Submission"`, `EventType="SubmissionCreated"`, payload per `activity-event-payloads.schema.json#SubmissionCreated`
11. `unitOfWork.CommitAsync(ct)` — atomic: submission + transition + timeline event
12. Return mapped SubmissionDto (reload with includes for denormalized names)

### Casbin Enforcement (Create)

- Resource: `submission`, Action: `create`
- Hydrate attrs: `{ subjectId = user.UserId }`
- Policy condition: `true` (all roles with create permission pass; query-layer scope N/A for create)
- Enforcement: check at endpoint level before calling service

### Timeline Event (SubmissionCreated)

- EventType: `SubmissionCreated`
- EntityType: `Submission`, EntityId: `submission.Id`
- EventDescription: `$"Submission created for {account.Name} via {broker.LegalName}"`
- BrokerDescription: `null` (InternalOnly)
- EventPayloadJson: per `activity-event-payloads.schema.json` → `SubmissionCreated`

### HTTP Responses (POST /submissions)

| Status | Body | Condition |
|--------|------|-----------|
| 201 Created | `SubmissionDto` | Success |
| 400 | ProblemDetails (`validation_error`) | Schema validation failure |
| 400 | ProblemDetails (`invalid_account`) | Account not found or soft-deleted |
| 400 | ProblemDetails (`invalid_broker`) | Broker not found, soft-deleted, or inactive |
| 400 | ProblemDetails (`region_mismatch`) | Account.Region not in broker's BrokerRegion set |
| 400 | ProblemDetails (`invalid_program`) | Program not found or soft-deleted |
| 400 | ProblemDetails (`invalid_lob`) | LOB not in known set |
| 403 | ProblemDetails (`policy_denied`) | Casbin deny |

---

### 6b — GetByIdAsync (S0003)

```
GetByIdAsync(Guid id, ICurrentUserService user, CancellationToken ct) → SubmissionDto?
```

1. `submissionRepo.GetByIdWithIncludesAsync(id, ct)` → if null, return null (404 at endpoint)
2. Compute `isStale` (see 6g)
3. Evaluate completeness via `EvaluateCompletenessAsync(submission, ct)` (see 6f)
4. Compute `availableTransitions` for the user's role and current state
5. Map to SubmissionDto with denormalized names from included navigation properties and `RowVersion`

### Casbin Enforcement (GetById)

- Resource: `submission`, Action: `read`
- Enforcement: at endpoint. ABAC scope: DistributionUser sees own (assigned); DistributionManager sees region; Underwriter sees assigned-only in F0006 scope; Admin sees all.

### HTTP Responses (GET /submissions/{id})

| Status | Body | Condition |
|--------|------|-----------|
| 200 OK | `SubmissionDto` | Success |
| 403 | ProblemDetails (`policy_denied`) | Casbin deny |
| 404 | ProblemDetails (`not_found`) | Not found or ABAC-hidden |

---

### 6c — UpdateAsync (S0003)

```
UpdateAsync(Guid id, SubmissionUpdateDto dto, uint expectedRowVersion, ICurrentUserService user, CancellationToken ct) → (SubmissionDto?, string?)
```

1. Load submission with includes → if null, return (null, "not_found")
2. Compare `submission.RowVersion` to `expectedRowVersion` → if mismatch, return (null, "precondition_failed")
3. If LineOfBusiness provided: validate against known LOB set → error `invalid_lob`
4. If ProgramId provided: validate exists and not soft-deleted via `referenceDataRepo.GetProgramByIdAsync` → error `invalid_program`
5. Track changed fields (build `changedFields` map with before/after)
6. Apply non-null fields from dto to submission
7. Set `UpdatedAt = now`, `UpdatedByUserId = user.UserId`
8. If any fields changed, append ActivityTimelineEvent: `EventType="SubmissionUpdated"`, payload with `changedFields`
9. `unitOfWork.CommitAsync(ct)` — catches `DbUpdateConcurrencyException` → return (null, "precondition_failed")
10. Return mapped SubmissionDto

### Casbin Enforcement (Update)

- Resource: `submission`, Action: `update`
- Enforcement: at endpoint level

### HTTP Responses (PUT /submissions/{id})

| Status | Body | Condition |
|--------|------|-----------|
| 200 OK | `SubmissionDto` | Success |
| 400 | ProblemDetails (`validation_error`) | Schema validation failure |
| 400 | ProblemDetails (`invalid_lob`) | LOB not in known set |
| 400 | ProblemDetails (`invalid_program`) | Program not found or soft-deleted |
| 403 | ProblemDetails (`policy_denied`) | Casbin deny |
| 404 | ProblemDetails (`not_found`) | Not found |
| 412 | ProblemDetails (`precondition_failed`) | Stale or missing `If-Match` precondition |

---

### 6d — TransitionAsync (S0004 — Enhanced)

```
TransitionAsync(Guid id, WorkflowTransitionRequestDto dto, uint expectedRowVersion, ICurrentUserService user, CancellationToken ct) → (WorkflowTransitionRecordDto?, string?)
```

1. Load submission with includes → if null, return (null, "not_found")
2. Compare `submission.RowVersion` to `expectedRowVersion` → if mismatch, return (null, "precondition_failed")
3. Validate transition allowed: `WorkflowStateMachine.IsValidTransition("Submission", current, dto.ToState)` → error `invalid_transition`
4. **Intake role gate** (application-layer, not Casbin):
   - Received→Triaging: DistributionUser, DistributionManager, Admin
   - Triaging→WaitingOnBroker: DistributionUser, DistributionManager, Admin
   - Triaging→ReadyForUWReview: DistributionUser, DistributionManager, Admin
   - WaitingOnBroker→ReadyForUWReview: DistributionUser, DistributionManager, Admin
   - Downstream (F0019): Underwriter, Admin (placeholder)
   - If user's role not in allowed set → return (null, "policy_denied")
5. **Completeness guard** (transitions to ReadyForUWReview only):
   - Evaluate completeness via `EvaluateCompletenessAsync(submission, ct)` (see 6f)
   - If `!completeness.IsComplete` → return (null, "missing_transition_prerequisite") with detail listing missing items
6. `var now = DateTime.UtcNow;`
7. Create WorkflowTransition: `WorkflowType="Submission"`, `EntityId=submission.Id`, `FromState=current`, `ToState=dto.ToState`, `Reason=dto.Reason`, `ActorUserId=user.UserId`, `OccurredAt=now`
8. Update submission: `CurrentStatus=dto.ToState`, `UpdatedAt=now`, `UpdatedByUserId=user.UserId`
9. Append ActivityTimelineEvent: `EventType="SubmissionTransitioned"`, payload per schema
10. `transitionRepo.AddAsync(transition, ct)`, `submissionRepo.UpdateAsync(submission, ct)`, `timelineRepo.AddEventAsync(event, ct)`
11. `unitOfWork.CommitAsync(ct)` — catches concurrency → return (null, "precondition_failed")
12. Return mapped WorkflowTransitionRecordDto

### Casbin Enforcement (Transition)

- Resource: `submission`, Action: `transition`
- Casbin gates broad action; application layer gates per-transition role (step 4 above)

### Timeline Event (SubmissionTransitioned)

- EventType: `SubmissionTransitioned`
- Payload: `{ fromState, toState, reason }`
- EventDescription: `$"Status changed from {from} to {to}"`

### HTTP Responses (POST /submissions/{id}/transitions)

| Status | Body | Condition |
|--------|------|-----------|
| 201 Created | `WorkflowTransitionRecordDto` | Success |
| 400 | ProblemDetails (`validation_error`) | Schema validation failure |
| 403 | ProblemDetails (`policy_denied`) | Casbin deny or role gate failure |
| 404 | ProblemDetails (`not_found`) | Not found |
| 409 | ProblemDetails (`invalid_transition`) | Disallowed transition pair |
| 409 | ProblemDetails (`missing_transition_prerequisite`) | Completeness or assignment guard failed |
| 412 | ProblemDetails (`precondition_failed`) | Stale or missing `If-Match` precondition |

---

### 6e — AssignAsync (S0006)

```
AssignAsync(Guid id, SubmissionAssignmentRequestDto dto, uint expectedRowVersion, ICurrentUserService user, CancellationToken ct) → (SubmissionDto?, string?)
```

1. Load submission with includes → if null, return (null, "not_found")
2. Compare `submission.RowVersion` to `expectedRowVersion` → if mismatch, return (null, "precondition_failed")
3. If `dto.AssignedToUserId == submission.AssignedToUserId` → no-op, return current SubmissionDto
4. Validate target user exists, is active → error `invalid_assignee`
5. If submission is in ReadyForUWReview: validate target user has Underwriter role → error `invalid_assignee` with detail
6. Track previous assignee for timeline event
7. `submission.AssignedToUserId = dto.AssignedToUserId`
8. `submission.UpdatedAt = now`, `submission.UpdatedByUserId = user.UserId`
9. Append ActivityTimelineEvent: `EventType="SubmissionAssigned"`, payload per schema
10. `unitOfWork.CommitAsync(ct)` — catches concurrency → return (null, "precondition_failed")
11. Reload with includes, return mapped SubmissionDto

### Casbin Enforcement (Assign)

- Resource: `submission`, Action: `assign`
- DistributionUser: can assign own submissions only (application-layer check)
- DistributionManager: can assign any in region scope
- Admin: can assign any

### Timeline Event (SubmissionAssigned)

- EventType: `SubmissionAssigned`
- Payload: `{ previousAssigneeUserId, previousAssigneeName, newAssigneeUserId, newAssigneeName, assignedByUserId }`
- EventDescription: `$"Reassigned from \"{previousName}\" to \"{newName}\""`

### HTTP Responses (PUT /submissions/{id}/assignment)

| Status | Body | Condition |
|--------|------|-----------|
| 200 OK | `SubmissionDto` | Success |
| 400 | ProblemDetails (`validation_error`) | Schema validation failure |
| 400 | ProblemDetails (`invalid_assignee`) | Target user not found, inactive, or wrong role |
| 403 | ProblemDetails (`policy_denied`) | Casbin deny |
| 404 | ProblemDetails (`not_found`) | Not found |
| 412 | ProblemDetails (`precondition_failed`) | Stale or missing `If-Match` precondition |

---

### 6f — EvaluateCompleteness (S0005)

```
EvaluateCompletenessAsync(Submission submission, CancellationToken ct) → SubmissionCompletenessDto
```

This is an application-level evaluator. It reads assignee role information from `IUserProfileRepository` and document-category presence from `ISubmissionDocumentChecklistReader`.

1. **Field checks** (required for ReadyForUWReview transition):
   - `AccountId`: status = submission.AccountId != Guid.Empty ? "pass" : "missing"
   - `BrokerId`: status = submission.BrokerId != Guid.Empty ? "pass" : "missing"
   - `EffectiveDate`: status = submission.EffectiveDate != default ? "pass" : "missing"
   - `LineOfBusiness`: status = !string.IsNullOrEmpty(submission.LineOfBusiness) ? "pass" : "missing"
   - `AssignedToUserId`: load `userProfileRepo.GetByIdAsync(submission.AssignedToUserId, ct)`; status = pass only when an active user exists and their `RolesJson` contains `Underwriter`
2. **Document checks** (soft dependency on F0020):
   - `var documentChecks = await submissionDocumentChecklistReader.GetChecklistAsync(submission.Id, ct);`
   - Default F0006 implementation returns `Application` + `Supporting Document` as `unavailable`
   - F0020 replaces the implementation with category presence checks from document metadata without changing the F0006 controller/service contract
3. **MissingItems**: collect human-readable strings for all `missing` field checks and any `missing` document categories; exclude `unavailable` document rows
4. **IsComplete**: true only when all required field checks pass AND all document checks are either `pass` or `unavailable`
5. **UI note:** when every required document check is `unavailable`, the detail view renders the section banner "Document management not yet configured"

---

### 6g — ComputeIsStale (S0008)

```
ComputeIsStale(Submission submission, DateTime now) → bool
```

1. If `CurrentStatus` is terminal (Bound, Declined, Withdrawn) or ReadyForUWReview → return false
2. Load WorkflowSlaThreshold for `EntityType="submission"` and `Status=submission.CurrentStatus`
3. If no threshold found → return false
4. Compute last transition timestamp: query `WorkflowTransition` for this submission, order by OccurredAt desc, take first → `lastTransitionAt`
5. If `lastTransitionAt` is null → use `submission.CreatedAt` as reference
6. `hoursInState = (now - referenceTimestamp).TotalHours`
7. Return `hoursInState > threshold.TargetDays * 24`

> **Performance note:** For the list endpoint, stale computation should be batched. Pre-load thresholds once per request; use the last WorkflowTransition per submission (a single query with window function or join). Avoid N+1.

---

### 6h — ListAsync (S0001)

```
ListAsync(SubmissionListQuery query, ICurrentUserService user, CancellationToken ct) → PaginatedResult<SubmissionListItemDto>
```

1. Build base IQueryable with includes (Account, Broker, AssignedToUser)
2. **ABAC scope filter** (query layer):
   - DistributionUser: `.Where(s => s.AssignedToUserId == user.UserId)`
   - DistributionManager: `.Where(s => s.Account.Region != null && user.Regions.Contains(s.Account.Region))`
   - Underwriter: `.Where(s => s.AssignedToUserId == user.UserId)` (assigned submissions only in F0006 intake scope)
   - RelationshipManager, ProgramManager: scope via managed broker/program (deferred to query helper)
   - Admin: no filter
3. Apply query filters (status, brokerId, accountId, LOB, assignedToUserId)
4. If `query.Stale == true`: filter to only stale submissions (apply stale computation in query)
5. Apply sort + pagination
6. Map results to SubmissionListItemDto with isStale computed
7. Return PaginatedResult

### Casbin Enforcement (List)

- Resource: `submission`, Action: `read`
- Enforcement: at endpoint. ABAC scope applied at query layer (step 2).

### HTTP Responses (GET /submissions)

| Status | Body | Condition |
|--------|------|-----------|
| 200 OK | `PaginatedResult<SubmissionListItemDto>` | Success |
| 400 | ProblemDetails (`validation_error`) | Invalid query params |
| 403 | ProblemDetails (`policy_denied`) | Casbin deny |

---

### 6i — Timeline (S0007)

Timeline reading reuses the existing `ITimelineRepository.ListEventsPagedAsync`.

### HTTP Responses (GET /submissions/{id}/timeline)

| Status | Body | Condition |
|--------|------|-----------|
| 200 OK | `PaginatedResult<ActivityTimelineEventDto>` | Success |
| 403 | ProblemDetails (`policy_denied`) | Casbin deny |
| 404 | ProblemDetails (`not_found`) | Submission not found |

---

## Step 7 — API Endpoints (7 Routes)

### SubmissionEndpoints (Full Rewrite)

```csharp
// Nebula.Api/Endpoints/SubmissionEndpoints.cs
public static class SubmissionEndpoints
{
    public static IEndpointRouteBuilder MapSubmissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/submissions")
            .WithTags("Submissions")
            .RequireAuthorization();

        group.MapGet("/", ListSubmissions);                              // S0001
        group.MapPost("/", CreateSubmission);                            // S0002
        group.MapGet("/{submissionId:guid}", GetSubmission);             // S0003
        group.MapPut("/{submissionId:guid}", UpdateSubmission);          // S0003
        group.MapPost("/{submissionId:guid}/transitions", PostTransition); // S0004
        group.MapPut("/{submissionId:guid}/assignment", AssignSubmission); // S0006
        group.MapGet("/{submissionId:guid}/timeline", GetTimeline);      // S0007

        return app;
    }
    // ... handler methods with Casbin enforcement pattern:
    // 1. Resolve IAuthorizationService + ICurrentUserService from DI
    // 2. Check user.Roles for at least one matching Casbin policy
    // 3. If denied → ProblemDetailsHelper.Forbidden()
    // 4. For update / assignment / transition: parse `If-Match`, convert to uint rowVersion, fail with PreconditionFailed() when stale
    // 5. Call service method
    // 6. Map result/error to HTTP response
}
```

### Endpoint → Casbin Mapping

| Endpoint | Casbin Resource | Casbin Action | Scope Layer |
|----------|-----------------|---------------|-------------|
| GET /submissions | submission | read | Query-layer ABAC |
| POST /submissions | submission | create | Endpoint check |
| GET /submissions/{id} | submission | read | Endpoint check + query filter |
| PUT /submissions/{id} | submission | update | Endpoint check |
| POST /submissions/{id}/transitions | submission | transition | Endpoint check + app-layer role gate |
| PUT /submissions/{id}/assignment | submission | assign | Endpoint check + app-layer scope check |
| GET /submissions/{id}/timeline | submission | read | Inherits submission:read scope |

---

## Step 8 — ProblemDetailsHelper Extensions

### New Helpers

```csharp
// Add to ProblemDetailsHelper.cs:

public static IResult RegionMismatch() => Results.Problem(
    title: "Region mismatch",
    detail: "Account region is not in the broker's licensed region set.",
    statusCode: 400,
    extensions: Ext("region_mismatch"));

public static IResult InvalidAccount(Guid id) => Results.Problem(
    title: "Invalid account",
    detail: $"Account {id} does not exist or is soft-deleted.",
    statusCode: 400,
    extensions: Ext("invalid_account"));

public static IResult InvalidBroker(Guid id) => Results.Problem(
    title: "Invalid broker",
    detail: $"Broker {id} does not exist, is soft-deleted, or is inactive.",
    statusCode: 400,
    extensions: Ext("invalid_broker"));

public static IResult InvalidProgram(Guid id) => Results.Problem(
    title: "Invalid program",
    detail: $"Program {id} does not exist or is soft-deleted.",
    statusCode: 400,
    extensions: Ext("invalid_program"));

public static IResult InvalidLob(string lob) => Results.Problem(
    title: "Invalid line of business",
    detail: $"'{lob}' is not in the known line of business set.",
    statusCode: 400,
    extensions: Ext("invalid_lob"));

public static IResult InvalidSubmissionAssignee(string detail) => Results.Problem(
    title: "Invalid assignee",
    detail: detail,
    statusCode: 400,
    extensions: Ext("invalid_assignee"));

public static IResult MissingTransitionPrerequisite(IReadOnlyList<string> missingItems) => Results.Problem(
    title: "Missing transition prerequisite",
    detail: $"Cannot transition: {string.Join("; ", missingItems)}",
    statusCode: 409,
    extensions: new Dictionary<string, object?>
    {
        ["code"] = "missing_transition_prerequisite",
        ["missingItems"] = missingItems,
        ["traceId"] = System.Diagnostics.Activity.Current?.Id,
    });

public static IResult PreconditionFailed() => Results.Problem(
    title: "Precondition failed",
    detail: "The submission was modified by another user. Refresh the detail view and retry with the current rowVersion.",
    statusCode: 412,
    extensions: Ext("precondition_failed"));
```

---

## Scope Breakdown

| Layer | Required Work | Owner | Status |
|-------|---------------|-------|--------|
| Backend (`engine/`) | Migration, entity, DTOs, validators, repository, service, endpoints, error helpers | Backend Developer | Not Started |
| Frontend (`experience/`) | Feature slice `features/submissions/`: list, detail, create, assignment, timeline pages; API hooks; dashboard nudge card | Frontend Developer | Not Started |
| AI (`neuron/`) | None in F0006 MVP | — | N/A |
| Quality | Workflow transition matrix, completeness guards, ABAC scoping, stale detection, E2E lifecycle | Quality Engineer | Not Started |
| DevOps/Runtime | Smoke test additions for submission endpoints | DevOps | Not Started |

## Dependency Order

```
Step 0 (Architect):   architecture review + spec finalization ← DONE
Step 1 (Backend):     migration — add columns, seed statuses/thresholds
Step 2 (Backend):     domain entity + catalog + state machine updates
Step 3 (Backend):     DTOs (rewrite + new)
Step 4 (Backend):     validators (rewrite + new)
Step 5 (Backend):     repository expansion
Step 6 (Backend):     service rewrite (create, update, list, transition, assign, completeness, stale)
Step 7 (Backend):     API endpoints (7 routes with Casbin)
Step 8 (Backend):     ProblemDetailsHelper extensions
  ──── Backend checkpoint: all 7 endpoints passing smoke tests ────
Step 9 (Frontend):    feature slice + API hooks
Step 10 (Frontend):   pipeline list with filters, sort, pagination
Step 11 (Frontend):   detail view with completeness panel, timeline, action bar
Step 12 (Frontend):   create submission form with region validation
Step 13 (Frontend):   dashboard stale-submission nudge card
  ──── Frontend checkpoint: full UI flow verified ────
Step 14 (QE):         comprehensive test coverage
```

## Integration Checkpoints

### After Step 8 (Backend)

- [ ] `POST /submissions` creates submission in Received status with auto-assignment to creator
- [ ] `POST /submissions` rejects region mismatch, invalid account, invalid broker, invalid program, invalid LOB
- [ ] `GET /submissions` returns paginated list filtered by status, broker, account, LOB, assigned user, stale
- [ ] `GET /submissions` enforces ABAC scope (DistributionUser sees own; DistributionManager sees region; Admin sees all)
- [ ] `GET /submissions/{id}` returns denormalized detail with completeness, available transitions, isStale
- [ ] `GET /submissions/{id}` includes `rowVersion` and omits paginated timeline payloads
- [ ] `PUT /submissions/{id}` updates mutable fields, emits SubmissionUpdated timeline event, and requires `If-Match`
- [ ] `POST /submissions/{id}/transitions` enforces state machine (only 4 intake transitions allowed)
- [ ] `POST /submissions/{id}/transitions` enforces completeness guard on →ReadyForUWReview, including underwriter-role validation inside completeness evaluation
- [ ] `PUT /submissions/{id}/assignment` validates target user, enforces underwriter requirement in ReadyForUWReview, and requires `If-Match`
- [ ] `GET /submissions/{id}/timeline` returns paginated timeline events for the submission
- [ ] Stale `If-Match` headers return HTTP 412 `precondition_failed` across update, assignment, and transition endpoints
- [ ] All error responses use RFC 7807 ProblemDetails with correct codes and traceId

### After Step 13 (Frontend)

- [ ] Pipeline list renders with working filters, sort, pagination
- [ ] Detail view shows completeness panel, timeline, transition buttons filtered by state+role
- [ ] Create submission form validates fields, shows region mismatch error inline
- [ ] Assignment picker uses existing user search (F0004 pattern)
- [ ] Dashboard nudge card shows stale submission count

### Cross-Story Verification

- [ ] Full lifecycle: Create → Triage → WaitOnBroker → ReadyForUWReview (with completeness passing)
- [ ] All Casbin policies enforced (6 roles + ExternalUser denied)
- [ ] Timeline events for full lifecycle are correct and ordered: SubmissionCreated, SubmissionTransitioned (×3), SubmissionAssigned, SubmissionUpdated
- [ ] ProblemDetails format consistent with existing endpoints (code + traceId)
- [ ] Stale detection: submission in Received for >48h shows isStale=true on list and detail
- [ ] Completeness guard: attempt Triaging→ReadyForUWReview without LOB → 409 with missing items
- [ ] Concurrency contract: stale `If-Match` on update, assignment, or transition returns 412 `precondition_failed`

## Integration Checklist

- [x] API contract compatibility validated (nebula-api.yaml: 7 endpoints match assembly steps)
- [x] JSON Schemas validated (7 schemas: submission, create-request, update-request, list-item, completeness, transition-request, assignment-request)
- [x] Casbin policies validated (policy.csv §2.3: read, create, update, transition, assign)
- [x] Error codes validated (error-codes.md: 9 F0006-relevant codes)
- [x] Timeline events validated (activity-event-payloads.schema.json: SubmissionCreated, SubmissionTransitioned, SubmissionAssigned, SubmissionUpdated)
- [x] Data model validated (data-model.md ERD matches entity spec)
- [ ] Frontend contract compatibility validated (pending frontend implementation)
- [ ] AI contract compatibility validated — N/A
- [ ] Test cases mapped to acceptance criteria
- [ ] Developer-owned fast-test responsibilities identified by layer
- [ ] Required runtime evidence artifacts identified
- [x] Framework vs solution boundary reviewed — all changes are in `engine/` and `experience/`, no `agents/**` drift

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Point lookup APIs for account/program validation are missing from `IReferenceDataRepository` | Medium | Add `GetAccountByIdAsync` and `GetProgramByIdAsync`; do not force SubmissionService to hydrate full cached reference-data lists for validation | Backend |
| WorkflowStateMachine has old states (WaitingOnDocuments, QuotePreparation, etc.) | Medium | Step 2 rewrites submission transitions to 10-state model; migration maps any existing records | Backend |
| Stale detection N+1 on list endpoint | Medium | Batch-load last transitions per submission via window function; pre-load thresholds once per request | Backend |
| F0020 (Document Management) not available for completeness document checks | Low | Use `UnavailableSubmissionDocumentChecklistReader` null-object implementation until F0020 provides a real metadata-backed adapter | Backend |
| ReferenceSubmissionStatus re-seed breaks existing records | Medium | Migration Up() must map old→new statuses before DELETE. If no production data, simple re-INSERT suffices | Backend |

## JSON Serialization Convention

- All property names: camelCase (System.Text.Json default)
- Dates: ISO 8601 (`yyyy-MM-dd` for date fields, `yyyy-MM-ddTHH:mm:ssZ` for datetime)
- UUIDs: lowercase string with hyphens
- Nulls: included in response (not omitted)

## DI Registration Changes

No new repository registrations are required beyond the existing submission/reference-data wiring. Add the F0020 soft-dependency default explicitly:

```csharp
services.AddScoped<ISubmissionDocumentChecklistReader, UnavailableSubmissionDocumentChecklistReader>();
```

## Casbin Policy Sync

Casbin policies for submission (§2.3 in policy.csv) are already updated with `create`, `update`, `assign` actions. No further policy.csv changes needed for F0006. Reminder: copy `planning-mds/security/policies/policy.csv` to the embedded resource location if the runtime reads from embedded resources.
