---
template: feature-prd
version: 2.0
applies_to: product-manager
---

# F0018: Policy Lifecycle & Policy 360

**Feature ID:** F0018
**Feature Name:** Policy Lifecycle & Policy 360
**Priority:** Critical
**Phase:** CRM Release MVP
**Status:** In Refinement

## Feature Statement

**As an** underwriter, distribution user, distribution manager, or relationship manager
**I want** a first-class Policy record with a dedicated Policy 360 view, explicit lifecycle states (including endorsements, versions, cancellation, and reinstatement), structured coverage lines, and stable linkage to accounts, submissions, and renewals
**So that** Nebula can act as a commercial P&C system of record, underwriters can see current and historical policy truth in one workspace, and downstream renewals, servicing, and reporting always have a reliable policy anchor

## Business Objective

- **Goal:** Make Policy a first-class insurance record in Nebula so the platform is credible as a commercial P&C CRM and system of record.
- **Metric:** Policy records created and linked to accounts (target: 100% of bound submissions produce a policy); time-to-context on a policy (target: users reach full policy history in ≤ 3 clicks); zero orphaned renewals (every renewal has a resolvable policy); every lifecycle event has an audit entry.
- **Baseline:** Policy information is implicit and stub-only. F0007 landed a minimal Policy FK for renewals, but there is no policy entity, no lifecycle workflow, no version history, no coverage structure, and no Policy 360 view.
- **Target:** Users manage and navigate full policy context directly in Nebula: create, issue, endorse, cancel, reinstate, and expire policies with complete history and first-class 360 composition.

## Problem Statement

- **Current State:** Nebula has no authoritative policy record. Renewals reference a `PolicyId` but there is no entity behind it, no coverage detail, no version history, no lifecycle rules, and no workspace. Underwriters cannot see current terms, prior versions, or cancellation context. Account 360 renders a placeholder policies rail against a non-existent entity. Downstream billing, servicing, and reporting have no policy to attach to.
- **Desired State:** Policies exist as first-class records with structured coverage lines, versioned immutable snapshots, lifecycle transitions (`Pending → Issued → Expired/Cancelled` with `Cancelled → Issued` reinstatement), endorsement events, renewal linkage (successor/predecessor), append-only timeline, and a composed Policy 360 view. Every mutation is audited; every dependent view has a stable policy reference.
- **Impact:** Underwriters make informed decisions with full policy history; distribution users see accurate renewal context; managers audit policy actions; Account 360 and Renewal views render real policy data; the platform is credible as a commercial P&C CRM.

## Scope & Boundaries

**In Scope (MVP):**
- `Policy` aggregate with first-class profile, identifiers, lifecycle state, and audit fields
- Globally unique, human-readable `PolicyNumber` (system-generated with deterministic format; user-provided allowed during import with conflict detection)
- Structured `PolicyCoverageLine` child records (LOB-aware; limits, deductibles, sub-limits, coverage form references)
- Immutable `PolicyVersion` snapshots (capture full issued-state terms + coverages at each lifecycle change)
- `PolicyEndorsement` events (linked child records describing the change that produced a new version)
- Policy lifecycle state machine: `Pending → Issued → Expired` (terminal), `Issued → Cancelled` (terminal-for-writes unless reinstated), `Cancelled → Issued` (reinstate within LOB-configurable window)
- Policy list view with search, filter (status, carrier, LOB, account, broker, expiration window), sort, pagination
- Policy detail (profile) view with inline edit on editable attributes and optimistic concurrency
- Policy 360 composed workspace with rails for: versions, endorsements, coverages (current), renewals (predecessor + successor), documents (empty-state placeholder until F0020 lands), activity timeline, summary metrics
- Policy creation paths (MVP): manual create from UI; import-lite path for existing-book records (CSV or manual bulk with validation). F0019 "bind-request → policy" handoff hook is specified but implementation deferred until F0019 lands.
- Lifecycle actions: issue (Pending→Issued), endorse (Issued→Issued producing a new version), cancel (Issued→Cancelled with reasonCode), reinstate (Cancelled→Issued within allowed window), expire (automatic on or after `ExpirationDate` when not already Cancelled)
- Renewal linkage: a policy may reference a `PredecessorPolicyId` (what this renewed from) and expose a `SuccessorPolicyId` computed from F0007 renewal completion
- Carrier handling: nullable `CarrierName` string plus optional FK to a lightweight `CarrierRef` lookup table (seeded; replaced by F0028 when it lands)
- Account relationship: every policy has a required `AccountId`; ABAC scope flows from Account (broker-of-record, territory, region)
- Append-only activity timeline for every policy mutation and lifecycle transition
- Policy summary projection exposing active/expired/cancelled counts, next-expiring date, total current premium for Account 360
- Dependent-view fallback contract support: policies denormalize `AccountDisplayNameAtLink` and honor the F0016 tombstone-forward contract
- ABAC enforcement: policy data is sensitive; enforce by broker-of-record, territory, region, and assigned role scope

**Out of Scope (Future):**
- Full claims handling and claim payment tracking (deferred to F0024)
- Billing, invoicing, premium collection, commission settlement (deferred to F0025, F0026)
- External carrier system synchronization and policy feeds (deferred to F0030)
- Outbound policy document generation (ACORD forms, COIs, declarations pages) (deferred to F0027)
- Automated renewal creation from expiring policies via Temporal scheduling (F0007 MVP is manual)
- Mid-term rate recalculation engine (MVP stores premium as an attribute; no rating engine)
- Policy comparison / redline across versions beyond side-by-side display (MVP displays two versions; diff tooling is a follow-up)
- Unreinstatable recovery (after reinstatement window has lapsed, a new policy must be created; no admin backdoor in MVP)
- Carrier master data management and carrier performance analytics (deferred to F0028)
- Policy-level custom forms / free-text addenda editor (MVP uses coverage form references only)
- Structured coverage forms library (MVP captures coverage form *references* by code; F0027 owns the forms library)

## Scope Boundary Clarifications

| Capability | Owns | Delegates To | Boundary |
|------------|------|--------------|----------|
| Policy entity, profile, lifecycle, versions, endorsements, coverages | **F0018** | — | F0018 owns the Policy aggregate |
| Policy 360 composition | **F0018** | — | Reads from own aggregate + dependent reads from F0016, F0007, F0020 |
| Structured coverage line items | **F0018** | — | F0018 owns `PolicyCoverageLine`; F0027 owns outbound coverage *forms* generation |
| Policy number uniqueness and format | **F0018** | — | Nebula-global uniqueness; friendly format; not a GUID |
| Carrier reference data | **F0018** (lightweight seed) | **F0028** | F0018 ships `CarrierRef` seed; F0028 replaces with full carrier master |
| Policy documents (attachments) | — | **F0020** | F0018 renders a documents rail via F0020 when F0020 is live; empty-state placeholder otherwise |
| Account context on policy (name, status, ABAC scope) | — | **F0016** | F0018 reads denormalized stable-display fields per F0016 fallback contract |
| Broker-of-record on policy | — | **F0002** / **F0016** | Policy reads broker via Account; does not store an independent broker link |
| Submission → Policy handoff | — | **F0019** | F0018 specifies the `POST /api/policies/from-bind` create-from-bind hook; F0019 calls it at bind time (deferred until F0019 ships) |
| Renewal lifecycle and renewal-specific states | — | **F0007** | F0018 exposes `SuccessorPolicyId` via the renewal linkage; F0007 owns Renewal entity and workflow |
| Policy lifecycle events consumed downstream | **F0018** (emit) | **F0007, F0019, future F0024/F0025** | F0018 emits domain events; consumers subscribe |
| Policy outbound document generation | — | **F0027** | F0018 provides data bindings; F0027 owns templates and rendering |
| Automated policy expiration scheduling | **F0018** (MVP: nightly job) | **F0007** (Temporal for renewal reminders) | MVP uses a simple date-based expiration job; full Temporal durable workflow deferred to follow-up |

## Personas

### Underwriter (Primary)

- **Policy job-to-be-done:** Review bound policies, understand current terms, endorse mid-term changes, decline or cancel coverage with proper reasoning, and rely on version history when questions arise about prior terms.
- **Key pain:** Prior terms are untraceable; endorsement history is implicit; cancellations lack audit; renewal decisions are made without stable policy context.
- **Success:** Reaches complete policy context (current + historical) in one workspace; can endorse, cancel, reinstate with confidence; every change is captured in version history.

### Distribution User (Primary)

- **Policy job-to-be-done:** See policies on their accounts, track renewal-eligible policies, surface policy context during broker interactions, and link submissions to issued policies.
- **Key pain:** Cannot tell a broker "here is the policy that's expiring"; cannot confirm what coverages are in force; relies on external systems for policy detail.
- **Success:** Policy 360 shows current terms; expiration windows are visible; renewal handoff to underwriting references a real policy.

### Distribution Manager (Primary)

- **Policy job-to-be-done:** Oversee the book of business: which policies are in force, expiring, cancelled; audit lifecycle actions; reassign responsibility when account ownership changes.
- **Key pain:** No book-level policy view; no audit of who cancelled what and why; cancellations surface only via revenue reports.
- **Success:** Can list, filter, and audit all policies in territory; every lifecycle transition is traceable to an actor and reason; cancellations/reinstatements are governed.

### Relationship Manager (Secondary)

- **Policy job-to-be-done:** Read-only visibility into policies tied to managed broker relationships; surface policy context during broker reviews.
- **Key pain:** Cannot confirm what a broker's book currently looks like inside Nebula.
- **Success:** Read-only Policy 360 and list filtered by managed broker scope.

### Admin

- **Policy job-to-be-done:** Maintain carrier seed data, review audit trails, handle exceptional reinstatement requests within window.
- **Success:** Policy data quality maintained; audit trail complete; no out-of-policy admin backdoors (reinstatement window enforced; no post-window override in MVP).

## Policy Lifecycle Workflow

### States

| State | Description | Owner Role | Entry Condition |
|-------|-------------|------------|-----------------|
| **Pending** | Policy has been created manually or via the F0019 bind hook but is not yet active. Coverage has not started. | Distribution User / Distribution Manager / Underwriter / Admin / System (bind-hook) | Created manually or by `POST /api/policies/from-bind` when F0019 is live |
| **Issued** | Policy is active; coverage is in force between `EffectiveDate` and `ExpirationDate`. Mutating endorsements produce new versions. | Underwriter / Admin / System (import) | `Pending → Issued` by authorized actor after required fields validated, or import-lite lands an already-bound row directly in `Issued` |
| **Expired** | Policy passed `ExpirationDate` without cancellation; terminal for writes except history reads. | — (terminal) | Automatic on or after `ExpirationDate` for policies in `Issued` |
| **Cancelled** | Policy cancelled mid-term (flat or pro-rata); terminal for writes unless reinstated within window. | Underwriter / Distribution Manager | `Issued → Cancelled` with `CancellationReasonCode`, `CancellationEffectiveDate` |

### Allowed Transitions

```
Pending   → Issued      (issue; requires complete coverages, effective/expiration dates, premium)
Issued    → Issued      (endorse; produces a new PolicyVersion; no state change)
Issued    → Expired     (automatic on or after ExpirationDate; no manual transition allowed unless Cancelled)
Issued    → Cancelled   (cancel; requires reasonCode and effectiveDate)
Cancelled → Issued      (reinstate; only within LOB-configurable reinstatement window; produces a new PolicyVersion)
```

### Transition Rules

- Invalid transition pairs return HTTP 409 `ProblemDetails` (`code=invalid_transition`).
- `Pending → Issued`: Actor must be Underwriter or Admin. Pre-conditions: at least one `PolicyCoverageLine`, valid `EffectiveDate` (today or future), valid `ExpirationDate` > `EffectiveDate`, non-null `TotalPremium`, non-null `LineOfBusiness`, non-null `AccountId`.
- `Issued → Issued` (endorse): Actor must be Underwriter or Admin. Requires `EndorsementReasonCode`, `EndorsementEffectiveDate`, and at least one changed field (profile, coverage, or premium). A new `PolicyVersion` snapshot is written.
- `Issued → Expired`: Automatic; emitted by a daily expiration job for every `Issued` policy whose `ExpirationDate` is in the past. Manual force-expire is not allowed (use cancel instead).
- `Issued → Cancelled`: Actor must be Underwriter, Distribution Manager, or Admin. Requires `CancellationReasonCode` (validated at API layer, not DB enum) and `CancellationEffectiveDate` (today or past; not future beyond `ExpirationDate`).
- `Cancelled → Issued` (reinstate): Actor must be Underwriter or Admin. Pre-condition: `now() ≤ CancellationEffectiveDate + LOB.reinstatementWindowDays`. Reason required. Reinstatement creates a new `PolicyVersion` and appends endorsement-style timeline entries.
- Every transition appends one `WorkflowTransition` record and one `ActivityTimelineEvent` record on the policy.
- Endorsements also emit one `PolicyEndorsement` child record.
- Transition records are immutable; corrections happen via compensating transitions (e.g., endorse-to-correct).

### Cancellation Reason Codes

Validated at API layer (not a DB enum):

- `Nonpayment` — Premium not paid within grace period
- `MaterialMisrepresentation` — Underwriting information was falsified or materially wrong
- `IncreasedHazard` — Risk profile changed materially after binding
- `InsuredRequested` — Insured voluntarily cancelled
- `CoverageReplaced` — Replaced by another policy (new carrier, new coverage)
- `NonRenewalProcessed` — Prior non-renewal notice; this is the administrative close-out
- `Other` — Free-text in `cancellationReasonDetail`

### Reinstatement Window

Configurable per Line of Business using the `WorkflowSlaThreshold` pattern from ADR-009. Default window:

| LOB | Default Reinstatement Window (days after cancellation effective) |
|-----|------------------------------------------------------------------|
| Property | 30 |
| GeneralLiability | 30 |
| WorkersCompensation | 60 |
| ProfessionalLiability | 30 |
| Cyber | 15 |
| Default (all others) | 30 |

After the window lapses, a new policy must be created; no admin override in MVP.

## Acceptance Criteria Overview

- [ ] Users can list, search, and filter policies by status, carrier, LOB, account, broker, effective date, expiration date, and expiration window
- [ ] Users can create a policy manually and via import-lite; F0019 bind hook is specified but not wired until F0019 ships
- [ ] Every policy has a globally unique, human-readable `PolicyNumber`; import-time conflicts are detected and blocked
- [ ] Users can view and inline-edit the policy profile with optimistic concurrency (`If-Match` / `RowVersion`)
- [ ] Policy 360 renders overview + paginated related lists for versions, endorsements, coverages (current), renewals (predecessor + successor), documents (via F0020 or empty-state), activity timeline
- [ ] Users can manage structured coverage lines (add / update / remove) during edit and endorsement; LOB-aware validation applies
- [ ] Every lifecycle transition (issue, endorse, cancel, reinstate, expire) follows the state machine with role gates and required fields
- [ ] Every endorsement produces an immutable `PolicyVersion` snapshot capturing the complete issued state (profile + coverages + premium)
- [ ] Cancellations require reason code + effective date; reinstatements are window-enforced per LOB
- [ ] Policy 360 for Expired/Cancelled policies remains readable (history view) and correctly surfaces terminal status badges
- [ ] Renewal linkage exposes `PredecessorPolicyId` (set at create time) and `SuccessorPolicyId` (derived from F0007 renewal completion)
- [ ] All mutations append to the policy activity timeline
- [ ] Policy summary projection exposes active/expired/cancelled counts, next-expiring date, and total current premium for Account 360
- [ ] ABAC enforcement: users only see policies within their broker / territory / region scope; policy data is sensitive and role-gated
- [ ] Dependent views (Account 360, Renewal detail) render stable references when a policy is Expired or Cancelled (no broken list items)

## UX / Screens

| Screen | Purpose | Key Actions |
|--------|---------|-------------|
| Policy List | Primary policy operating surface | Search by policy number / account name / legal name; filter by status, carrier, LOB, account, broker, expiration window (90/60/45/expired); sort by expiration / policy number / premium; paginate (25/page) |
| Policy Create / Edit | Create or edit a policy | Enter profile, coverage lines, carrier, dates, premium; save with optimistic concurrency; import-lite accepts a CSV with per-row validation errors |
| Policy Detail (Profile) | Full profile view | Inline edit profile and coverages; issue; endorse; cancel; reinstate (within window); view versions |
| Policy 360 | Composed policy workspace | Overview metrics + tabs / rails for Versions, Endorsements, Coverages, Renewals (predecessor + successor), Documents (via F0020 when live; empty-state otherwise), Activity |
| Policy Version Compare | Side-by-side display of two versions | Pick two versions; display field-by-field with changed fields highlighted (no diff tooling in MVP) |
| Policy Cancel Flow | Cancel an issued policy | Pick reason code; enter effective date and detail; confirm; see timeline entries on source |
| Policy Reinstate Flow | Reinstate a cancelled policy within window | Show window deadline; enter reason; confirm; new version written |
| Expired / Cancelled Header | History-only view | Read-only header + status badge; all rails remain browseable for audit |

**Key Workflows:**

1. **Manual Policy Creation Flow** — Distribution user opens an Account 360 → clicks "Create Policy" → enters policy number (auto-suggested) / carrier / LOB / dates / coverages / premium → saves in `Pending` → underwriter opens the policy → reviews → transitions to `Issued` → Account 360 policies rail now shows the new policy.

2. **Import-Lite Bulk Policy Load** — Admin opens Policy Import → uploads CSV with existing book policies → system validates each row (unique policy number, account match, coverage structure, date ranges) → error report returned for failed rows → successful rows land in `Issued` directly with `ImportSource` attribution → all imported policies have a `PolicyVersion` snapshot reflecting their initial state.

3. **Endorsement Flow** — Underwriter opens an Issued policy → clicks "Endorse" → changes coverage limit on a line → selects reason code and endorsement effective date → confirms → new `PolicyVersion` snapshot written → new `PolicyEndorsement` event created → timeline entry added → Policy 360 versions rail now shows 2 versions.

4. **Cancellation Flow** — Distribution manager opens an Issued policy → clicks "Cancel" → selects reason code (`InsuredRequested`) and cancellation effective date → confirms → policy moves to `Cancelled` → reinstatement window deadline is now surfaced in header → dependent renewals show the cancelled state correctly.

5. **Reinstatement Flow** — Underwriter sees a Cancelled policy whose insured requested reinstatement within the 15-day Cyber window → opens the policy → sees "Reinstate (within 11 days)" affordance → clicks → enters reason → confirms → policy moves back to `Issued` → new `PolicyVersion` snapshot records the reinstatement.

6. **Expiration Flow** — Daily expiration job runs at 00:15 UTC → scans `Issued` policies with `ExpirationDate < today` → transitions each to `Expired` → writes a `WorkflowTransition` and `ActivityTimelineEvent` per policy → no manual intervention required.

7. **Policy 360 Navigation Flow** — Underwriter opens Account 360 → clicks a policy in the policies rail → lands on Policy 360 → reviews current coverages → clicks Versions tab to see historical terms → clicks Renewals tab to see the successor renewal currently `InReview` → returns to Account 360.

## Data Requirements

**Core Entity: Policy**

- `Id` (uuid, PK) — Stable identifier, never reused
- `PolicyNumber` (string, unique, NOT NULL) — Globally unique human-readable identifier (format: `NEB-{LOB-prefix}-{yyyy}-{6-digit-sequence}`; user-provided allowed on import)
- `AccountId` (uuid, FK → Account, NOT NULL) — The insured account
- `CarrierRefId` (uuid, FK → CarrierRef, nullable) — Structured carrier link (preferred)
- `CarrierName` (string, nullable) — Free-text carrier name for import-lite records where `CarrierRefId` is not yet mapped
- `LineOfBusiness` (string, NOT NULL) — LOB classification (per ADR-009 LOB set)
- `Status` (enum: `Pending`, `Issued`, `Expired`, `Cancelled`)
- `EffectiveDate` (date, NOT NULL) — Policy start date
- `ExpirationDate` (date, NOT NULL) — Policy end date (must be > `EffectiveDate`)
- `IssuedAt` (timestamp, nullable) — Set when transitioning `Pending → Issued`
- `CancelledAt` (timestamp, nullable) — Set when transitioning `Issued → Cancelled`
- `CancellationEffectiveDate` (date, nullable) — When cancellation takes effect
- `CancellationReasonCode` (string, nullable) — Required when `Status = Cancelled`
- `CancellationReasonDetail` (string, nullable) — Free-text when code = `Other`
- `ReinstatementDeadline` (date, nullable) — Computed `CancellationEffectiveDate + LOB.reinstatementWindowDays` when cancelled
- `TotalPremium` (decimal(18,2), nullable) — Total policy premium; set at issue; updated via endorsement
- `PremiumCurrency` (string(3), default 'USD') — ISO 4217 code
- `PredecessorPolicyId` (uuid, FK → Policy, nullable) — Policy this record renewed from (set on create for renewal-continuity)
- `CurrentVersionId` (uuid, FK → PolicyVersion, nullable) — Points at the version reflecting current terms; null until first issue
- `ImportSource` (string, nullable) — Set when created via import (`csv-import`, `f0019-bind-hook`, `manual`)
- `AccountDisplayNameAtLink` (string) — Snapshot for fallback rendering per F0016 contract
- `CreatedAt`, `CreatedByUserId`, `UpdatedAt`, `UpdatedByUserId`, `RowVersion`, `IsDeleted` — Standard audit fields

**Entity: PolicyVersion** (append-only immutable snapshot)

- `Id` (uuid, PK)
- `PolicyId` (uuid, FK → Policy, NOT NULL)
- `VersionNumber` (int, NOT NULL) — Monotonically increasing within a policy, starting at 1
- `VersionReason` (string) — `IssuedInitial`, `Endorsement`, `Reinstatement`
- `EffectiveDate` (date) — Version effective date (typically policy `EffectiveDate` for v1, endorsement effective date thereafter)
- `ProfileSnapshot` (jsonb) — Full profile fields captured at version time (for audit replay)
- `CoverageSnapshot` (jsonb) — Complete `PolicyCoverageLine` set at version time
- `PremiumSnapshot` (decimal(18,2))
- `EndorsementId` (uuid, FK → PolicyEndorsement, nullable) — Set when `VersionReason = Endorsement`
- `CreatedAt`, `CreatedByUserId`

**Entity: PolicyEndorsement** (event record)

- `Id` (uuid, PK)
- `PolicyId` (uuid, FK → Policy, NOT NULL)
- `EndorsementNumber` (int, NOT NULL) — Monotonic within a policy
- `EndorsementReasonCode` (string, NOT NULL) — `CoverageChange`, `LimitChange`, `DeductibleChange`, `AdditionalInsured`, `PremiumAdjustment`, `AddressChange`, `NamedInsuredChange`, `Other`
- `EndorsementReasonDetail` (string, nullable) — Free-text when code = `Other`
- `EffectiveDate` (date, NOT NULL)
- `ResultingVersionId` (uuid, FK → PolicyVersion, NOT NULL)
- `CreatedAt`, `CreatedByUserId`

**Entity: PolicyCoverageLine** (structured coverage; current snapshot lives on each PolicyVersion via jsonb; also materialized for current-version query performance)

- `Id` (uuid, PK)
- `PolicyId` (uuid, FK → Policy, NOT NULL)
- `VersionId` (uuid, FK → PolicyVersion, NOT NULL) — Each version re-materializes its coverage set
- `CoverageCode` (string, NOT NULL) — LOB-scoped coverage code (e.g., `GL-PREMISES`, `PROP-BLDG`, `WC-EMPLOYERS-LIAB`)
- `CoverageFormReference` (string, nullable) — Form reference number (e.g., `CG 00 01 04 13`)
- `LimitPerOccurrence` (decimal(18,2), nullable)
- `LimitAggregate` (decimal(18,2), nullable)
- `SubLimitDescription` (string, nullable) — Free-text for sub-limit specifics (pending full structured sub-limit model, which is a follow-up)
- `Deductible` (decimal(18,2), nullable)
- `DeductibleType` (string, nullable) — `PerOccurrence`, `PerClaim`, `Aggregate`
- `Premium` (decimal(18,2), nullable) — Per-line premium; sum of these may differ from `Policy.TotalPremium` due to taxes/fees (captured separately via follow-up if needed)
- `Notes` (string, nullable) — Line-level notes
- `CreatedAt`, `CreatedByUserId`

**Entity: CarrierRef** (lightweight carrier lookup; seeded; replaced by F0028 later)

- `Id` (uuid, PK)
- `CarrierCode` (string, unique) — Short code (e.g., `TRAVELERS-US`)
- `CarrierName` (string, NOT NULL)
- `NaicGroup` (string, nullable) — NAIC group code if known
- `IsActive` (bool)
- `CreatedAt`, `UpdatedAt`

**Validation Rules:**

- `PolicyNumber` required; 6–40 chars; alphanumeric + `-/_`; globally unique (case-insensitive, trimmed); import-time duplicate returns a row-level error
- `AccountId` required and must reference an `Active` or `Inactive` account (not `Merged` or `Deleted`; if account is later merged, policy follows F0016 tombstone-forward contract at read time)
- `EffectiveDate` required; `ExpirationDate` required and must be strictly > `EffectiveDate`
- `LineOfBusiness` required; must be a known LOB from ADR-009
- At least one `PolicyCoverageLine` required before `Pending → Issued`
- `TotalPremium` required for `Pending → Issued`
- `CancellationEffectiveDate` required when `Status = Cancelled`; must be ≤ `ExpirationDate`
- `ReinstatementDeadline` auto-computed on cancel; reinstatement attempts after deadline return 409 `code=reinstatement_window_expired`
- `VersionNumber`, `EndorsementNumber` monotonic per policy; enforced by ordered insert under row lock
- `PredecessorPolicyId` (if set) must point to a policy in `Expired` or `Cancelled` state (renewal continuity semantics)
- `RowVersion`-based optimistic concurrency on all policy mutations
- `CarrierName` required when `CarrierRefId` is null and the record was created via import-lite; otherwise either may be set

**Data Relationships:**

- Policy → Account: Required N:1
- Policy → CarrierRef: Optional N:1
- Policy → PolicyVersion: 1:N (append-only; monotonic version numbers)
- Policy → PolicyEndorsement: 1:N (append-only; monotonic endorsement numbers)
- PolicyVersion → PolicyCoverageLine: 1:N (each version materializes its coverage set)
- Policy → Policy (Predecessor): Optional N:1 (self-referential; renewal continuity)
- Renewal → Policy: existing N:1 (F0007 owns; reads policy data)
- Renewal → Policy (BoundPolicy): existing N:1 (F0007-owned link from completed renewal to successor policy; exposes `SuccessorPolicyId` on the predecessor policy)
- Submission → Policy (bound): Optional N:1 via F0019 bind hook (deferred implementation)

## Role-Based Access

| Role | Read | Create | Update (profile) | Issue | Endorse | Cancel | Reinstate | Coverages CRUD |
|------|------|--------|------------------|-------|---------|--------|-----------|----------------|
| Distribution User | scope: own region + assigned broker(s) | Yes (Pending only) | Yes (Pending only) | No | No | No | No | Yes (Pending only) |
| Distribution Manager | scope: own territory | Yes (Pending only; import-lite via `policy:import`) | Yes (Pending only) | No | No | Yes | No | Yes (Pending only) |
| Underwriter | scope: own assigned book | Yes (Pending only) | Yes (Pending + non-material on Issued) | Yes | Yes | Yes | Yes | Yes (Pending; Issued via endorse) |
| Relationship Manager | read-only, own managed broker(s) | No | No | No | No | No | No | No |
| Program Manager | read-only, own program scope | No | No | No | No | No | No | No |
| Admin | All | Yes (Pending/manual; import-lite via `policy:import`) | Yes (state-allowed fields) | Yes | Yes | Yes | Yes | Yes (Pending CRUD; Issued via endorse) |

**Data Visibility:**

- InternalOnly in MVP (no external broker / portal policy access — deferred to F0029).
- ABAC enforced via Casbin on resource `policy` and actions `read`, `create`, `update`, `issue`, `endorse`, `cancel`, `reinstate`, `coverage:manage`.
- Scope predicates reuse existing region / broker / territory patterns from F0002 / F0007 / F0016.
- Policy data is classified as sensitive business data. Audit logging on every read of a `Cancelled` policy (fraud-risk pattern) is deferred to a follow-up.

## Non-Functional Expectations

- **Performance:**
  - Policy list p95 ≤ 300 ms for up to 50 000 policies with filter + pagination
  - Policy 360 overview render p95 ≤ 500 ms; related-list rails paginate independently and lazy-load (no N+1)
  - Endorsement commit p95 ≤ 1 s for policies with ≤ 50 coverage lines
  - Daily expiration job completes within 10 minutes for 100 000 Issued policies
- **Security:**
  - ABAC enforcement on every endpoint; `policy:*` Casbin rules added
  - Audit trail on every profile change, lifecycle transition, and endorsement
  - Premium and carrier data classified as sensitive; no PII beyond business-context in MVP
  - Reinstatement window enforced server-side; client-side deadline display is advisory only
- **Reliability:**
  - Dependent list / detail views MUST NOT fail when a referenced account is `Merged` or `Deleted` (F0016 fallback contract applies)
  - Optimistic concurrency on all mutations via `RowVersion` / `If-Match`
  - Issue, endorse, cancel, reinstate actions idempotent on retry (same-input same-output; no duplicate timeline events or versions)
  - Daily expiration job idempotent; re-running the same day produces no duplicate `Expired` transitions
- **Indexing:**
  - `Policies(PolicyNumber)` unique (case-insensitive)
  - `Policies(AccountId, Status)`, `Policies(ExpirationDate, Status)`, `Policies(Status, LineOfBusiness)` for list filters
  - `Policies(CarrierRefId, Status)` for carrier-filter queries
  - `PolicyVersions(PolicyId, VersionNumber DESC)` for version history
  - `PolicyEndorsements(PolicyId, EndorsementNumber DESC)` for endorsement history
  - `PolicyCoverageLines(VersionId)` for coverage retrieval by version
  - `Policies(PredecessorPolicyId)` partial index for renewal-successor lookup

## Success Criteria

- Every bound submission in Nebula produces a policy record (target: 100% coverage as F0019 lands)
- Users reach complete policy context (current + history) from Account 360 in ≤ 3 clicks
- Zero broken renders on dependent views (Account 360, Renewal detail) when a policy is Expired or Cancelled
- Every lifecycle transition is auditable from Policy 360 timeline
- Policy 360 becomes the canonical policy workspace; no user relies on external systems for current term data inside Nebula
- Import-lite path successfully loads an existing book of at least 1 000 policies with < 1% error rate (validation failure is not an error; data-quality issues in source are)

## Risks & Assumptions

- **Risk:** Scope creeps into full AMS-grade servicing (billing, claims, carrier sync).
  - **Mitigation:** Scope boundary table above is explicit; deferred capabilities are named with their destination feature ID.
- **Risk:** Structured coverage modeling becomes a time sink.
  - **Mitigation:** MVP models fields per coverage line but does not ship a coverage *forms library*; coverage form references are captured by code only. Full forms library is F0027 scope.
- **Risk:** Policy number format becomes a migration headache.
  - **Mitigation:** System-generated format is `NEB-{LOB-prefix}-{yyyy}-{6-digit-sequence}`; import-lite accepts arbitrary carrier-provided numbers with conflict detection; both coexist under one unique constraint.
- **Risk:** Expiration job misses policies due to time-zone drift.
  - **Mitigation:** Store dates as `date` (not `timestamp`); run at 00:15 UTC and compare `ExpirationDate < today_utc()`. Re-runs are idempotent.
- **Risk:** Reinstatement outside window creates support escalation.
  - **Mitigation:** No admin backdoor in MVP; product decision is that a new policy must be created. Documented in FAQ.
- **Risk:** F0019 is not yet implemented; `Pending` state could stagnate.
  - **Mitigation:** Manual `Pending → Issued` transition is the MVP path; import-lite records can land directly in `Issued`; `Pending` is the expected state for F0019-originated policies once F0019 ships.
- **Risk:** Dependent views (Account 360, Renewal detail) don't adopt the policy fallback pattern.
  - **Mitigation:** Denormalize `AccountDisplayNameAtLink` on policy records; dependent views consume it; regression tests at F0016 / F0007 levels.
- **Assumption:** F0016 Account 360 fallback contract is already in place and dependable (archived / done).
- **Assumption:** F0007 Renewal Pipeline is already in place and consumes `PolicyId` (archived / done); `BoundPolicyId` on Renewal will now reference a real Policy row after F0018 ships.
- **Assumption:** F0020 will ship during this release; Policy 360 documents rail renders an empty-state placeholder in the interim.
- **Assumption:** Single-carrier policies only (MVP). Layered / shared-limit programs are deferred.
- **Assumption:** `WorkflowSlaThreshold` pattern from ADR-009 is extensible to add a `PolicyReinstatementWindow` category; PM/Architect to confirm during Phase B.

## Dependencies

- **F0016 Account 360 & Insured Management** — Provides Account entity, fallback contract, and ABAC scoping primitives. (Done / archived 2026-04-14.)
- **F0007 Renewal Pipeline** — Consumer; currently carries the Policy stub. After F0018 ships, renewals reference real policies and `BoundPolicyId` on completed renewals feeds `SuccessorPolicyId` on predecessor policies. (Done / archived 2026-04-12.)
- **F0002 Broker & MGA Relationship Management** — Provides broker entity and broker-of-record pattern reused via Account. (Done / archived.)
- **F0009 Authentication + Role-Based Login** — Provides role/identity primitives used by `policy:*` Casbin rules. (Done / archived.)
- **F0020 Document Management & ACORD Intake** — Policy 360 documents rail integrates with F0020 when F0020 is live; empty-state placeholder otherwise. (Planned; CRM Release MVP Now.)
- **F0019 Submission Quoting, Proposal & Approval Workflow** — Future consumer; will call `POST /api/policies/from-bind` to create `Pending` policies. F0018 specifies this contract; implementation of the call site is F0019's scope. (Planned.)
- **F0028 Carrier & Market Relationship Management** — Future owner of carrier master; F0018 ships a lightweight `CarrierRef` seed which F0028 will replace. (Planned, MVP+.)

## Architecture & Solution Design

### Solution Components

- Introduce `Policy` as a first-class aggregate with dedicated endpoints for list, detail, profile mutation, lifecycle transitions, endorsements, cancellations, reinstatements, and coverage management.
- Introduce `PolicyVersion` (append-only immutable snapshot) and `PolicyEndorsement` (event record) as child aggregates with monotonic numbering enforced at the aggregate boundary.
- Introduce `PolicyCoverageLine` as a structured child of `PolicyVersion` so each version re-materializes its coverage set (current coverage is `PolicyCoverageLine` where `VersionId = Policy.CurrentVersionId`).
- Introduce a Policy 360 composition service that reads denormalized summary fields + paginated related-record endpoints; it does not own the underlying records. Follows the same shape as F0016's Account 360.
- Introduce a daily expiration job (scheduled; MVP uses a simple cron-style job, not Temporal) that transitions `Issued` policies to `Expired` when `ExpirationDate < today`. Temporal-backed durable workflow for expiration and reinstatement-window enforcement is a follow-up aligned with ADR-010.
- Introduce a `CarrierRef` seed table and seed script; a follow-up feature (F0028) will replace it with a full carrier master.
- Reuse existing ABAC / region / territory scoping primitives from F0002 / F0016; extend Casbin policy with `policy:*` actions.
- Reuse the existing `WorkflowTransition` + `ActivityTimelineEvent` pattern (ADR-011) for lifecycle auditing.
- Extend the `WorkflowSlaThreshold` pattern from ADR-009 with a `PolicyReinstatementWindow` category for per-LOB window configuration.

### Data & Workflow Design

- Explicit state machine for policy lifecycle with role-gated transitions and append-only history.
- Denormalize `AccountId`, `AccountDisplayNameAtLink`, and `AccountStatusAtRead` projections on the policy row so Policy 360 / list views do not need to re-join Account on every read.
- Summary projection (active/expired/cancelled counts, next-expiring date, total current premium) materialized via query-time composition in MVP; a dedicated materialized projection is a follow-up if it becomes a hotspot.
- Coverage lines owned per version; current coverage is always reachable via `Policy.CurrentVersionId → PolicyVersion.Id → PolicyCoverageLine[VersionId = …]`.
- Endorsement semantics: a single atomic write creates the `PolicyEndorsement` row, the new `PolicyVersion` row with full snapshots, the `PolicyCoverageLine` set for that version, and updates `Policy.CurrentVersionId` + `Policy.RowVersion`.

### API & Integration Design

- REST endpoints under `/api/policies/**` for list, detail, create, profile update, lifecycle transitions, endorsements, cancellations, reinstatements, and coverages.
- Policy 360 composed endpoint (`/api/policies/{id}/summary`) returns overview metrics + summary counts in a single call; paginated related-list endpoints (`/api/policies/{id}/versions`, `/endorsements`, `/coverages`, `/renewals`, `/timeline`, and a documents rail via F0020) handle the rails.
- Tombstone responses mirror F0016 patterns: `Expired` and `Cancelled` are readable (not 410); the status is surfaced in responses and UI badges handle display.
- All write endpoints use `If-Match` with `RowVersion` for optimistic concurrency.
- F0019 bind-hook contract: `POST /api/policies/from-bind` (specified but not implemented until F0019 ships) accepts a submission-bind payload and creates a `Pending` policy with predecessor linkage and initial coverage structure.
- Policy lifecycle events emitted on state changes so downstream modules (renewals, reporting, future billing) can consume without tight coupling.

### Security & Operational Considerations

- Casbin policies for `policy:read`, `policy:create`, `policy:update`, `policy:issue`, `policy:endorse`, `policy:cancel`, `policy:reinstate`, `policy:coverage:manage`, `policy:import`.
- Region / territory / broker scoping identical in shape to F0016 policies.
- Reinstatement authority limited to Underwriter + Admin; reason required; window enforced server-side.
- All mutations emit timeline events.
- Indexes called out in NFR section must ship in the F0018 migration.
- `CarrierRef` seed script delivered with the migration; idempotent upsert on `CarrierCode`.

## Architecture Traceability

**Taxonomy Reference:** [Feature Architecture Traceability Taxonomy](../../architecture/feature-architecture-traceability-taxonomy.md)

| Classification | Artifact / Decision | ADR |
|----------------|---------------------|-----|
| Introduces: Feature-Local Component | Policy aggregate, PolicyVersion / PolicyEndorsement / PolicyCoverageLine children, Policy 360 composition, daily expiration job, reinstatement window enforcement | PRD only |
| Reuses: Established Cross-Cutting Pattern | Workflow state machine + append-only transition / timeline history | [ADR-011](../../architecture/decisions/ADR-011-crm-workflow-state-machines-and-transition-history.md) |
| Reuses: Established Cross-Cutting Pattern | LOB classification and SLA configuration (reinstatement window extends the `WorkflowSlaThreshold` category set) | [ADR-009](../../architecture/decisions/ADR-009-lob-classification-and-sla-configuration.md) |
| Integrates With: Planned Cross-Cutting Component | Policy documents rail on Policy 360 via shared document subsystem | [ADR-012](../../architecture/decisions/ADR-012-shared-document-storage-and-metadata-architecture.md) |
| Integrates With: Cross-Cutting Component | Durable workflow orchestration for future expiration/reinstatement-window automation (MVP uses simple scheduled job; Temporal migration is a follow-up) | [ADR-010](../../architecture/decisions/ADR-010-temporal-durable-workflow-orchestration.md) |
| Reuses: Established Cross-Cutting Pattern | Account tombstone-forward fallback contract on dependent policy views | [ADR-017](../../architecture/decisions/ADR-017-account-merge-tombstone-and-fallback-contract.md) |

## Related User Stories

Stories are colocated in this feature folder as `F0018-S{NNNN}-{slug}.md`.

- [F0018-S0001](./F0018-S0001-policy-list-with-search-and-filtering.md) — Policy list with search and filtering
- [F0018-S0002](./F0018-S0002-create-policy.md) — Create policy (manual and import-lite; F0019 bind hook specified)
- [F0018-S0003](./F0018-S0003-policy-detail-and-profile-edit.md) — Policy detail and profile edit
- [F0018-S0004](./F0018-S0004-policy-360-composition.md) — Policy 360 composed workspace
- [F0018-S0005](./F0018-S0005-policy-version-history.md) — Immutable policy version snapshots and version history
- [F0018-S0006](./F0018-S0006-policy-endorsements.md) — Policy endorsement events and term changes
- [F0018-S0007](./F0018-S0007-policy-cancellation.md) — Policy cancellation (mid-term and flat)
- [F0018-S0008](./F0018-S0008-policy-reinstatement.md) — Policy reinstatement within LOB-configurable window
- [F0018-S0009](./F0018-S0009-policy-renewal-linkage.md) — Renewal linkage (predecessor/successor) and handoff
- [F0018-S0010](./F0018-S0010-policy-activity-timeline-and-audit.md) — Policy activity timeline and audit trail
- [F0018-S0011](./F0018-S0011-policy-summary-projection.md) — Policy summary projection for Account 360
