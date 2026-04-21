# Feature Assembly Plan — F0007: Renewal Pipeline

**Created:** 2026-03-26
**Last Refined:** 2026-04-11
**Author:** Architect Agent
**Status:** Approved (refined)

## Overview

F0007 redesigns the Renewal entity for policy-linked lifecycle tracking with 6 workflow states, role-gated transitions, configurable per-LOB timing windows, and ownership handoff. The existing Renewal entity, service, endpoints, DTOs, validator, repository, and configuration must all be restructured. The WorkflowStateMachine and OpportunityStatusCatalog must be updated to reflect the new 6-status model.

### Implementation Drift Note (2026-04-11)

The implementation is aligned to the shipped contracts, not the future-state Policy 360 experience. `planning-mds/api/nebula-api.yaml` currently exposes renewal create/list/detail/assignment/transition/timeline endpoints, but it does not yet expose a policy search/read picker endpoint for the UI. F0007 therefore implements renewal creation with direct `PolicyId` entry backed by the Policy stub delivered in this change set. If F0018 expands shared policy semantics later, that richer picker flow should land there and the ontology/KG mappings should be updated in the same change set.

## Build Order

| Step | Scope | Stories | Rationale |
|------|-------|---------|-----------|
| 1 | Migration 006: Policy stub table | S0006 | F0007 requires PolicyId FK — stub must exist before Renewal restructure |
| 2 | Migration 007: Restructure Renewals table | S0001–S0007 | Foundation — drops old columns, adds new, re-seeds statuses, creates filtered unique index |
| 3 | Migration 008: WorkflowSlaThreshold per-LOB extension | S0001, S0005 | Timing windows for overdue/approaching detection |
| 4 | Domain entity + catalog updates | S0001–S0007 | Renewal.cs, Policy.cs, OpportunityStatusCatalog, WorkflowStateMachine |
| 5 | DTOs (rewritten + new) | S0001–S0007 | New create, response, list-item, transition, assignment DTOs |
| 6 | Validators (rewritten + new) | S0003, S0004, S0006 | Create, transition (conditional), assignment validators |
| 7 | Repository expansion | S0001–S0007 | IRenewalRepository + RenewalRepository with list, create, urgency, active-per-policy check |
| 8 | Service rewrite | S0001–S0007 | RenewalService with create, transition (role-gated + conditional), assignment, list, detail |
| 9 | API endpoints (6 routes) | S0001–S0007 | Full RenewalEndpoints rewrite with Casbin enforcement |
| 10 | ProblemDetailsHelper + DI | S0003, S0006 | New error helpers; no new DI registrations needed |

## Existing Code (Must Be Restructured)

| File | Current State | F0007 Change |
|------|---------------|--------------|
| `Nebula.Domain/Entities/Renewal.cs` | 7 fields, 3 nav props | **Rewrite** — 14+ fields, 5 nav props, new FKs |
| `Nebula.Application/DTOs/RenewalDto.cs` | 12-param record | **Rewrite** — 20+ fields including computed urgency, availableTransitions |
| `Nebula.Application/DTOs/RenewalCreateDto.cs` | 7-param record (AccountId, BrokerId, etc.) | **Rewrite** — 3-param (PolicyId, AssignedToUserId?, LineOfBusiness?) |
| `Nebula.Application/Validators/RenewalCreateValidator.cs` | Validates old fields | **Rewrite** — PolicyId required, optional LOB/assignee |
| `Nebula.Application/Services/RenewalService.cs` | GetById, GetTransitions, Transition | **Rewrite** — Add Create, List, Assign; rewrite Transition with role gates + conditional fields |
| `Nebula.Application/Interfaces/IRenewalRepository.cs` | GetByIdAsync, UpdateAsync | **Expand** — Add AddAsync, ListAsync, HasActiveRenewalForPolicyAsync, GetWithPolicyAsync |
| `Nebula.Infrastructure/Repositories/RenewalRepository.cs` | 2 methods | **Expand** — Implement new interface methods |
| `Nebula.Infrastructure/Persistence/Configurations/RenewalConfiguration.cs` | Old schema | **Rewrite** — New columns, FKs, filtered unique index |
| `Nebula.Api/Endpoints/RenewalEndpoints.cs` | 3 routes (get, transitions) | **Rewrite** — 6 routes with Casbin enforcement |
| `Nebula.Application/Services/WorkflowStateMachine.cs` | Old 15-state renewal map | **Rewrite** — New 6-state transition map with role gate support |
| `Nebula.Domain/Workflow/OpportunityStatusCatalog.cs` | Old 15 renewal statuses | **Rewrite** — 6 statuses |
| `Nebula.Application/DTOs/WorkflowTransitionRequestDto.cs` | 2 fields (ToState, Reason) | **Extend** — Add ReasonCode, ReasonDetail, BoundPolicyId, RenewalSubmissionId |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `Nebula.Domain/Entities/Policy.cs` | Domain | Policy stub entity (F0018 surface) |
| `Nebula.Application/DTOs/RenewalListItemDto.cs` | Application | Pipeline list item with computed fields |
| `Nebula.Application/DTOs/RenewalAssignmentRequestDto.cs` | Application | Assignment request |
| `Nebula.Application/DTOs/RenewalTransitionRequestDto.cs` | Application | Renewal-specific transition request (extends base) |
| `Nebula.Application/DTOs/RenewalListQuery.cs` | Application | List query parameters object |
| `Nebula.Application/Validators/RenewalTransitionValidator.cs` | Application | Conditional validation (Lost→reasonCode, Completed→boundPolicyId) |
| `Nebula.Application/Validators/RenewalAssignmentValidator.cs` | Application | Assignment validation |
| `Nebula.Infrastructure/Persistence/Configurations/PolicyConfiguration.cs` | Infrastructure | Policy stub EF Core config |

---

## Step 1 — Migration 006: Policy Stub Table

> Skip if F0018 has already landed the Policy entity. Check: `db.Policies` DbSet exists and table has required columns.

### Stub Lifecycle and F0018 Handoff (clarified 2026-04-11)

F0007 owns this stub *only* because F0018 (Policy Lifecycle & Policy 360) has not yet been planned. The stub is intentionally minimal — it provides only the columns Renewal needs (`PolicyNumber`, `AccountId`, `BrokerId`, `Carrier`, `LineOfBusiness`, `EffectiveDate`, `ExpirationDate`, `Premium`, `CurrentStatus`) and is sufficient for renewal discovery, list, detail, transitions, and assignment. The migration is named `F0018_PolicyStub` (not `F0007_PolicyStub`) to signal that the Policy entity is F0018 surface area being landed early.

When F0018 is planned and built:
- F0018 must extend (not rewrite) the `Policies` table via additive migrations.
- F0018 must preserve `PolicyNumber` uniqueness, the existing FKs, and the `xmin` concurrency token.
- The `IRenewalRepository.HasActiveRenewalForPolicyAsync` and `RenewalService.CreateAsync` policy lookup paths will become consumers of F0018's `IPolicyRepository` — F0007 should not introduce its own `IPolicyRepository` interface.
- F0007 reconcile migrations (`20260407151358_F0007_ReconcileRenewalWorkflowStates`, `20260408013334_F0007_ReconcileRenewalEntityShape`) already assume `PolicyId` exists; if F0018 lands first, this Step 1 is skipped but the column references remain valid.

If F0018 reaches "Planned" status before F0007 build begins, this step should be removed from the F0007 build path and F0018-S0001 (or its equivalent Policy entity story) becomes a hard prerequisite for F0007-S0006.

### New Files

| File | Layer |
|------|-------|
| `Nebula.Infrastructure/Persistence/Migrations/*_F0018_PolicyStub.cs` | Infrastructure |
| `Nebula.Domain/Entities/Policy.cs` | Domain |
| `Nebula.Infrastructure/Persistence/Configurations/PolicyConfiguration.cs` | Infrastructure |

### Domain Entity

```csharp
// Nebula.Domain/Entities/Policy.cs
namespace Nebula.Domain.Entities;

public class Policy : BaseEntity
{
    public string PolicyNumber { get; set; } = default!;
    public Guid AccountId { get; set; }
    public Guid BrokerId { get; set; }
    public string? Carrier { get; set; }
    public string? LineOfBusiness { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public decimal? Premium { get; set; }
    public string CurrentStatus { get; set; } = "Active";

    // Navigation
    public Account Account { get; set; } = default!;
    public Broker Broker { get; set; } = default!;
}
```

### EF Core Configuration

```csharp
// Nebula.Infrastructure/Persistence/Configurations/PolicyConfiguration.cs
public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("Policies");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.PolicyNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(e => e.PolicyNumber).IsUnique().HasDatabaseName("IX_Policies_PolicyNumber");

        builder.Property(e => e.AccountId).IsRequired();
        builder.Property(e => e.BrokerId).IsRequired();
        builder.Property(e => e.Carrier).HasMaxLength(100);
        builder.Property(e => e.LineOfBusiness).HasMaxLength(50);
        builder.Property(e => e.EffectiveDate).IsRequired();
        builder.Property(e => e.ExpirationDate).IsRequired();
        builder.Property(e => e.Premium).HasColumnType("decimal(18,2)");
        builder.Property(e => e.CurrentStatus).IsRequired().HasMaxLength(30).HasDefaultValue("Active");

        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.Account).WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Broker).WithMany().HasForeignKey(e => e.BrokerId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin").HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
```

### AppDbContext Addition

```csharp
// Add to AppDbContext.cs
public DbSet<Policy> Policies => Set<Policy>();
```

### Seed Data (DevSeedData.cs)

Seed 5–10 sample policies with varied expiration dates (30, 60, 90, 120, 180 days out) across LOBs, linked to existing dev accounts and brokers.

### Migration Command

```bash
cd engine/src/Nebula.Infrastructure
dotnet ef migrations add F0018_PolicyStub -s ../Nebula.Api
```

---

## Step 2 — Migration 007: Restructure Renewals Table

### Migration Changes

```
DROP columns:
  - RenewalDate
  - SubmissionId (FK to Submissions)

ADD columns:
  - PolicyId            uuid NOT NULL FK → Policies.Id
  - PolicyExpirationDate date NOT NULL
  - TargetOutreachDate   date NOT NULL
  - LostReasonCode       varchar(50) NULL
  - LostReasonDetail     varchar(500) NULL
  - BoundPolicyId        uuid NULL FK → Policies.Id
  - RenewalSubmissionId  uuid NULL FK → Submissions.Id

ALTER columns:
  - CurrentStatus default: 'Created' → 'Identified'

ADD indexes:
  - IX_Renewals_PolicyId_Active        UNIQUE filtered (PolicyId) WHERE IsDeleted=false AND CurrentStatus NOT IN ('Completed','Lost')
  - IX_Renewals_PolicyExpirationDate_CurrentStatus  (PolicyExpirationDate, CurrentStatus)
  - IX_Renewals_AccountId              (AccountId)
  - IX_Renewals_BrokerId               (BrokerId)
  - IX_Renewals_TargetOutreachDate     PARTIAL (TargetOutreachDate) WHERE IsDeleted=false AND CurrentStatus='Identified'

RE-SEED ReferenceRenewalStatus (6 values):
  - DELETE existing 15 entries
  - INSERT: Identified, Outreach, InReview, Quoted, Completed, Lost
```

### ReferenceRenewalStatus Seed

```sql
-- In migration Up():
DELETE FROM "ReferenceRenewalStatuses";
INSERT INTO "ReferenceRenewalStatuses" ("Code", "DisplayName", "Description", "IsTerminal", "DisplayOrder", "ColorGroup")
VALUES
  ('Identified',  'Identified',  'Renewal identified from expiring policy',         false, 1, 'intake'),
  ('Outreach',    'Outreach',    'Active broker/account outreach begun',             false, 2, 'waiting'),
  ('InReview',    'In Review',   'Under underwriter review for renewal terms',       false, 3, 'review'),
  ('Quoted',      'Quoted',      'Renewal quote issued',                             false, 4, 'decision'),
  ('Completed',   'Completed',   'Renewal completed — policy renewed and bound',     true,  5, 'won'),
  ('Lost',        'Lost',        'Renewal lost — not retained',                      true,  6, 'lost');
```

### Filtered Unique Index (PostgreSQL raw SQL)

The filtered unique index cannot be expressed via EF Core fluent API. Use raw SQL in migration:

```csharp
migrationBuilder.Sql(@"
    CREATE UNIQUE INDEX ""IX_Renewals_PolicyId_Active""
    ON ""Renewals"" (""PolicyId"")
    WHERE ""IsDeleted"" = false
      AND ""CurrentStatus"" NOT IN ('Completed', 'Lost');
");
```

### Partial Index for Overdue Detection

```csharp
migrationBuilder.Sql(@"
    CREATE INDEX ""IX_Renewals_TargetOutreachDate""
    ON ""Renewals"" (""TargetOutreachDate"")
    WHERE ""IsDeleted"" = false
      AND ""CurrentStatus"" = 'Identified';
");
```

### Migration Command

```bash
dotnet ef migrations add F0007_RenewalRedesign -s ../Nebula.Api
```

**Note:** If existing renewal records exist in dev/test, the migration must map old statuses to new:
- `Created`, `Early` → `Identified`
- `OutreachStarted` → `Outreach`
- `DataReview`, `WaitingOnBroker` → `InReview`
- `Quoted`, `Negotiation`, `BindRequested` → `Quoted`
- `Bound` → `Completed`
- `NotRenewed`, `Lost`, `Lapsed`, `Withdrawn`, `Expired` → `Lost`

Easiest approach: the dev seed will re-create all data after `docker-compose down -v`, so mapping old records is only necessary if the migration must be non-destructive in shared environments.

---

## Step 3 — Migration 008: WorkflowSlaThreshold Per-LOB Extension

### Migration Changes

```
ADD column:
  - LineOfBusiness varchar(50) NULL

DROP constraint:
  - IX_WorkflowSlaThresholds_EntityType_Status (old UNIQUE)

ADD constraint (raw SQL):
  - IX_WorkflowSlaThresholds_EntityType_Status_LOB UNIQUE
    ON (EntityType, Status, COALESCE(LineOfBusiness, '__default__'))
```

### Raw SQL for Unique Expression Index

```csharp
migrationBuilder.Sql(@"
    DROP INDEX IF EXISTS ""IX_WorkflowSlaThresholds_EntityType_Status"";
    CREATE UNIQUE INDEX ""IX_WorkflowSlaThresholds_EntityType_Status_LOB""
    ON ""WorkflowSlaThresholds"" (""EntityType"", ""Status"", COALESCE(""LineOfBusiness"", '__default__'));
");
```

### Seed Renewal Timing Thresholds

```csharp
// In migration Up()
migrationBuilder.InsertData(table: "WorkflowSlaThresholds", columns: new[] {
    "Id", "EntityType", "Status", "LineOfBusiness", "WarningDays", "TargetDays"
}, values: new object[,] {
    { Guid.NewGuid(), "renewal", "Identified", null,                    60, 90  },
    { Guid.NewGuid(), "renewal", "Identified", "Property",              60, 90  },
    { Guid.NewGuid(), "renewal", "Identified", "GeneralLiability",      60, 90  },
    { Guid.NewGuid(), "renewal", "Identified", "WorkersCompensation",   90, 120 },
    { Guid.NewGuid(), "renewal", "Identified", "ProfessionalLiability", 60, 90  },
    { Guid.NewGuid(), "renewal", "Identified", "Cyber",                 45, 60  },
});
```

### WorkflowSlaThreshold Entity Update

Add `LineOfBusiness` property to existing entity:

```csharp
// Nebula.Domain/Entities/WorkflowSlaThreshold.cs — add field:
public string? LineOfBusiness { get; set; }
```

### WorkflowSlaThresholdConfiguration Update

```csharp
// Add to existing configuration:
builder.Property(e => e.LineOfBusiness).HasMaxLength(50);
// Remove old unique index; new expression index is raw SQL in migration
```

### Migration Command

```bash
dotnet ef migrations add F0007_WorkflowSlaThresholdPerLob -s ../Nebula.Api
```

---

## Step 4 — Domain Entity + Catalog Updates

### 4a. Renewal Entity (Rewrite)

```csharp
// Nebula.Domain/Entities/Renewal.cs
namespace Nebula.Domain.Entities;

public class Renewal : BaseEntity
{
    public Guid AccountId { get; set; }
    public Guid BrokerId { get; set; }
    public Guid PolicyId { get; set; }
    public string CurrentStatus { get; set; } = "Identified";
    public string? LineOfBusiness { get; set; }
    public DateTime PolicyExpirationDate { get; set; }
    public DateTime TargetOutreachDate { get; set; }
    public Guid AssignedToUserId { get; set; }
    public string? LostReasonCode { get; set; }
    public string? LostReasonDetail { get; set; }
    public Guid? BoundPolicyId { get; set; }
    public Guid? RenewalSubmissionId { get; set; }

    // Navigation properties
    public Account Account { get; set; } = default!;
    public Broker Broker { get; set; } = default!;
    public Policy Policy { get; set; } = default!;
    public Policy? BoundPolicy { get; set; }
    public Submission? RenewalSubmission { get; set; }
}
```

### 4b. RenewalConfiguration (Rewrite)

```csharp
// Nebula.Infrastructure/Persistence/Configurations/RenewalConfiguration.cs
public class RenewalConfiguration : IEntityTypeConfiguration<Renewal>
{
    public void Configure(EntityTypeBuilder<Renewal> builder)
    {
        builder.ToTable("Renewals");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CurrentStatus).IsRequired().HasMaxLength(30).HasDefaultValue("Identified");
        builder.Property(e => e.LineOfBusiness).HasMaxLength(50);
        builder.Property(e => e.PolicyExpirationDate).IsRequired();
        builder.Property(e => e.TargetOutreachDate).IsRequired();
        builder.Property(e => e.AssignedToUserId).IsRequired();
        builder.Property(e => e.LostReasonCode).HasMaxLength(50);
        builder.Property(e => e.LostReasonDetail).HasMaxLength(500);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // Foreign keys
        builder.HasOne(e => e.Account).WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Broker).WithMany().HasForeignKey(e => e.BrokerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Policy).WithMany().HasForeignKey(e => e.PolicyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.BoundPolicy).WithMany().HasForeignKey(e => e.BoundPolicyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.RenewalSubmission).WithMany().HasForeignKey(e => e.RenewalSubmissionId).OnDelete(DeleteBehavior.Restrict);

        // Optimistic concurrency
        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin").HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasQueryFilter(e => !e.IsDeleted);

        // Indexes (non-expression indexes; filtered/partial indexes are raw SQL in migration)
        builder.HasIndex(e => e.CurrentStatus).HasDatabaseName("IX_Renewals_CurrentStatus");
        builder.HasIndex(e => new { e.AssignedToUserId, e.CurrentStatus }).HasDatabaseName("IX_Renewals_AssignedToUserId_CurrentStatus");
        builder.HasIndex(e => new { e.PolicyExpirationDate, e.CurrentStatus }).HasDatabaseName("IX_Renewals_PolicyExpirationDate_CurrentStatus");
        builder.HasIndex(e => e.AccountId).HasDatabaseName("IX_Renewals_AccountId");
        builder.HasIndex(e => e.BrokerId).HasDatabaseName("IX_Renewals_BrokerId");
    }
}
```

### 4c. OpportunityStatusCatalog (Rewrite RenewalStatuses)

Replace `RenewalStatuses` in `Nebula.Domain/Workflow/OpportunityStatusCatalog.cs`:

```csharp
public static readonly IReadOnlyList<WorkflowStatusDefinition> RenewalStatuses =
[
    new("Identified",  "Identified",  "Renewal identified from expiring policy",     false, 1, "intake"),
    new("Outreach",    "Outreach",    "Active broker/account outreach begun",         false, 2, "waiting"),
    new("InReview",    "In Review",   "Under underwriter review for renewal terms",   false, 3, "review"),
    new("Quoted",      "Quoted",      "Renewal quote issued",                         false, 4, "decision"),
    new("Completed",   "Completed",   "Renewal completed — policy renewed and bound", true,  5, "won"),
    new("Lost",        "Lost",        "Renewal lost — not retained",                  true,  6, "lost"),
];
```

### 4d. WorkflowStateMachine (Rewrite RenewalTransitions + Add Role Gates)

Replace `RenewalTransitions` in `Nebula.Application/Services/WorkflowStateMachine.cs`:

```csharp
private static readonly Dictionary<string, HashSet<string>> RenewalTransitions = new()
{
    ["Identified"] = ["Outreach"],
    ["Outreach"]   = ["InReview"],
    ["InReview"]   = ["Quoted", "Lost"],
    ["Quoted"]     = ["Completed", "Lost"],
};
```

Add role-gate map and lookup method:

```csharp
// Roles allowed for each renewal transition (from → to → allowed roles)
private static readonly Dictionary<(string From, string To), HashSet<string>> RenewalTransitionRoles = new()
{
    [("Identified", "Outreach")]   = ["DistributionUser", "DistributionManager", "Admin"],
    [("Outreach",   "InReview")]   = ["DistributionUser", "DistributionManager", "Underwriter", "Admin"],
    [("InReview",   "Quoted")]     = ["Underwriter", "Admin"],
    [("InReview",   "Lost")]       = ["Underwriter", "Admin"],
    [("Quoted",     "Completed")]  = ["Underwriter", "Admin"],
    [("Quoted",     "Lost")]       = ["Underwriter", "Admin"],
};

/// <summary>
/// Returns null if the transition is valid for the given roles, or an error string if denied.
/// </summary>
public static string? ValidateRenewalTransition(string from, string to, IReadOnlyList<string> userRoles)
{
    if (!IsValidTransition("Renewal", from, to))
        return "invalid_transition";

    if (!RenewalTransitionRoles.TryGetValue((from, to), out var allowed))
        return "invalid_transition";

    if (!userRoles.Any(r => allowed.Contains(r)))
        return "policy_denied";

    return null; // valid
}

/// <summary>
/// Returns the list of valid target states for the given current state and user roles.
/// Used to populate availableTransitions on the response DTO.
/// </summary>
public static IReadOnlyList<string> GetAvailableRenewalTransitions(string currentStatus, IReadOnlyList<string> userRoles)
{
    if (!RenewalTransitions.TryGetValue(currentStatus, out var targets))
        return [];

    return targets
        .Where(to => RenewalTransitionRoles.TryGetValue((currentStatus, to), out var allowed)
                     && userRoles.Any(r => allowed.Contains(r)))
        .ToList();
}
```

---

## Step 5 — DTOs (Rewritten + New)

### 5a. RenewalCreateRequestDto (Rewrite)

```csharp
// Nebula.Application/DTOs/RenewalCreateDto.cs — REWRITE
namespace Nebula.Application.DTOs;

public record RenewalCreateRequestDto(
    Guid PolicyId,
    Guid? AssignedToUserId,
    string? LineOfBusiness);
```

### 5b. RenewalDto (Rewrite)

```csharp
// Nebula.Application/DTOs/RenewalDto.cs — REWRITE
namespace Nebula.Application.DTOs;

public record RenewalDto(
    Guid Id,
    Guid AccountId,
    Guid BrokerId,
    Guid PolicyId,
    string CurrentStatus,
    string? LineOfBusiness,
    DateTime PolicyExpirationDate,
    DateTime TargetOutreachDate,
    Guid AssignedToUserId,
    string? LostReasonCode,
    string? LostReasonDetail,
    Guid? BoundPolicyId,
    Guid? RenewalSubmissionId,
    string? Urgency,
    IReadOnlyList<string> AvailableTransitions,
    string? AssignedUserDisplayName,
    string? AccountName,
    string? BrokerName,
    string? PolicyNumber,
    string? Carrier,
    string RowVersion,
    DateTime CreatedAt,
    Guid? CreatedByUserId,
    DateTime UpdatedAt,
    Guid? UpdatedByUserId);
```

### 5c. RenewalListItemDto (New)

```csharp
// Nebula.Application/DTOs/RenewalListItemDto.cs
namespace Nebula.Application.DTOs;

public record RenewalListItemDto(
    Guid Id,
    string AccountName,
    string BrokerName,
    string? LineOfBusiness,
    string CurrentStatus,
    DateTime PolicyExpirationDate,
    DateTime TargetOutreachDate,
    Guid AssignedToUserId,
    string? AssignedUserDisplayName,
    string? Urgency);
```

### 5d. RenewalTransitionRequestDto (New — renewal-specific)

```csharp
// Nebula.Application/DTOs/RenewalTransitionRequestDto.cs
namespace Nebula.Application.DTOs;

public record RenewalTransitionRequestDto(
    string ToState,
    string? Reason,
    string? ReasonCode,
    string? ReasonDetail,
    Guid? BoundPolicyId,
    Guid? RenewalSubmissionId);
```

### 5e. RenewalAssignmentRequestDto (New)

```csharp
// Nebula.Application/DTOs/RenewalAssignmentRequestDto.cs
namespace Nebula.Application.DTOs;

public record RenewalAssignmentRequestDto(
    Guid AssignedToUserId);
```

### 5f. RenewalListQuery (New)

```csharp
// Nebula.Application/DTOs/RenewalListQuery.cs
namespace Nebula.Application.DTOs;

public record RenewalListQuery(
    Guid CallerUserId,
    IReadOnlyList<string> CallerRoles,
    IReadOnlyList<string>? CallerRegions,
    string? BrokerTenantId,
    // Filters
    string[]? StatusFilter,
    string[]? LineOfBusinessFilter,
    Guid? AssigneeId,
    string? Urgency,            // "overdue" | "approaching"
    DateTime? ExpirationFrom,
    DateTime? ExpirationTo,
    string? AccountNameSearch,
    // Pagination + sort
    int Page = 1,
    int PageSize = 25,
    string SortBy = "policyExpirationDate",
    string SortDirection = "asc");
```

### 5g. WorkflowTransitionRequestDto (Keep as-is)

The existing `WorkflowTransitionRequestDto(ToState, Reason)` is still used by submission transitions. Renewal transitions use the new `RenewalTransitionRequestDto`. No change needed.

---

## Step 6 — Validators (Rewritten + New)

### 6a. RenewalCreateValidator (Rewrite)

```csharp
// Nebula.Application/Validators/RenewalCreateValidator.cs — REWRITE
using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class RenewalCreateValidator : AbstractValidator<RenewalCreateRequestDto>
{
    public RenewalCreateValidator()
    {
        RuleFor(x => x.PolicyId).NotEmpty();

        RuleFor(x => x.LineOfBusiness)
            .Must(LineOfBusinessValidation.IsValid)
            .When(x => x.LineOfBusiness is not null)
            .WithMessage(LineOfBusinessValidation.ErrorMessage);
    }
}
```

### 6b. RenewalTransitionValidator (New)

```csharp
// Nebula.Application/Validators/RenewalTransitionValidator.cs
using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class RenewalTransitionValidator : AbstractValidator<RenewalTransitionRequestDto>
{
    private static readonly string[] ValidLostReasonCodes =
        ["NonRenewal", "CompetitiveLoss", "BusinessClosed", "CoverageNoLongerNeeded", "PricingDeclined", "Other"];

    public RenewalTransitionValidator()
    {
        RuleFor(x => x.ToState)
            .NotEmpty()
            .Must(s => s is "Outreach" or "InReview" or "Quoted" or "Completed" or "Lost")
            .WithMessage("ToState must be one of: Outreach, InReview, Quoted, Completed, Lost.");

        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.ReasonDetail).MaximumLength(500);

        // Lost → reasonCode required
        RuleFor(x => x.ReasonCode)
            .NotEmpty()
            .WithMessage("reasonCode is required when transitioning to Lost.")
            .When(x => x.ToState == "Lost");

        RuleFor(x => x.ReasonCode)
            .Must(rc => rc is null || ValidLostReasonCodes.Contains(rc))
            .WithMessage($"reasonCode must be one of: {string.Join(", ", ValidLostReasonCodes)}.");

        // Lost + Other → reasonDetail required
        RuleFor(x => x.ReasonDetail)
            .NotEmpty()
            .WithMessage("reasonDetail is required when reasonCode is Other.")
            .When(x => x.ReasonCode == "Other");

        // Completed → boundPolicyId OR renewalSubmissionId required
        RuleFor(x => x)
            .Must(x => x.BoundPolicyId.HasValue || x.RenewalSubmissionId.HasValue)
            .WithMessage("boundPolicyId or renewalSubmissionId is required when transitioning to Completed.")
            .When(x => x.ToState == "Completed");
    }
}
```

### 6c. RenewalAssignmentValidator (New)

```csharp
// Nebula.Application/Validators/RenewalAssignmentValidator.cs
using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class RenewalAssignmentValidator : AbstractValidator<RenewalAssignmentRequestDto>
{
    public RenewalAssignmentValidator()
    {
        RuleFor(x => x.AssignedToUserId).NotEmpty();
    }
}
```

---

## Step 7 — Repository Expansion

### 7a. IRenewalRepository (Expand)

```csharp
// Nebula.Application/Interfaces/IRenewalRepository.cs — REWRITE
using Nebula.Application.DTOs;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IRenewalRepository
{
    Task<Renewal?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Load renewal with Policy, Account, Broker, UserProfile joins for detail view.</summary>
    Task<Renewal?> GetByIdWithRelationsAsync(Guid id, CancellationToken ct = default);

    Task<bool> HasActiveRenewalForPolicyAsync(Guid policyId, CancellationToken ct = default);

    Task AddAsync(Renewal renewal, CancellationToken ct = default);

    Task UpdateAsync(Renewal renewal, CancellationToken ct = default);

    /// <summary>Paginated list with filters, joins, and urgency computation.</summary>
    Task<(IReadOnlyList<RenewalListItemDto> Items, int TotalCount)> ListAsync(
        RenewalListQuery query, CancellationToken ct = default);
}
```

### 7b. RenewalRepository (Rewrite)

```csharp
// Nebula.Infrastructure/Repositories/RenewalRepository.cs — REWRITE
using Microsoft.EntityFrameworkCore;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class RenewalRepository(AppDbContext db) : IRenewalRepository
{
    public async Task<Renewal?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Renewals.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Renewal?> GetByIdWithRelationsAsync(Guid id, CancellationToken ct = default) =>
        await db.Renewals
            .Include(r => r.Policy)
            .Include(r => r.Account)
            .Include(r => r.Broker)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<bool> HasActiveRenewalForPolicyAsync(Guid policyId, CancellationToken ct = default) =>
        await db.Renewals
            .AnyAsync(r => r.PolicyId == policyId
                && !r.IsDeleted
                && r.CurrentStatus != "Completed"
                && r.CurrentStatus != "Lost", ct);

    public async Task AddAsync(Renewal renewal, CancellationToken ct = default) =>
        await db.Renewals.AddAsync(renewal, ct);

    public async Task UpdateAsync(Renewal renewal, CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);

    public async Task<(IReadOnlyList<RenewalListItemDto> Items, int TotalCount)> ListAsync(
        RenewalListQuery query, CancellationToken ct = default)
    {
        var q = db.Renewals
            .Include(r => r.Account)
            .Include(r => r.Broker)
            .AsQueryable();

        // ── ABAC scope filtering ──────────────────────────────────────────
        // BrokerUser: denied at Casbin layer (no read policy). Shouldn't reach here.
        // DistributionUser: own assignments only
        if (query.CallerRoles.Contains("DistributionUser") && !query.CallerRoles.Contains("DistributionManager") && !query.CallerRoles.Contains("Admin"))
            q = q.Where(r => r.AssignedToUserId == query.CallerUserId);
        // DistributionManager: region-scoped (via Account.PrimaryState IN CallerRegions)
        else if (query.CallerRoles.Contains("DistributionManager") && !query.CallerRoles.Contains("Admin"))
        {
            if (query.CallerRegions is { Count: > 0 })
                q = q.Where(r => query.CallerRegions.Contains(r.Account.PrimaryState));
        }
        // Underwriter: assigned + team — MVP simplification: assigned only
        else if (query.CallerRoles.Contains("Underwriter") && !query.CallerRoles.Contains("Admin"))
            q = q.Where(r => r.AssignedToUserId == query.CallerUserId);
        // RelationshipManager, ProgramManager: read-only scoped — MVP: all visible
        // Admin: unscoped

        // ── Optional filters ──────────────────────────────────────────────
        if (query.StatusFilter is { Length: > 0 })
            q = q.Where(r => query.StatusFilter.Contains(r.CurrentStatus));

        if (query.LineOfBusinessFilter is { Length: > 0 })
            q = q.Where(r => r.LineOfBusiness != null && query.LineOfBusinessFilter.Contains(r.LineOfBusiness));

        if (query.AssigneeId.HasValue)
            q = q.Where(r => r.AssignedToUserId == query.AssigneeId.Value);

        if (query.ExpirationFrom.HasValue)
            q = q.Where(r => r.PolicyExpirationDate >= query.ExpirationFrom.Value);

        if (query.ExpirationTo.HasValue)
            q = q.Where(r => r.PolicyExpirationDate <= query.ExpirationTo.Value);

        if (!string.IsNullOrEmpty(query.AccountNameSearch))
            q = q.Where(r => EF.Functions.ILike(r.Account.Name, $"%{query.AccountNameSearch}%"));

        // Count before pagination
        var totalCount = await q.CountAsync(ct);

        // Sort
        q = query.SortBy switch
        {
            "accountName"          => query.SortDirection == "desc" ? q.OrderByDescending(r => r.Account.Name) : q.OrderBy(r => r.Account.Name),
            "currentStatus"        => query.SortDirection == "desc" ? q.OrderByDescending(r => r.CurrentStatus) : q.OrderBy(r => r.CurrentStatus),
            "assignedUserDisplayName" => query.SortDirection == "desc" ? q.OrderByDescending(r => r.AssignedToUserId) : q.OrderBy(r => r.AssignedToUserId),
            _ /* policyExpirationDate */ => query.SortDirection == "desc" ? q.OrderByDescending(r => r.PolicyExpirationDate) : q.OrderBy(r => r.PolicyExpirationDate),
        };

        // Paginate
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new RenewalListItemDto(
                r.Id,
                r.Account.Name,
                r.Broker.LegalName,
                r.LineOfBusiness,
                r.CurrentStatus,
                r.PolicyExpirationDate,
                r.TargetOutreachDate,
                r.AssignedToUserId,
                null, // AssignedUserDisplayName — resolved in service layer
                null  // Urgency — computed in service layer
            ))
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
```

**Note:** `AssignedUserDisplayName` and `Urgency` are computed post-query in the service layer (see Step 8) to avoid complex subqueries. This is the same pattern used by TaskService for `linkedEntityName`.

---

## Step 8 — Service Rewrite

### RenewalService.cs — Full Rewrite

```csharp
// Nebula.Application/Services/RenewalService.cs — REWRITE
using System.Text.Json;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class RenewalService(
    IRenewalRepository renewalRepo,
    IWorkflowTransitionRepository transitionRepo,
    ITimelineRepository timelineRepo,
    IUserProfileRepository userProfileRepo,
    IUnitOfWork unitOfWork,
    IWorkflowSlaThresholdRepository slaRepo)  // new dependency — see Note below
{
```

**Note on IWorkflowSlaThresholdRepository:** If this interface doesn't exist yet, create it:

```csharp
// Nebula.Application/Interfaces/IWorkflowSlaThresholdRepository.cs
public interface IWorkflowSlaThresholdRepository
{
    Task<WorkflowSlaThreshold?> GetThresholdAsync(string entityType, string status, string? lineOfBusiness, CancellationToken ct = default);
}
```

Implementation queries with LOB fallback:
```csharp
public async Task<WorkflowSlaThreshold?> GetThresholdAsync(
    string entityType, string status, string? lineOfBusiness, CancellationToken ct = default)
{
    // Try exact LOB match first
    if (lineOfBusiness is not null)
    {
        var exact = await db.WorkflowSlaThresholds
            .FirstOrDefaultAsync(t => t.EntityType == entityType && t.Status == status && t.LineOfBusiness == lineOfBusiness, ct);
        if (exact is not null) return exact;
    }
    // Fallback to default (null LOB)
    return await db.WorkflowSlaThresholds
        .FirstOrDefaultAsync(t => t.EntityType == entityType && t.Status == status && t.LineOfBusiness == null, ct);
}
```

Register in DependencyInjection.cs:
```csharp
services.AddScoped<IWorkflowSlaThresholdRepository, WorkflowSlaThresholdRepository>();
```

---

### 8a. CreateAsync (S0006 — Create renewal from expiring policy)

```csharp
public async Task<(RenewalDto? Dto, string? ErrorCode)> CreateAsync(
    RenewalCreateRequestDto dto, ICurrentUserService user, CancellationToken ct = default)
{
    // 1. Load policy
    var policy = await db.Policies.FirstOrDefaultAsync(p => p.Id == dto.PolicyId, ct);
    if (policy is null) return (null, "not_found");

    // 2. Check one-active-per-policy
    if (await renewalRepo.HasActiveRenewalForPolicyAsync(dto.PolicyId, ct))
        return (null, "duplicate_renewal");

    // 3. Resolve assignee
    var assigneeId = dto.AssignedToUserId ?? user.UserId;
    var isManager = user.Roles.Contains("DistributionManager") || user.Roles.Contains("Admin");

    if (!isManager && assigneeId != user.UserId)
        return (null, "policy_denied"); // non-managers can only self-assign

    if (assigneeId != user.UserId)
    {
        var assignee = await userProfileRepo.GetByIdAsync(assigneeId, ct);
        if (assignee is null) return (null, "invalid_assignee");
        if (!assignee.IsActive) return (null, "inactive_assignee");
    }

    // 4. Resolve LOB: explicit override > policy LOB > null
    var lob = dto.LineOfBusiness ?? policy.LineOfBusiness;

    // 5. Compute TargetOutreachDate from WorkflowSlaThreshold
    var threshold = await slaRepo.GetThresholdAsync("renewal", "Identified", lob, ct);
    var targetDays = threshold?.TargetDays ?? 90; // hardcoded fallback
    var targetOutreachDate = policy.ExpirationDate.AddDays(-targetDays);

    // 6. Build entity
    var now = DateTime.UtcNow;
    var renewal = new Renewal
    {
        AccountId = policy.AccountId,
        BrokerId = policy.BrokerId,
        PolicyId = policy.Id,
        CurrentStatus = "Identified",
        LineOfBusiness = lob,
        PolicyExpirationDate = policy.ExpirationDate,
        TargetOutreachDate = targetOutreachDate,
        AssignedToUserId = assigneeId,
        CreatedAt = now,
        UpdatedAt = now,
        CreatedByUserId = user.UserId,
        UpdatedByUserId = user.UserId,
    };

    await renewalRepo.AddAsync(renewal, ct);

    // 7. Initial transition record: null → Identified
    var transition = new WorkflowTransition
    {
        WorkflowType = "Renewal",
        EntityId = renewal.Id,
        FromState = "(created)",
        ToState = "Identified",
        ActorUserId = user.UserId,
        OccurredAt = now,
    };
    await transitionRepo.AddAsync(transition, ct);

    // 8. Timeline event
    var assigneeName = assigneeId == user.UserId
        ? user.DisplayName
        : (await userProfileRepo.GetByIdAsync(assigneeId, ct))?.DisplayName;

    await timelineRepo.AddEventAsync(new ActivityTimelineEvent
    {
        EntityType = "Renewal",
        EntityId = renewal.Id,
        EventType = "RenewalCreated",
        EventDescription = $"Renewal created from policy {policy.PolicyNumber}",
        BrokerDescription = null, // InternalOnly
        ActorUserId = user.UserId,
        ActorDisplayName = user.DisplayName,
        OccurredAt = now,
        EventPayloadJson = JsonSerializer.Serialize(new
        {
            policyId = policy.Id,
            policyNumber = policy.PolicyNumber,
            accountName = renewal.Account?.Name, // may be null if not loaded
            brokerName = renewal.Broker?.LegalName,
            lineOfBusiness = lob,
            assignedToUserId = assigneeId,
            assignedToDisplayName = assigneeName,
        }),
    }, ct);

    // 9. Commit
    await unitOfWork.CommitAsync(ct);

    // 10. Return detail DTO
    return (await BuildDetailDtoAsync(renewal.Id, user, ct), null);
}
```

### HTTP Responses (POST /renewals)

| Status | Body | Condition |
|--------|------|-----------|
| 201 Created | `RenewalDto` | Success |
| 400 | ProblemDetails (`validation_error`) | Schema validation failure |
| 403 | ProblemDetails (`policy_denied`) | Casbin deny or non-manager cross-assignment |
| 404 | ProblemDetails (`not_found`) | Policy doesn't exist or soft-deleted |
| 409 | ProblemDetails (`duplicate_renewal`) | Active renewal already exists for this policy |
| 422 | ProblemDetails (`invalid_assignee`) | Assignee user not found |
| 422 | ProblemDetails (`inactive_assignee`) | Assignee user is inactive |

---

### 8b. TransitionAsync (S0003 — Renewal status transitions)

```csharp
public async Task<(WorkflowTransitionRecordDto? Dto, string? ErrorCode)> TransitionAsync(
    Guid renewalId, RenewalTransitionRequestDto dto, ICurrentUserService user, CancellationToken ct = default)
{
    var renewal = await renewalRepo.GetByIdAsync(renewalId, ct);
    if (renewal is null) return (null, "not_found");

    // 1. Validate transition + role gate
    var error = WorkflowStateMachine.ValidateRenewalTransition(renewal.CurrentStatus, dto.ToState, user.Roles);
    if (error is not null) return (null, error);

    var now = DateTime.UtcNow;

    // 2. Apply conditional fields
    if (dto.ToState == "Lost")
    {
        renewal.LostReasonCode = dto.ReasonCode;
        renewal.LostReasonDetail = dto.ReasonDetail;
    }
    else if (dto.ToState == "Completed")
    {
        renewal.BoundPolicyId = dto.BoundPolicyId;
        renewal.RenewalSubmissionId = dto.RenewalSubmissionId;
    }

    // 3. Transition record
    var transition = new WorkflowTransition
    {
        WorkflowType = "Renewal",
        EntityId = renewalId,
        FromState = renewal.CurrentStatus,
        ToState = dto.ToState,
        Reason = dto.Reason ?? dto.ReasonCode, // persist reasonCode as Reason for Lost transitions
        ActorUserId = user.UserId,
        OccurredAt = now,
    };

    // 4. Update entity
    renewal.CurrentStatus = dto.ToState;
    renewal.UpdatedAt = now;
    renewal.UpdatedByUserId = user.UserId;

    await transitionRepo.AddAsync(transition, ct);

    // 5. Timeline event
    await timelineRepo.AddEventAsync(new ActivityTimelineEvent
    {
        EntityType = "Renewal",
        EntityId = renewalId,
        EventType = "RenewalTransitioned",
        EventDescription = $"Renewal transitioned from {transition.FromState} to {transition.ToState}",
        BrokerDescription = null,
        ActorUserId = user.UserId,
        ActorDisplayName = user.DisplayName,
        OccurredAt = now,
        EventPayloadJson = JsonSerializer.Serialize(new
        {
            fromState = transition.FromState,
            toState = transition.ToState,
            reason = dto.Reason,
            reasonCode = dto.ReasonCode,
            reasonDetail = dto.ReasonDetail,
            boundPolicyId = dto.BoundPolicyId,
            renewalSubmissionId = dto.RenewalSubmissionId,
        }),
    }, ct);

    // 6. Commit atomically
    await unitOfWork.CommitAsync(ct);

    return (MapTransition(transition), null);
}
```

### HTTP Responses (POST /renewals/{id}/transitions)

| Status | Body | Condition |
|--------|------|-----------|
| 201 Created | `WorkflowTransitionRecordDto` | Success |
| 400 | ProblemDetails (`validation_error`) | Schema validation failure |
| 403 | ProblemDetails (`policy_denied`) | Casbin deny or role not allowed for this transition |
| 404 | ProblemDetails (`not_found`) | Renewal not found |
| 409 | ProblemDetails (`invalid_transition`) | Invalid from→to pair |
| 409 | ProblemDetails (`missing_transition_prerequisite`) | Missing conditional fields |
| 409 | ProblemDetails (`concurrency_conflict`) | Optimistic concurrency failure |

---

### 8c. AssignAsync (S0004 — Renewal ownership assignment)

```csharp
public async Task<(RenewalDto? Dto, string? ErrorCode)> AssignAsync(
    Guid renewalId, RenewalAssignmentRequestDto dto, ICurrentUserService user, CancellationToken ct = default)
{
    var renewal = await renewalRepo.GetByIdAsync(renewalId, ct);
    if (renewal is null) return (null, "not_found");

    // Cannot reassign terminal-state renewals
    if (WorkflowStateMachine.IsTerminalState("Renewal", renewal.CurrentStatus))
        return (null, "invalid_transition");

    // Validate assignee exists and is active
    var assignee = await userProfileRepo.GetByIdAsync(dto.AssignedToUserId, ct);
    if (assignee is null) return (null, "invalid_assignee");
    if (!assignee.IsActive) return (null, "inactive_assignee");

    var previousAssigneeId = renewal.AssignedToUserId;
    var previousAssigneeName = (await userProfileRepo.GetByIdAsync(previousAssigneeId, ct))?.DisplayName;

    var now = DateTime.UtcNow;
    renewal.AssignedToUserId = dto.AssignedToUserId;
    renewal.UpdatedAt = now;
    renewal.UpdatedByUserId = user.UserId;

    // Timeline event
    await timelineRepo.AddEventAsync(new ActivityTimelineEvent
    {
        EntityType = "Renewal",
        EntityId = renewalId,
        EventType = "RenewalAssigned",
        EventDescription = $"Renewal reassigned from {previousAssigneeName ?? "unknown"} to {assignee.DisplayName}",
        BrokerDescription = null,
        ActorUserId = user.UserId,
        ActorDisplayName = user.DisplayName,
        OccurredAt = now,
        EventPayloadJson = JsonSerializer.Serialize(new
        {
            previousAssigneeUserId = previousAssigneeId,
            previousAssigneeName,
            newAssigneeUserId = dto.AssignedToUserId,
            newAssigneeName = assignee.DisplayName,
            assignedByUserId = user.UserId,
        }),
    }, ct);

    await unitOfWork.CommitAsync(ct);

    return (await BuildDetailDtoAsync(renewalId, user, ct), null);
}
```

### HTTP Responses (PUT /renewals/{id}/assignment)

| Status | Body | Condition |
|--------|------|-----------|
| 200 OK | `RenewalDto` | Success |
| 400 | ProblemDetails (`validation_error`) | Schema validation failure |
| 403 | ProblemDetails (`policy_denied`) | Casbin deny (no `assign` action for role) |
| 404 | ProblemDetails (`not_found`) | Renewal not found |
| 409 | ProblemDetails (`invalid_transition`) | Renewal in terminal state |
| 422 | ProblemDetails (`invalid_assignee`) | Assignee not found |
| 422 | ProblemDetails (`inactive_assignee`) | Assignee inactive |

---

### 8d. GetDetailAsync (S0002 — Detail view with policy context)

```csharp
public async Task<RenewalDto?> GetDetailAsync(
    Guid renewalId, ICurrentUserService user, CancellationToken ct = default)
{
    return await BuildDetailDtoAsync(renewalId, user, ct);
}
```

### 8e. ListAsync (S0001 — Pipeline list with due-window filtering)

```csharp
public async Task<(IReadOnlyList<RenewalListItemDto> Items, int TotalCount)> ListAsync(
    RenewalListQuery query, CancellationToken ct = default)
{
    var (items, totalCount) = await renewalRepo.ListAsync(query, ct);

    // Batch-resolve display names and urgency
    var userIds = items.Select(i => i.AssignedToUserId).Distinct().ToList();
    var userNames = await ResolveUserDisplayNamesAsync(userIds, ct);

    // Load SLA thresholds for urgency computation
    var today = DateTime.UtcNow.Date;
    var enriched = new List<RenewalListItemDto>(items.Count);

    foreach (var item in items)
    {
        var displayName = userNames.GetValueOrDefault(item.AssignedToUserId);
        var urgency = await ComputeUrgencyAsync(item.CurrentStatus, item.PolicyExpirationDate, item.LineOfBusiness, today, ct);

        enriched.Add(item with
        {
            AssignedUserDisplayName = displayName,
            Urgency = urgency,
        });
    }

    // Post-filter by urgency if requested (must be post-query since urgency is computed)
    if (!string.IsNullOrEmpty(query.Urgency))
        return (enriched.Where(i => i.Urgency == query.Urgency).ToList(), totalCount);

    return (enriched, totalCount);
}
```

### 8f. GetTimelineAsync (S0007 — Activity timeline)

```csharp
public async Task<IReadOnlyList<ActivityTimelineEventDto>> GetTimelineAsync(
    Guid renewalId, int page, int pageSize, CancellationToken ct = default)
{
    // Verify renewal exists
    var renewal = await renewalRepo.GetByIdAsync(renewalId, ct);
    if (renewal is null) return [];

    return await timelineRepo.GetEventsAsync("Renewal", renewalId, page, pageSize, ct);
}
```

### 8g. Helper: BuildDetailDtoAsync

```csharp
private async Task<RenewalDto?> BuildDetailDtoAsync(
    Guid renewalId, ICurrentUserService user, CancellationToken ct)
{
    var renewal = await renewalRepo.GetByIdWithRelationsAsync(renewalId, ct);
    if (renewal is null) return null;

    var assigneeName = (await userProfileRepo.GetByIdAsync(renewal.AssignedToUserId, ct))?.DisplayName;
    var today = DateTime.UtcNow.Date;
    var urgency = await ComputeUrgencyAsync(
        renewal.CurrentStatus, renewal.PolicyExpirationDate, renewal.LineOfBusiness, today, ct);

    var availableTransitions = WorkflowStateMachine.GetAvailableRenewalTransitions(
        renewal.CurrentStatus, user.Roles);

    return new RenewalDto(
        renewal.Id,
        renewal.AccountId,
        renewal.BrokerId,
        renewal.PolicyId,
        renewal.CurrentStatus,
        renewal.LineOfBusiness,
        renewal.PolicyExpirationDate,
        renewal.TargetOutreachDate,
        renewal.AssignedToUserId,
        renewal.LostReasonCode,
        renewal.LostReasonDetail,
        renewal.BoundPolicyId,
        renewal.RenewalSubmissionId,
        urgency,
        availableTransitions,
        assigneeName,
        renewal.Account?.Name,
        renewal.Broker?.LegalName,
        renewal.Policy?.PolicyNumber,
        renewal.Policy?.Carrier,
        renewal.RowVersion.ToString(),
        renewal.CreatedAt,
        renewal.CreatedByUserId,
        renewal.UpdatedAt,
        renewal.UpdatedByUserId);
}
```

### 8h. Helper: ComputeUrgencyAsync

```csharp
private async Task<string?> ComputeUrgencyAsync(
    string currentStatus, DateTime policyExpirationDate, string? lineOfBusiness,
    DateTime today, CancellationToken ct)
{
    if (currentStatus != "Identified") return null;

    var threshold = await slaRepo.GetThresholdAsync("renewal", "Identified", lineOfBusiness, ct);
    var targetDays = threshold?.TargetDays ?? 90;
    var warningDays = threshold?.WarningDays ?? 60;

    var overdueDate = policyExpirationDate.AddDays(-targetDays);
    var approachingDate = policyExpirationDate.AddDays(-targetDays - warningDays);

    if (today > overdueDate) return "overdue";
    if (today > approachingDate) return "approaching";
    return null;
}

private async Task<Dictionary<Guid, string?>> ResolveUserDisplayNamesAsync(
    IReadOnlyList<Guid> userIds, CancellationToken ct)
{
    var result = new Dictionary<Guid, string?>();
    foreach (var id in userIds)
    {
        if (!result.ContainsKey(id))
        {
            var profile = await userProfileRepo.GetByIdAsync(id, ct);
            result[id] = profile?.DisplayName;
        }
    }
    return result;
}
```

### 8i. Helper: MapTransition (unchanged)

```csharp
private static WorkflowTransitionRecordDto MapTransition(WorkflowTransition t) => new(
    t.Id, t.WorkflowType, t.EntityId, t.FromState, t.ToState, t.Reason, t.OccurredAt);
```

---

## Step 9 — API Endpoints (6 Routes)

### RenewalEndpoints.cs — Full Rewrite

```csharp
// Nebula.Api/Endpoints/RenewalEndpoints.cs — REWRITE
using FluentValidation;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;
using IAuthorizationService = Nebula.Application.Interfaces.IAuthorizationService;

namespace Nebula.Api.Endpoints;

public static class RenewalEndpoints
{
    public static IEndpointRouteBuilder MapRenewalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/renewals")
            .WithTags("Renewals")
            .RequireAuthorization();

        group.MapGet("/", ListRenewals);                              // S0001
        group.MapPost("/", CreateRenewal);                             // S0006
        group.MapGet("/{renewalId:guid}", GetRenewal);                 // S0002
        group.MapPost("/{renewalId:guid}/transitions", PostTransition); // S0003
        group.MapPut("/{renewalId:guid}/assignment", PutAssignment);   // S0004
        group.MapGet("/{renewalId:guid}/timeline", GetTimeline);       // S0007

        return app;
    }
```

### 9a. GET /renewals (S0001)

```csharp
    private static async Task<IResult> ListRenewals(
        [AsParameters] RenewalListQueryParams queryParams,
        RenewalService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        // Casbin: renewal:read
        if (!await AuthorizeAny(authz, user, "renewal", "read"))
            return ProblemDetailsHelper.Forbidden();

        var query = new RenewalListQuery(
            CallerUserId: user.UserId,
            CallerRoles: user.Roles,
            CallerRegions: user.Regions,
            BrokerTenantId: user.BrokerTenantId,
            StatusFilter: queryParams.Status?.Split(','),
            LineOfBusinessFilter: queryParams.Lob?.Split(','),
            AssigneeId: queryParams.AssigneeId,
            Urgency: queryParams.Urgency,
            ExpirationFrom: queryParams.ExpirationFrom,
            ExpirationTo: queryParams.ExpirationTo,
            AccountNameSearch: queryParams.AccountName,
            Page: queryParams.Page ?? 1,
            PageSize: Math.Clamp(queryParams.PageSize ?? 25, 1, 100),
            SortBy: queryParams.SortBy ?? "policyExpirationDate",
            SortDirection: queryParams.SortDir ?? "asc");

        var (items, totalCount) = await svc.ListAsync(query, ct);

        return Results.Ok(new { items, totalCount, page = query.Page, pageSize = query.PageSize });
    }
```

**Query params DTO (bind from query string):**

```csharp
// Nested record or separate file in DTOs/
public record RenewalListQueryParams(
    string? Status,
    string? Lob,
    Guid? AssigneeId,
    string? Urgency,
    DateTime? ExpirationFrom,
    DateTime? ExpirationTo,
    string? AccountName,
    int? Page,
    int? PageSize,
    string? SortBy,
    string? SortDir);
```

### HTTP Responses (GET /renewals)

| Status | Body | Condition |
|--------|------|-----------|
| 200 OK | `{ items, totalCount, page, pageSize }` | Success |
| 403 | ProblemDetails (`policy_denied`) | Casbin deny |

---

### 9b. POST /renewals (S0006)

```csharp
    private static async Task<IResult> CreateRenewal(
        RenewalCreateRequestDto dto,
        IValidator<RenewalCreateRequestDto> validator,
        RenewalService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        // Casbin: renewal:create
        if (!await AuthorizeAny(authz, user, "renewal", "create"))
            return ProblemDetailsHelper.Forbidden();

        var (result, error) = await svc.CreateAsync(dto, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Policy", dto.PolicyId),
            "duplicate_renewal" => ProblemDetailsHelper.DuplicateRenewal(),
            "policy_denied" => ProblemDetailsHelper.Forbidden(),
            "invalid_assignee" => ProblemDetailsHelper.InvalidAssignee(),
            "inactive_assignee" => ProblemDetailsHelper.InactiveAssignee(),
            _ => Results.Created($"/renewals/{result!.Id}", result),
        };
    }
```

### 9c. GET /renewals/{id} (S0002)

```csharp
    private static async Task<IResult> GetRenewal(
        Guid renewalId,
        RenewalService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await AuthorizeAny(authz, user, "renewal", "read"))
            return ProblemDetailsHelper.Forbidden();

        var result = await svc.GetDetailAsync(renewalId, user, ct);
        return result is null ? ProblemDetailsHelper.NotFound("Renewal", renewalId) : Results.Ok(result);
    }
```

### HTTP Responses (GET /renewals/{id})

| Status | Body | Condition |
|--------|------|-----------|
| 200 OK | `RenewalDto` | Success |
| 403 | ProblemDetails (`policy_denied`) | Casbin deny |
| 404 | ProblemDetails (`not_found`) | Renewal not found |

---

### 9d. POST /renewals/{id}/transitions (S0003)

```csharp
    private static async Task<IResult> PostTransition(
        Guid renewalId,
        RenewalTransitionRequestDto dto,
        IValidator<RenewalTransitionRequestDto> validator,
        RenewalService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        // Casbin: renewal:transition (broad gate; per-transition role check in service)
        if (!await AuthorizeAny(authz, user, "renewal", "transition"))
            return ProblemDetailsHelper.Forbidden();

        var (result, error) = await svc.TransitionAsync(renewalId, dto, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Renewal", renewalId),
            "invalid_transition" => ProblemDetailsHelper.InvalidTransition("current", dto.ToState),
            "policy_denied" => ProblemDetailsHelper.Forbidden(),
            "missing_transition_prerequisite" => Results.Problem(
                title: "Missing transition prerequisite",
                detail: "Required fields are missing for this transition.",
                statusCode: 409,
                extensions: new Dictionary<string, object?> { ["code"] = "missing_transition_prerequisite" }),
            _ => Results.Created($"/renewals/{renewalId}/transitions", result),
        };
    }
```

### 9e. PUT /renewals/{id}/assignment (S0004)

```csharp
    private static async Task<IResult> PutAssignment(
        Guid renewalId,
        RenewalAssignmentRequestDto dto,
        IValidator<RenewalAssignmentRequestDto> validator,
        RenewalService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(
                validation.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        // Casbin: renewal:assign (only DistributionManager and Admin)
        if (!await AuthorizeAny(authz, user, "renewal", "assign"))
            return ProblemDetailsHelper.Forbidden();

        var (result, error) = await svc.AssignAsync(renewalId, dto, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Renewal", renewalId),
            "invalid_transition" => ProblemDetailsHelper.InvalidTransition("terminal", "reassign"),
            "invalid_assignee" => ProblemDetailsHelper.InvalidAssignee(),
            "inactive_assignee" => ProblemDetailsHelper.InactiveAssignee(),
            _ => Results.Ok(result),
        };
    }
```

### 9f. GET /renewals/{id}/timeline (S0007)

```csharp
    private static async Task<IResult> GetTimeline(
        Guid renewalId,
        int? page,
        int? pageSize,
        RenewalService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await AuthorizeAny(authz, user, "renewal", "read"))
            return ProblemDetailsHelper.Forbidden();

        var events = await svc.GetTimelineAsync(
            renewalId,
            page ?? 1,
            Math.Clamp(pageSize ?? 25, 1, 100),
            ct);

        return Results.Ok(events);
    }
```

### 9g. Shared Authorization Helper

```csharp
    private static async Task<bool> AuthorizeAny(
        IAuthorizationService authz, ICurrentUserService user,
        string resource, string action)
    {
        var attrs = new Dictionary<string, object>
        {
            ["subjectId"] = user.UserId,
        };
        foreach (var role in user.Roles)
        {
            if (await authz.AuthorizeAsync(role, resource, action, attrs))
                return true;
        }
        return false;
    }
}
```

---

## Step 10 — ProblemDetailsHelper + DI Updates

### ProblemDetailsHelper Addition

Add to `Nebula.Api/Helpers/ProblemDetailsHelper.cs`:

```csharp
public static IResult DuplicateRenewal() => Results.Problem(
    title: "Duplicate renewal",
    detail: "An active renewal already exists for this policy.",
    statusCode: 409,
    extensions: Ext("duplicate_renewal"));

public static IResult MissingTransitionPrerequisite() => Results.Problem(
    title: "Missing transition prerequisite",
    detail: "Required fields are missing for this transition.",
    statusCode: 409,
    extensions: Ext("missing_transition_prerequisite"));
```

### DI Updates

**DependencyInjection.cs** — Add WorkflowSlaThresholdRepository (if created):

```csharp
services.AddScoped<IWorkflowSlaThresholdRepository, WorkflowSlaThresholdRepository>();
```

**Program.cs** — RenewalService constructor gained new dependencies (`IUserProfileRepository`, `IUnitOfWork`, `IWorkflowSlaThresholdRepository`). Since RenewalService is registered as `AddScoped<RenewalService>()`, ASP.NET DI will resolve these automatically. No Program.cs changes needed — the DI container resolves primary constructor parameters.

### Casbin Policy Update

The `policy.csv` is embedded in the Infrastructure assembly. Ensure the updated `policy.csv` (with §2.4 `create` and `assign` actions) is copied to:

```
engine/src/Nebula.Infrastructure/Authorization/policy.csv
```

This file is loaded as an embedded resource by `CasbinAuthorizationService`. The source of truth is `planning-mds/security/policies/policy.csv` — copy the §2.4 section.

---

## Integration Checkpoints

### After Steps 1–3 (Migrations)

- [ ] `docker-compose down -v && docker-compose up` runs cleanly
- [ ] `Policies` table exists with sample seed data
- [ ] `Renewals` table has new schema (PolicyId, PolicyExpirationDate, etc.)
- [ ] `ReferenceRenewalStatuses` has exactly 6 entries
- [ ] `WorkflowSlaThresholds` has renewal timing entries with LineOfBusiness
- [ ] Filtered unique index `IX_Renewals_PolicyId_Active` exists
- [ ] Partial index `IX_Renewals_TargetOutreachDate` exists

### After Steps 4–8 (Domain + Services)

- [ ] WorkflowStateMachine accepts new 6-state transitions, rejects old states
- [ ] `WorkflowStateMachine.ValidateRenewalTransition()` enforces role gates
- [ ] `WorkflowStateMachine.GetAvailableRenewalTransitions()` returns correct transitions per role
- [ ] `OpportunityStatusCatalog.RenewalStatuses` has 6 entries
- [ ] `OpportunityStatusCatalog.RenewalTerminalStatusCodes` = `{"Completed", "Lost"}`

### After Step 9 (Endpoints — Full API Smoke Test)

- [ ] `POST /renewals` with valid policyId → 201, returns RenewalDto with urgency and availableTransitions
- [ ] `POST /renewals` with same policyId again → 409 `duplicate_renewal`
- [ ] `GET /renewals` returns paginated list with urgency badges
- [ ] `GET /renewals?status=Identified&urgency=overdue` filters correctly
- [ ] `GET /renewals/{id}` returns full detail with policy context
- [ ] `POST /renewals/{id}/transitions` with `{toState: "Outreach"}` by DistributionUser → 201
- [ ] `POST /renewals/{id}/transitions` with `{toState: "Quoted"}` by DistributionUser → 403 (role gate)
- [ ] `POST /renewals/{id}/transitions` with `{toState: "Lost", reasonCode: "CompetitiveLoss"}` → 201
- [ ] `POST /renewals/{id}/transitions` with `{toState: "Completed", boundPolicyId: "..."}` → 201
- [ ] `POST /renewals/{id}/transitions` with `{toState: "Lost"}` (no reasonCode) → 400 validation error
- [ ] `PUT /renewals/{id}/assignment` by DistributionManager → 200
- [ ] `PUT /renewals/{id}/assignment` by DistributionUser → 403 (no assign policy)
- [ ] `GET /renewals/{id}/timeline` returns ordered events
- [ ] Optimistic concurrency: concurrent transition → 409 `concurrency_conflict`

### Cross-Story Verification

- [ ] Full lifecycle: Create → Outreach → InReview → Quoted → Completed
- [ ] Full lifecycle: Create → Outreach → InReview → Lost (with reasonCode)
- [ ] All Casbin policies enforced (6 roles + ExternalUser denied)
- [ ] Timeline events for full lifecycle are correct and ordered
- [ ] ProblemDetails format consistent with existing endpoints (code + traceId)
- [ ] One-active-per-policy constraint prevents duplicate renewals
- [ ] Urgency computation matches LOB-specific thresholds

---

## JSON Serialization Convention

C# property `PolicyExpirationDate (DateTime)` serializes to JSON `policyExpirationDate (string, date format)` via System.Text.Json camelCase naming policy. `RowVersion (uint)` serializes as a string. This is consistent with existing DTOs.

## No New Infrastructure Required

F0007 MVP is query-time computation only. No Temporal, no new background services, no new Docker containers. Temporal design is documented in the F0007 README for future phase.
