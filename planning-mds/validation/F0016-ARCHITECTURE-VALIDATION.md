# Architecture Validation Report — F0016 (Account 360 & Insured Management)

**Scope:** Architecture + Implementation alignment (code on branch `feat/F0016-account-360-and-insured-management`, uncommitted)
**Date:** 2026-04-15
**Reviewer:** Architect agent (independent re-validation; did not defer to existing `F0016-ARCHITECT-REVIEW.md`)

## Summary

- **Assessment:** VALID WITH RECOMMENDATIONS
- **Areas checked:** BLUEPRINT §4, PRD, feature-assembly-plan, ADR-011, ADR-017, SOLUTION-PATTERNS, OpenAPI `/accounts` surface, Casbin `policy.csv`, 3 F0016 migrations, `Account`/`AccountContact`/`AccountRelationshipHistory` entities + EF configurations, `AccountService`/`AccountRepository`, dependent `Submission`/`Renewal`/`Policy` entities/DTOs/services/repositories, frontend route wiring.
- **Issues found:** 0 critical / 0 high / 3 medium / 3 low

## Findings by Severity

### Critical (fundamental architecture flaws)

_None._

### High (significant gaps)

_None._

### Medium (minor inconsistencies)

1. **Denormalized fallback columns are nullable in EF config, violating ADR-017 "enforce NOT NULL at link time" intent.**
   - Location: `engine/src/Nebula.Infrastructure/Persistence/Configurations/SubmissionConfiguration.cs:21-22`, `RenewalConfiguration.cs:22-23`, `PolicyConfiguration.cs:24-25`; migration `20260414122000_F0016_DependentFallbackDenormalization.cs:18-73` adds all three columns as `nullable: true` on all three tables.
   - ADR-017 "Implementation Notes" (`planning-mds/architecture/decisions/ADR-017-account-merge-tombstone-and-fallback-contract.md:125`) states: "add nullable column → backfill from current account rows → enforce NOT NULL at link time (new rows always populate; historical backfill is one-shot)." The backfill is present (lines 75-106), but no follow-up `AlterColumn(nullable:false)` or check constraint is applied and there is no DB-level invariant that new rows must populate the columns.
   - Impact: If a future code path inserts a `Submission`/`Renewal`/`Policy` without passing through `SubmissionService`/`RenewalService` (which do populate the fields at lines 121-123 / 103-105), dependent list rendering degrades silently to a cold join — the very failure ADR-017 exists to prevent.
   - Recommendation: Either (a) add `IsRequired()` in the three EF configs and a follow-up migration setting `NOT NULL` after backfill, or (b) add a CHECK constraint / DB trigger; at minimum, document the deferred NOT NULL step in STATUS.md follow-ups.

2. **`AccountRelationshipHistory.EffectiveAt` index is not descending, diverging from the planned `DESC` ordering.**
   - Location: Migration `20260414121000_F0016_AccountContactsAndRelationshipHistory.cs:82-85` creates `IX_AccountRelationshipHistory_AccountId_EffectiveAt` as an ascending composite index; the feature-assembly-plan (`feature-assembly-plan.md:173-175`) specifies `(AccountId, EffectiveAt DESC)` for recent-change retrieval.
   - Impact: Recent-change reads (latest relationship first) cannot use the index scan direction as designed; at scale PostgreSQL may still serve it via backward scan, so correctness is preserved, but the NFR "relationship history retrieval optimized for recent-first" is not hardware-enforced.
   - Recommendation: Respin the index with `descending: new[] { false, true }` on `EffectiveAt`. Low-risk re-create.

3. **`Accounts(TaxId)` unique-filtered index does not appear in the EF model snapshot / `AccountConfiguration.cs`, only in raw SQL inside the migration.**
   - Location: Migration `20260414120000_F0016_AccountLifecycle.cs:221-232` creates `IX_Accounts_TaxId_Active` and `IX_Accounts_DisplayName_Trgm` via `migrationBuilder.Sql(...)`; `engine/src/Nebula.Infrastructure/Persistence/Configurations/AccountConfiguration.cs` (full file, 80 lines) does not declare them via `HasIndex(...).HasFilter(...)`.
   - Impact: Any future EF-generated migration will not detect these indexes, potentially emitting a spurious "add missing index" or (worse) dropping state if someone runs `dotnet ef migrations add` and trusts the diff. Runtime enforcement is fine because the migration ran.
   - Recommendation: Add `builder.HasIndex(e => e.TaxId).HasFilter("\"Status\" = 'Active' AND \"TaxId\" IS NOT NULL AND \"IsDeleted\" = false").IsUnique();` in `AccountConfiguration.cs`, or add a suppression note. Matches the pattern already used for other filtered indexes in the codebase.

### Low (optimization opportunities)

1. **`AccountRepository.UpdateAsync` is a no-op (`Task UpdateAsync(...) => Task.CompletedTask;` at `engine/src/Nebula.Infrastructure/Repositories/AccountRepository.cs:157`).**
   - EF change tracking on the loaded `Account` aggregate handles persistence, so this is functionally correct. However, the unconditional no-op hides the EF-tracking assumption and will surprise a future contributor who expects `UpdateAsync` to actually stage the write. Consider either removing the method from `IAccountRepository` or adding a comment noting the change-tracking contract.

2. **`Account` entity keeps legacy `Name` / `PrimaryState` compatibility properties with `[NotMapped]` facades `DisplayName` / `State` (`Nebula.Domain/Entities/Account.cs:8-52`).**
   - This is a pragmatic bridge to avoid touching F0006/F0007 consumers, and the migration renames the columns (`20260414120000_F0016_AccountLifecycle.cs:21-29`), so end-state is correct. Flag for future cleanup: once dependent modules drift-align, the `Name`/`PrimaryState` shims can be removed so the domain entity reads cleanly.

3. **`AccountLifecycleStateMachine` lives in `Nebula.Domain/Workflow/`, but the feature-assembly-plan Step 4 called it out under `Nebula.Domain/Entities/` line-up.**
   - Location: `engine/src/Nebula.Domain/Workflow/AccountLifecycleStateMachine.cs:1-22` vs plan line 63. `Workflow/` is the more correct namespace (aligns with `ADR-011` pattern and existing `LineOfBusinessCatalog`/`OpportunityStatusCatalog`). Not a defect; only mentioned for traceability — the plan could be annotated to match reality.

## Checklist Results

- **Completeness: PASS.** BLUEPRINT §4.x lists F0016 at `planning-mds/BLUEPRINT.md:184` and the feature owns its own PRD, feature-assembly-plan, 11 story files, STATUS, 5 review reports, and ADR-017. OpenAPI has a complete `/accounts` surface (`planning-mds/api/nebula-api.yaml:344-...`). Casbin rules exist for every `account:*` action (`planning-mds/security/policies/policy.csv:212-244`). Three EF migrations implement schema, children, and dependent fallback columns.

- **Requirements Alignment: PASS.** Every PRD AC traces to code:
  - List/search/filter → `AccountEndpoints.ListAccounts` (`AccountEndpoints.cs:45-89`) + `AccountRepository.ListAsync` with trigram index.
  - Create (manual + from-submission/policy) → `CreateAccount` (`AccountEndpoints.cs:91-116`) + `AccountService.CreateAsync` (`AccountService.cs:79-157`).
  - Detail + inline edit + optimistic concurrency → `GetAccount`/`UpdateAccount` (`AccountEndpoints.cs:118-188`) with `If-Match`/`xmin` (`AccountConfiguration.cs:57-61`).
  - Account 360 composition → `/summary`, `/contacts`, `/submissions`, `/renewals`, `/policies`, `/timeline` endpoints (`AccountEndpoints.cs:268-540`).
  - Lifecycle (deactivate/reactivate/delete) → `TransitionLifecycle` (`AccountEndpoints.cs:190-230`) + `AccountLifecycleStateMachine`; Merged toState blocked at validator (`AccountLifecycleValidator.cs:13`) so merge is only reachable via `/merge`.
  - Merge + tombstone → `MergeAccount` (`AccountEndpoints.cs:232-266`) + `AccountService.MergeAsync` (`AccountService.cs:477-586`); idempotent same-survivor branch at lines 504-509.
  - Fallback contract → denormalized columns on `Submission`/`Renewal`/`Policy` entities + `AccountRepository.PropagateFallbackStateAsync` (`AccountRepository.cs:128-155`), consumed in `SubmissionService` (lines 511-521) and `RenewalService` (lines 409-419).
  - Activity timeline → append-only `ActivityTimelineEvent` emission on every mutation path in `AccountService` (create: 132-149; update: 234-244; relationship: 346-362; lifecycle: 447-463; merge source + survivor: 537-574).
  - Summary projection → `GetSummary` (`AccountEndpoints.cs:268-295`) + `AccountRepository.GetSummaryProjectionAsync`.
  - ABAC → `HasAccessAsync` (`AccountEndpoints.cs:548-561`) invoked on every handler.

- **Pattern Compliance: PASS.**
  - Clean-architecture layering respected: Domain → Application → Infrastructure → Api; EF types stay in Infrastructure.
  - Audit fields present on `Account` (via `BaseEntity`) and `AccountContact` (migration columns `CreatedAt/By`, `UpdatedAt/By`, `DeletedAt/By`, `IsDeleted`, `xmin`).
  - API naming follows `/{resource}/{id}/{sub}` (`AccountEndpoints.cs:25-40`).
  - Errors use `ProblemDetails` consistently (all `error` switches in `AccountEndpoints.cs`).
  - Workflow transitions are append-only — no `Update` method on `IWorkflowTransitionRepository` is invoked; every transition is an `AddAsync`.
  - Timeline events fire on all mutations; merge emits events on both source and survivor per ADR-017.
  - Casbin rules cover all endpoint actions: read, create, update, deactivate, reactivate, delete, merge, contact:manage, relationship:change — all verified in `policy.csv:212-244`.

- **Implementation Alignment: PASS.**
  - **Schema:** Migration SQL matches plan Step 1–3 (lifecycle columns, contacts, relationship history, denormalized columns + backfill). Medium issues #1–#3 above are the only deltas.
  - **API:** All 17 endpoints declared in plan Step 9 are registered in `AccountEndpoints.MapAccountEndpoints` and wired in `Program.cs:266`.
  - **Domain:** `Account` has lifecycle state + `xmin` rowversion + audit; `AccountLifecycleStateMachine` enforces the exact transition matrix in PRD §Workflow.
  - **Dependent read paths:** `Submission`/`Renewal`/`Policy` entities carry `AccountDisplayNameAtLink`/`AccountStatusAtRead`/`AccountSurvivorId`; DTOs project them; services fall back to them when the live `Account` is non-Active.
  - **Casbin:** All 9 `account:*` actions enforced at endpoint entry; tombstone-forward (410 Gone on Deleted) implemented at `AccountEndpoints.cs:132-143` and `AccountService.cs:71-77`.
  - **ADR-011:** Account lifecycle reuses `WorkflowTransition` with `WorkflowType="AccountLifecycle"` (`AccountService.cs:430`) and `"AccountRelationship"` for relationship changes (`AccountService.cs:338`). History is append-only — no update path.
  - **ADR-017:** Tombstone-forward merge implemented: source row keeps FK, gets `Status=Merged` + `MergedIntoAccountId` + frozen `StableDisplayName` + `RemovedAt` (`AccountService.cs:519-524`); dependent FKs are NOT re-pointed (no bulk-update on `Submissions`/`Renewals`/`Policies` AccountId); denormalization is propagated via `PropagateFallbackStateAsync` (`AccountRepository.cs:128-155`) instead. Idempotent retry logic at `AccountService.cs:504-509`.

## Recommendation

**PROCEED.** Architecture is sound and implementation faithfully realizes the plan, PRD, ADR-011, and ADR-017. The three medium findings are data-integrity hardening / EF-model hygiene items — they do not threaten the MVP contract, and can be resolved as follow-up (strongly recommended before the next dependent-module work that writes to `Submissions`/`Renewals`/`Policies`). The three low findings are cosmetic/cleanup.
