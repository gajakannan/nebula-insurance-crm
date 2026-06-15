# ADR-025: Submission Downstream Workflow — Quote/Approval/Bind, Recorded-Not-Computed Packet, and Archive/Deactivate

**Status:** Accepted (G5 Phase B approval — user, 2026-06-01; recorded in plan run `2026-06-01-2ac02e13` `gate-decisions.md`)
**Date:** 2026-06-01 (F0019 Phase B architecture)
**Owners:** Architect
**Related Features:** F0019, F0006, F0018, F0020, F0034, F0027
**Related ADRs:** ADR-011 (workflow state machines), ADR-012 (document storage), ADR-018 (policy aggregate), ADR-020 (LOB attributes)

## Context

F0006 owns the submission workflow through `ReadyForUWReview`; the runtime rejects
`ReadyForUWReview -> InReview` and later transitions with `409 invalid_transition`. The canonical
`workflow:submission` model already *declares* the downstream states (`InReview`, `Quoted`,
`BindRequested`, `Bound`, `Declined`, `Withdrawn`). F0019 **activates** them and adds the quote
packet, approval checkpoint, bind decision, and archive lifecycle.

A central product constraint (operator decision, 2026-06-01): **Nebula is a CRM, not an underwriting
workbench.** F0019 coordinates status and records facts; it must never compute pricing, rate risk, or
score eligibility. This ADR codifies that boundary so it survives implementation and future change.

## Decision

### 1. Activate the downstream submission state machine (per ADR-011)

The deliberate boundary move from F0006 is an explicit F0019 deliverable (F0019-S0001), not a side
effect. Transition authorization is two-layer per ADR-011: Casbin gates the broad `submission:transition`
action; the application layer enforces the per-transition from→to role matrix and guards.

| Transition | Guard | App-layer roles |
|------------|-------|-----------------|
| `ReadyForUWReview → InReview` | — (boundary move) | Underwriter, Admin |
| `InReview → Quoted` | packet readiness (docs complete) | Underwriter, Admin |
| `Quoted → BindRequested` | approval `Granted` + packet `Approved` | Underwriter, Admin |
| `BindRequested → Bound` | idempotent confirm | Underwriter, Admin |
| `InReview/Quoted → Declined` | `reasonCode` required | Underwriter, Admin |
| `Quoted/BindRequested → Withdrawn` | `reasonCode` required | Underwriter, Admin, DistributionUser |

Every successful transition appends one `WorkflowTransition` (`WorkflowType='Submission'`) and one
`ActivityTimelineEvent`, atomically with the status change (ADR-011 atomicity). Terminal states
(`Bound`, `Declined`, `Withdrawn`) reject forward transitions (`409`).

### 2. SubmissionQuotePacket — a coordination record (recorded, never computed)

A submission-scoped packet (1:1 with submission in MVP) carrying packet `status`
(`Draft → ReadyForApproval → Approved → BindRequested`), `linkedDocumentRefs[]` (F0020 /
ADR-012 submission-parented documents), a readiness signal (document completeness + approval state —
**status only**), and **recorded reference facts** (`recordedPremiumAmount`, `recordedLimits`,
`recordedDeductibles`, `effectiveDate`, `carrierMarket`). Value-bearing fields are validated for
**presence/format only**. Structured coverage/product attributes are read from F0034 product-schema
attributes, not re-modeled. The packet does **not** store or render document binaries (F0027 owns
rendering). `InReview → Quoted` requires a coordination-ready packet (docs complete), never a priced
computation.

### 3. Single authorized approver (extensible)

A `SubmissionApprovalDecision` append-only record captures `decision` (`Granted | Declined`),
`approverUserId`, `reason`, `authorityContext`, `decidedAt`, optional `blockingConditions[]`. MVP grants
the new `submission:approve` action to **Underwriter** and **Admin** (no dedicated underwriting-manager
role, no maker-checker, no authority limits — these are explicitly Future, enabled by the
`authorityContext` shape without a workflow rewrite). Approval is distinct from `submission:transition`
and is a precondition for `Quoted → BindRequested`.

### 4. Bind decision and F0018 policy handoff

`BindRequested → Bound` is gated by a granted approval and an approved packet, and is **idempotent**
(an `idempotencyKey` coalesces retries/duplicate confirms). On `Bound`, F0019 emits a policy-creation
**handoff** to F0018 with a correlation id and a snapshot of recorded reference facts. This is a
handoff point only — F0019 does not create policies, issue documents, or post billing. A downstream
F0018 handoff failure does not roll back the `Bound` state; the handoff is recorded as
pending/retryable.

### 5. Archive/deactivate — explicit lifecycle action, not delete

F0019 owns the submission end-of-life contract replacing F0006's descoped soft-delete claim. Archive is
modeled as a dedicated `isArchived` lifecycle flag (deliberately **separate from `IsDeleted`** — a
`Bound` submission is completed, not deleted), set via an explicit archive action allowed **only for
terminal states** (`Bound`, `Declined`, `Withdrawn`). It reuses the established
**`IgnoreQueryFilters` pattern** (SOLUTION-PATTERNS §6): a global EF query filter excludes archived
submissions from default lists; archived records remain fully discoverable via an explicit
"include archived" path for audit/reporting and can be reactivated (also audited). There is **no
generic delete endpoint** for submissions; all workflow/approval/transition history is preserved.

### 6. CRM workflow, not underwriting workbench (boundary)

F0019 records status and reference facts and moves submissions through approval and bind. **Forbidden:**
rating/pricing computation, premium derivation, risk/eligibility scoring, quote optimization or
comparison engines, coverage-illustration generation, and carrier rating integration. Quote figures are
**recorded** (manual entry or captured from a document), **never computed**. This boundary is enforced
by: PRD non-goals + guardrail, the packet contract above, capture/track/transition story acceptance
criteria, this ADR, and a **boundary regression** asserting no rating/calculation endpoint or
computed-pricing field exists. `capability:submission-workflow` carries this rationale in the knowledge
graph so any future feature touching `workflow:submission` inherits the constraint.

## API Contract (additions; finalized in `nebula-api.yaml` at implementation)

| Method | Route | Purpose | Casbin action | Notable status codes |
|--------|-------|---------|---------------|----------------------|
| POST | `/api/submissions/{id}/transitions` | downstream transitions (existing endpoint, downstream states activated) | `submission:transition` | 200 / 403 / 404 / 409 |
| GET/PUT | `/api/submissions/{id}/quote-packet` | read / edit packet (recorded facts + doc links) | `submission:update` | 200 / 400 / 403 / 409 / 412 |
| POST | `/api/submissions/{id}/approval` | grant / decline approval | `submission:approve` | 201 / 400 / 403 / 409 |
| POST | `/api/submissions/{id}/bind` | request / confirm bind (idempotent) | `submission:transition` | 200 / 403 / 409 |
| POST | `/api/submissions/{id}/archive` · `/reactivate` | archive / reactivate (terminal-only) | `submission:archive` | 200 / 403 / 404 / 409 |

Mutations on existing aggregates use optimistic concurrency (`If-Match` / `RowVersion` → `412`,
SOLUTION-PATTERNS §6) and RFC 7807 ProblemDetails for errors.

## Authorization Deltas

Two new Casbin actions on `resource=submission` (policy.csv + authorization-matrix.md):

- `submission:approve` → Underwriter, Admin
- `submission:archive` → Underwriter, Admin

(`submission:transition` already exists and covers downstream + bind transitions; per-transition role
refinement is application-layer.)

## Data Model (additions)

- **SubmissionQuotePacket** — `Id`, `SubmissionId` (FK, unique), `Status`, recorded reference facts,
  `ReadinessState`; audit fields + `RowVersion` (SOLUTION-PATTERNS §6). Linked documents via F0020 refs.
- **SubmissionApprovalDecision** — `Id`, `SubmissionId` (FK), `Decision`, `ApproverUserId`, `Reason`,
  `AuthorityContext`, `DecidedAt`, `BlockingConditions`; append-only (no update/delete).
- **Submission** — add `IsArchived`, `ArchivedAt`, `ArchivedByUserId` (distinct from `IsDeleted`);
  downstream states already declared. Reuses existing `WorkflowTransition` + `ActivityTimelineEvent`.

## Non-Functional Requirements

- Transitions/approval < 500ms p95; packet save < 700ms p95; downstream list < 1s p95.
- Approval and bind actions idempotent; transition+audit atomic (single transaction).
- Archive reversible and never destructive; default list excludes archived without scan penalty.

## Consequences

**Positive:** completes the submission journey with audit-ready history; the workbench boundary is
explicit and queryable; archive is honest (not a delete) and reuses an existing pattern; the approval
model is simple now but shaped for maker-checker/limits later.

**Negative:** a distinct `isArchived` flag adds a second lifecycle dimension alongside `IsDeleted`
(documented to avoid conflation); the F0018 handoff introduces an eventual-consistency seam that must be
monitored.

## Pattern Compliance

ADR-011 (state machine + append-only atomic audit) · ADR-012 (document linkage) · SOLUTION-PATTERNS §2
(timeline events, pre-rendered descriptions) · §3 (REST conventions, ProblemDetails, pagination) · §6
(audit fields, soft-delete/IgnoreQueryFilters, optimistic concurrency, GUID keys).

## Follow-up

- ADR-011 Follow-up references this ADR for the F0019 submission downstream specification.
- F0019 stories S0001–S0008 implement this ADR; `nebula-api.yaml` endpoints + canonical endpoint/event
  nodes are finalized at implementation (feature.md).
- Future: maker-checker / authority-limit approval (extends `authorityContext`); F0027 reusable packet
  rendering; carrier rating remains out of scope.
