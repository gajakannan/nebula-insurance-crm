---
template: feature-prd
version: 2.0
applies_to: product-manager
---

# F0016: Account 360 & Insured Management

**Feature ID:** F0016
**Feature Name:** Account 360 & Insured Management
**Priority:** Critical
**Phase:** CRM Release MVP
**Status:** In Refinement

## Feature Statement

**As an** underwriter, distribution user, distribution manager, or relationship manager
**I want** a first-class insured-centered Account record with a dedicated 360 view and a defined lifecycle (including merge and tombstone behavior)
**So that** I can see and act on the full insured relationship — contacts, submissions, policies, renewals, and activity — from one workspace and rely on stable references even after accounts are deactivated, merged, or deleted

## Business Objective

- **Goal:** Make Account the primary insured context surface inside Nebula.
- **Metric:** Time-to-context on an insured (target: users reach full context in ≤ 3 clicks from global nav); number of workflows originated from Account 360 (submissions, renewals, contact edits); zero broken dependent renders after account lifecycle changes.
- **Baseline:** Account context is fragmented or implicit; linked-record screens break if the underlying account is removed.
- **Target:** Users navigate the full insured relationship from one account workspace, and every dependent view renders predictably when an account is deactivated, merged, or deleted.

## Problem Statement

- **Current State:** There is no dedicated Account 360. Accounts exist as a thin referential row behind submissions/policies/brokers but have no first-class workspace, no lifecycle semantics, and no merge/tombstone behavior. Linked views assume the account row is always live.
- **Desired State:** Accounts are a first-class CRM record with related people, workflow, and policy context; lifecycle actions (deactivate, reactivate, merge, delete) are explicit and auditable; every dependent module has a stable read contract when accounts are no longer live.
- **Impact:** Better underwriting decisions, faster servicing, no lost history, no broken dependent views, and a reliable anchor for all downstream CRM workflows.

## Scope & Boundaries

**In Scope (MVP):**
- Account entity with first-class profile, identifiers, audit fields, and lifecycle states
- Account list view with search, filter (status, territory, broker, LOB, region), sort, pagination
- Account create — manual creation and from-submission / from-policy convenience creation
- Account detail (profile) view with inline edit capabilities and optimistic concurrency
- Account 360 view composing related submissions, policies, renewals, contacts, activity timeline, and summary metrics (progressive loading)
- Account contacts — lightweight CRUD owned by F0016 (name, role, email, phone, primary flag)
- Account relationships — broker-of-record (1:1 active at a time), producer ownership, territory / region assignment
- Account lifecycle — Active → Inactive (deactivate / reactivate), Active/Inactive → Merged (with survivor), Active/Inactive → Deleted (with reason)
- Account merge (synchronous) with duplicate detection hint at create time
- Deleted/merged account fallback contract for dependent submission, policy, renewal, timeline, and search views (picks up the F0006 descope and becomes F0016's owning contract)
- Account-level activity timeline and audit trail (append-only)
- Account summary projection (active policy count, open submission count, renewal-due count, last activity date)
- ABAC enforcement by broker-of-record, territory, region, and role scope

**Out of Scope (Future):**
- Claims and service-case detail (deferred to F0024)
- Full billing, commission, and finance operations (deferred to F0025, F0026)
- External self-service / broker portal visibility (deferred to F0029)
- Carrier system sync and integration hub (deferred to F0030)
- Bulk merge operations (MVP supports single-merge at a time)
- Automated duplicate resolution / fuzzy-match workflows (MVP exposes duplicate hint only; resolution is manual)
- Temporal-backed async merge for multi-thousand-linked-record accounts (synchronous in MVP)
- Saved views, shared lists, and reporting over accounts (deferred to F0023)
- Unmerge / undelete admin recovery flows (not in MVP; tracked as follow-ups)
- Versioned history snapshots of the account row (MVP uses append-only timeline + row-level audit fields)

## Scope Boundary Clarifications

| Capability | Owns | Delegates To | Boundary |
|------------|------|--------------|----------|
| Account entity, profile, lifecycle, merge | **F0016** | — | F0016 owns the Account aggregate and lifecycle |
| Account contacts (lightweight) | **F0016** | — | F0016 owns Account-scoped Contact CRUD for MVP; a later F0021 / shared contact module may generalize |
| Broker-of-record relationship state on an account | **F0016** | **F0002** | F0016 stores and transitions the relationship; F0002 remains authoritative source for Broker entity |
| Deleted / merged account fallback contract across dependent views | **F0016** | — | Owning contract that F0006 descoped; all dependent modules consume it |
| Submissions, renewals, policies listed in Account 360 | — | **F0006 (archived), F0007 (archived), F0018** | F0016 reads + composes; it does not own these records |
| Territory / region assignment rules | **F0016** (storage) | **F0017** (advanced rules, future) | F0016 stores the assignment; F0017 adds hierarchy and rule-based territory management later |
| Documents attached to an account | — | **F0020** | F0016 renders a document section via F0020; does not own document storage |
| Policy data (terms, premium, carrier) | — | **F0018** | F0016 reads the Policy stub today (landed by F0007) and will extend into F0018 when F0018 lands |
| Communication history on account | — | **F0021** | MVP: activity timeline is F0016's; F0021 adds structured comms later |

## Personas

### Underwriter (Primary)
- **Account job-to-be-done:** Understand the complete insured picture — active policies, current renewals, open submissions, broker of record, key contacts — before underwriting a submission or renewal.
- **Key pain:** Context is scattered across screens; underwriting decisions are made with partial insured history; deleted/merged accounts leave submissions "orphaned" on-screen.
- **Success:** Reaches a complete insured context from one workspace in ≤ 3 clicks; can trust that linked records always display a stable account reference.

### Distribution User (Primary)
- **Account job-to-be-done:** Find, create, and update accounts as part of new-business intake and renewal outreach; maintain broker-of-record and contact information.
- **Key pain:** Creates duplicate accounts because there is no search-before-create or duplicate hint; cannot merge confidently because dependent views break.
- **Success:** Can search, create, and merge accounts with confidence; all dependent submissions/renewals continue to render after a merge.

### Distribution Manager (Primary)
- **Account job-to-be-done:** Oversee the accuracy, ownership, and health of the account book in their territory; reassign broker-of-record; resolve duplicates.
- **Key pain:** No single surface showing account counts, ownership, and data quality; no authority trail for lifecycle changes.
- **Success:** Can list, filter, and audit accounts in territory; has authority to merge and delete; every action is auditable.

### Relationship Manager (Secondary)
- **Account job-to-be-done:** Read-only visibility into accounts tied to managed broker relationships; update contacts on those accounts.
- **Key pain:** Cannot confirm whether a broker's books are accurately represented in Nebula.
- **Success:** Read-only visibility to accounts and Account 360 filtered to their broker scope; can update contacts.

## Account Lifecycle Workflow

### States

| State | Description | Owner Role | Entry Condition |
|-------|-------------|------------|-----------------|
| **Active** | Normal operating state; readable and mutable per ABAC | Distribution User / Manager | Created (manual or from submission / policy) |
| **Inactive** | Deactivated; read-only; reactivatable | Distribution Manager | `Active → Inactive` by authorized actor |
| **Merged** | Terminal-for-writes; tombstone forward to survivor account | Distribution Manager / Admin | `Active/Inactive → Merged` with `survivorAccountId` |
| **Deleted** | Terminal-for-writes; tombstone with stable display name, no survivor | Distribution Manager / Admin | `Active/Inactive → Deleted` with `reasonCode` |

### Allowed Transitions

```
Active   → Inactive    (deactivate)
Inactive → Active      (reactivate)
Active   → Merged      (merge; requires survivorAccountId)
Inactive → Merged      (merge; requires survivorAccountId)
Active   → Deleted     (delete; requires reasonCode)
Inactive → Deleted     (delete; requires reasonCode)
```

### Transition Rules

- Invalid transition pairs return HTTP 409 (`code=invalid_transition`).
- `Active → Inactive` and `Inactive → Active`: Actor must be Distribution Manager or Admin.
- `* → Merged`: Actor must be Distribution Manager or Admin; requires `survivorAccountId`; survivor must be Active; a single account may not merge into itself; an already-Merged/Deleted row may not be a merge source.
- `* → Deleted`: Actor must be Distribution Manager or Admin; requires `reasonCode`.
- Merge is synchronous in MVP: on commit, the source account moves to `Merged`, a `mergedIntoAccountId` column is set, and dependent views render via the tombstone-forward contract (see fallback contract below).
- Unmerge and undelete are NOT supported in MVP (deferred follow-up).
- Every lifecycle transition appends one `WorkflowTransition` record and one `ActivityTimelineEvent` record on the account.
- Every merge also writes an `ActivityTimelineEvent` on the survivor account indicating the merge-in event.

### Delete Reason Codes

Validated at API layer (not a DB enum):

- `DuplicateOfAnother` — a duplicate was created in error and cannot be preserved (prefer Merge in most cases)
- `InvalidOrTestRecord` — row was created in error or for testing
- `ReplacedByExternalSystem` — superseded by an external source of truth
- `RequestedByInsured` — removal requested per compliance or legal contract
- `Other` — free-text in `reasonDetail`

## Deleted / Merged Account Fallback Contract

**This is the authoritative contract the F0006 closeout descoped onto F0016.** Every dependent module (submissions, renewals, policies, activity timeline, global search, dashboards) must follow it.

### Stable Display Fallback

- Every linked record persists the **stable display name** and **account identifier** at link time in an immutable field (e.g., `AccountDisplayNameAtLink`) so dependent views can always render even if the live account row is not readable.
- Dependent detail pages MUST render the stable display name with a status suffix: `"Acme Industrial Supply [Deleted]"` or `"Acme Industrial Supply [Merged → Acme Industrial Co]"`.
- Dependent list queries MUST NOT fail if the joined account row is in `Merged` or `Deleted` state; the API returns the stable reference.

### Behavior by Dependent State

| Dependent view state | Deleted Account | Merged Account |
|----------------------|-----------------|----------------|
| List item label | `"<stable name> [Deleted]"` — no clickthrough | `"<stable name> → <survivor name>"` — clickthrough navigates to survivor Account 360 |
| Detail page account header | Shows stable name + `[Deleted]` badge; no link | Auto-redirects to survivor Account 360 on first render (tombstone-forward) |
| Global search | Deleted accounts not returned by default; admins may opt in via a `includeRemoved` flag | Merged source not returned; survivor returned |
| Create-from-this-account actions | Disabled with explanatory tooltip | Disabled on source; user is redirected to survivor |
| Account 360 load on tombstone | Returns 410 Gone for deleted with a stable payload containing name + removed-at + reasonCode; 301-equivalent payload for merged (`{ survivorAccountId }`) | |

### API Semantics

- `GET /api/accounts/{id}` for a deleted account returns `410 Gone` with a ProblemDetails body including `stableDisplayName`, `removedAt`, and `reasonCode`.
- `GET /api/accounts/{id}` for a merged account returns `200` with `status=Merged` and `survivorAccountId` populated; the frontend navigates to `/accounts/{survivorAccountId}` on detection.
- Dependent list endpoints (submissions, renewals, policies) return `accountDisplayName` and `accountStatus` denormalized on the list item payload so UI never needs a second round-trip for the fallback.

### Regression Coverage Requirement

- Every dependent feature with an active list and detail view (F0006 archived, F0007 archived, F0018) must have at least one integration test that loads the list with a Deleted account and one with a Merged account.

## Acceptance Criteria Overview

- [ ] Users can list, search, and filter accounts by status, territory, broker, LOB/industry, region
- [ ] Users can create an account manually and from a submission or policy
- [ ] Users can view and inline-edit the account profile with optimistic concurrency (If-Match / rowVersion)
- [ ] Account 360 renders overview + paginated related lists for submissions, policies, renewals, contacts, activity
- [ ] Users can manage account-scoped contacts (create, update, delete, set primary)
- [ ] Broker-of-record, producer, and territory assignment changes are audited
- [ ] Account lifecycle transitions (deactivate, reactivate, merge, delete) follow the state machine with role gates and required fields
- [ ] Deleted / Merged accounts render per the fallback contract in every dependent view
- [ ] Account summary projection exposes active policy count, open submission count, renewal-due count, and last activity date
- [ ] All mutations append to the account activity timeline
- [ ] ABAC enforcement: users only see accounts within their broker / territory / region scope

## UX / Screens

| Screen | Purpose | Key Actions |
|--------|---------|-------------|
| Account List | Primary account operating surface | Search by name / legal name / tax id; filter by status, territory, broker, LOB, region; sort by name / last activity / policy count; paginate (25/page) |
| Account Create / Edit | Create a new account or update an existing one | Enter profile; duplicate hint on name/tax id match; save with optimistic concurrency |
| Account Detail (Profile) | Full profile view | Inline edit; deactivate / reactivate; delete; initiate merge |
| Account 360 | Composed insured workspace | Overview metrics + tabs / rails for Submissions, Policies, Renewals, Contacts, Activity, Documents (via F0020); each rail paginates independently |
| Account Merge Flow | Resolve a duplicate by merging into a survivor | Pick survivor; preview impact (count of linked submissions / policies / renewals); confirm; see merged timeline entries |
| Deleted / Merged Tombstone | Stable fallback page | Read-only header + `[Deleted]` / `[Merged → X]` badge; link to survivor if merged |

**Key Workflows:**

1. **New-business Account Create Flow** — Distribution user initiates submission intake → is prompted to pick-or-create account → searches by name/tax id → sees duplicate hint if matches → creates account with broker-of-record and territory → submission proceeds with the new account linked.

2. **Account 360 Underwriting Flow** — Underwriter opens an assigned submission → clicks through to Account 360 → reviews active policies, open submissions, renewal-due count, activity timeline → returns to the submission with context.

3. **Duplicate Resolution (Merge) Flow** — Distribution manager identifies a duplicate → opens the source → initiates merge → picks survivor → previews impact (N submissions, M policies, P renewals) → confirms → source transitions to Merged; all dependent views now forward to survivor; timeline recorded on both sides.

4. **Account Deactivation Flow** — Distribution manager deactivates an account that is no longer transacting → account moves to Inactive → read-only from this point → dependent records still render the account normally (Inactive is still a "live" state).

5. **Deleted-Account Linked-View Flow** — A linked submission's account was deleted → submission detail page shows the stable name + `[Deleted]` badge → submission remains readable and advanceable by underwriting → no 500s, no blank pages.

## Data Requirements

**Core Entity: Account**

- `Id` (uuid, PK) — Stable identifier, never reused
- `DisplayName` (string) — Trading / insured display name
- `LegalName` (string, nullable) — Legal entity name if different
- `TaxId` (string, nullable, indexed) — FEIN / EIN / equivalent for duplicate detection
- `Industry` (string, nullable) — Industry classification (NAICS label or free text for MVP)
- `PrimaryLineOfBusiness` (string, nullable) — Primary LOB indicator (per ADR-009 LOB set when known)
- `Status` (enum: `Active`, `Inactive`, `Merged`, `Deleted`)
- `BrokerOfRecordId` (uuid, nullable, FK → Broker) — Active broker of record relationship
- `PrimaryProducerUserId` (uuid, nullable, FK → UserProfile) — Producer / account owner
- `TerritoryCode` (string, nullable) — Territory assignment (MVP: string; F0017 adds hierarchy)
- `Region` (string, nullable) — Used by ABAC region scoping
- `Address1`, `Address2`, `City`, `State`, `PostalCode`, `Country` — Mailing / primary address fields
- `MergedIntoAccountId` (uuid, nullable, FK → Account) — Set when `Status=Merged`
- `DeleteReasonCode` (string, nullable) — Required when `Status=Deleted`
- `DeleteReasonDetail` (string, nullable) — Free-text when reason = Other
- `RemovedAt` (timestamp, nullable) — Set when `Status` becomes Merged or Deleted
- `StableDisplayName` (string) — Snapshot used by fallback contract; set at create and on profile change; never mutated after `Status ∈ {Merged, Deleted}`
- `CreatedAt`, `CreatedByUserId`, `UpdatedAt`, `UpdatedByUserId`, `RowVersion`, `IsDeleted` — Standard audit fields

**Entity: AccountContact** (F0016-owned lightweight)

- `Id`, `AccountId` (FK), `FullName`, `Role` (string), `Email`, `Phone`, `IsPrimary` (bool), audit fields

**Entity: AccountRelationshipHistory** (append-only)

- `Id`, `AccountId`, `RelationshipType` (enum: `BrokerOfRecord`, `PrimaryProducer`, `Territory`), `PreviousValue`, `NewValue`, `EffectiveAt`, `ActorUserId`, `Notes`

**Validation Rules:**

- `DisplayName` required; length ≤ 200
- `TaxId` when provided: must be unique per active account (case-insensitive, trimmed); duplicate hint (not block) on create by name-match OR tax-id-match
- `Status=Merged` requires `MergedIntoAccountId` and forbids same-id self-merge
- `Status=Deleted` requires `DeleteReasonCode`; `DeleteReasonDetail` required when code = `Other`
- `BrokerOfRecordId` must reference an active broker (F0002)
- `StableDisplayName` updates only while `Status ∈ {Active, Inactive}`
- `RowVersion`-based optimistic concurrency on all account mutations

**Data Relationships:**

- Account → Broker (BrokerOfRecord): Optional N:1
- Account → UserProfile (PrimaryProducer): Optional N:1
- Account → AccountContact: 1:N
- Account → AccountRelationshipHistory: 1:N (append-only)
- Account → Account (MergedInto): Optional N:1 (self-referential; survivor relationship)
- Submission → Account: existing N:1 (read by F0016; denormalized fallback columns added on submission entity)
- Renewal → Account: existing N:1 (same pattern)
- Policy → Account: existing N:1 (Policy stub today; F0018 later)

## Role-Based Access

| Role | Read | Create | Update (profile) | Deactivate / Reactivate | Delete | Merge | Contacts CRUD |
|------|------|--------|------------------|--------------------------|--------|-------|----------------|
| Distribution User | scope: own region + assigned broker(s) | Yes | Yes | No | No | No | Yes |
| Distribution Manager | scope: own territory | Yes | Yes | Yes | Yes | Yes | Yes |
| Underwriter | read-only, own assigned book | No | No | No | No | No | No |
| Relationship Manager | read-only, own managed broker(s) | No | No (profile); Yes (contacts) | No | No | No | Yes (on managed brokers' accounts) |
| Admin | All | Yes | Yes | Yes | Yes | Yes | Yes |

**Data Visibility:**

- InternalOnly in MVP (no external broker / portal access — deferred to F0029).
- ABAC enforced via Casbin on resource `account` and actions `read`, `create`, `update`, `deactivate`, `reactivate`, `delete`, `merge`, `contact:manage`.
- Scope predicates reuse existing region / broker / territory patterns from F0002 and F0007.

## Non-Functional Expectations

- **Performance:**
  - Account list p95 ≤ 300 ms for up to 10 000 accounts with filter + pagination
  - Account 360 overview render p95 ≤ 500 ms; related-list rails paginate independently and lazy-load (no N+1)
  - Merge synchronous commit p95 ≤ 2 s for accounts with ≤ 500 linked records (submissions + policies + renewals + timeline); async / Temporal-backed merge for larger accounts deferred to a Future follow-up
- **Security:**
  - ABAC enforcement on every endpoint; `account:*` Casbin rules added
  - Audit trail on every profile change and lifecycle transition
  - Tax id / contact data classified as sensitive business data; no PII beyond business-context contact fields in MVP
- **Reliability:**
  - Dependent list / detail views MUST NOT fail when a joined account is `Merged` or `Deleted` (fallback contract)
  - Optimistic concurrency on all mutations via `RowVersion` / `If-Match`
  - Merge and delete actions idempotent on retry (same-input same-output; no duplicate timeline events)
- **Indexing:**
  - `Accounts(TaxId)` unique filtered where `Status='Active'` (duplicate detection)
  - `Accounts(Status, Region)`, `Accounts(BrokerOfRecordId)`, `Accounts(TerritoryCode)` for list filters
  - `AccountContacts(AccountId, IsPrimary)` for Account 360 overview

## Success Criteria

- Users reach a complete insured context from one workspace in ≤ 3 clicks
- Zero broken dependent renders after account deactivation, merge, or delete in integration tests
- Duplicate accounts reduced via search-before-create and duplicate hints
- Every lifecycle transition is auditable from Account 360 timeline
- Account 360 becomes the navigation anchor for submissions, policies, renewals, and contacts

## Risks & Assumptions

- **Risk:** Account scope becomes a dumping ground for unrelated features.
  - **Mitigation:** Keep F0016 focused on master record, relationships, 360 composition, lifecycle, and fallback contract. Out-of-scope capabilities are listed explicitly above.
- **Risk:** Account lifecycle actions break dependent views if read models assume the account is always live.
  - **Mitigation:** Denormalize stable account display name and status at link time on submissions / renewals / policies; enforce the fallback contract in integration tests on every dependent feature.
- **Risk:** Merge authority and history are ambiguous.
  - **Mitigation:** Merge restricted to Distribution Manager + Admin; both source and survivor get timeline entries; `MergedIntoAccountId` preserved permanently; unmerge is an explicit Future scope item.
- **Assumption:** Policy stub landed by F0007 is sufficient to render Account 360 policy counts and list in MVP; F0018 will extend it without rewriting.
- **Assumption:** F0002 Broker entity is already available and stable (archived / done).
- **Assumption:** Territory is a flat string code in MVP; F0017 hierarchy / rule engine is Future.
- **Assumption:** Synchronous merge is acceptable for MVP volumes; Temporal-backed async merge is a Future follow-up.

## Dependencies

- **F0002 Broker & MGA Relationship Management** — Broker entity; broker-of-record assignment reads from F0002. (Done / archived.)
- **F0006 Submission Intake Workflow** — Dependent module; must adopt the fallback contract. (Done / archived; follow-up change in this feature will harden denormalized columns.)
- **F0007 Renewal Pipeline** — Dependent module; must adopt the fallback contract. (Done / archived.)
- **F0018 Policy Lifecycle & Policy 360** — Soft dependency; Account 360 reads the Policy stub today and will consume F0018 when it lands. Not a blocking dependency for F0016 MVP.
- **F0020 Document Management & ACORD Intake** — Soft dependency; Account 360 shows a Documents rail via F0020 when F0020 is available; MVP renders an empty-state placeholder if F0020 is not yet live.

## Architecture & Solution Design

### Solution Components

- Introduce `Account` as a first-class aggregate with dedicated endpoints for list, detail, profile mutation, lifecycle transitions, merge, and contact CRUD.
- Introduce an Account 360 composition service that reads denormalized summary fields + paginated related-record endpoints; it does not own the underlying records.
- Introduce a **Tombstone & Fallback** pattern in the shared architecture layer (see ADR proposal below) that governs how dependent modules render deleted / merged accounts.
- Reuse existing ABAC / region / territory scoping primitives from F0002 and F0007; extend Casbin policy with `account:*` actions.
- Reuse the existing `WorkflowTransition` + `ActivityTimelineEvent` pattern (ADR-011) for lifecycle auditing.

### Data & Workflow Design

- Explicit state machine for account lifecycle with role-gated transitions and append-only history.
- Denormalize `AccountId`, `AccountDisplayNameAtLink`, `AccountStatusAtRead` projections into dependent list endpoints (submissions, renewals, policies) rather than making them each re-join.
- Summary projection (active policy count, open submission count, renewal-due count, last activity date) materialized via a query-time composition in MVP; a dedicated materialized projection is a Future follow-up if it becomes a hotspot.
- Contacts owned as a lightweight child collection; generalization to a cross-feature Contact module is Future.

### API & Integration Design

- REST endpoints under `/api/accounts/**` for list, detail, create, profile update, lifecycle transitions, merge, and contacts.
- Account 360 composed endpoint (`/api/accounts/{id}/summary`) returns overview metrics + summary counts in a single call; paginated related-list endpoints (`/api/accounts/{id}/submissions`, `/policies`, `/renewals`, `/contacts`, `/timeline`) handle the rails.
- Tombstone responses follow the fallback contract above (`410 Gone` for deleted, `200 status=Merged` with `survivorAccountId` for merged).
- All write endpoints use `If-Match` with `RowVersion` for optimistic concurrency.

### Security & Operational Considerations

- Casbin policies for `account:read`, `account:create`, `account:update`, `account:deactivate`, `account:reactivate`, `account:delete`, `account:merge`, `account:contact:manage`.
- Region / territory / broker scoping identical in shape to F0002 and F0007 policies.
- All merge and delete actions require Distribution Manager or Admin role plus scope match.
- All mutations emit timeline events; merges emit events on both source and survivor accounts.
- Indexes called out in NFR section must ship in the F0016 migration.
- Dependent-module denormalization is a one-time additive migration on submissions / renewals / policies (nullable → backfill → required).

## Architecture Traceability

**Taxonomy Reference:** [Feature Architecture Traceability Taxonomy](../../architecture/feature-architecture-traceability-taxonomy.md)

| Classification | Artifact / Decision | ADR |
|----------------|---------------------|-----|
| Introduces: Feature-Local Component | Account aggregate, Account 360 composition service, account summary projection, account contacts | PRD only |
| Introduces / Standardizes: Cross-Cutting Pattern | Account Merge & Tombstone-Forward semantics, including the dependent-view fallback contract | [ADR-017](../../architecture/decisions/ADR-017-account-merge-tombstone-and-fallback-contract.md) — Account merge, tombstone semantics, and dependent-view fallback contract |
| Reuses: Established Cross-Cutting Pattern | Workflow state machine + append-only transition / timeline history | [ADR-011](../../architecture/decisions/ADR-011-crm-workflow-state-machines-and-transition-history.md) |
| Reuses: Established Cross-Cutting Pattern | LOB classification on Account where applicable | [ADR-009](../../architecture/decisions/ADR-009-lob-classification-and-sla-configuration.md) |
| Integrates With: Planned Cross-Cutting Component | Documents rail on Account 360 via shared document subsystem | [ADR-012](../../architecture/decisions/ADR-012-shared-document-storage-and-metadata-architecture.md) |

## Related User Stories

Stories are colocated in this feature folder as `F0016-S{NNNN}-{slug}.md`.

- [F0016-S0001](./F0016-S0001-account-list-with-search-and-filtering.md) — Account list with search and filtering
- [F0016-S0002](./F0016-S0002-create-account.md) — Create account (manual and from submission / policy)
- [F0016-S0003](./F0016-S0003-account-detail-and-profile-edit.md) — Account detail and profile edit
- [F0016-S0004](./F0016-S0004-account-360-composition.md) — Account 360 composed workspace
- [F0016-S0005](./F0016-S0005-account-contacts-management.md) — Account-scoped contacts CRUD
- [F0016-S0006](./F0016-S0006-account-relationships-broker-producer-territory.md) — Account relationships (broker of record, producer, territory)
- [F0016-S0007](./F0016-S0007-account-lifecycle-deactivate-reactivate-delete.md) — Account lifecycle: deactivate, reactivate, delete
- [F0016-S0008](./F0016-S0008-account-merge-and-duplicate-handling.md) — Account merge and duplicate handling
- [F0016-S0009](./F0016-S0009-deleted-merged-account-fallback-contract.md) — Deleted / merged account fallback contract for dependent views
- [F0016-S0010](./F0016-S0010-account-activity-timeline-and-audit.md) — Account activity timeline and audit trail
- [F0016-S0011](./F0016-S0011-account-summary-projection.md) — Account summary projection (policy / submission / renewal counts, last activity)
