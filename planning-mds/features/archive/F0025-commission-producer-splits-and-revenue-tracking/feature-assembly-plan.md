# Feature Assembly Plan - F0025: Commission, Producer Splits & Revenue Tracking

**Created:** 2026-07-07
**Author:** Architect Agent
**Status:** Draft - G0
**Feature Run:** `2026-07-07-9859bad4`

## Overview

F0025 adds an internal-only `CommissionRevenue` vertical slice for expected commission visibility, effective-dated commission schedules, effective-dated producer split assignments, single-step adjustment approval, and source-authorized revenue attribution rollups. The implementation must follow ADR-032 and keep the product out of accounting, billing, payments, reconciliation, tax, statements, GL export, and producer payout execution.

## Build Order

| Step | Scope | Stories | Rationale |
|------|-------|---------|-----------|
| 1 | Backend commission model and persistence | S0002, S0003, S0004, S0005, S0006 | Tables, row versions, repositories, and migrations must exist before services/endpoints can persist or query commission data. |
| 2 | Backend services, validators, authorization, endpoints | S0001-S0006 | API behavior must enforce commission-specific Casbin actions and source-record visibility before frontend integration. |
| 3 | Frontend commission feature slice and route | S0001-S0006 | UI consumes stable contracts through TanStack Query hooks and keeps mutation stories reload-verifiable. |
| 4 | QE, security, and deployability evidence | S0001-S0006 | Economic data requires acceptance, security scan/verdict, migration/deployability, and runtime evidence before approval. |
| 5 | Architect KG reconciliation and PM closeout | S0001-S0006 | Bind as-built code paths, validate evidence, archive, regenerate story/KG indexes, and publish the approved run. |

## Existing Code (Must Be Modified)

| File | Current State | F0025 Change |
|------|---------------|--------------|
| `engine/src/Nebula.Api/Program.cs` | Registers application services and endpoint maps through `app.Map*Endpoints()` | **Expand** - register `CommissionRevenueService`; map `app.MapCommissionEndpoints()`. |
| `engine/src/Nebula.Infrastructure/DependencyInjection.cs` | Registers repository and service interfaces for persistence and reporting | **Expand** - register `ICommissionRepository`, `IRevenueAttributionRepository`, and concrete implementations. |
| `engine/src/Nebula.Infrastructure/Persistence/AppDbContext.cs` | Owns existing EF `DbSet<>` declarations and model configuration | **Expand** - add `DbSet` entries for commission entities and apply new configurations. |
| `engine/src/Nebula.Api/data/**/policy.csv` or embedded Casbin policy source if present | Runtime Casbin seed mirrors planning policy rows | **Sync** - ensure `commission:*` rows from planning policy are available at runtime. |
| `experience/src/App.tsx` | React Router route registry | **Expand** - add protected `/commissions` and `/commissions/:expectedCommissionId` routes. |
| `experience/src/components/layout/Sidebar.tsx` | Primary navigation | **Expand** - add internal-only Commission navigation item if the current layout supports role-filtered nav. |
| `planning-mds/knowledge-graph/code-index.yaml` | As-built code bindings for completed features | **G7 only** - Architect binds final F0025 paths after implementation, not during G0. |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `engine/src/Nebula.Domain/Entities/CommissionSchedule.cs` | Domain | Effective-dated schedule reference row. |
| `engine/src/Nebula.Domain/Entities/ProducerSplitAssignment.cs` | Domain | Policy-scoped effective-dated split assignment. |
| `engine/src/Nebula.Domain/Entities/ProducerSplitParticipant.cs` | Domain | Producer participant percentage child row. |
| `engine/src/Nebula.Domain/Entities/ExpectedCommission.cs` | Domain | Persisted CRM review record with source snapshots and derived amounts. |
| `engine/src/Nebula.Domain/Entities/CommissionAdjustment.cs` | Domain | Pending/approved/rejected adjustment workflow row. |
| `engine/src/Nebula.Domain/Entities/RevenueAttributionProjection.cs` | Domain | Read-side projection row for rollups. |
| `engine/src/Nebula.Application/DTOs/CommissionDtos.cs` | Application | Request/response records matching planning JSON Schemas. |
| `engine/src/Nebula.Application/Interfaces/ICommissionRepository.cs` | Application | Persistence port for schedules, splits, expected commissions, and adjustments. |
| `engine/src/Nebula.Application/Interfaces/IRevenueAttributionRepository.cs` | Application | Persistence/query port for rollup projections. |
| `engine/src/Nebula.Application/Services/CommissionRevenueService.cs` | Application | Business orchestration, calculations, validation guards, timeline events. |
| `engine/src/Nebula.Application/Validators/CommissionValidators.cs` | Application | FluentValidation validators for schedule, split, adjustment, and rollup requests. |
| `engine/src/Nebula.Infrastructure/Repositories/CommissionRepository.cs` | Infrastructure | EF implementation for commission write/read operations. |
| `engine/src/Nebula.Infrastructure/Repositories/RevenueAttributionRepository.cs` | Infrastructure | EF implementation for rollup projection queries. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/Commission*.cs` | Infrastructure | EF table, relationship, index, and row-version configuration. |
| `engine/src/Nebula.Infrastructure/Persistence/Migrations/*_F0025_CommissionRevenue.cs` | Infrastructure | Migration for F0025 tables and indexes. |
| `engine/src/Nebula.Api/Endpoints/CommissionEndpoints.cs` | API | Minimal API route group for F0025 contracts. |
| `engine/tests/Nebula.Tests/Unit/CommissionRevenue/CommissionRevenueServiceTests.cs` | Tests | Domain/service validation and calculation behavior. |
| `engine/tests/Nebula.Tests/Integration/CommissionEndpointTests.cs` | Tests | API authorization, validation, persistence, and reload evidence. |
| `experience/src/features/commissions/types.ts` | Frontend | TypeScript contract types for commission API payloads. |
| `experience/src/features/commissions/hooks.ts` | Frontend | TanStack Query hooks and mutations. |
| `experience/src/features/commissions/components/*.tsx` | Frontend | Workspace, detail, schedule, split, calculation, adjustment, rollup panels. |
| `experience/src/pages/CommissionsPage.tsx` | Frontend | Commission workspace and rollup route. |
| `experience/src/pages/CommissionDetailPage.tsx` | Frontend | Commission detail route. |
| `experience/src/features/commissions/tests/*.test.tsx` | Frontend tests | Component and mutation/reload tests. |
| `experience/tests/e2e/f0025-commissions.spec.ts` | E2E | Browser workflow coverage. |

---

## Step 1 - Backend Model And Persistence (S0002-S0006)

### Entity / DTO / Code

```csharp
// engine/src/Nebula.Domain/Entities/CommissionSchedule.cs
public class CommissionSchedule
{
    public Guid Id { get; set; }
    public Guid CarrierMarketId { get; set; }
    public string LineOfBusiness { get; set; } = string.Empty;
    public string? State { get; set; }
    public string? ProductCode { get; set; }
    public string Basis { get; set; } = "premium_percent";
    public decimal? RatePercent { get; set; }
    public decimal? FlatAmount { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string SourceNote { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? UpdatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public uint RowVersion { get; set; }
}

// engine/src/Nebula.Domain/Entities/ProducerSplitAssignment.cs
public class ProducerSplitAssignment
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? UpdatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public uint RowVersion { get; set; }
    public List<ProducerSplitParticipant> Participants { get; set; } = [];
}

// engine/src/Nebula.Domain/Entities/ProducerSplitParticipant.cs
public class ProducerSplitParticipant
{
    public Guid Id { get; set; }
    public Guid ProducerSplitAssignmentId { get; set; }
    public Guid ProducerId { get; set; }
    public decimal SplitPercent { get; set; }
    public string? SourceOwnershipSnapshotJson { get; set; }
    public uint RowVersion { get; set; }
}

// engine/src/Nebula.Domain/Entities/ExpectedCommission.cs
public class ExpectedCommission
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public Guid? PolicyVersionId { get; set; }
    public Guid CarrierMarketId { get; set; }
    public Guid? CommissionScheduleId { get; set; }
    public Guid? ProducerSplitAssignmentId { get; set; }
    public decimal? PremiumBasisAmount { get; set; }
    public decimal? ExpectedGrossCommission { get; set; }
    public decimal ApprovedAdjustmentTotal { get; set; }
    public decimal? AdjustedExpectedCommission { get; set; }
    public string Status { get; set; } = "MissingInputs";
    public string ExceptionState { get; set; } = "missing_schedule";
    public string SourceSnapshotJson { get; set; } = "{}";
    public DateTime? CalculatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public uint RowVersion { get; set; }
    public List<CommissionAdjustment> Adjustments { get; set; } = [];
}

// engine/src/Nebula.Domain/Entities/CommissionAdjustment.cs
public class CommissionAdjustment
{
    public Guid Id { get; set; }
    public Guid ExpectedCommissionId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string RequestedByUserId { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public string? DecidedByUserId { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNote { get; set; }
    public uint RowVersion { get; set; }
}

// engine/src/Nebula.Domain/Entities/RevenueAttributionProjection.cs
public class RevenueAttributionProjection
{
    public Guid Id { get; set; }
    public Guid ExpectedCommissionId { get; set; }
    public Guid PolicyId { get; set; }
    public Guid? ProducerId { get; set; }
    public Guid? BrokerId { get; set; }
    public Guid? TerritoryId { get; set; }
    public Guid CarrierMarketId { get; set; }
    public DateOnly PolicyPeriodStart { get; set; }
    public DateOnly PolicyPeriodEnd { get; set; }
    public string LineOfBusiness { get; set; } = string.Empty;
    public decimal ExpectedGrossCommission { get; set; }
    public decimal ApprovedAdjustmentTotal { get; set; }
    public decimal AdjustedExpectedCommission { get; set; }
    public decimal ProducerAllocationAmount { get; set; }
    public int UnresolvedExceptionCount { get; set; }
    public DateTime SourceRefreshedAt { get; set; }
    public uint RowVersion { get; set; }
}
```

### Logic Flow

1. Add EF configurations with `RowVersion` using the existing xmin/row-version pattern.
2. Add non-overlap indexes for schedule scope and split assignment scope; use raw migration SQL if EF cannot express the filtered/exclusion constraint.
3. Add foreign keys to `Policy`, `PolicyVersion`, `CarrierMarket`, `Producer`, and optional broker/territory references without cascading deletes.
4. Add repositories with read methods that always accept current-user context or source-record visibility filters from the service.
5. Seed no production commission data; dev seed may add minimal demo rows only if existing seed conventions support it.

### Migration SQL

Use PostgreSQL exclusion constraints or equivalent overlap checks for:

```sql
-- CommissionSchedule: no overlapping active schedule for carrier/market + lob + optional state/product.
-- ProducerSplitAssignment: no overlapping active assignment for the same policy.
```

If the local EF provider cannot support exclusion constraints, enforce overlap in service and repository transaction checks, and add supporting B-tree indexes on scope columns plus effective dates.

---

## Step 2 - Backend Services And Endpoints (S0001-S0006)

### Entity / DTO / Code

```csharp
// engine/src/Nebula.Application/Interfaces/ICommissionRepository.cs
public interface ICommissionRepository
{
    Task<PaginatedResult<ExpectedCommissionDto>> SearchExpectedCommissionsAsync(CommissionSearchQuery query, CurrentUserScope user, CancellationToken ct);
    Task<ExpectedCommissionDetailDto?> GetExpectedCommissionAsync(Guid expectedCommissionId, CurrentUserScope user, CancellationToken ct);
    Task<CommissionScheduleDto> CreateScheduleAsync(CommissionScheduleUpsertDto dto, ICurrentUserService user, CancellationToken ct);
    Task<CommissionScheduleDto?> UpdateScheduleAsync(Guid scheduleId, CommissionScheduleUpsertDto dto, uint expectedRowVersion, ICurrentUserService user, CancellationToken ct);
    Task<ProducerSplitAssignmentDto> UpsertProducerSplitAsync(ProducerSplitAssignmentUpsertDto dto, ICurrentUserService user, CancellationToken ct);
    Task<IReadOnlyList<ProducerSplitAssignmentDto>> ListPolicySplitsAsync(Guid policyId, CurrentUserScope user, CancellationToken ct);
    Task<ExpectedCommissionDto?> RecalculateExpectedCommissionAsync(Guid expectedCommissionId, ICurrentUserService user, CancellationToken ct);
    Task<IReadOnlyList<CommissionAdjustmentDto>> ListAdjustmentsAsync(Guid expectedCommissionId, CurrentUserScope user, CancellationToken ct);
    Task<CommissionAdjustmentDto> RequestAdjustmentAsync(Guid expectedCommissionId, CommissionAdjustmentRequestDto dto, ICurrentUserService user, CancellationToken ct);
    Task<CommissionAdjustmentDto?> DecideAdjustmentAsync(Guid adjustmentId, CommissionAdjustmentDecisionRequestDto dto, ICurrentUserService user, CancellationToken ct);
}

// engine/src/Nebula.Application/Interfaces/IRevenueAttributionRepository.cs
public interface IRevenueAttributionRepository
{
    Task<RevenueAttributionRollupResponseDto> GetRollupsAsync(RevenueAttributionRollupQuery query, CurrentUserScope user, CancellationToken ct);
}

// engine/src/Nebula.Application/Services/CommissionRevenueService.cs
public sealed class CommissionRevenueService
{
    public Task<PaginatedResult<ExpectedCommissionDto>> SearchAsync(CommissionSearchQuery query, ICurrentUserService user, CancellationToken ct);
    public Task<ExpectedCommissionDetailDto?> GetDetailAsync(Guid expectedCommissionId, ICurrentUserService user, CancellationToken ct);
    public Task<(CommissionScheduleDto? Result, string? Error)> CreateScheduleAsync(CommissionScheduleUpsertDto dto, ICurrentUserService user, CancellationToken ct);
    public Task<(CommissionScheduleDto? Result, string? Error)> UpdateScheduleAsync(Guid scheduleId, CommissionScheduleUpsertDto dto, uint rowVersion, ICurrentUserService user, CancellationToken ct);
    public Task<(ProducerSplitAssignmentDto? Result, string? Error)> UpsertSplitAsync(ProducerSplitAssignmentUpsertDto dto, ICurrentUserService user, CancellationToken ct);
    public Task<(ExpectedCommissionDto? Result, string? Error)> CalculateAsync(Guid expectedCommissionId, ICurrentUserService user, CancellationToken ct);
    public Task<(CommissionAdjustmentDto? Result, string? Error)> RequestAdjustmentAsync(Guid expectedCommissionId, CommissionAdjustmentRequestDto dto, ICurrentUserService user, CancellationToken ct);
    public Task<(CommissionAdjustmentDto? Result, string? Error)> DecideAdjustmentAsync(Guid adjustmentId, CommissionAdjustmentDecisionRequestDto dto, ICurrentUserService user, CancellationToken ct);
    public Task<RevenueAttributionRollupResponseDto> GetRollupsAsync(RevenueAttributionRollupQuery query, ICurrentUserService user, CancellationToken ct);
}
```

### Endpoint Table

| Endpoint | Story | Service Method | Authorization |
|----------|-------|----------------|---------------|
| `GET /expected-commissions` | S0001 | `SearchAsync` | `commission:read` plus source policy/carrier/producer/territory visibility before rows/counts. |
| `GET /expected-commissions/{expectedCommissionId}` | S0001/S0004 | `GetDetailAsync` | `commission:read` plus source visibility. |
| `GET /commission-schedules` | S0002 | repository list via service | `commission:read`. |
| `POST /commission-schedules` | S0002 | `CreateScheduleAsync` | `commission:schedule_manage`. |
| `PUT /commission-schedules/{scheduleId}` | S0002 | `UpdateScheduleAsync` | `commission:schedule_manage`; requires `If-Match`. |
| `POST /producer-splits` | S0003 | `UpsertSplitAsync` | `commission:split_assign`. |
| `GET /policies/{policyId}/producer-splits` | S0003 | `ListPolicySplitsAsync` | `commission:read` plus policy visibility. |
| `POST /expected-commissions/{expectedCommissionId}/calculate` | S0004 | `CalculateAsync` | `commission:calculate`. |
| `GET /expected-commissions/{expectedCommissionId}/adjustments` | S0005 | `ListAdjustmentsAsync` | `commission:read`. |
| `POST /expected-commissions/{expectedCommissionId}/adjustments` | S0005 | `RequestAdjustmentAsync` | `commission:adjustment_request`. |
| `POST /commission-adjustments/{adjustmentId}/decision` | S0005 | `DecideAdjustmentAsync` | `commission:adjustment_approve`; reject same-user approval. |
| `GET /revenue-attribution/rollups` | S0006 | `GetRollupsAsync` | `commission:rollup_read`; filter before aggregation. |

### Mutation Traceability

| Screen / Entry Point | User Action | Endpoint | Service Method | Entity / Carrier | Authorization | Concurrency | Validation Failure | Audit / Timeline | Test Expectation |
|----------------------|-------------|----------|----------------|------------------|---------------|-------------|--------------------|------------------|------------------|
| Commission Detail -> Schedule Maintenance Panel | Save schedule | `POST /commission-schedules` or `PUT /commission-schedules/{scheduleId}` | `CreateScheduleAsync` / `UpdateScheduleAsync` | `CommissionSchedule` | `commission:schedule_manage` | `If-Match` on update | 400 missing basis/rate/effective date/source note; 409 overlap | `CommissionScheduleCreated` / `CommissionScheduleUpdated` | Integration and UI test reload detail and see schedule history. |
| Commission Detail -> Split Assignment Panel | Save split participants | `POST /producer-splits` | `UpsertSplitAsync` | `ProducerSplitAssignment`, `ProducerSplitParticipant` | `commission:split_assign` | Existing assignment row version when replacing active row | 400 split total != 100; 400 inactive producer; 409 overlap | `ProducerSplitAssigned` | Integration and UI test reload detail and see participants/history. |
| Commission Detail -> Calculation Review Section | Recalculate | `POST /expected-commissions/{id}/calculate` | `CalculateAsync` | `ExpectedCommission`, `RevenueAttributionProjection` | `commission:calculate` | Expected commission row version if supplied; otherwise transaction-scoped source snapshot | 409 stale source; 422 missing schedule/split/premium represented as exception status | `ExpectedCommissionCalculated` only for persisted recalculation | API test verifies deterministic exception states and calculated amounts. |
| Commission Detail -> Adjustment Review Panel | Submit adjustment | `POST /expected-commissions/{id}/adjustments` | `RequestAdjustmentAsync` | `CommissionAdjustment` | `commission:adjustment_request` | N/A create | 400 missing amount/reason/effective date | `CommissionAdjustmentRequested` | API/UI reload shows pending request and audit summary. |
| Commission Detail -> Adjustment Review Panel | Approve/reject adjustment | `POST /commission-adjustments/{id}/decision` | `DecideAdjustmentAsync` | `CommissionAdjustment`, `ExpectedCommission`, `RevenueAttributionProjection` | `commission:adjustment_approve` | Pending adjustment row version if supplied | 400 missing decision note; 403 same-user approval; 409 not pending | `CommissionAdjustmentApproved` / `CommissionAdjustmentRejected` | API/UI reload shows decision and adjusted amount when approved. |

### HTTP Responses

| Status | Body | Condition |
|--------|------|-----------|
| 200 | DTO or paginated result | Successful read/update/action. |
| 201 | DTO | Created schedule, split assignment, or adjustment. |
| 400 | ProblemDetails `validation_error` | Schema or FluentValidation failure. |
| 403 | ProblemDetails `policy_denied` or existing forbidden helper | Casbin or source-record visibility deny. |
| 404 | ProblemDetails `not_found` | Entity not found or hidden by source visibility where IDOR-safe. |
| 409 | ProblemDetails `commission_effective_period_overlap`, `commission_not_pending`, or concurrency conflict | Business conflict. |
| 422 | ProblemDetails `commission_missing_inputs` | Calculation cannot produce final amount but persists/reports exception state. |
| 428 | ProblemDetails | `If-Match` required for row-version update endpoints. |

---

## Step 3 - Frontend Feature Slice (S0001-S0006)

### Required Work

- Add `experience/src/features/commissions/` with `types.ts`, `hooks.ts`, `index.ts`, and components for workspace, detail, schedule panel, split panel, calculation panel, adjustment panel, and rollup view.
- Add `CommissionsPage.tsx` and `CommissionDetailPage.tsx`.
- Add protected routes `/commissions` and `/commissions/:expectedCommissionId`.
- Use TanStack Query keys rooted at `['commissions']`; invalidate detail/search/rollup queries after schedule, split, calculation, and adjustment mutations.
- Use React Hook Form plus AJV/JSON Schema where the existing form engine is practical; otherwise mirror established controlled-form patterns and include validation tests.
- Use existing UI primitives and lucide icons; no raw palette classes outside established theme tokens.

### Frontend Acceptance Mapping

| Story | UI Proof |
|-------|----------|
| S0001 | Search filters do not request before 2 chars, omit hidden counts, open detail. |
| S0002 | Authorized user saves schedule, query invalidates, reload shows history; unauthorized user sees read-only panel. |
| S0003 | Split editor enforces 100 percent and reloads persisted participants. |
| S0004 | Calculation panel separates source inputs, derived amounts, and exception states. |
| S0005 | Adjustment request and decision flow shows pending/approved/rejected history and same-user deny copy. |
| S0006 | Rollup filters/grouping render authorized totals and drill into workspace filters. |

---

## Scope Breakdown

| Layer | Required Work | Owner | Status |
|------|---------------|-------|--------|
| Backend (`engine/`) | Domain entities, migration, repositories, service, validators, endpoints, unit/integration tests | Backend Developer | Pending G1 |
| Frontend (`experience/`) | Commission pages, feature hooks, forms, components, routing, component/E2E tests | Frontend Developer | Pending G1 |
| AI (`neuron/`) | None | AI Engineer | Not required unless scope changes |
| Quality | Test plan, E2E, acceptance coverage, security scan execution artifacts | Quality Engineer | Pending G1 |
| DevOps/Runtime | Runtime preflight, migration/deployability smoke, env/config notes | DevOps | Pending G1 |
| Security | Threat/security review for economic data, authz, hidden counts, scan interpretation | Security Reviewer | Pending G3 |

## Dependency Order

```text
G0 Architect: assembly plan + evidence validation
G1 DevOps/orchestrator: runtime preflight before any compile/test/lint/scan
Backend Step 1: model, repositories, migration
Backend Step 2: service, validators, endpoints, tests
Frontend Step 3: feature slice, route, hooks, tests
QE/DevOps Step 4: E2E, coverage, security scan artifacts, deployability
G3 Reviews: code and security
G4-G8: user approval, signoff, evidence validation, KG reconciliation, PM closeout
```

## Integration Checkpoints

### After Backend

- [ ] `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter Commission` passes in application runtime.
- [ ] Migration applies against the configured test database.
- [ ] Commission endpoints return ProblemDetails for validation, policy deny, missing source, and conflicts.
- [ ] External roles receive no commission data, row counts, snippets, facets, or rollup totals.

### After Frontend

- [ ] `pnpm --dir experience lint`, `lint:theme`, `build`, and feature tests pass.
- [ ] Schedule, split, calculation, adjustment, and rollup UI states fit desktop and narrow viewports.
- [ ] Mutation tests prove query invalidation/reload persistence, not render-only success.

### Cross-Story Verification

- [ ] Full lifecycle: search record -> add schedule -> add split -> calculate expected commission -> request/approve adjustment -> view adjusted rollup -> drill back to contributing records.
- [ ] All commission Casbin policies enforced and external users denied.
- [ ] Timeline events exist for every mutation and no timeline event is emitted for read-only search/rollup.
- [ ] Rollups filter source records before aggregation.
- [ ] F0025 remains CRM review/attribution only, not ledger/payment/payout behavior.

## Integration Checklist

- [x] API contract compatibility identified.
- [x] Frontend contract compatibility identified.
- [x] AI contract compatibility marked not applicable.
- [x] Test cases mapped to acceptance criteria.
- [x] Developer-owned fast-test responsibilities identified by layer.
- [x] Required runtime evidence artifacts identified.
- [x] Framework vs solution boundary reviewed.
- [x] Mutation traceability tables completed for every mutation path.
- [x] Render-only tests are not used to close mutation stories.
- [x] Run/deploy instructions to be updated by DevOps if migration/config changes require them.

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Economic data can leak through counts or rollups | High | Filter source records before counts/totals; security review must test hidden-row behavior. | Backend + Security |
| Effective-date overlap rules are easy to race | High | Use transaction checks and database constraints where possible; integration test concurrent overlap. | Backend |
| Calculation semantics can drift toward accounting | Medium | Keep expected commission as review record only; no ledger, payment, payout, statement, or reconciliation code. | Architect + Code Reviewer |
| Frontend mutation stories could pass with render-only tests | Medium | Require reload/query invalidation proof for schedule, split, calculation, and adjustment flows. | Frontend + QE |

## JSON Serialization Convention

Use existing ASP.NET Core JSON conventions. API responses should remain camelCase. Currency/percentage values use decimals in JSON numbers. Date-only values serialize as `YYYY-MM-DD`. Row versions follow the local endpoint convention for the underlying entity; use `uint` where matching existing CarrierMarket/ServiceCase patterns and string only where an existing dependent contract requires it.

## DI Registration Changes

- Add `builder.Services.AddScoped<CommissionRevenueService>();` in `Nebula.Api/Program.cs`.
- Add repository registrations in `Nebula.Infrastructure/DependencyInjection.cs`.
- Add FluentValidation validators through the existing assembly scanning or explicit registration pattern already used by the API.

## Casbin Policy Sync

Planning policy rows already exist in `planning-mds/security/policies/policy.csv`. During implementation, copy/sync those `commission` rows into the runtime embedded policy source if it is separate from planning docs, then add unit tests in `CasbinAuthorizationServiceTests` for every F0025 action.

## Knowledge-Graph Binding Plan

G7 must bind the as-built paths for:

- `capability:commission-revenue-tracking`
- commission endpoint nodes
- commission entity nodes
- commission policy rules
- `workflow:commission-adjustment`
- schema nodes already created during Phase B

Expected code-index globs: `engine/src/Nebula.Api/Endpoints/CommissionEndpoints.cs`, `engine/src/Nebula.Application/Services/CommissionRevenueService.cs`, `engine/src/Nebula.Application/DTOs/CommissionDtos.cs`, `engine/src/Nebula.Application/Interfaces/ICommissionRepository.cs`, `engine/src/Nebula.Infrastructure/Repositories/CommissionRepository.cs`, `engine/src/Nebula.Infrastructure/Persistence/Configurations/Commission*.cs`, `experience/src/features/commissions/**`, `experience/src/pages/CommissionsPage.tsx`, and `experience/src/pages/CommissionDetailPage.tsx`.
