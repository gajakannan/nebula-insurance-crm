# Feature Assembly Plan — F0016: Account 360 & Insured Management

**Created:** 2026-04-13
**Author:** Architect Agent
**Status:** Proposed (pending Phase B approval)

## Overview

F0016 introduces Account as a first-class aggregate with profile, contacts, lifecycle (Active/Inactive/Merged/Deleted), merge + tombstone semantics, and a composed 360 workspace. It also **lands the owning contract** — descoped from F0006 at its closeout — that governs how dependent modules (submissions, renewals, policies, search, documents) render deleted and merged accounts.

Per [ADR-017](../../architecture/decisions/ADR-017-account-merge-tombstone-and-fallback-contract.md), merge is implemented with tombstone-forward semantics and denormalized fallback columns on dependent list/detail contracts. FK re-pointing was considered and rejected.

## Dependencies and Preconditions

| Dependency | Status | How F0016 consumes |
|------------|--------|---------------------|
| F0002 Broker | Done / archived | Reads Broker entity for BrokerOfRecord assignment |
| F0006 Submissions | Done / archived | Adds denormalized account columns to submission list payload |
| F0007 Renewals | Done / archived | Adds denormalized account columns to renewal list payload |
| F0007 Policy stub | Landed by F0007 | Read-only for Account 360 policy rail + summary counts |
| F0018 Policy Lifecycle | Soft dep (future) | When it lands, Account 360 reads the real Policy entity instead of the stub (no breaking change) |
| F0020 Documents | Soft dep (future) | Account 360 renders Documents rail via F0020; MVP shows placeholder |
| ADR-008 Casbin ABAC | In use | Extends policy file with `account:*` rules |
| ADR-011 Workflow state machine | In use | Account lifecycle adopts the shared state-machine + append-only transition history pattern |

## Build Order

| Step | Scope | Stories | Rationale |
|------|-------|---------|-----------|
| 1 | Migration: Accounts table restructure (status, lifecycle columns, stable-display, row-version) | S0002, S0007, S0008 | Foundation for all other work |
| 2 | Migration: AccountContacts + AccountRelationshipHistory tables | S0005, S0006 | Child aggregates |
| 3 | Migration: Dependent denormalization columns on Submissions, Renewals, Policies (nullable add + backfill) | S0009 | Fallback contract migration |
| 4 | Domain entities + value objects (Account, AccountContact, AccountRelationshipHistory, AccountLifecycleStateMachine) | S0001–S0011 | Clean-architecture domain layer |
| 5 | DTOs (account, account-list-item, account-summary, account-create-request, account-update-request, account-lifecycle-request, account-merge-request, account-contact-*) | S0001–S0011 | API contract surface |
| 6 | Validators (create, update, lifecycle-transition conditional, merge, contact) | S0002, S0005, S0007, S0008 | FluentValidation over DTOs |
| 7 | Repositories (IAccountRepository, IAccountContactRepository, IAccountRelationshipHistoryRepository) | S0001–S0011 | Data-access interfaces + EF implementations |
| 8 | Services (AccountService, AccountLifecycleService, AccountMergeService, AccountSummaryService, AccountContactService) | S0001–S0011 | Application-layer orchestration |
| 9 | API endpoints (`/accounts/**`) with Casbin enforcement | S0001–S0011 | Minimal-API endpoint surface |
| 10 | Casbin policy extension (policies/account-policies.csv or policy.csv section) | S0001–S0011 | `account:*` ABAC rules |
| 11 | Dependent denormalization read paths updated in F0006 + F0007 list endpoints | S0009 | Makes fallback contract live end-to-end |
| 12 | Seed data (Accounts, AccountContacts) + integration tests | S0001–S0011 | Fixture coverage for happy path + deleted/merged paths |

## Existing Code (Must Be Extended)

| File | Current State | F0016 Change |
|------|---------------|--------------|
| `Nebula.Domain/Entities/Account.cs` | Thin row (Id, Name, audit) | **Extend** — add lifecycle fields, stable display name, merge pointer, delete-reason, territory, region, address, broker-of-record, primary-producer |
| `Nebula.Api/Endpoints/AccountEndpoints.cs` | Minimal `GET /accounts` list stub | **Rewrite** — full CRUD + lifecycle + merge + composed rails |
| `Nebula.Application/DTOs/*Account*` | N/A (to be created) | **Create** — full DTO family |
| `Nebula.Application/Services/Account*Service.cs` | N/A | **Create** — Account, Lifecycle, Merge, Summary, Contact services |
| `Nebula.Infrastructure/Repositories/*Account*` | Minimal | **Expand** — new interfaces + EF implementations |
| `Nebula.Infrastructure/Persistence/Configurations/AccountConfiguration.cs` | Thin | **Rewrite** — new columns, indexes, unique filters, row-version |
| `Nebula.Api/Endpoints/SubmissionEndpoints.cs` (dependent) | Live | **Extend list payload** — add `accountDisplayName`, `accountStatus`, `accountSurvivorId` (denormalized) |
| `Nebula.Api/Endpoints/RenewalEndpoints.cs` (dependent) | Live | **Extend list payload** — same denormalized fields |
| Casbin `policy.csv` | Existing | **Extend** — add `account:read`, `account:create`, `account:update`, `account:deactivate`, `account:reactivate`, `account:delete`, `account:merge`, `account:contact:manage` |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `Nebula.Domain/Entities/AccountContact.cs` | Domain | Account-scoped contact entity |
| `Nebula.Domain/Entities/AccountRelationshipHistory.cs` | Domain | Append-only relationship change log |
| `Nebula.Domain/Workflow/AccountLifecycleStateMachine.cs` | Domain | Explicit state machine per ADR-011 |
| `Nebula.Application/DTOs/AccountDto.cs` | Application | Detail response |
| `Nebula.Application/DTOs/AccountListItemDto.cs` | Application | List row |
| `Nebula.Application/DTOs/AccountSummaryDto.cs` | Application | Overview counts payload |
| `Nebula.Application/DTOs/AccountCreateRequestDto.cs` | Application | Create request |
| `Nebula.Application/DTOs/AccountUpdateRequestDto.cs` | Application | Profile update request |
| `Nebula.Application/DTOs/AccountLifecycleRequestDto.cs` | Application | Deactivate/Reactivate/Delete request (toState + reasonCode/detail) |
| `Nebula.Application/DTOs/AccountMergeRequestDto.cs` | Application | Merge request with survivorAccountId |
| `Nebula.Application/DTOs/AccountContactRequestDto.cs` | Application | Contact create/update request |
| `Nebula.Application/DTOs/AccountRelationshipRequestDto.cs` | Application | Broker-of-record / producer / territory change request |
| `Nebula.Application/Services/AccountService.cs` | Application | Core profile operations |
| `Nebula.Application/Services/AccountLifecycleService.cs` | Application | State-machine transitions with role gates |
| `Nebula.Application/Services/AccountMergeService.cs` | Application | Synchronous merge; idempotent; emits both-sides timeline |
| `Nebula.Application/Services/AccountSummaryService.cs` | Application | Composed overview payload (counts + last activity) |
| `Nebula.Application/Services/AccountContactService.cs` | Application | Contact CRUD |
| `Nebula.Application/Interfaces/IAccountRepository.cs` | Application | Repository interface |
| `Nebula.Application/Interfaces/IAccountContactRepository.cs` | Application | Contact repository interface |
| `Nebula.Application/Interfaces/IAccountRelationshipHistoryRepository.cs` | Application | History repository interface |
| `Nebula.Application/Validators/Account*Validator.cs` | Application | FluentValidation for each request DTO |
| `Nebula.Infrastructure/Repositories/AccountRepository.cs` | Infrastructure | EF implementation |
| `Nebula.Infrastructure/Repositories/AccountContactRepository.cs` | Infrastructure | EF implementation |
| `Nebula.Infrastructure/Repositories/AccountRelationshipHistoryRepository.cs` | Infrastructure | EF implementation |
| `Nebula.Infrastructure/Persistence/Configurations/AccountConfiguration.cs` | Infrastructure | EF config (extended) |
| `Nebula.Infrastructure/Persistence/Configurations/AccountContactConfiguration.cs` | Infrastructure | EF config |
| `Nebula.Infrastructure/Persistence/Configurations/AccountRelationshipHistoryConfiguration.cs` | Infrastructure | EF config |
| `Nebula.Infrastructure/Persistence/Migrations/*_F0016_AccountLifecycle.cs` | Infrastructure | Accounts column migration |
| `Nebula.Infrastructure/Persistence/Migrations/*_F0016_AccountContacts.cs` | Infrastructure | Contacts + history migration |
| `Nebula.Infrastructure/Persistence/Migrations/*_F0016_DependentFallbackDenormalization.cs` | Infrastructure | Fallback contract migration on Submissions/Renewals/Policies |

## Step 1 — Migration: Accounts Lifecycle

**Migration name:** `F0016_AccountLifecycle`

### Column changes

```
ALTER TABLE Accounts
  ADD COLUMN LegalName             varchar(200) NULL,
  ADD COLUMN TaxId                 varchar(50)  NULL,
  ADD COLUMN Industry              varchar(100) NULL,
  ADD COLUMN PrimaryLineOfBusiness varchar(50)  NULL,
  ADD COLUMN Status                varchar(20)  NOT NULL DEFAULT 'Active',
  ADD COLUMN BrokerOfRecordId      uuid         NULL REFERENCES Brokers(Id),
  ADD COLUMN PrimaryProducerUserId uuid         NULL,
  ADD COLUMN TerritoryCode         varchar(50)  NULL,
  ADD COLUMN Region                varchar(50)  NULL,
  ADD COLUMN Address1              varchar(200) NULL,
  ADD COLUMN Address2              varchar(200) NULL,
  ADD COLUMN City                  varchar(100) NULL,
  ADD COLUMN State                 varchar(50)  NULL,
  ADD COLUMN PostalCode            varchar(20)  NULL,
  ADD COLUMN Country               varchar(50)  NULL,
  ADD COLUMN MergedIntoAccountId   uuid         NULL REFERENCES Accounts(Id),
  ADD COLUMN DeleteReasonCode      varchar(50)  NULL,
  ADD COLUMN DeleteReasonDetail    varchar(500) NULL,
  ADD COLUMN RemovedAt             timestamptz  NULL,
  ADD COLUMN StableDisplayName     varchar(200) NOT NULL DEFAULT '';
```

### Indexes

```
CREATE UNIQUE INDEX IX_Accounts_TaxId_Active
  ON Accounts(LOWER(TRIM(TaxId))) WHERE Status = 'Active' AND TaxId IS NOT NULL;

CREATE INDEX IX_Accounts_Status_Region        ON Accounts(Status, Region);
CREATE INDEX IX_Accounts_BrokerOfRecordId     ON Accounts(BrokerOfRecordId);
CREATE INDEX IX_Accounts_TerritoryCode        ON Accounts(TerritoryCode);
CREATE INDEX IX_Accounts_MergedIntoAccountId  ON Accounts(MergedIntoAccountId);
CREATE INDEX IX_Accounts_DisplayName_Trgm     ON Accounts USING gin (DisplayName gin_trgm_ops);
```

### Backfill

- `StableDisplayName` ← `DisplayName` for all existing rows.
- `Status` ← `'Active'` (default) for all existing rows.

## Step 2 — Migration: Contacts + Relationship History

**Migration name:** `F0016_AccountContactsAndRelationshipHistory`

```
CREATE TABLE AccountContacts (
  Id               uuid PRIMARY KEY,
  AccountId        uuid NOT NULL REFERENCES Accounts(Id) ON DELETE RESTRICT,
  FullName         varchar(200) NOT NULL,
  Role             varchar(100) NULL,
  Email            varchar(200) NULL,
  Phone            varchar(50)  NULL,
  IsPrimary        boolean      NOT NULL DEFAULT false,
  CreatedAt        timestamptz  NOT NULL,
  CreatedByUserId  uuid         NOT NULL,
  UpdatedAt        timestamptz  NOT NULL,
  UpdatedByUserId  uuid         NOT NULL,
  IsDeleted        boolean      NOT NULL DEFAULT false,
  RowVersion       xid          NOT NULL
);
CREATE UNIQUE INDEX IX_AccountContacts_AccountId_Primary
  ON AccountContacts(AccountId) WHERE IsPrimary = true AND IsDeleted = false;
CREATE INDEX IX_AccountContacts_AccountId ON AccountContacts(AccountId);

CREATE TABLE AccountRelationshipHistory (
  Id                uuid PRIMARY KEY,
  AccountId         uuid NOT NULL REFERENCES Accounts(Id) ON DELETE RESTRICT,
  RelationshipType  varchar(30)  NOT NULL,
  PreviousValue     varchar(200) NULL,
  NewValue          varchar(200) NULL,
  EffectiveAt       timestamptz  NOT NULL,
  ActorUserId       uuid         NOT NULL,
  Notes             varchar(500) NULL
);
CREATE INDEX IX_AccountRelationshipHistory_AccountId_EffectiveAt
  ON AccountRelationshipHistory(AccountId, EffectiveAt DESC);
```

## Step 3 — Migration: Dependent Fallback Denormalization

**Migration name:** `F0016_DependentFallbackDenormalization`

```
ALTER TABLE Submissions
  ADD COLUMN AccountDisplayNameAtLink varchar(200) NULL,
  ADD COLUMN AccountStatusAtRead      varchar(20)  NULL,
  ADD COLUMN AccountSurvivorId        uuid         NULL;

ALTER TABLE Renewals
  ADD COLUMN AccountDisplayNameAtLink varchar(200) NULL,
  ADD COLUMN AccountStatusAtRead      varchar(20)  NULL,
  ADD COLUMN AccountSurvivorId        uuid         NULL;

ALTER TABLE Policies
  ADD COLUMN AccountDisplayNameAtLink varchar(200) NULL,
  ADD COLUMN AccountStatusAtRead      varchar(20)  NULL,
  ADD COLUMN AccountSurvivorId        uuid         NULL;
```

Backfill: populate the three columns from current `Accounts` join in a one-shot data migration step inside the migration.

## Step 4 — Domain Entities

### Account (extended)

```csharp
public class Account : BaseEntity
{
    public string DisplayName { get; set; } = default!;
    public string StableDisplayName { get; set; } = default!;
    public string? LegalName { get; set; }
    public string? TaxId { get; set; }
    public string? Industry { get; set; }
    public string? PrimaryLineOfBusiness { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public Guid? BrokerOfRecordId { get; set; }
    public Guid? PrimaryProducerUserId { get; set; }
    public string? TerritoryCode { get; set; }
    public string? Region { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public Guid? MergedIntoAccountId { get; set; }
    public string? DeleteReasonCode { get; set; }
    public string? DeleteReasonDetail { get; set; }
    public DateTime? RemovedAt { get; set; }

    // Navigation
    public ICollection<AccountContact> Contacts { get; } = new List<AccountContact>();
    public Account? MergedInto { get; set; }
    public Broker? BrokerOfRecord { get; set; }
}

public enum AccountStatus { Active, Inactive, Merged, Deleted }
```

### AccountLifecycleStateMachine

Allowed transitions per ADR-011 pattern:

| From | To | Guards | Role |
|------|----|--------|------|
| Active | Inactive | — | DistributionManager, Admin |
| Inactive | Active | — | DistributionManager, Admin |
| Active | Merged | `survivorAccountId` required; survivor `Active`; not self | DistributionManager, Admin |
| Inactive | Merged | same | DistributionManager, Admin |
| Active | Deleted | `reasonCode` required; `reasonDetail` required if `reasonCode=Other` | DistributionManager, Admin |
| Inactive | Deleted | same | DistributionManager, Admin |

All other transitions return `409 invalid_transition`.

## Step 5 — DTOs

Minimum DTO family:

- `AccountDto` — detail response (see `schemas/account.schema.json`)
- `AccountListItemDto` — list row; includes denormalized count columns when `include=summary`
- `AccountSummaryDto` — counts + last activity
- `AccountCreateRequestDto` — profile + optional broker/territory + optional linked entity (for from-submission / from-policy flow)
- `AccountUpdateRequestDto` — profile fields; `If-Match` required
- `AccountLifecycleRequestDto` — `toState`, `reasonCode?`, `reasonDetail?`
- `AccountMergeRequestDto` — `survivorAccountId`, optional `notes`
- `AccountContactRequestDto` — for create and update
- `AccountRelationshipRequestDto` — for broker-of-record/producer/territory change

## Step 6 — Validators

- `AccountCreateValidator` — `DisplayName` required; `TaxId` format if present; `BrokerOfRecordId` existence
- `AccountUpdateValidator` — same; `RowVersion` required via `If-Match` header
- `AccountLifecycleValidator` — conditional: `reasonCode` required when `toState=Deleted`; `survivorAccountId` required when `toState=Merged`
- `AccountMergeValidator` — `survivorAccountId` required and not-self; survivor must be active
- `AccountContactValidator` — `FullName` required; email / phone format; `IsPrimary` sets single-primary (enforced via filtered unique index at DB level)
- `AccountRelationshipValidator` — `RelationshipType` ∈ enum; `NewValue` format (uuid for broker/producer, string for territory)

## Step 7 — Repositories

```csharp
public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Account?> GetByIdIncludingTombstoneAsync(Guid id, CancellationToken ct);
    Task<PagedResult<Account>> ListAsync(AccountListQuery q, CancellationToken ct);
    Task<bool> ExistsByTaxIdAsync(string taxId, CancellationToken ct);
    Task<bool> HasDuplicateByNameAsync(string displayName, CancellationToken ct);
    Task<AccountSummary> GetSummaryAsync(Guid id, CancellationToken ct);
    Task AddAsync(Account account, CancellationToken ct);
    Task UpdateAsync(Account account, CancellationToken ct);
}

public interface IAccountContactRepository { /* CRUD + SetPrimaryAsync */ }
public interface IAccountRelationshipHistoryRepository { Task AddAsync(...); Task<IReadOnlyList<AccountRelationshipHistory>> GetAsync(Guid accountId); }
```

## Step 8 — Services

- `AccountService` — profile read/create/update; summary read; list with ABAC filter composition
- `AccountLifecycleService` — Deactivate / Reactivate / Delete; appends `WorkflowTransition` (type=`AccountLifecycle`) + `ActivityTimelineEvent`
- `AccountMergeService` — merge source → survivor:
  1. Validate both exist, source is Active/Inactive, survivor is Active.
  2. Transaction: set `source.Status=Merged`, `source.MergedIntoAccountId=survivor.Id`, `source.RemovedAt=now`, `source.StableDisplayName` frozen; append `WorkflowTransition` on source; append `ActivityTimelineEvent` on source AND survivor.
  3. Do NOT re-point any dependent FKs (tombstone-forward per ADR-017).
  4. Idempotency: return `200` with same payload if called again on an already-merged source with matching survivor; return `409 conflict` if survivor differs.
- `AccountSummaryService` — composed query returning `activePolicyCount`, `openSubmissionCount`, `renewalDueCount`, `lastActivityAt`. Single aggregated query per account (no N+1). Counts enforce ABAC via same predicate as submission/renewal list queries.
- `AccountContactService` — CRUD with `SetPrimary` enforcing at-most-one primary (DB filtered unique).

## Step 9 — API Endpoints

See [planning-mds/api/nebula-api.yaml](../../api/nebula-api.yaml) for full OpenAPI contract. Endpoint surface:

| Method | Route | Purpose | Authorization |
|--------|-------|---------|---------------|
| GET | `/accounts` | Paginated + filtered + sorted list | `account:read` |
| POST | `/accounts` | Create (with optional link-from) | `account:create` |
| GET | `/accounts/{id}` | Detail (tombstone-aware: 410 Deleted, 200 status=Merged) | `account:read` |
| PUT | `/accounts/{id}` | Profile update (`If-Match` required) | `account:update` |
| POST | `/accounts/{id}/lifecycle` | Deactivate / Reactivate / Delete transition | `account:deactivate` / `account:reactivate` / `account:delete` |
| POST | `/accounts/{id}/merge` | Synchronous merge with survivor | `account:merge` |
| GET | `/accounts/{id}/summary` | Overview metrics + counts | `account:read` |
| GET | `/accounts/{id}/contacts` | Contact rail | `account:read` |
| POST | `/accounts/{id}/contacts` | Create contact | `account:contact:manage` |
| PUT | `/accounts/{id}/contacts/{contactId}` | Update contact | `account:contact:manage` |
| DELETE | `/accounts/{id}/contacts/{contactId}` | Soft-delete contact | `account:contact:manage` |
| POST | `/accounts/{id}/relationships` | Broker-of-record / producer / territory change | `account:update` |
| GET | `/accounts/{id}/submissions` | Submissions rail (paginated) | `account:read` + `submission:read` filter |
| GET | `/accounts/{id}/policies` | Policies rail (paginated) | `account:read` + `policy:read` filter |
| GET | `/accounts/{id}/renewals` | Renewals rail (paginated) | `account:read` + `renewal:read` filter |
| GET | `/accounts/{id}/timeline` | Timeline rail (paginated, append-only) | `account:read` |

All write endpoints require `If-Match` with `RowVersion` for optimistic concurrency.
All endpoints return RFC 7807 ProblemDetails on error.

## Step 10 — Casbin Policy

Extend `policy.csv` with:

```
p, role:distribution-user,    account, read,              region-scope, account-broker-scope
p, role:distribution-user,    account, create,            region-scope
p, role:distribution-user,    account, update,            region-scope, account-broker-scope
p, role:distribution-user,    account, contact:manage,    region-scope, account-broker-scope
p, role:distribution-manager, account, read,              territory-scope
p, role:distribution-manager, account, create,            territory-scope
p, role:distribution-manager, account, update,            territory-scope
p, role:distribution-manager, account, deactivate,        territory-scope
p, role:distribution-manager, account, reactivate,        territory-scope
p, role:distribution-manager, account, delete,            territory-scope
p, role:distribution-manager, account, merge,             territory-scope
p, role:distribution-manager, account, contact:manage,    territory-scope
p, role:underwriter,          account, read,              assigned-book-scope
p, role:relationship-manager, account, read,              managed-broker-scope
p, role:relationship-manager, account, contact:manage,    managed-broker-scope
p, role:admin,                account, *,                 *
# external-user: no rules — default deny
```

## Step 11 — Dependent Denormalization in F0006 + F0007

- Submission list endpoint: populate `accountDisplayName`, `accountStatus`, `accountSurvivorId` on every row from denormalized columns (no re-join).
- Renewal list endpoint: same.
- Regression tests (per ADR-017): add one test per module that renders a Deleted account and one that renders a Merged account.

## Step 12 — Seed Data

Seed:
- 20 dev accounts across regions and territories with mixed Active/Inactive statuses.
- 2 explicitly-Merged account pairs (source → survivor) to exercise fallback.
- 1 explicitly-Deleted account linked to existing submissions/renewals for fallback smoke test.
- 3 contacts per account with one marked primary.

## Architecture Traceability

- [ADR-011](../../architecture/decisions/ADR-011-crm-workflow-state-machines-and-transition-history.md) — Reused for account lifecycle state machine + append-only `WorkflowTransition` history (`WorkflowType="AccountLifecycle"`).
- [ADR-008](../../architecture/decisions/ADR-008-casbin-enforcer-adoption.md) — Reused for `account:*` ABAC enforcement.
- [ADR-009](../../architecture/decisions/ADR-009-lob-classification-and-sla-configuration.md) — Reused for `PrimaryLineOfBusiness` classification.
- [ADR-012](../../architecture/decisions/ADR-012-shared-document-storage-and-metadata-architecture.md) — Integrated with on Account 360 Documents rail.
- [ADR-017](../../architecture/decisions/ADR-017-account-merge-tombstone-and-fallback-contract.md) — Owned: merge + tombstone + fallback contract introduced by F0016.

## Non-Functional Acceptance

- Account list p95 ≤ 300 ms @ 10 000 accounts under filter + pagination; gin trigram index on `DisplayName` satisfies prefix/contains search.
- Account 360 overview p95 ≤ 500 ms; each rail independently ≤ 400 ms.
- Merge synchronous commit p95 ≤ 2 s for source accounts with ≤ 500 linked records (submissions + policies + renewals + timeline).
- Merge retry idempotent: same input → same timeline; no duplicate events.
- Dependent list endpoints never 500 when joined account is `Merged` or `Deleted`; enforced via regression tests.

## Open Items for Build-Time Refinement

- Exact summary-projection SQL (single aggregated query) will be finalized in the build step for S0011.
- Temporal-backed async merge for > 500-linked-record accounts is a Future follow-up; not part of F0016 MVP.
- F0023 global search `includeRemoved=true` flag is governed by this contract but lives in F0023.
