# ADR-017: Account Merge, Tombstone Semantics, and Dependent-View Fallback Contract

**Status:** Proposed
**Date:** 2026-04-13
**Owners:** Architect
**Related Features:** F0016, F0006 (archived), F0007 (archived), F0018, F0020, F0023

## Context

F0016 introduces Account as a first-class aggregate with a full lifecycle (Active, Inactive, Merged, Deleted). Several dependent modules already have read paths that join to Account: submissions (F0006, archived), renewals (F0007, archived), policies (F0018 surface, Policy stub seeded by F0007), Account 360 composition, and — later — global search (F0023) and documents (F0020).

Before F0016, dependent views assumed the Account row is always live. Any lifecycle removal (merge or delete) would make a join fail, producing broken headers, 500s, or empty list rows. The F0006 closeout explicitly descoped this contract, naming F0016 as the owning feature.

Two design directions were considered:

1. **FK re-pointing at merge time** — rewrite every dependent foreign key from the source account to the survivor inside a single merge transaction.
2. **Tombstone-forward + denormalized fallback columns** — keep the FK on the source; mark the source `Merged` with `MergedIntoAccountId`; have dependent list/detail views render from denormalized stable display fields and follow the forward pointer on detail navigation.

Direction (1) scales poorly. A merge can fan out to thousands of rows across multiple modules, requires a distributed transaction or a long-running Temporal workflow, and makes merge partially observable (row partially re-pointed) if interrupted. Direction (2) keeps the merge commit small, makes it idempotent on retry, and lets dependent modules remain decoupled from the merge execution model.

## Decision

Account merge and delete are implemented using **tombstone-forward semantics with denormalized fallback columns** on every dependent list/detail contract.

### Account-Side Rules

- `Status=Active` and `Status=Inactive` are writable; `Status=Merged` and `Status=Deleted` are read-only terminals for the Account aggregate (no profile mutations, no contact mutations, no relationship mutations).
- When transitioning to `Merged`, the source row persists with `MergedIntoAccountId` set to the survivor id, `RemovedAt` timestamp set, and `StableDisplayName` frozen.
- When transitioning to `Deleted`, the row persists with `DeleteReasonCode`, optional `DeleteReasonDetail`, `RemovedAt`, and `StableDisplayName` frozen.
- `MergedIntoAccountId` and `DeleteReasonCode` are never rewritten after set. Unmerge / undelete are explicit Future scope.
- Merge commit is synchronous in MVP and must be idempotent on retry (same input → same output, no duplicate timeline events). Large-account async/Temporal-backed merge is deferred.
- Merge and delete each append exactly one `WorkflowTransition` row and one `ActivityTimelineEvent` row on the account, and — for merge — one additional `ActivityTimelineEvent` row on the survivor describing the merge-in event.

### Dependent-Module Contract

Every dependent list endpoint (submissions, renewals, policies, activity timeline, and future global search) that joins Account MUST:

1. **Denormalize stable account fields** onto the dependent row's read contract:
   - `accountId` (existing FK)
   - `accountDisplayName` (sourced from `Account.StableDisplayName` at link time; updated opportunistically while the source account remains `Active`/`Inactive`)
   - `accountStatus` (current status at read time)
   - `accountSurvivorId` (nullable; populated when `accountStatus=Merged`)
2. Never 500 when the live account is `Merged` or `Deleted`. The denormalized fields are the source of truth for rendering the label.
3. Enforce this via at-least-one integration test per dependent feature that loads the list and detail with a Deleted account and with a Merged account.

### API Semantics

| Path | State | Response |
|------|-------|----------|
| `GET /api/accounts/{id}` | Active / Inactive | `200` with full account payload |
| `GET /api/accounts/{id}` | Merged | `200` with `status=Merged` and `survivorAccountId` populated (frontend navigates to survivor on detection) |
| `GET /api/accounts/{id}` | Deleted | `410 Gone` with ProblemDetails body containing `stableDisplayName`, `removedAt`, `reasonCode` |
| Dependent list / detail | Any account state | Returns the dependent payload with denormalized account fields populated |

### UI Rendering Rules

- Deleted label: `"<stable name> [Deleted]"` red pill, no clickthrough.
- Merged label: `"<stable name> → <survivor name>"` amber pill, clickthrough to survivor Account 360.
- Tombstone-forward redirect: the Account 360 frontend follows one hop to the survivor on a merged source and surfaces a toast; chained merges (survivor itself merged) stop after one hop and warn the user.
- Create-from-account actions disabled with explanatory tooltip when the account is `Merged` or `Deleted`.

### Scope of This ADR

**In scope:**

- Account aggregate state model and tombstone semantics
- The denormalization and read-side contract for every dependent list/detail endpoint
- The HTTP response semantics and UI labeling rules

**Out of scope:**

- FK re-pointing at merge time (decided against; see Alternatives)
- Unmerge and undelete admin flows (deferred follow-up)
- Async / Temporal-backed merge for large accounts (deferred; MVP is synchronous up to ≤ 500 linked records)
- Chained-merge collapsing or deduplication (deferred)

## Alternatives Considered

### Alternative A: FK re-pointing on merge

Rewrite every dependent FK from source account to survivor inside the merge transaction.

- **Pros:** Dependent views only see live accounts; no denormalization needed.
- **Cons:** Merge transaction becomes very large; cross-module write fan-out; partial failure leaves the system in a mixed state; effectively forces distributed transaction or Temporal orchestration for every merge; makes dependent modules coupled to merge execution.

Rejected because it pushes merge execution cost into every dependent module and makes merge non-idempotent without substantial additional work.

### Alternative B: Soft-delete Account with no explicit Merged state

Treat merges as a soft-delete with an external "survivor" pointer in a separate reconciliation table.

- **Pros:** Smaller schema change on the Account aggregate.
- **Cons:** Splits the merged-relationship across two tables; makes audit harder; dependent views still need a fallback path, so the denormalization requirement does not go away.

Rejected because it complicates the relationship without removing the fallback-contract requirement.

### Alternative C: Deny reads on removed accounts

Return `404` for `Merged` and `Deleted` accounts and require callers to handle it.

- **Cons:** Dependent views lose the stable display name; regressions would cascade across every consumer; error-handling every `404` is more brittle than reading a denormalized field.

Rejected because the cost of broken dependent views is exactly what this ADR exists to prevent.

## Consequences

### Positive

- Merge commit stays small, fast, and idempotent.
- Dependent modules are decoupled from merge execution; no distributed transaction.
- Dependent list queries render correctly with zero extra round-trips thanks to denormalized fields.
- Audit trail is complete: one `WorkflowTransition` and `ActivityTimelineEvent` on source plus one mirror event on survivor.
- Extending the account lifecycle later (e.g., unmerge / undelete) does not require re-pointing FKs.

### Negative

- Every dependent module must ship a one-time additive migration for the denormalized columns plus a backfill step.
- Dependent modules must honor the contract in integration tests; drift would be invisible at the schema level but break the user-visible rendering.
- The `StableDisplayName` field snapshots the account name at link time (with opportunistic refresh while the account is live); this is surfaced in the contract so consumers understand why a just-renamed account may briefly show its previous name on already-linked rows until the refresh runs.
- Chained merges (source → A → B) render with one forward hop in the UI, not end-to-end transparency; collapsing is deferred.

## Implementation Notes

- The Account aggregate stores `StableDisplayName` and freezes it at the moment `Status` transitions into a terminal state.
- Dependent denormalization migrations are additive: add nullable column → backfill from current account rows → enforce NOT NULL at link time (new rows always populate; historical backfill is one-shot).
- Casbin policies extend the existing ABAC shape with `account:merge` and `account:delete` actions, restricted to Distribution Manager and Admin.
- Every lifecycle transition and merge appends a `WorkflowTransition` record under `WorkflowType="AccountLifecycle"` (see ADR-011).

## Related ADRs

- [ADR-011](./ADR-011-crm-workflow-state-machines-and-transition-history.md) — State machine + append-only history pattern reused for account lifecycle.
- [ADR-008](./ADR-008-casbin-enforcer-adoption.md) — ABAC enforcement mechanism used for `account:*` actions.
- [ADR-009](./ADR-009-lob-classification-and-sla-configuration.md) — LOB classification on accounts when known.
- [ADR-012](./ADR-012-shared-document-storage-and-metadata-architecture.md) — Documents rail on Account 360 reuses the shared document subsystem.

## Related Features

- [F0016 Account 360 & Insured Management](../../features/F0016-account-360-and-insured-management/PRD.md)
- F0006 Submission Intake Workflow (archived; consumer of fallback contract)
- F0007 Renewal Pipeline (archived; consumer of fallback contract)
- F0018 Policy Lifecycle & Policy 360 (future; consumer of fallback contract)
- F0020 Document Management & ACORD Intake (future; Documents rail on Account 360)
- F0023 Global Search & Saved Views (future; `includeRemoved` flag governed by this ADR)
