# Feature Assembly Plan - F0018: Policy Lifecycle & Policy 360

**Created:** 2026-04-22  
**Author:** Architect Agent  
**Status:** Draft - G0 dry-run validated

## Overview

F0018 replaces the existing F0007 policy stub with a first-class Policy aggregate, immutable version snapshots, endorsement records, current-version coverage lines, policy lifecycle transitions, Policy 360 rails, and account-scoped policy summary reads. Implementation must treat the existing OpenAPI and JSON Schema artifacts as the contract authority where they differ from PRD/story wording, while preserving the product intent from the F0018 stories.

Primary implementation boundaries:

- Backend owns the aggregate, data access, API endpoints, state transitions, idempotency, ABAC enforcement, timeline events, and the daily expiration job.
- Frontend owns Policy List, Policy Create/Import surfaces, Policy Detail/Profile, Policy 360 rails, lifecycle action modals, route/navigation integration, and Account 360 policy rail/summary wiring.
- Quality owns cross-story test mapping, runtime evidence, and regression coverage for workflow, ABAC, fallback-account, rail isolation, idempotency, and concurrency behavior.
- DevOps owns migration/runtime evidence and scheduling readiness for the expiration job.
- AI is not in scope; no F0018 story touches `{PRODUCT_ROOT}/neuron/`.

## Source Artifacts and Contract Authority

Read for this plan:

- `planning-mds/features/archive/F0018-policy-lifecycle-and-policy-360/PRD.md`
- `planning-mds/features/archive/F0018-policy-lifecycle-and-policy-360/F0018-S0001..S0011-*.md`
- `planning-mds/features/archive/F0018-policy-lifecycle-and-policy-360/F0018-ARCHITECT-REVIEW.md`
- `planning-mds/api/nebula-api.yaml`
- `planning-mds/schemas/policy*.schema.json`, `carrier-ref.schema.json`, `line-of-business.schema.json`, `timeline-event.schema.json`, `activity-event-payloads.schema.json`
- `planning-mds/security/policies/policy.csv`
- `planning-mds/architecture/decisions/ADR-018-policy-aggregate-versioning-and-reinstatement-window.md`
- `planning-mds/architecture/SOLUTION-PATTERNS.md`
- Existing backend/frontend source files listed below.

Reconciliation decisions:

| Topic | Product wording | Contract implementation authority | Decision |
|------|-----------------|-----------------------------------|----------|
| Carrier field | PRD uses nullable `CarrierRefId` plus free-text `CarrierName`. | OpenAPI/JSON Schema use required `carrierId` and display `carrierName`; `CarrierRef` schema uses `code`, `name`, `naicCode`, `amBestRating`. | Implement `CarrierRef` with required `CarrierId` on create/from-bind and optional denormalized `CarrierName` on reads. Import rows must resolve or seed a `CarrierRef`; do not ship free-text-only policy rows. |
| Broker field | Current stub uses `BrokerId`; contract uses `brokerOfRecordId`. | OpenAPI/JSON Schema use `brokerOfRecordId`. | Rewrite policy domain and DTOs to `BrokerOfRecordId`; leave migration/backfill logic to map old `BrokerId` into the new column. |
| Policy status | Current stub uses `CurrentStatus` values `Active`/`Bound`; PRD uses Pending/Issued/Expired/Cancelled. | OpenAPI/JSON Schema use `status` enum `Pending`, `Issued`, `Cancelled`, `Expired`. | Rewrite the aggregate to `Status`; migrate legacy `Active` -> `Issued`, `Bound` -> `Issued`. |
| Coverage fields | PRD names limit-per-occurrence, aggregate, deductible type, form references. | OpenAPI/JSON Schema use `coverageCode`, `coverageName`, `limit`, `deductible`, `premium`, `exposureBasis`, `exposureQuantity`. | MVP implementation follows the schema shape exactly. Rich sub-limits/form references remain future schema work unless the contract is updated by Architect. |
| Cancellation codes | PRD uses `Nonpayment`, `InsuredRequested`, `CoverageReplaced`, etc. | OpenAPI/JSON Schema use `NonPayment`, `InsuredRequest`, `UnderwritingDecision`, `MaterialMisrepresentation`, `CoverageNoLongerNeeded`, `CarrierWithdrawal`, `Other`. | Implement the schema enum exactly. UI labels may translate display text, but payload values must match schema. |
| Endorsement endpoint | Story mentions `POST /api/policies/{id}/endorsements`. | OpenAPI defines `POST /policies/{policyId}/endorse`. | Implement `/policies/{policyId}/endorse` and read-only `/policies/{policyId}/endorsements`. |
| Workflow type | ADR-018 says `WorkflowType="PolicyLifecycle"`. Existing pattern uses entity names like `Renewal`. | ADR is more specific for policy. | Use `WorkflowType = "PolicyLifecycle"` for policy lifecycle transitions and `EntityType = "Policy"` for timeline events. |

This plan is the primary implementation spec after G0. If a story conflicts with this plan, follow this plan and log the reconciliation under `workstate.py decision --topic plan-story-reconcile`.

## Slice Order

Implementation agents must preserve this sequence when using `SLICE_ORDER_SOURCE=assembly-plan`.

| Entry | Parallel Slice Set | Stories | Required completion signal |
|------|---------------------|---------|----------------------------|
| 1 | `[backend-domain-contract-reconcile]` | S0002, S0005, S0007, S0008, S0010 | Migration, entities, repository contracts, and endpoint stubs compile. |
| 2 | `[backend-read-create-profile, frontend-list-create-shell, quality-fixtures]` | S0001, S0002, S0003, S0011 | List/detail/create/profile/account summary APIs and shell screens work against mocks/test data. |
| 3 | `[backend-lifecycle-versioning, frontend-lifecycle-actions]` | S0005, S0006, S0007, S0008, S0010 | Issue, endorse, cancel, reinstate, versions, timeline, concurrency, and idempotency pass backend tests and UI action tests. |
| 4 | `[backend-policy-360-rails, frontend-policy-360-rails, devops-expiration-job]` | S0004, S0009, S0010, S0011 | Summary, rails, renewal linkage, account policy summary, documents placeholder, and expiration job evidence are complete. |
| 5 | `[quality-review-evidence]` | S0001-S0011 | Runtime evidence captured under `planning-mds/operations/evidence/**`; all required role self-reviews have evidence paths. |

## Existing Code (Must Be Modified)

| File | Current State | F0018 Change |
|------|---------------|--------------|
| `engine/src/Nebula.Domain/Entities/Policy.cs` | 13-property stub from F0007 (`BrokerId`, `Carrier`, `Premium`, `CurrentStatus`, fallback fields, Account/Broker navs). | Rewrite as the contract aggregate parent with `BrokerOfRecordId`, `CarrierId`, `Status`, lifecycle stamps, predecessor/current version pointers, version count, premium, producer, import source, fallback account fields, and nav collections. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/PolicyConfiguration.cs` | Maps stub `Policies` table and unique `PolicyNumber`; no version/coverage/endorsement constraints. | Rewrite mappings, case-insensitive policy-number uniqueness, status/filter indexes, `CurrentVersionId` FK, predecessor partial index, account/broker/carrier FKs, and row-version token. |
| `engine/src/Nebula.Infrastructure/Persistence/AppDbContext.cs` | Has `DbSet<Policy>` only. | Add `DbSet<PolicyVersion>`, `DbSet<PolicyEndorsement>`, `DbSet<PolicyCoverageLine>`, `DbSet<CarrierRef>`, and `DbSet<PolicyNumberSequence>`. |
| `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260411122220_F0018_PolicyStubAndF0007RenewalSlaReconcile.cs` | Created stub Policies table and legacy rows from renewals. | Leave historical migration intact; add a new F0018 reconciliation migration that transforms the current schema forward. |
| `engine/src/Nebula.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs` | Snapshot reflects policy stub. | EF migration updates snapshot after the new migration is generated. |
| `engine/src/Nebula.Infrastructure/Persistence/DevSeedData.cs` | Seeds policy stubs for renewal samples and updates fallback fields during account merge/deletion seed flows. | Seed CarrierRef rows, policy number sequence, policy versions/coverage lines for seeded policies, and PolicyReinstatementWindow thresholds. Update fallback refresh to new field names. |
| `engine/src/Nebula.Application/Interfaces/IReferenceDataRepository.cs` | Exposes `GetPolicyByIdAsync` for renewal creation. | Keep for compatibility, but add policy-specific repository methods to `IPolicyRepository` instead of expanding reference-data responsibilities. |
| `engine/src/Nebula.Infrastructure/Repositories/ReferenceDataRepository.cs` | Includes Policy with Account/Broker for renewal create. | Update include path to `BrokerOfRecord` and keep enough data for F0007. Do not put F0018 policy lifecycle queries here. |
| `engine/src/Nebula.Application/Services/RenewalService.cs` | Reads `policy.BrokerId`, `policy.Carrier`, `policy.Premium`, `policy.LineOfBusiness`, and `Policy.CurrentStatus` stub semantics. | Update to `BrokerOfRecordId`, `CarrierName`, `TotalPremium`, `Status`; successor lookup remains read-only from Renewal rows. |
| `engine/src/Nebula.Infrastructure/Repositories/RenewalRepository.cs` | Includes Policy and BoundPolicy against stub shape. | Include new Policy navs needed by mapping; no renewal workflow ownership changes. |
| `engine/src/Nebula.Api/Program.cs` | Registers existing services and endpoint groups; no policies endpoint group. | Register `PolicyService`, `PolicyExpirationHostedService`, validators, and map `app.MapPolicyEndpoints()`. |
| `engine/src/Nebula.Infrastructure/DependencyInjection.cs` | Registers repositories; no policy repository. | Register `IPolicyRepository -> PolicyRepository` and `ICarrierRefRepository` if split. |
| `engine/src/Nebula.Infrastructure/Authorization/PolicyAuthorizationService.cs` | Existing Casbin bridge. | No shape change expected; add policy attribute hydration only if current helper cannot evaluate account/broker/region scope. |
| `planning-mds/security/policies/policy.csv` | Already includes `policy:*` action rows for internal roles. | Verify copied embedded policy resource, if any, matches planning CSV before runtime evidence. |
| `planning-mds/security/authorization-matrix.md` | Does not yet show a policy-specific matrix section. | Security reviewer must add/reconcile matrix evidence before G4.5 if policy auth evidence is required. |
| `experience/src/App.tsx` | Routes include accounts, submissions, renewals, brokers, tasks; no policies routes. | Add `/policies`, `/policies/new`, `/policies/import`, `/policies/:policyId`. |
| `experience/src/components/layout/Sidebar.tsx` | Navigation has no Policy item. | Add Policies nav item using existing icon/button conventions and active route handling. |
| `experience/src/features/accounts/types.ts` | `AccountPolicyListItemDto` still uses stub fields (`carrier`, `premium`, `currentStatus`). | Replace with contract-shaped policy list item fields and add `PolicyAccountSummaryDto` composition fields. |
| `experience/src/features/accounts/hooks.ts` | `useAccountPolicies` reads `/accounts/{id}/policies`; account summary ignores F0018 policy summary. | Keep rail hook, update types/query params, add `useAccountPolicySummary`, and invalidate policies on policy mutations. |
| `experience/src/pages/AccountDetailPage.tsx` | Shows Account 360 policy rail from stub DTO. | Render status badges, fallback account context, policy links, active/expired/cancelled/pending summary chips, and create-policy entry point. |
| `experience/src/services/api.ts` | Supports GET/POST/PUT/DELETE with headers. | No core change expected; policy hooks use existing headers for `If-Match` and `Idempotency-Key`. |
| `experience/src/mocks/handlers.ts`, `experience/src/mocks/data.ts` | No F0018 policy handlers/data. | Add policy list/detail/summary/rails/action mock handlers and account policy summary mocks. |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `engine/src/Nebula.Domain/Entities/PolicyVersion.cs` | Domain | Immutable version snapshot entity. |
| `engine/src/Nebula.Domain/Entities/PolicyEndorsement.cs` | Domain | Append-only endorsement event entity. |
| `engine/src/Nebula.Domain/Entities/PolicyCoverageLine.cs` | Domain | Materialized coverage line per policy version. |
| `engine/src/Nebula.Domain/Entities/CarrierRef.cs` | Domain | Lightweight carrier seed lookup. |
| `engine/src/Nebula.Domain/Entities/PolicyNumberSequence.cs` | Domain | Per-LOB-per-year policy number sequence row. |
| `engine/src/Nebula.Domain/Workflow/PolicyLifecycleStateMachine.cs` | Domain | State/transition validation and available transition computation. |
| `engine/src/Nebula.Application/DTOs/PolicyDtos.cs` | Application | DTO records matching OpenAPI schemas. |
| `engine/src/Nebula.Application/DTOs/PolicyListQuery.cs` | Application | Policy list filter/sort/page query. |
| `engine/src/Nebula.Application/Interfaces/IPolicyRepository.cs` | Application | Repository contract for policy reads/writes/locking/numbering. |
| `engine/src/Nebula.Application/Services/PolicyService.cs` | Application | Use-case service for list/create/update/lifecycle/rails/summary. |
| `engine/src/Nebula.Application/Services/PolicyNumberGenerator.cs` | Application | Deterministic `NEB-{LOB-prefix}-{yyyy}-{6-digit-sequence}` generator. |
| `engine/src/Nebula.Application/Services/PolicyExpirationService.cs` | Application | Idempotent daily sweep logic. |
| `engine/src/Nebula.Application/Validators/PolicyCreateValidator.cs` | Application | Create/from-bind request validation. |
| `engine/src/Nebula.Application/Validators/PolicyUpdateValidator.cs` | Application | Profile edit validation. |
| `engine/src/Nebula.Application/Validators/PolicyEndorsementValidator.cs` | Application | Endorsement validation. |
| `engine/src/Nebula.Application/Validators/PolicyCancelValidator.cs` | Application | Cancellation validation. |
| `engine/src/Nebula.Application/Validators/PolicyReinstateValidator.cs` | Application | Reinstatement validation. |
| `engine/src/Nebula.Infrastructure/Repositories/PolicyRepository.cs` | Infrastructure | EF Core repository and ABAC-scoped query composition. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/PolicyVersionConfiguration.cs` | Infrastructure | Version table mapping and unique `(PolicyId, VersionNumber)`. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/PolicyEndorsementConfiguration.cs` | Infrastructure | Endorsement table mapping and unique `(PolicyId, EndorsementNumber)`. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/PolicyCoverageLineConfiguration.cs` | Infrastructure | Coverage mapping and current-version query indexes. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/CarrierRefConfiguration.cs` | Infrastructure | Carrier lookup mapping and seed uniqueness. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/PolicyNumberSequenceConfiguration.cs` | Infrastructure | Sequence-row mapping for concurrency-safe numbering. |
| `engine/src/Nebula.Infrastructure/Services/PolicyExpirationHostedService.cs` | Infrastructure | Hosted service wrapper that invokes the sweep at 00:15 UTC. |
| `engine/src/Nebula.Api/Endpoints/PolicyEndpoints.cs` | API | Minimal API group for all `/policies` and account policy endpoints. |
| `engine/tests/Nebula.Tests/Unit/PolicyLifecycleStateMachineTests.cs` | Tests | State transition and role matrix tests. |
| `engine/tests/Nebula.Tests/Unit/PolicyNumberGeneratorTests.cs` | Tests | Format, rollover, and concurrency retry tests. |
| `engine/tests/Nebula.Tests/Integration/PolicyEndpointTests.cs` | Tests | API happy/edge paths. |
| `engine/tests/Nebula.Tests/Integration/PolicyAuthorizationTests.cs` | Tests | Casbin and ABAC scope tests. |
| `engine/tests/Nebula.Tests/Integration/PolicyTimelineTests.cs` | Tests | Transactional timeline/transition tests. |
| `experience/src/features/policies/types.ts` | Frontend | Contract-shaped Policy TypeScript types. |
| `experience/src/features/policies/hooks.ts` | Frontend | TanStack Query hooks and mutations for policy APIs. |
| `experience/src/features/policies/lib/format.ts` | Frontend | Policy number/status/premium/date helpers. |
| `experience/src/features/policies/lib/validation.ts` | Frontend | React Hook Form/AJV schema validators. |
| `experience/src/features/policies/components/PolicyStatusBadge.tsx` | Frontend | Status badge with text plus visual treatment. |
| `experience/src/features/policies/components/PolicyFilterToolbar.tsx` | Frontend | Search/filter/sort controls. |
| `experience/src/features/policies/components/PolicyListTable.tsx` | Frontend | Accessible paginated policy table. |
| `experience/src/features/policies/components/PolicySummaryHeader.tsx` | Frontend | Policy detail/360 header and quick actions. |
| `experience/src/features/policies/components/PolicyProfilePanel.tsx` | Frontend | Profile read/edit panel. |
| `experience/src/features/policies/components/CoverageLinesEditor.tsx` | Frontend | Create/edit/endorse coverage-line editor. |
| `experience/src/features/policies/components/PolicyRails.tsx` | Frontend | Versions, endorsements, coverages, renewals, documents, activity rails. |
| `experience/src/features/policies/components/IssuePolicyDialog.tsx` | Frontend | Pending -> Issued action dialog. |
| `experience/src/features/policies/components/EndorsePolicyDialog.tsx` | Frontend | Endorsement wizard/dialog. |
| `experience/src/features/policies/components/CancelPolicyDialog.tsx` | Frontend | Cancellation dialog. |
| `experience/src/features/policies/components/ReinstatePolicyDialog.tsx` | Frontend | Reinstatement dialog. |
| `experience/src/features/policies/index.ts` | Frontend | Feature exports. |
| `experience/src/pages/PoliciesPage.tsx` | Frontend | Policy List route. |
| `experience/src/pages/CreatePolicyPage.tsx` | Frontend | Manual create route. |
| `experience/src/pages/PolicyImportPage.tsx` | Frontend | Import-lite route. |
| `experience/src/pages/PolicyDetailPage.tsx` | Frontend | Policy detail and Policy 360 route. |
| `experience/src/features/policies/tests/PolicyListPage.integration.test.tsx` | Frontend tests | List filters, navigation, empty/error states. |
| `experience/src/features/policies/tests/PolicyDetailPage.integration.test.tsx` | Frontend tests | Header, rails, lifecycle actions. |
| `experience/src/features/policies/tests/PolicyForms.validation.test.tsx` | Frontend tests | Schema-backed form validation. |

## Step 1 - Backend Domain, Persistence, and Contract Reconciliation (S0002, S0005, S0007, S0008, S0010)

### New Files

| File | Layer |
|------|-------|
| `engine/src/Nebula.Domain/Entities/PolicyVersion.cs` | Domain |
| `engine/src/Nebula.Domain/Entities/PolicyEndorsement.cs` | Domain |
| `engine/src/Nebula.Domain/Entities/PolicyCoverageLine.cs` | Domain |
| `engine/src/Nebula.Domain/Entities/CarrierRef.cs` | Domain |
| `engine/src/Nebula.Domain/Entities/PolicyNumberSequence.cs` | Domain |
| `engine/src/Nebula.Domain/Workflow/PolicyLifecycleStateMachine.cs` | Domain |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/*Policy*.cs` | Infrastructure |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/CarrierRefConfiguration.cs` | Infrastructure |

### Modified Files

| File | Change |
|------|--------|
| `engine/src/Nebula.Domain/Entities/Policy.cs` | Replace stub with contract parent aggregate. |
| `engine/src/Nebula.Infrastructure/Persistence/AppDbContext.cs` | Add DbSets. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/PolicyConfiguration.cs` | Replace stub mapping and indexes. |
| `engine/src/Nebula.Infrastructure/Persistence/DevSeedData.cs` | Seed carrier refs, policy versions, coverage lines, reinstatement thresholds. |

### Entity Code Signatures

```csharp
// engine/src/Nebula.Domain/Entities/Policy.cs
namespace Nebula.Domain.Entities;

public class Policy : BaseEntity
{
    public Guid AccountId { get; set; }
    public Guid BrokerOfRecordId { get; set; }
    public Guid CarrierId { get; set; }
    public string PolicyNumber { get; set; } = default!;
    public string LineOfBusiness { get; set; } = default!;
    public string Status { get; set; } = PolicyStatuses.Pending;
    public DateTime EffectiveDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public DateTime? BoundAt { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? CancellationEffectiveDate { get; set; }
    public string? CancellationReasonCode { get; set; }
    public string? CancellationReasonDetail { get; set; }
    public DateTime? ReinstatementDeadline { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public Guid? PredecessorPolicyId { get; set; }
    public Guid? CurrentVersionId { get; set; }
    public int CurrentVersionNumber { get; set; }
    public int VersionCount { get; set; }
    public decimal TotalPremium { get; set; }
    public string PremiumCurrency { get; set; } = "USD";
    public Guid? ProducerUserId { get; set; }
    public string? ImportSource { get; set; }
    public string? ExternalPolicyReference { get; set; }
    public string AccountDisplayNameAtLink { get; set; } = default!;
    public string AccountStatusAtRead { get; set; } = default!;
    public Guid? AccountSurvivorId { get; set; }

    public Account Account { get; set; } = default!;
    public Broker BrokerOfRecord { get; set; } = default!;
    public CarrierRef Carrier { get; set; } = default!;
    public Policy? PredecessorPolicy { get; set; }
    public PolicyVersion? CurrentVersion { get; set; }
    public UserProfile? ProducerUser { get; set; }
    public ICollection<PolicyVersion> Versions { get; } = new List<PolicyVersion>();
    public ICollection<PolicyEndorsement> Endorsements { get; } = new List<PolicyEndorsement>();
    public ICollection<PolicyCoverageLine> CoverageLines { get; } = new List<PolicyCoverageLine>();
}

public static class PolicyStatuses
{
    public const string Pending = "Pending";
    public const string Issued = "Issued";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";
}
```

```csharp
// engine/src/Nebula.Domain/Entities/PolicyVersion.cs
namespace Nebula.Domain.Entities;

public class PolicyVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PolicyId { get; set; }
    public int VersionNumber { get; set; }
    public string VersionReason { get; set; } = default!;
    public Guid? EndorsementId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public decimal TotalPremium { get; set; }
    public string PremiumCurrency { get; set; } = "USD";
    public string ProfileSnapshotJson { get; set; } = default!;
    public string CoverageSnapshotJson { get; set; } = default!;
    public string PremiumSnapshotJson { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public Policy Policy { get; set; } = default!;
    public PolicyEndorsement? Endorsement { get; set; }
    public ICollection<PolicyCoverageLine> CoverageLines { get; } = new List<PolicyCoverageLine>();
}
```

```csharp
// engine/src/Nebula.Domain/Entities/PolicyEndorsement.cs
namespace Nebula.Domain.Entities;

public class PolicyEndorsement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PolicyId { get; set; }
    public int EndorsementNumber { get; set; }
    public Guid PolicyVersionId { get; set; }
    public string EndorsementReasonCode { get; set; } = default!;
    public string? EndorsementReasonDetail { get; set; }
    public DateTime EffectiveDate { get; set; }
    public decimal PremiumDelta { get; set; }
    public string PremiumCurrency { get; set; } = "USD";
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public Policy Policy { get; set; } = default!;
    public PolicyVersion PolicyVersion { get; set; } = default!;
}
```

```csharp
// engine/src/Nebula.Domain/Entities/PolicyCoverageLine.cs
namespace Nebula.Domain.Entities;

public class PolicyCoverageLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PolicyId { get; set; }
    public Guid PolicyVersionId { get; set; }
    public int VersionNumber { get; set; }
    public string CoverageCode { get; set; } = default!;
    public string? CoverageName { get; set; }
    public decimal Limit { get; set; }
    public decimal? Deductible { get; set; }
    public decimal Premium { get; set; }
    public string PremiumCurrency { get; set; } = "USD";
    public string? ExposureBasis { get; set; }
    public decimal? ExposureQuantity { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime CreatedAt { get; set; }

    public Policy Policy { get; set; } = default!;
    public PolicyVersion PolicyVersion { get; set; } = default!;
}
```

```csharp
// engine/src/Nebula.Domain/Entities/CarrierRef.cs
namespace Nebula.Domain.Entities;

public class CarrierRef
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? NaicCode { get; set; }
    public string? AmBestRating { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

```csharp
// engine/src/Nebula.Domain/Entities/PolicyNumberSequence.cs
namespace Nebula.Domain.Entities;

public class PolicyNumberSequence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string LineOfBusiness { get; set; } = default!;
    public int Year { get; set; }
    public int LastSequence { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Persistence Rules

1. New migration name: `F0018_PolicyLifecycleAndPolicy360`.
2. Preserve existing `Policies.Id` values used by F0007 `Renewal.PolicyId` and `Renewal.BoundPolicyId`.
3. Rename/migrate stub columns:
   - `BrokerId` -> `BrokerOfRecordId`.
   - `Carrier` -> `CarrierName` during migration only; create/resolve `CarrierRef` rows and populate `CarrierId`.
   - `Premium` -> `TotalPremium` defaulting to 0 when null.
   - `CurrentStatus` `Active`/`Bound` -> `Issued`; unknown -> `Pending`.
4. Add tables `CarrierRefs`, `PolicyVersions`, `PolicyEndorsements`, `PolicyCoverageLines`, `PolicyNumberSequences`.
5. For every existing stub policy, insert a v1 `PolicyVersion` with `VersionReason=IssuedInitial`, `CoverageSnapshotJson=[]`, `PremiumSnapshotJson`, and point `Policy.CurrentVersionId` to it.
6. Seed `WorkflowSlaThreshold` rows where `EntityType="PolicyReinstatementWindow"`, `Status="ReinstatementWindow"`, `LineOfBusiness` is Property/GeneralLiability/WorkersCompensation/ProfessionalLiability/Cyber/null, and `TargetDays` holds the window days.
7. Indexes:
   - unique case-insensitive policy number index on `lower("PolicyNumber")`.
   - `IX_Policies_AccountId_Status`.
   - `IX_Policies_ExpirationDate_Status`.
   - `IX_Policies_Status_LineOfBusiness`.
   - `IX_Policies_CarrierId_Status`.
   - partial `IX_Policies_PredecessorPolicyId` where non-null.
   - unique `UX_PolicyVersions_PolicyId_VersionNumber`.
   - unique `UX_PolicyEndorsements_PolicyId_EndorsementNumber`.
   - `IX_PolicyCoverageLines_PolicyVersionId`.
   - `IX_PolicyCoverageLines_PolicyId_IsCurrent`.

### Step 1 Completion Criteria

- Domain and EF mappings represent the OpenAPI/JSON Schema contract.
- Existing F0007 renewal links continue to resolve.
- Legacy policy stubs are migrated to contract-compatible rows.
- No API endpoint behavior is claimed complete until Step 2/3.

## Step 2 - Backend Read/Create/Profile APIs and Account Summary (S0001, S0002, S0003, S0011)

### New Files

| File | Layer |
|------|-------|
| `engine/src/Nebula.Application/DTOs/PolicyDtos.cs` | Application |
| `engine/src/Nebula.Application/DTOs/PolicyListQuery.cs` | Application |
| `engine/src/Nebula.Application/Interfaces/IPolicyRepository.cs` | Application |
| `engine/src/Nebula.Application/Services/PolicyService.cs` | Application |
| `engine/src/Nebula.Application/Services/PolicyNumberGenerator.cs` | Application |
| `engine/src/Nebula.Application/Validators/PolicyCreateValidator.cs` | Application |
| `engine/src/Nebula.Application/Validators/PolicyUpdateValidator.cs` | Application |
| `engine/src/Nebula.Infrastructure/Repositories/PolicyRepository.cs` | Infrastructure |
| `engine/src/Nebula.Api/Endpoints/PolicyEndpoints.cs` | API |

### DTO and Repository Signatures

```csharp
// engine/src/Nebula.Application/DTOs/PolicyDtos.cs
namespace Nebula.Application.DTOs;

public sealed record PolicyDto(
    Guid Id,
    Guid AccountId,
    Guid BrokerOfRecordId,
    string PolicyNumber,
    string LineOfBusiness,
    Guid CarrierId,
    string? CarrierName,
    string Status,
    DateTime EffectiveDate,
    DateTime ExpirationDate,
    DateTime? BoundAt,
    DateTime? IssuedAt,
    DateTime? CancelledAt,
    DateTime? CancellationEffectiveDate,
    string? CancellationReasonCode,
    string? CancellationReasonDetail,
    DateTime? ReinstatementDeadline,
    DateTime? ExpiredAt,
    Guid? PredecessorPolicyId,
    Guid? SuccessorPolicyId,
    Guid? CurrentVersionId,
    int CurrentVersionNumber,
    int VersionCount,
    decimal TotalPremium,
    string PremiumCurrency,
    Guid? ProducerUserId,
    string? ProducerDisplayName,
    string? ImportSource,
    string? ExternalPolicyReference,
    string? AccountDisplayNameAtLink,
    string? AccountStatusAtRead,
    Guid? AccountSurvivorId,
    IReadOnlyList<string> AvailableTransitions,
    string RowVersion,
    DateTime CreatedAt,
    Guid? CreatedByUserId,
    DateTime UpdatedAt,
    Guid? UpdatedByUserId);

public sealed record PolicyListItemDto(
    Guid Id,
    string PolicyNumber,
    Guid AccountId,
    string? AccountDisplayName,
    string? AccountStatus,
    Guid? AccountSurvivorId,
    Guid BrokerOfRecordId,
    string? BrokerName,
    Guid? CarrierId,
    string? CarrierName,
    string LineOfBusiness,
    string Status,
    DateTime EffectiveDate,
    DateTime ExpirationDate,
    decimal? TotalPremium,
    string? PremiumCurrency,
    Guid? ProducerUserId,
    string? ProducerDisplayName,
    int? VersionCount,
    int? EndorsementCount,
    bool? HasOpenRenewal,
    DateTime? ReinstatementDeadline,
    string RowVersion);

public sealed record PolicyCreateRequestDto(
    Guid AccountId,
    Guid BrokerOfRecordId,
    string LineOfBusiness,
    Guid CarrierId,
    DateTime EffectiveDate,
    DateTime ExpirationDate,
    Guid? PredecessorPolicyId,
    Guid? ProducerUserId,
    decimal? TotalPremium,
    string? PremiumCurrency,
    string? ImportMode,
    string? ExternalPolicyReference,
    IReadOnlyList<PolicyCoverageInputDto>? Coverages);

public sealed record PolicyUpdateRequestDto(
    Guid? ProducerUserId,
    Guid? BrokerOfRecordId,
    Guid? CarrierId,
    DateTime? EffectiveDate,
    DateTime? ExpirationDate,
    decimal? TotalPremium,
    string? ExternalPolicyReference);

public sealed record PolicyCoverageInputDto(
    string CoverageCode,
    string? CoverageName,
    decimal Limit,
    decimal? Deductible,
    decimal Premium,
    string? ExposureBasis,
    decimal? ExposureQuantity);

public sealed record PolicyAccountSummaryDto(
    Guid AccountId,
    int ActivePolicyCount,
    int ExpiredPolicyCount,
    int CancelledPolicyCount,
    int PendingPolicyCount,
    DateTime? NextExpiringDate,
    Guid? NextExpiringPolicyId,
    string? NextExpiringPolicyNumber,
    decimal TotalCurrentPremium,
    string PremiumCurrency,
    DateTime ComputedAt);
```

```csharp
// engine/src/Nebula.Application/Interfaces/IPolicyRepository.cs
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IPolicyRepository
{
    Task<PaginatedResult<Policy>> ListAsync(PolicyListQuery query, CancellationToken ct = default);
    Task<Policy?> GetByIdWithRelationsAsync(Guid policyId, CancellationToken ct = default);
    Task<Policy?> GetByIdForUpdateAsync(Guid policyId, CancellationToken ct = default);
    Task<Policy?> GetPredecessorAsync(Guid predecessorPolicyId, CancellationToken ct = default);
    Task<Guid?> GetSuccessorPolicyIdAsync(Guid policyId, CancellationToken ct = default);
    Task<bool> PolicyNumberExistsAsync(string policyNumber, CancellationToken ct = default);
    Task<int> NextPolicyNumberSequenceAsync(string lineOfBusiness, int year, CancellationToken ct = default);
    Task AddAsync(Policy policy, CancellationToken ct = default);
    Task AddVersionAsync(PolicyVersion version, CancellationToken ct = default);
    Task AddCoverageLinesAsync(IEnumerable<PolicyCoverageLine> coverageLines, CancellationToken ct = default);
    Task AddEndorsementAsync(PolicyEndorsement endorsement, CancellationToken ct = default);
    Task<CarrierRef?> GetCarrierAsync(Guid carrierId, CancellationToken ct = default);
    Task<Account?> GetAccountAsync(Guid accountId, CancellationToken ct = default);
    Task<PolicyAccountSummaryDto> GetAccountSummaryAsync(Guid accountId, ICurrentUserService user, CancellationToken ct = default);
    Task<IReadOnlyList<PolicyCoverageLine>> ListCurrentCoveragesAsync(Guid policyId, CancellationToken ct = default);
    Task<PaginatedResult<PolicyVersion>> ListVersionsAsync(Guid policyId, int page, int pageSize, CancellationToken ct = default);
    Task<PaginatedResult<PolicyEndorsement>> ListEndorsementsAsync(Guid policyId, int page, int pageSize, CancellationToken ct = default);
}
```

### API Endpoint Group

```csharp
// engine/src/Nebula.Api/Endpoints/PolicyEndpoints.cs
namespace Nebula.Api.Endpoints;

public static class PolicyEndpoints
{
    public static IEndpointRouteBuilder MapPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/policies")
            .WithTags("Policies")
            .RequireAuthorization();

        group.MapGet("/", ListPolicies);
        group.MapPost("/", CreatePolicy);
        group.MapPost("/from-bind", CreatePolicyFromBind);
        group.MapPost("/import", ImportPolicies);
        group.MapGet("/{policyId:guid}", GetPolicy);
        group.MapPut("/{policyId:guid}", UpdatePolicy);
        group.MapGet("/{policyId:guid}/summary", GetPolicySummary);
        group.MapPost("/{policyId:guid}/issue", IssuePolicy);
        group.MapPost("/{policyId:guid}/endorse", EndorsePolicy);
        group.MapPost("/{policyId:guid}/cancel", CancelPolicy);
        group.MapPost("/{policyId:guid}/reinstate", ReinstatePolicy);
        group.MapGet("/{policyId:guid}/versions", ListVersions);
        group.MapGet("/{policyId:guid}/versions/{versionId:guid}", GetVersion);
        group.MapGet("/{policyId:guid}/endorsements", ListEndorsements);
        group.MapGet("/{policyId:guid}/coverages", ListCoverages);
        group.MapGet("/{policyId:guid}/timeline", ListTimeline);

        app.MapGet("/accounts/{accountId:guid}/policies", ListAccountPolicies)
            .WithTags("Policies", "Accounts")
            .RequireAuthorization();
        app.MapGet("/accounts/{accountId:guid}/policies/summary", GetAccountPolicySummary)
            .WithTags("Policies", "Accounts")
            .RequireAuthorization();

        return app;
    }
}
```

### Logic Flow - `PolicyService.CreateAsync`

```
CreateAsync(PolicyCreateRequestDto dto, ICurrentUserService user, string idempotencyKey, CancellationToken ct)
-> returns (PolicyDto? Dto, string? ErrorCode, string? ErrorDetail)
```

1. Check idempotency store using endpoint key `POST:/policies:{idempotencyKey}`; return stored response if present.
2. Authorize `policy:create` for manual rows, `policy:import` for import rows.
3. Load Account with BrokerOfRecord and fallback fields. Reject `Merged` with `account_merged_reject`; reject `Deleted` with `account_deleted_reject`.
4. Validate `BrokerOfRecordId` belongs to the account or is authorized by role scope; reject with `policy_denied`.
5. Load active `CarrierRef` by `CarrierId`; reject inactive/missing with `invalid_carrier`.
6. Validate date range, LOB, premium, predecessor policy, and predecessor account/state.
7. Generate `PolicyNumber` using `PolicyNumberGenerator.NextAsync(lineOfBusiness, effectiveDate.Year)` unless a future import-specific payload allows user-provided numbers.
8. Build `Policy` with:
   - `Status = Pending` for manual/from-bind, `Issued` for import rows using `ImportMode=csv-import`.
   - `AccountDisplayNameAtLink = account.StableDisplayName`.
   - `AccountStatusAtRead = account.Status`.
   - `AccountSurvivorId = account.MergedIntoAccountId`.
   - audit fields from `user.UserId` and `DateTime.UtcNow`.
9. Add the policy.
10. If import row lands as Issued, create v1 `PolicyVersion`, materialized `PolicyCoverageLine[]`, `WorkflowTransition` from null to Issued, and `policy.imported` timeline event. If manual Pending with coverages, store draft coverage only if the schema remains contract-approved; otherwise no version until issue.
11. Add `policy.created` timeline event for manual/create path.
12. Commit with `unitOfWork.CommitAsync(ct)`.
13. Store idempotency result and return reloaded `PolicyDto`.

### Logic Flow - `PolicyService.UpdateAsync`

```
UpdateAsync(Guid policyId, PolicyUpdateRequestDto dto, uint expectedRowVersion, ICurrentUserService user, CancellationToken ct)
-> returns (PolicyDto? Dto, string? ErrorCode)
```

1. Load policy for update with Account, BrokerOfRecord, Carrier, CurrentVersion.
2. Return `not_found` if not found or not readable in caller scope.
3. Compare `policy.RowVersion` to `expectedRowVersion`; return `precondition_failed` if stale.
4. Authorize `policy:update`.
5. If `Status=Pending`, allow contract fields in `PolicyUpdateRequestDto`.
6. If `Status=Issued`, allow only non-material fields already in contract and approved for issued state. For material fields (`carrierId`, `effectiveDate`, `expirationDate`, `totalPremium`), return 409 `must_use_endorse`.
7. If `Status=Cancelled` or `Expired`, return 409 `readonly_state`.
8. Update audit fields and append `ActivityTimelineEvent` `policy.profile_updated` with a JSON diff summary.
9. Commit and return updated DTO.

### Account Summary Rules

`GetAccountPolicySummaryAsync` must:

1. Verify the account exists and is visible.
2. Apply the same policy ABAC scope as policy list.
3. Count `Issued`, `Expired`, `Cancelled`, and `Pending`.
4. Compute `nextExpiringDate` and `nextExpiringPolicyId` from readable `Issued` policies.
5. Sum `TotalPremium` for readable `Issued` policies.
6. Return `premiumCurrency="MIXED"` only if multiple currencies appear; MVP should normally return `USD`.

### HTTP Responses

| Endpoint | Success | Key error mappings |
|----------|---------|--------------------|
| `GET /policies` | 200 paginated `PolicyListItemDto` | 400 invalid filter, 403 policy denied |
| `POST /policies` | 201 `PolicyDto` | 400 validation, 403 denied, 409 `policy_number_conflict` |
| `POST /policies/from-bind` | 201 `PolicyDto` or 501 until F0019 implementation is enabled | 400/403/409/501 |
| `POST /policies/import` | 200 `{ accepted, rejected }` | 400/403 |
| `GET /policies/{id}` | 200 `PolicyDto` | 403/404 |
| `PUT /policies/{id}` | 200 `PolicyDto` | 400, 403, 404, 409 `must_use_endorse`/`readonly_state`, 412 |
| `GET /accounts/{id}/policies/summary` | 200 `PolicyAccountSummaryDto` | 403/404 |

## Step 3 - Backend Lifecycle, Versions, Endorsements, Cancellation, Reinstatement, Timeline (S0005, S0006, S0007, S0008, S0010)

### New Files

| File | Layer |
|------|-------|
| `engine/src/Nebula.Application/Validators/PolicyEndorsementValidator.cs` | Application |
| `engine/src/Nebula.Application/Validators/PolicyCancelValidator.cs` | Application |
| `engine/src/Nebula.Application/Validators/PolicyReinstateValidator.cs` | Application |
| `engine/src/Nebula.Application/Services/PolicyExpirationService.cs` | Application |

### Request and Result Signatures

```csharp
// engine/src/Nebula.Application/DTOs/PolicyDtos.cs
public sealed record PolicyIssueRequestDto(string? IssueNotes);

public sealed record PolicyEndorsementRequestDto(
    string EndorsementReasonCode,
    string? EndorsementReasonDetail,
    DateTime EffectiveDate,
    decimal? PremiumDelta,
    IReadOnlyList<PolicyCoverageInputDto> Coverages);

public sealed record PolicyCancelRequestDto(
    string CancellationReasonCode,
    string? CancellationReasonDetail,
    DateTime CancellationEffectiveDate);

public sealed record PolicyReinstateRequestDto(
    string ReinstatementReason,
    string? ReinstatementDetail);

public sealed record PolicyVersionDto(
    Guid Id,
    Guid PolicyId,
    int VersionNumber,
    string VersionReason,
    Guid? EndorsementId,
    DateTime EffectiveDate,
    DateTime ExpirationDate,
    decimal TotalPremium,
    string PremiumCurrency,
    object ProfileSnapshot,
    IReadOnlyList<object> CoverageSnapshot,
    object PremiumSnapshot,
    DateTime CreatedAt,
    Guid? CreatedByUserId);

public sealed record PolicyEndorsementDto(
    Guid Id,
    Guid PolicyId,
    int EndorsementNumber,
    Guid PolicyVersionId,
    int? VersionNumber,
    string EndorsementReasonCode,
    string? EndorsementReasonDetail,
    DateTime EffectiveDate,
    decimal PremiumDelta,
    string? PremiumCurrency,
    DateTime CreatedAt,
    Guid? CreatedByUserId);
```

### Logic Flow - `IssueAsync`

```
IssueAsync(Guid policyId, PolicyIssueRequestDto dto, uint expectedRowVersion, ICurrentUserService user, string idempotencyKey, CancellationToken ct)
-> returns (PolicyDto? Dto, string? ErrorCode)
```

1. Return stored idempotency result for `POST:/policies/{id}/issue:{idempotencyKey}` if present.
2. Load policy for update with current/draft coverage state.
3. Authorize `policy:issue` and readable account scope.
4. Check `RowVersion`; stale returns `precondition_failed`.
5. Validate `Status=Pending`; otherwise `invalid_transition`.
6. Validate issue preconditions:
   - at least one coverage line or request coverage set exists,
   - `ExpirationDate > EffectiveDate`,
   - `TotalPremium >= 0`,
   - `LineOfBusiness` in schema set,
   - carrier and account references valid.
7. Create v1 `PolicyVersion` with `VersionReason=IssuedInitial`, profile snapshot, coverage snapshot, premium snapshot.
8. Create `PolicyCoverageLine[]` for v1 and mark `IsCurrent=true`.
9. Update parent: `Status=Issued`, `BoundAt=now`, `IssuedAt=now`, `CurrentVersionId=v1.Id`, `CurrentVersionNumber=1`, `VersionCount=1`, audit fields.
10. Add `WorkflowTransition` with `WorkflowType="PolicyLifecycle"`, `FromState=Pending`, `ToState=Issued`.
11. Add timeline event `policy.issued`.
12. Commit, store idempotency result, return `PolicyDto`.

### Logic Flow - `EndorseAsync`

```
EndorseAsync(Guid policyId, PolicyEndorsementRequestDto dto, uint expectedRowVersion, ICurrentUserService user, string idempotencyKey, CancellationToken ct)
-> returns (PolicyEndorsementDto? Dto, string? ErrorCode)
```

1. Return stored idempotency result if present.
2. Load policy for update with current version and current coverage lines.
3. Authorize `policy:endorse`.
4. Check row version.
5. Validate `Status=Issued`; otherwise `invalid_transition`.
6. Validate reason code enum from schema, detail when `OtherAdministrative`, and `EffectiveDate` within policy term.
7. Compare incoming full coverage set plus premium delta to current version. If no actual difference, return `empty_endorsement`.
8. Compute next endorsement number and version number under the same transaction.
9. Set all previous current coverage lines `IsCurrent=false`.
10. Create `PolicyEndorsement`.
11. Create new `PolicyVersion` with `VersionReason=Endorsement`, complete snapshots, and `EndorsementId`.
12. Create replacement `PolicyCoverageLine[]` with `IsCurrent=true`.
13. Update parent `CurrentVersionId`, `CurrentVersionNumber`, `VersionCount`, `TotalPremium`, audit fields.
14. Add `WorkflowTransition` `Issued -> Issued` with reason.
15. Add timeline event `policy.endorsed`.
16. Commit atomically.

### Logic Flow - `CancelAsync`

```
CancelAsync(Guid policyId, PolicyCancelRequestDto dto, uint expectedRowVersion, ICurrentUserService user, string idempotencyKey, CancellationToken ct)
-> returns (PolicyDto? Dto, string? ErrorCode)
```

1. Return stored idempotency result if present.
2. Load policy for update.
3. Authorize `policy:cancel`.
4. Check row version.
5. Validate `Status=Issued`; otherwise `invalid_transition`.
6. Validate reason code enum from schema and detail when `Other`.
7. Validate `CancellationEffectiveDate` within `[EffectiveDate, ExpirationDate]`. Values before `EffectiveDate` must be clamped to `EffectiveDate` only if API response includes a warning header; otherwise reject and keep behavior consistent with schema.
8. Resolve reinstatement window from `WorkflowSlaThreshold` using `EntityType="PolicyReinstatementWindow"`, `Status="ReinstatementWindow"`, and LOB fallback.
9. Update parent: `Status=Cancelled`, `CancelledAt=now`, cancellation fields, `ReinstatementDeadline`, audit fields.
10. Add `WorkflowTransition` `Issued -> Cancelled`.
11. Add timeline event `policy.cancelled`.
12. Commit and return `PolicyDto`.

### Logic Flow - `ReinstateAsync`

```
ReinstateAsync(Guid policyId, PolicyReinstateRequestDto dto, uint expectedRowVersion, ICurrentUserService user, string idempotencyKey, CancellationToken ct)
-> returns (PolicyDto? Dto, string? ErrorCode)
```

1. Return stored idempotency result if present.
2. Load policy for update with current version and current coverage lines.
3. Authorize `policy:reinstate`.
4. Check row version.
5. Validate `Status=Cancelled`; otherwise `invalid_transition`.
6. Validate `DateTime.UtcNow.Date <= ReinstatementDeadline`; otherwise `reinstatement_window_expired`.
7. Validate reason enum and detail when `Other`.
8. Create new `PolicyVersion` with `VersionReason=Reinstatement` from the pre-cancellation current terms.
9. Mark previous coverage rows non-current and re-materialize rows under the new version with `IsCurrent=true`.
10. Update parent: `Status=Issued`, clear cancellation fields and reinstatement deadline, update current version pointer/count and audit fields.
11. Add `WorkflowTransition` `Cancelled -> Issued`.
12. Add timeline event `policy.reinstated`.
13. Commit and return `PolicyDto`.

### Logic Flow - `ExpireIssuedPoliciesAsync`

```
ExpireIssuedPoliciesAsync(DateTime utcNow, CancellationToken ct)
-> returns PolicyExpirationRunResult(processed, expired, skipped, failed)
```

1. Compute `today = utcNow.Date`.
2. Page through `Issued` policies where `ExpirationDate < today`.
3. For each policy, lock row and skip if no longer `Issued`.
4. Set `Status=Expired`, `ExpiredAt=utcNow`, audit user to configured system user id.
5. Add `WorkflowTransition` `Issued -> Expired`.
6. Add timeline event `policy.expired` with actor display name `System`.
7. Commit in bounded batches.
8. Expose run counts in logs/evidence.

### Lifecycle HTTP Responses

| Endpoint | Success | Key error mappings |
|----------|---------|--------------------|
| `POST /policies/{id}/issue` | 200 `PolicyDto` | 400 missing precondition, 403, 404, 409 `invalid_transition`, 412 |
| `POST /policies/{id}/endorse` | 201 `PolicyEndorsementDto` | 400 empty/bad endorsement, 403, 404, 409, 412 |
| `POST /policies/{id}/cancel` | 200 `PolicyDto` | 400 bad reason/date, 403, 404, 409, 412 |
| `POST /policies/{id}/reinstate` | 200 `PolicyDto` | 400 bad reason, 403, 404, 409 `reinstatement_window_expired`, 412 |

## Step 4 - Backend Policy 360 Rails and Renewal Linkage (S0004, S0005, S0006, S0009, S0010, S0011)

### Required Work

1. `GET /policies/{policyId}/summary` returns `PolicySummaryDto` with rail counts and one-hop predecessor/successor labels.
2. `GET /policies/{policyId}/versions` returns paginated newest-first `PolicyVersionDto`.
3. `GET /policies/{policyId}/versions/{versionId}` returns one immutable version snapshot.
4. `GET /policies/{policyId}/endorsements` returns paginated newest-first endorsement rows.
5. `GET /policies/{policyId}/coverages` returns `IsCurrent=true` coverage lines.
6. `GET /policies/{policyId}/timeline` delegates to `TimelineService.ListEventsPagedAsync("Policy", policyId, ...)`.
7. Renewal rail data is composed from `Renewals` where `PolicyId = policyId` or `BoundPolicyId = policyId`; F0018 reads but does not mutate F0007 renewal state.
8. Documents rail returns a stable placeholder until F0020 is live; no F0020 code dependency in MVP.

### Policy Summary Logic

```
GetSummaryAsync(Guid policyId, ICurrentUserService user, CancellationToken ct)
-> returns PolicySummaryDto?
```

1. Load policy with Account, BrokerOfRecord, Carrier, CurrentVersion.
2. Apply policy read scope.
3. Compute `SuccessorPolicyId` from `Renewals.BoundPolicyId` where `Renewals.PolicyId == policyId`, newest completed renewal wins if duplicates exist; log warning on duplicates.
4. Count versions and endorsements using indexed queries.
5. Count current coverage lines using `Policy.CurrentVersionId`.
6. Count open renewals via F0007 terminal status catalog.
7. Compute available transitions from `PolicyLifecycleStateMachine.GetAvailableTransitions(policy.Status, user.Roles, policy.ReinstatementDeadline, today)`.
8. Map fallback account fields from denormalized policy columns and account survivor metadata.

### Rail Isolation

Frontend rails call separate endpoints. Backend endpoints must fail independently and return ProblemDetails without requiring the summary endpoint to join all rail data. Policy 360 should still render the header if one rail fails.

## Step 5 - Frontend Policy List, Create, Detail, 360, and Account 360 Wiring (S0001-S0011)

### New Files

| File | Purpose |
|------|---------|
| `experience/src/features/policies/types.ts` | TypeScript contract types. |
| `experience/src/features/policies/hooks.ts` | Query/mutation hooks. |
| `experience/src/features/policies/lib/format.ts` | Formatting helpers. |
| `experience/src/features/policies/lib/validation.ts` | AJV/RHF validation helpers. |
| `experience/src/features/policies/components/*.tsx` | Feature UI components. |
| `experience/src/pages/PoliciesPage.tsx` | List route. |
| `experience/src/pages/CreatePolicyPage.tsx` | Create route. |
| `experience/src/pages/PolicyImportPage.tsx` | Import-lite route. |
| `experience/src/pages/PolicyDetailPage.tsx` | Detail/360 route. |

### TypeScript Signatures

```ts
// experience/src/features/policies/types.ts
export type PolicyStatus = 'Pending' | 'Issued' | 'Cancelled' | 'Expired';
export type PolicyImportSource = 'manual' | 'csv-import' | 'f0019-bind-hook';
export type CancellationReasonCode =
  | 'NonPayment'
  | 'InsuredRequest'
  | 'UnderwritingDecision'
  | 'MaterialMisrepresentation'
  | 'CoverageNoLongerNeeded'
  | 'CarrierWithdrawal'
  | 'Other';
export type EndorsementReasonCode =
  | 'CoverageIncrease'
  | 'CoverageDecrease'
  | 'CoverageAdded'
  | 'CoverageRemoved'
  | 'PremiumAdjustment'
  | 'NamedInsuredChange'
  | 'AddressChange'
  | 'DeductibleChange'
  | 'OtherAdministrative';
export type ReinstatementReason =
  | 'InsuredPaidOutstandingPremium'
  | 'CancellationInError'
  | 'AgreementReached'
  | 'Other';

export interface PolicyCoverageInputDto {
  coverageCode: string;
  coverageName?: string | null;
  limit: number;
  deductible?: number | null;
  premium: number;
  exposureBasis?: string | null;
  exposureQuantity?: number | null;
}

export interface PolicyDto {
  id: string;
  accountId: string;
  brokerOfRecordId: string;
  policyNumber: string;
  lineOfBusiness: string;
  carrierId: string;
  carrierName: string | null;
  status: PolicyStatus;
  effectiveDate: string;
  expirationDate: string;
  boundAt: string | null;
  issuedAt: string | null;
  cancelledAt: string | null;
  cancellationEffectiveDate: string | null;
  cancellationReasonCode: CancellationReasonCode | null;
  cancellationReasonDetail: string | null;
  reinstatementDeadline: string | null;
  expiredAt: string | null;
  predecessorPolicyId: string | null;
  successorPolicyId: string | null;
  currentVersionId: string | null;
  currentVersionNumber: number;
  versionCount: number;
  totalPremium: number;
  premiumCurrency: 'USD';
  producerUserId: string | null;
  producerDisplayName: string | null;
  importSource: PolicyImportSource | null;
  externalPolicyReference: string | null;
  accountDisplayNameAtLink: string | null;
  accountStatusAtRead: 'Active' | 'Inactive' | 'Merged' | 'Deleted' | null;
  accountSurvivorId: string | null;
  availableTransitions: string[];
  rowVersion: string;
  createdAt: string;
  createdByUserId: string | null;
  updatedAt: string;
  updatedByUserId: string | null;
}

export interface PolicyListItemDto {
  id: string;
  policyNumber: string;
  accountId: string;
  accountDisplayName: string | null;
  accountStatus: 'Active' | 'Inactive' | 'Merged' | 'Deleted' | null;
  accountSurvivorId: string | null;
  brokerOfRecordId: string;
  brokerName: string | null;
  carrierId: string | null;
  carrierName: string | null;
  lineOfBusiness: string;
  status: PolicyStatus;
  effectiveDate: string;
  expirationDate: string;
  totalPremium: number | null;
  premiumCurrency: 'USD' | null;
  producerUserId: string | null;
  producerDisplayName: string | null;
  versionCount: number | null;
  endorsementCount: number | null;
  hasOpenRenewal: boolean | null;
  reinstatementDeadline: string | null;
  rowVersion: string;
}

export interface PolicyListQuery {
  q?: string;
  status?: string;
  lineOfBusiness?: string;
  carrierId?: string;
  brokerId?: string;
  expiringBefore?: string;
  sort?: 'expirationDate:asc' | 'expirationDate:desc' | 'policyNumber:asc' | 'policyNumber:desc' | 'totalPremium:asc' | 'totalPremium:desc';
  page?: number;
  pageSize?: number;
}
```

### Hook Signatures

```ts
// experience/src/features/policies/hooks.ts
export function usePolicies(query?: PolicyListQuery): UseQueryResult<PaginatedResponse<PolicyListItemDto>>;
export function usePolicy(policyId: string): UseQueryResult<PolicyDto>;
export function usePolicySummary(policyId: string): UseQueryResult<PolicySummaryDto>;
export function usePolicyVersions(policyId: string, page?: number, pageSize?: number): UseQueryResult<PaginatedResponse<PolicyVersionDto>>;
export function usePolicyEndorsements(policyId: string, page?: number, pageSize?: number): UseQueryResult<PaginatedResponse<PolicyEndorsementDto>>;
export function usePolicyCoverages(policyId: string): UseQueryResult<PolicyCoverageLineDto[]>;
export function usePolicyTimeline(policyId: string, pageSize?: number): UseInfiniteQueryResult<PaginatedResponse<TimelineEventDto>>;
export function useCreatePolicy(): UseMutationResult<PolicyDto, ApiError, PolicyCreateRequestDto>;
export function useUpdatePolicy(policyId: string): UseMutationResult<PolicyDto, ApiError, { dto: PolicyUpdateRequestDto; rowVersion: string }>;
export function useIssuePolicy(policyId: string): UseMutationResult<PolicyDto, ApiError, { dto: PolicyIssueRequestDto; rowVersion: string }>;
export function useEndorsePolicy(policyId: string): UseMutationResult<PolicyEndorsementDto, ApiError, { dto: PolicyEndorsementRequestDto; rowVersion: string }>;
export function useCancelPolicy(policyId: string): UseMutationResult<PolicyDto, ApiError, { dto: PolicyCancelRequestDto; rowVersion: string }>;
export function useReinstatePolicy(policyId: string): UseMutationResult<PolicyDto, ApiError, { dto: PolicyReinstateRequestDto; rowVersion: string }>;
```

### Screen Requirements

#### Policy List

- Route: `/policies`.
- Uses `DashboardLayout` with a dense filter toolbar.
- Search box updates query after debounce.
- Filters: status, LOB, carrier, broker, expiring-before/window.
- Table columns: policy number, account display name, status, LOB, carrier, effective date, expiration date, premium, last/current activity if available.
- Row click uses `Link` to `/policies/{id}`.
- Empty state and error state must be explicit.
- No clickable `div`; use buttons/links.

#### Policy Create

- Route: `/policies/new`.
- Manual form uses React Hook Form + AJV over `policy-create-request.schema.json`.
- Account entry point from Account 360 pre-fills `accountId`.
- Coverage editor supports rows with `coverageCode`, `coverageName`, `limit`, `deductible`, `premium`, `exposureBasis`, `exposureQuantity`.
- On success, navigate to `/policies/{id}`.

#### Policy Import

- Route: `/policies/import`.
- MVP UI posts JSON-shaped `PolicyImportRequest` rows, not raw CSV parsing unless backend exposes file upload later.
- Error report displays accepted/rejected rows with row index and ProblemDetails codes.

#### Policy Detail and 360

- Route: `/policies/:policyId`.
- Header renders policy number, status badge, account fallback badge/link, LOB, carrier, dates, premium, rowVersion-backed actions.
- Quick actions:
  - Pending: Issue.
  - Issued: Endorse, Cancel.
  - Cancelled within window: Reinstate.
  - Expired: read-only.
- Tabs/rails:
  - Profile.
  - Versions.
  - Endorsements.
  - Coverages.
  - Renewals.
  - Documents placeholder.
  - Activity.
- Each rail owns loading/error/empty state and must not block other rails.

### Frontend Guardrails

- Use semantic theme classes (`text-text-*`, `bg-surface-*`, `border-surface-*`), not raw palette classes.
- Use hardened `Modal`, `Tabs`, `Select`, `TextInput`, `Badge`, `Card`, `Skeleton`, `ErrorFallback` primitives.
- Dialogs must trap focus through existing primitive behavior.
- All icon-only controls need `aria-label`.
- Lifecycle dialogs require confirmation and clear consequence copy.
- Store UX evidence under `planning-mds/operations/evidence/frontend-ux/ux-audit-YYYY-MM-DD.md` during implementation, not during this G0 dry run.

## Step 6 - Quality, Security, DevOps, and Evidence

### Backend Tests

| Test File | Coverage |
|----------|----------|
| `engine/tests/Nebula.Tests/Unit/PolicyLifecycleStateMachineTests.cs` | Valid/invalid transitions, roles, available transitions, terminal states. |
| `engine/tests/Nebula.Tests/Unit/PolicyNumberGeneratorTests.cs` | LOB prefix, year sequence, uniqueness retry. |
| `engine/tests/Nebula.Tests/Integration/PolicyEndpointTests.cs` | List/create/detail/update/account summary/version/rail endpoints. |
| `engine/tests/Nebula.Tests/Integration/PolicyAuthorizationTests.cs` | Role gates for `policy:*`, scoped reads, denied actions. |
| `engine/tests/Nebula.Tests/Integration/PolicyTimelineTests.cs` | Timeline/transition writes in same transaction and idempotent retry. |
| `engine/tests/Nebula.Tests/Integration/PolicyLifecycleEndpointTests.cs` | Issue, endorse, cancel, reinstate, expire, concurrency, idempotency. |

### Frontend Tests

| Test File | Coverage |
|----------|----------|
| `experience/src/features/policies/tests/PolicyListPage.integration.test.tsx` | Filters, pagination, status/account fallback rendering, navigation. |
| `experience/src/features/policies/tests/PolicyDetailPage.integration.test.tsx` | Header, tabs, rail isolation, error/empty states. |
| `experience/src/features/policies/tests/PolicyLifecycleActions.integration.test.tsx` | Issue/endorse/cancel/reinstate dialogs and invalid states. |
| `experience/src/features/policies/tests/PolicyForms.validation.test.tsx` | Create/update/endorsement/cancel/reinstate schema validation. |
| `experience/src/pages/tests/AccountDetailPage.integration.test.tsx` | Account policy rail and account policy summary chips. |

### Security Review Checklist

- Verify every endpoint checks Casbin resource `policy` with the exact action before service mutation/read.
- Verify service-level scope filters use account region, broker-of-record, territory, managed broker, own book, and admin-all rules.
- Verify `policy:import` is DistributionManager/Admin only.
- Verify `policy:issue`, `policy:endorse`, `policy:reinstate` are Underwriter/Admin only.
- Verify `policy:cancel` is DistributionManager/Underwriter/Admin.
- Verify read count/summary endpoints do not leak cross-scope policies through totals.
- Add or update `planning-mds/security/authorization-matrix.md` for policy rows if security signoff requires matrix parity with `policy.csv`.

### DevOps Runtime Checklist

- Migration applies cleanly in the backend runtime container.
- Expiration hosted service can be disabled/enabled by configuration for tests and local dev.
- Scheduled sweep logs structured fields: `processed`, `expired`, `skipped`, `failed`, `runId`.
- CarrierRef and PolicyReinstatementWindow seed data are idempotent.
- Runtime evidence path must be under `planning-mds/operations/evidence/F0018/devops/`.

### Required Runtime Commands for Closeout

These are not run in this G0 dry run. Later implementation must run them inside runtime containers per `agents/actions/feature.md`:

```bash
dotnet test {PRODUCT_ROOT}/engine/tests/Nebula.Tests/Nebula.Tests.csproj
pnpm --dir {PRODUCT_ROOT}/experience lint
pnpm --dir {PRODUCT_ROOT}/experience lint:theme
pnpm --dir {PRODUCT_ROOT}/experience build
pnpm --dir {PRODUCT_ROOT}/experience test
pnpm --dir {PRODUCT_ROOT}/experience test:visual:theme
python3 agents/product-manager/scripts/validate-trackers.py
python3 {PRODUCT_ROOT}/scripts/kg/validate.py
python3 {PRODUCT_ROOT}/scripts/kg/validate.py --check-drift
python3 agents/scripts/validate_templates.py
```

## Scope Breakdown

| Layer | Required Work | Owner | Status |
|------|----------------|-------|--------|
| Backend (`{PRODUCT_ROOT}/engine/`) | Rewrite policy stub; add version/endorsement/coverage/carrier/sequence entities; repositories; services; endpoints; expiration job; tests. | Backend Developer | Planned |
| Frontend (`{PRODUCT_ROOT}/experience/`) | Policy list/create/import/detail/360; lifecycle dialogs; account rail/summary updates; mocks and tests. | Frontend Developer | Planned |
| AI (`{PRODUCT_ROOT}/neuron/`) | None. | AI Engineer | Not required |
| Quality | Test mapping, integration tests, evidence review, runtime command proof. | Quality Engineer | Planned |
| DevOps/Runtime | Migration evidence, seed idempotency, expiration job scheduling/config evidence. | DevOps | Planned |
| Security | Policy ABAC review, authorization matrix parity, sensitive-data handling. | Security Reviewer | Required |
| Code Review | Architecture/pattern conformance, transaction/idempotency/concurrency review. | Code Reviewer | Required |

## Dependency Order

```
Step 0 (Architect):   feature-local execution plan and G0 validation.
Step 1 (Backend):     domain/persistence/contract reconciliation.
Step 2 (Backend):     read/create/profile/account-summary APIs.
Step 2 (Frontend):    policy feature shell, list/create route scaffolding using mocks.
  ---- checkpoint: contract DTOs, routes, and basic list/create flow compile ----
Step 3 (Backend):     issue/endorse/cancel/reinstate/version/timeline flows.
Step 3 (Frontend):    lifecycle action dialogs and detail action state.
  ---- checkpoint: rowVersion and Idempotency-Key honored in hooks and APIs ----
Step 4 (Backend):     Policy 360 rails, renewal linkage, expiration job.
Step 4 (Frontend):    Policy 360 rails and Account 360 policy composition.
  ---- checkpoint: rail isolation, fallback account rendering, and renewal successor/predecessor paths pass ----
Step 5 (Quality):     focused unit/integration/frontend evidence, then code/security review.
```

## Integration Checkpoints

### After Step 1 (Domain/Persistence)

- [ ] EF model includes Policy, PolicyVersion, PolicyEndorsement, PolicyCoverageLine, CarrierRef, PolicyNumberSequence.
- [ ] Existing `Renewal.PolicyId` and `Renewal.BoundPolicyId` still point at valid `Policy` rows.
- [ ] Migration maps stub statuses to contract statuses.
- [ ] `CarrierRef` and `WorkflowSlaThreshold` seeds are idempotent.

### After Step 2 (Read/Create/Profile)

- [ ] `GET /policies` returns paginated contract-shaped `PolicyListItem`.
- [ ] `POST /policies` creates Pending policy and appends `policy.created`.
- [ ] `PUT /policies/{id}` enforces `If-Match` and material-term restrictions.
- [ ] `GET /accounts/{id}/policies/summary` is ABAC-scoped and single-query.
- [ ] Frontend Policy List and Create shells work against MSW mocks.

### After Step 3 (Lifecycle)

- [ ] Issue creates v1 version, coverage lines, transition, and timeline event.
- [ ] Endorse creates one endorsement, one new version, one current coverage replacement set, one transition, and one timeline event.
- [ ] Cancel sets reinstatement deadline and does not create a version.
- [ ] Reinstate creates a new version and clears live cancellation fields.
- [ ] Idempotent retries do not duplicate versions, endorsements, transitions, or timeline events.

### After Step 4 (Policy 360)

- [ ] Summary endpoint returns header data and rail counts without N+1.
- [ ] Rails load independently.
- [ ] Documents rail placeholder is stable when F0020 is absent.
- [ ] Renewal linkage exposes predecessor and computed successor without mutating F0007.
- [ ] Account 360 policy rail and summary chips render contract fields and fallback account states.

### Cross-Story Verification

- [ ] Full lifecycle: create -> issue -> endorse -> cancel -> reinstate -> expire.
- [ ] All `policy:*` Casbin actions are enforced and ABAC counts do not leak.
- [ ] Timeline events for full lifecycle are correct and ordered.
- [ ] ProblemDetails include code and traceId.
- [ ] F0016 fallback contract holds for merged/deleted accounts.
- [ ] F0007 renewal successor lookup handles no successor, one successor, and duplicate-successor warning.
- [ ] Policy API and JSON schemas remain synchronized.

## Integration Checklist

- [ ] API contract compatibility validated against `planning-mds/api/nebula-api.yaml`.
- [ ] JSON Schema compatibility validated for all `policy*.schema.json` files.
- [ ] Frontend contract compatibility validated through TypeScript types and MSW mocks.
- [ ] AI contract compatibility not applicable.
- [ ] Test cases mapped to all 11 stories.
- [ ] Developer-owned fast-test responsibilities identified by layer.
- [ ] Required runtime evidence artifacts identified under `planning-mds/operations/evidence/**`.
- [ ] Framework vs solution boundary reviewed; no `agents/**` product-specific drift in implementation.
- [ ] Run/deploy instructions updated after implementation, not in this dry run.

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| PRD/story wording differs from OpenAPI/JSON Schema for carrier, coverage, reason-code, and endpoint names. | High | Use OpenAPI/JSON Schema as implementation authority; this plan records the mapping. Any future contract change routes to Architect. | Architect |
| Existing policy stub is already in runtime migrations and renewal code. | High | Add forward migration preserving IDs and update Renewal read code; do not drop/recreate policies. | Backend Developer |
| Authorization matrix lacks a visible policy section while `policy.csv` has policy rules. | Medium | Security reviewer reconciles matrix evidence before G4.5 if required. | Security Reviewer |
| Expiration job infrastructure does not exist today. | Medium | Add hosted service with config flag and evidence; keep Temporal migration deferred. | DevOps / Backend Developer |
| Policy 360 can become a large page. | Medium | Rail isolation and lazy loading are required; no summary endpoint joins all rails. | Frontend Developer |
| Reinstatement windows overload `WorkflowSlaThreshold`. | Medium | Follow ADR-018 and seed category explicitly; admin UI clarity deferred to F0032. | Backend Developer |

## JSON Serialization Convention

- API payloads use camelCase properties matching OpenAPI.
- Date-only fields are serialized as `YYYY-MM-DD`.
- Date-time fields are UTC ISO-8601 strings.
- `RowVersion` is serialized as a string and passed back as `If-Match: "{rowVersion}"`.
- JSON snapshot columns are stored as JSON strings in EF entities and emitted as objects/arrays in DTOs.
- Nullable OpenAPI fields use `nullable: true`; JSON Schema uses Draft-07 type arrays.

## DI Registration Changes

Add:

```csharp
// engine/src/Nebula.Infrastructure/DependencyInjection.cs
services.AddScoped<IPolicyRepository, PolicyRepository>();

// engine/src/Nebula.Api/Program.cs
builder.Services.AddScoped<PolicyService>();
builder.Services.AddScoped<PolicyNumberGenerator>();
builder.Services.AddScoped<PolicyExpirationService>();
builder.Services.AddHostedService<PolicyExpirationHostedService>();
app.MapPolicyEndpoints();
```

Validators:

```csharp
builder.Services.AddScoped<IValidator<PolicyCreateRequestDto>, PolicyCreateValidator>();
builder.Services.AddScoped<IValidator<PolicyUpdateRequestDto>, PolicyUpdateValidator>();
builder.Services.AddScoped<IValidator<PolicyEndorsementRequestDto>, PolicyEndorsementValidator>();
builder.Services.AddScoped<IValidator<PolicyCancelRequestDto>, PolicyCancelValidator>();
builder.Services.AddScoped<IValidator<PolicyReinstateRequestDto>, PolicyReinstateValidator>();
```

## Casbin Policy Sync

`planning-mds/security/policies/policy.csv` already contains the F0018 policy action rows:

- `policy:read`
- `policy:create`
- `policy:update`
- `policy:issue`
- `policy:endorse`
- `policy:cancel`
- `policy:reinstate`
- `policy:coverage:manage`
- `policy:import`

Implementation must verify the runtime Casbin policy source consumes this file or has an identical embedded copy. Any drift between planning CSV and runtime policy resource is a blocking security finding.

## G0 Validation Checklist

| Check | Verdict | Evidence |
|------|---------|----------|
| Feature assembly plan exists | PASS | This file. |
| Feature scope and handoffs are explicit | PASS | Scope Breakdown, Slice Order, Dependency Order. |
| Integration/test checkpoints defined | PASS | Integration Checkpoints and Cross-Story Verification. |
| Required signoff role matrix initialized | PASS | `STATUS.md` already lists Quality Engineer, Code Reviewer, Security Reviewer, DevOps, and Architect as required. |
| Scope split matches story requirements | PASS | Steps map S0001-S0011. |
| Dependencies between agents identified | PASS | Slice Order and Scope Breakdown. |
| Integration checkpoints are feasible | PASS WITH FRICTION | Feasible, but runtime validation is intentionally deferred by dry-run scope. |
| No missing or conflicting artifact ownership | PASS WITH RECORDED RECONCILIATION | Contract/story naming drift is recorded above; OpenAPI/JSON Schema are the implementation authority. |
