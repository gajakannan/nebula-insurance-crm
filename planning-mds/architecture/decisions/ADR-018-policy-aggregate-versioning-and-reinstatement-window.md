# ADR-018: Policy Aggregate, Version/Endorsement Semantics, and Reinstatement-Window Extension

**Status:** Proposed
**Date:** 2026-04-18
**Owners:** Architect
**Related Features:** F0018 (owner), F0007 (archived consumer), F0016 (archived consumer), F0019 (future consumer), F0020 (future consumer), F0028 (future consumer)
**Related ADRs:** ADR-008 (Casbin), ADR-009 (LOB/SLA), ADR-010 (Temporal), ADR-011 (Workflow State Machines), ADR-012 (Document subsystem), ADR-014-workflow-sla (per-LOB extension), ADR-017 (Account tombstone-forward)

## Context

F0018 introduces Policy as a first-class aggregate. Prior to F0018, `PolicyId` existed only as a stub referenced by F0007 Renewal; there was no Policy entity, no lifecycle workflow, no version history, no structured coverages, and no Policy 360 composition. The platform cannot be a credible commercial P&C CRM without a system-of-record policy aggregate.

Three design problems drive this ADR:

1. **Aggregate shape.** Does the Policy live as a single mutable row, or as a parent row with immutable version snapshots for each lifecycle change? How is current-state query performance balanced against audit-replay requirements?
2. **Cancellation → reinstatement window enforcement.** Cancellations must be reversible within an LOB-configurable grace period. Where does the window configuration live, how is it enforced server-side, and how does it integrate with existing ADR-009 / ADR-014-workflow-sla patterns?
3. **Lifecycle scheduling.** Expired transitions are time-driven, not actor-driven. Does this warrant a durable workflow (Temporal, per ADR-010), or is a simple scheduled sweep sufficient for MVP?

Two additional constraints are load-bearing:

- **F0016 fallback contract (ADR-017) applies.** Policies link to Account; merged/deleted accounts must not break Policy 360 or dependent views. Denormalized `accountDisplayNameAtLink` is required on the policy row.
- **F0019 bind-hook is a future consumer.** F0018 must specify the contract (`POST /api/policies/from-bind`) so F0019 can implement against it, but the call site lives in F0019. Running the contract as a plain REST endpoint (rather than a separate binding protocol) keeps the dependency one-directional.

## Decision

### 1. Aggregate Shape — Parent + Immutable Versions + Materialized Current Coverage

Policy is modeled as a **parent row with append-only immutable `PolicyVersion` snapshots** and **materialized `PolicyCoverageLine` rows per version**. The parent row carries identity, lifecycle state, denormalized fallback fields, and a `CurrentVersionId` pointer.

- **Parent row (`Policy`)** owns: identity, globally-unique `PolicyNumber`, `AccountId` + denormalized fallback (`AccountDisplayNameAtLink`, `AccountStatusAtRead`, `AccountSurvivorId`), `LineOfBusiness`, `Status`, `EffectiveDate`, `ExpirationDate`, `CancellationEffectiveDate` / `CancellationReasonCode` / `ReinstatementDeadline` (cancellation metadata; cleared on reinstate), `PredecessorPolicyId`, `CurrentVersionId`, `TotalPremium` (synced with current version snapshot), `RowVersion`, audit fields.
- **Version row (`PolicyVersion`)** is append-only, monotonically numbered per policy, and carries `ProfileSnapshot` (jsonb), `CoverageSnapshot` (jsonb), `PremiumSnapshot`, `VersionReason ∈ {IssuedInitial, Endorsement, Reinstatement}`, optional `EndorsementId` back-reference.
- **Endorsement row (`PolicyEndorsement`)** is append-only, monotonically numbered per policy, and references the `ResultingVersionId`.
- **Coverage line rows (`PolicyCoverageLine`)** are materialized per version under `VersionId`. The current coverage set is always reachable via `Policy.CurrentVersionId → PolicyCoverageLine[VersionId=…]`. The jsonb `CoverageSnapshot` on the version is the replay-authoritative copy; the rows are a read-performance optimization.
- **Carrier reference (`CarrierRef`)** is a lightweight lookup table seeded by F0018 and replaced in full by F0028. Policies may reference `CarrierRefId` (preferred) or fall back to free-text `CarrierName` (common for import-lite).

**Rationale:**
- Pure mutable model loses audit replay — cannot answer "what was in force on date X?" for endorsement disputes.
- Append-only versions without materialized current coverage forces every list query to join + deserialize jsonb; rejected for p95 budgets.
- The hybrid aligns with how insurance systems-of-record actually work (issued state + endorsement events + version snapshots) and matches ADR-011's append-only pattern already applied to account / submission / renewal workflows.

### 2. Lifecycle State Machine (workflow:policy)

```
Pending    → Issued       (issue; pre-conditions: ≥1 coverage line, valid dates, total premium, LOB)
Issued     → Issued       (endorse; produces new PolicyVersion; no state change)
Issued     → Expired      (automatic; nightly job; terminal for writes)
Issued     → Cancelled    (cancel; requires reason code + effective date; sets ReinstatementDeadline)
Cancelled  → Issued       (reinstate; window-gated; produces new PolicyVersion)
```

- `Pending → Issued`: Underwriter or Admin.
- `Issued → Issued` (endorse): Underwriter or Admin; atomic write of `PolicyEndorsement` + `PolicyVersion` + `PolicyCoverageLine[]` + `Policy.CurrentVersionId` + `Policy.RowVersion` + `WorkflowTransition` + `ActivityTimelineEvent`.
- `Issued → Expired`: Scheduled sweep (see §4).
- `Issued → Cancelled`: Underwriter, Distribution Manager, or Admin; sets `CancellationEffectiveDate`, `CancellationReasonCode`, `ReinstatementDeadline` atomically with the `WorkflowTransition`.
- `Cancelled → Issued` (reinstate): Underwriter or Admin; window-gated; produces `VersionReason=Reinstatement` snapshot. Cancellation metadata fields on the parent row are cleared (history reachable via `WorkflowTransition` + prior `PolicyVersion`).

Every transition appends exactly one `WorkflowTransition` under `WorkflowType="PolicyLifecycle"` and one `ActivityTimelineEvent` per ADR-011. No re-entry of the same state produces duplicate history (idempotent via `Idempotency-Key` at the service layer).

### 3. Reinstatement Window — Extend `WorkflowSlaThreshold` Category

Reuse the existing `WorkflowSlaThreshold` pattern (ADR-009 + ADR-014-workflow-sla) with a new category for policy reinstatement windows. No new table.

- New `WorkflowSlaThreshold` category: `PolicyReinstatementWindow`.
- Per-LOB seed rows (Property 30, GeneralLiability 30, WorkersCompensation 60, ProfessionalLiability 30, Cyber 15, Default 30) loaded by the F0018 migration seed script.
- At cancellation time, the service resolves the window from `WorkflowSlaThreshold` by `(category=PolicyReinstatementWindow, lineOfBusiness)` and persists `ReinstatementDeadline = CancellationEffectiveDate + windowDays` on the policy row.
- At reinstatement time, the window is re-verified server-side (`today ≤ ReinstatementDeadline`). The client countdown is strictly advisory.
- The window configuration is operator-editable post-go-live via F0032 (Admin Configuration Console); MVP ships with defaults.

**Alternative rejected:** a dedicated `PolicyReinstatementWindow` entity. Rejected because `WorkflowSlaThreshold` already has per-LOB shape (ADR-014-workflow-sla) and admin tooling in flight; a parallel table would fragment SLA/window configuration across two places.

### 4. Expiration Scheduling — Scheduled Job in MVP, Temporal in Follow-Up

MVP uses a **daily scheduled sweep** (cron-style at 00:15 UTC) that transitions `Issued` policies whose `ExpirationDate < today` to `Expired` with a `system` actor. The job is idempotent (re-runs same day produce no duplicate transitions).

Follow-up: migrate the sweep to a Temporal durable workflow per ADR-010 (mirror the pattern used for renewal reminders). Deferred because:
- The sweep has no per-entity timers or escalations (unlike renewal outreach which ADR-010 targets).
- A nightly job is operationally simpler and re-run cost is low.
- Temporal migration can happen without breaking the state machine or dependent contracts.

### 5. API Surface and Dependency Direction

- REST endpoints under `/api/policies/**` owned by F0018.
- `POST /api/policies/from-bind` is specified here and may return `501 Not Implemented` until F0019 ships; when implemented it creates a `Pending` policy and leaves `Pending → Issued` as an explicit downstream lifecycle action. F0019 implements the call site. This keeps the dependency F0019 → F0018, never reverse.
- `GET /api/accounts/{id}/policies/summary` is owned by F0018 and composed into the account-360 summary endpoint owned by F0016.
- All write endpoints use `If-Match` with `RowVersion` for optimistic concurrency.
- All mutating endpoints accept `Idempotency-Key` and return the prior result on retry.
- Cancelled and Expired policies remain readable (`200`), never `410`. Status is surfaced in the payload; UI renders the badge. (Contrast with `GET /api/accounts/{id}` returning `410` for `Deleted` accounts — policies are lifecycle entities whose terminal states are part of the audit view, not tombstones.)

### 6. Authorization (`policy:*` Casbin rules)

New Casbin actions on resource `policy`:

- `policy:read` — scoped by account ABAC (broker-of-record, territory, region, own book).
- `policy:create` — Distribution User, Distribution Manager, Underwriter, Admin for manual / bind-hook paths that create `Pending` policies. Import-lite is separately gated by `policy:import`.
- `policy:update` — Distribution User, Distribution Manager, Underwriter, Admin; state-aware. Pending allows profile edits, Issued restricts edits to non-material fields (notes only) for Underwriter/Admin, and Cancelled/Expired are read-only.
- `policy:issue` — Underwriter, Admin.
- `policy:endorse` — Underwriter, Admin.
- `policy:cancel` — Distribution Manager, Underwriter, Admin.
- `policy:reinstate` — Underwriter, Admin.
- `policy:coverage:manage` — Distribution User, Distribution Manager, Underwriter, Admin; aligned with state-aware profile edit (Pending CRUD; Issued via endorsement path only).
- `policy:import` — Distribution Manager, Admin.

Scope predicates reuse existing region / broker / territory patterns from F0002 / F0016.

### 7. Account Fallback Contract Adoption (ADR-017)

- Every policy row denormalizes `AccountDisplayNameAtLink`, `AccountStatusAtRead`, and `AccountSurvivorId` populated at create time and refreshed opportunistically while the account is `Active`/`Inactive`.
- Policy list and Policy 360 headers render from denormalized fields; no join to Account required at read time.
- `AccountId` is frozen post-create. A mistake requires a new policy; re-linking is not in MVP.
- Integration tests at F0018 level include: list load with a Merged source account (survivor label rendered); list load with a Deleted source account (tombstone rendered); Policy 360 header load for both.

## Consequences

### Positive

- **Audit-replay guarantee.** Every state at any time is reconstructable from `PolicyVersion` snapshots.
- **Current-state query performance.** `CurrentVersionId` + materialized `PolicyCoverageLine` rows keep Policy 360 overview and list reads sub-500 ms without joining jsonb.
- **Atomicity.** Endorsement transactions bundle all writes (endorsement + version + coverages + policy pointer + transition + timeline) in a single transaction; rollback leaves the aggregate consistent.
- **Reuse.** `WorkflowTransition`, `ActivityTimelineEvent`, `WorkflowSlaThreshold`, Casbin scope predicates, and the F0016 fallback contract are all reused — no new infrastructure.
- **Dependency direction is clean.** F0019 → F0018 (not reverse); F0018 → F0007 (read side only); F0018 → F0016 (fallback contract consumer); F0020 → F0018 (documents rail delegates when live); F0028 → F0018 (carrier master replaces seed).

### Negative

- **Write amplification on endorsement.** Each endorsement writes ≥ 5 rows (endorsement, version, N coverage lines, transition, timeline) plus updates the parent. Acceptable in MVP; not a hotspot for typical volumes.
- **Migration complexity.** The F0018 migration introduces 5 new tables (`Policies`, `PolicyVersions`, `PolicyEndorsements`, `PolicyCoverageLines`, `CarrierRef`) plus seed data for `WorkflowSlaThreshold` category `PolicyReinstatementWindow` and `CarrierRef`. Back-fill is not required — this is a greenfield feature; the F0007-landed `PolicyId` stub will be honored via a light reconciliation at go-live (documented separately in the feature-assembly plan).
- **Nightly expiration job is not durable.** Missed-run recovery relies on the next day's run to catch up (idempotent). Acceptable because expiration is a date-comparison, not a multi-step workflow.
- **`WorkflowSlaThreshold` category overloading.** The category set grows from aging SLAs to include reinstatement windows. This is explicit and documented here; F0032 admin UI must surface categories clearly.

### Scope of This ADR

**In scope:**
- Policy aggregate shape (Policy + PolicyVersion + PolicyEndorsement + PolicyCoverageLine + CarrierRef)
- Lifecycle state machine and transition rules
- Reinstatement window via `WorkflowSlaThreshold` category extension
- Expiration scheduling for MVP
- API surface + concurrency + idempotency contracts
- Casbin `policy:*` actions
- Account fallback contract adoption

**Out of scope:**
- Rating / pricing engine (no mid-term re-pricing; premium is user-supplied)
- Full structured sub-limit model (MVP uses `SubLimitDescription` free-text)
- Coverage forms library (F0027 owns; F0018 captures code references only)
- Policy documents generation (F0027) and carrier-facing paper (out)
- Carrier master data management (F0028)
- Post-window reinstatement override (no admin backdoor)
- Multi-currency premium handling beyond a `MIXED` marker
- Policy cloning (use predecessor linkage for renewals instead)
- Async F0019 bind-hook eventing (MVP is synchronous REST)

## Alternatives Considered

### Alternative A — Mutable single-row Policy with event-log side table

Keep a single mutable Policy row and record every change in an event log.

- **Pros:** Simpler schema; fewer tables.
- **Cons:** Replay requires reassembling state from the log for every audit read; coverage history needs its own event log or snapshot story anyway; drift between live row and log accrues over time.

Rejected because audit replay is a primary requirement, and a version-centric model matches industry expectations for a system-of-record policy.

### Alternative B — Event-sourcing the Policy aggregate

Treat the log as the source of truth; materialize current state on demand.

- **Pros:** Pure auditability; flexible projections.
- **Cons:** Operational maturity gap (no event-sourcing infrastructure in Nebula today); projections require bespoke rebuild tooling; mismatch with every other CRM aggregate that uses ADR-011's simpler append-only transition log.

Rejected because the investment cost is large for a feature that needs to ship in CRM Release MVP, and the chosen hybrid already guarantees the audit properties without the tooling overhead.

### Alternative C — Dedicated `PolicyReinstatementWindow` entity

Separate table for reinstatement windows.

- **Pros:** Clear naming; no overloading of `WorkflowSlaThreshold`.
- **Cons:** Fragments SLA/window configuration across two tables; duplicates admin-editing tooling; conflicts with the trajectory of ADR-014-workflow-sla.

Rejected as described in §3.

### Alternative D — Temporal-backed expiration from MVP

Use Temporal for the expiration sweep.

- **Pros:** Aligned with long-term ADR-010 direction; durable.
- **Cons:** Over-engineered for a nightly date comparison with no escalations or per-entity timers; increases MVP complexity without MVP benefit.

Rejected for MVP; named explicitly as a follow-up migration.

## Implementation Notes

- Migration adds: `Policies`, `PolicyVersions`, `PolicyEndorsements`, `PolicyCoverageLines`, `CarrierRef`; plus `WorkflowSlaThreshold` seed rows for `PolicyReinstatementWindow` and `CarrierRef` seed rows.
- Required indexes (declared here, enforced in migration):
  - `Policies(PolicyNumber)` unique, case-insensitive.
  - `Policies(AccountId, Status)`, `Policies(ExpirationDate, Status)`, `Policies(Status, LineOfBusiness)`, `Policies(CarrierRefId, Status)`, `Policies(PredecessorPolicyId)` partial (non-null).
  - `PolicyVersions(PolicyId, VersionNumber DESC)`, `PolicyEndorsements(PolicyId, EndorsementNumber DESC)`, `PolicyCoverageLines(VersionId)`.
- Version and endorsement numbering concurrency-safe via ordered insert under row lock on the parent policy row, with unique `(PolicyId, VersionNumber)` and `(PolicyId, EndorsementNumber)` constraints as a second line of defense.
- `PolicyNumber` generator: `NEB-{LOB-prefix}-{yyyy}-{6-digit-sequence}`, per-LOB-per-year sequence with year rollover. Collision handling: unique index + retry on conflict.
- `PolicyVersion.ProfileSnapshot` must contain all profile fields required to reconstruct the version without joining the live row (audit-replay guarantee).
- `Idempotency-Key` stored in a short-lived idempotency table keyed by `(endpoint, key)` with 24-hour TTL; prior responses replayed on retry.
- Casbin rules added via the existing policy-file governance path (ADR-008).
- Daily expiration job schedules at 00:15 UTC via the existing background-job runner; metrics: processed count, transitions emitted, skipped (already expired), errors.

## Related ADRs

- [ADR-008 Casbin Enforcer Adoption](./ADR-008-casbin-enforcer-adoption.md) — mechanism for `policy:*` rules.
- [ADR-009 LOB Classification and SLA Configuration](./ADR-009-lob-classification-and-sla-configuration.md) — `WorkflowSlaThreshold` foundation.
- [ADR-010 Temporal Durable Workflow Orchestration](./ADR-010-temporal-durable-workflow-orchestration.md) — targeted follow-up for expiration job migration.
- [ADR-011 CRM Workflow State Machines and Transition History](./ADR-011-crm-workflow-state-machines-and-transition-history.md) — workflow + transition + timeline pattern reused.
- [ADR-012 Shared Document Storage](./ADR-012-shared-document-storage-and-metadata-architecture.md) — Policy 360 documents rail delegates to F0020.
- [ADR-014 Workflow SLA Threshold Per-LOB Extension](./ADR-014-workflow-sla-threshold-per-lob-extension.md) — per-LOB window storage mechanism reused.
- [ADR-017 Account Merge Tombstone and Fallback Contract](./ADR-017-account-merge-tombstone-and-fallback-contract.md) — consumed on policy denormalization.

## Related Features

- [F0018 Policy Lifecycle & Policy 360](../../features/archive/F0018-policy-lifecycle-and-policy-360/PRD.md) — owner.
- F0016 Account 360 & Insured Management (archived) — account aggregate and fallback contract provider.
- F0007 Renewal Pipeline (archived) — consumes `Policy.Id`; provides `BoundPolicyId` feeding computed `SuccessorPolicyId`.
- F0019 Submission Quoting, Proposal & Approval Workflow (planned) — future bind-hook caller.
- F0020 Document Management & ACORD Intake (planned) — Policy 360 documents rail.
- F0028 Carrier & Market Relationship Management (planned) — replaces CarrierRef seed with carrier master.
