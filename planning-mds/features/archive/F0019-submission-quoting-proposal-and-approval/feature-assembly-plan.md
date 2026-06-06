# Feature Assembly Plan — F0019: Submission Quoting, Proposal & Approval Workflow

**Created:** 2026-06-03
**Author:** Architect Agent
**Status:** Draft

## Overview

F0019 activates the downstream submission workflow after F0006's intake boundary and adds the CRM-only quote/proposal packet, approval checkpoint, bind handoff, terminal outcomes, archive lifecycle, downstream list visibility, and downstream timeline surfacing. The implementation reuses the existing `/submissions` aggregate and transition endpoint where ADR-025 requires it, but adds explicit packet/approval/bind/archive endpoints and persistence so quote facts are recorded, never computed.

## Build Order

| Step | Scope | Stories | Rationale |
|------|-------|---------|-----------|
| 1 | Downstream transition activation and boundary regression | F0019-S0001 | Establishes `ReadyForUWReview -> InReview` as the deliberate F0019 boundary move before packet, approval, or bind flows depend on downstream states. |
| 2 | Quote/proposal packet persistence and mark-ready transition | F0019-S0002 | Packet readiness is the guard for `InReview -> Quoted`; recorded reference facts must exist before approval. |
| 3 | Approval checkpoint | F0019-S0003 | Approval gates bind and introduces `submission:approve` audit semantics. |
| 4 | Bind requested/bound handoff | F0019-S0004 | Uses approved packet plus idempotency to emit the F0018 policy handoff without embedding issuance. |
| 5 | Terminal decline/withdraw plus archive/reactivate | F0019-S0005, F0019-S0006 | Terminal outcomes must precede archive eligibility; archive is lifecycle state, not delete. |
| 6 | Downstream list/timeline UI and verification | F0019-S0007, F0019-S0008 | Read surfaces consolidate state, approval, stale/SLA, archived, and audit history after mutation paths exist. |

## Existing Code (Must Be Modified)

| File | Current State | F0019 Change |
|------|---------------|--------------|
| `engine/src/Nebula.Domain/Entities/Submission.cs` | 18 submission fields plus account/broker/program/lob/assignee navigations; no archive flag or packet/approval navigations. | **Expand** — add `IsArchived`, `ArchivedAt`, `ArchivedByUserId`, packet/approval/handoff navigation collections where needed. |
| `engine/src/Nebula.Application/Services/WorkflowStateMachine.cs` | Downstream states and transitions are already declared; terminal states come from `OpportunityStatusCatalog`. | **Affirm/adjust** — keep ADR-025 transition matrix and ensure terminal/role behavior matches service guards. |
| `engine/src/Nebula.Application/Services/SubmissionService.cs` | Create/update/list/detail/transition/assign/completeness; transition writes `WorkflowTransition` + `ActivityTimelineEvent` atomically but only guards completeness for `ReadyForUWReview`. | **Expand** — add packet, approval, bind, archive/reactivate methods; add downstream guards to `TransitionAsync`; filter archived by default; map packet/approval/archive/list fields. |
| `engine/src/Nebula.Application/Interfaces/ISubmissionRepository.cs` | Detail/list/stale APIs only. | **Expand** — add include-archived query support and packet/approval/handoff repository access through new focused interfaces. |
| `engine/src/Nebula.Infrastructure/Repositories/SubmissionRepository.cs` | ABAC-scoped list/detail with stale filtering; default query filter excludes `IsDeleted` only. | **Expand** — default excludes `IsArchived`, allow `includeArchived`, compute age/stuck flags, include packet/approval projections. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/SubmissionConfiguration.cs` | Maps submission fields and `IsDeleted` query filter. | **Expand** — map archive columns, indexes, and relations. |
| `engine/src/Nebula.Api/Endpoints/SubmissionEndpoints.cs` | Existing `/submissions` endpoints; transition endpoint uses `submission:transition`. | **Expand** — add quote-packet, approval, bind, archive/reactivate routes with `If-Match`/idempotency handling and ProblemDetails results. |
| `planning-mds/api/nebula-api.yaml` | F0006 submission endpoints plus downstream enum values; ADR-025 endpoint additions are not yet landed. | **Update** — add F0019 endpoint paths and schemas. |
| `planning-mds/schemas/activity-event-payloads.schema.json` | F0006 submission events; no F0019 packet/approval/bind/archive payload definitions. | **Update** — add downstream event definitions with pre-rendered descriptions. |
| `planning-mds/security/policies/policy.csv` | `submission:approve` and `submission:archive` rows already present for Underwriter/Admin. | **Verify** — no policy expansion expected unless implementation adds a new action. |
| `experience/src/features/submissions/types.ts` | Intake DTOs and transition request types only. | **Expand** — add packet, approval, bind, archive/list fields and request DTOs. |
| `experience/src/features/submissions/hooks/useTransitionSubmission.ts` | Transition mutation with `If-Match`, invalidates detail/timeline/list. | **Reuse/expand** — keep transition hook; add hooks for packet update, approval decision, bind, archive, reactivate. |
| `experience/src/pages/SubmissionDetailPage.tsx` | Intake detail, edit/assign, generic transition modal, completeness, documents, timeline. | **Expand** — add packet panel, approval panel, bind/archive actions, terminal reason handling, archived banner/read-only state. |
| `experience/src/pages/SubmissionsPage.tsx` | Intake filters/status/stale list. | **Expand** — downstream statuses, approval pending, stuck-only, include-archived toggle, archived flag and approval chips. |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `engine/src/Nebula.Domain/Entities/SubmissionQuotePacket.cs` | Domain | Submission-scoped packet coordination record with recorded reference facts and document refs. |
| `engine/src/Nebula.Domain/Entities/SubmissionApprovalDecision.cs` | Domain | Append-only approval grant/decline history. |
| `engine/src/Nebula.Domain/Entities/SubmissionBindHandoff.cs` | Domain | Idempotent F0018 handoff tracking with retryable pending state. |
| `engine/src/Nebula.Application/DTOs/SubmissionQuotePacketDto.cs` | Application | Packet response and update DTOs. |
| `engine/src/Nebula.Application/DTOs/SubmissionApprovalDecisionDto.cs` | Application | Approval request/response DTOs. |
| `engine/src/Nebula.Application/DTOs/SubmissionBindRequestDto.cs` | Application | Bind request DTO carrying idempotency key or header-derived key. |
| `engine/src/Nebula.Application/DTOs/SubmissionArchiveRequestDto.cs` | Application | Archive/reactivate request DTO. |
| `engine/src/Nebula.Application/Interfaces/ISubmissionQuotePacketRepository.cs` | Application | Packet persistence boundary. |
| `engine/src/Nebula.Application/Interfaces/ISubmissionApprovalDecisionRepository.cs` | Application | Approval history persistence boundary. |
| `engine/src/Nebula.Application/Interfaces/ISubmissionBindHandoffRepository.cs` | Application | Bind handoff idempotency/pending tracking boundary. |
| `engine/src/Nebula.Infrastructure/Repositories/SubmissionQuotePacketRepository.cs` | Infrastructure | EF implementation of packet repository. |
| `engine/src/Nebula.Infrastructure/Repositories/SubmissionApprovalDecisionRepository.cs` | Infrastructure | EF implementation of approval repository. |
| `engine/src/Nebula.Infrastructure/Repositories/SubmissionBindHandoffRepository.cs` | Infrastructure | EF implementation of handoff repository. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/SubmissionQuotePacketConfiguration.cs` | Infrastructure | EF mapping and unique packet-per-submission constraint. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/SubmissionApprovalDecisionConfiguration.cs` | Infrastructure | EF mapping for append-only approval history. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/SubmissionBindHandoffConfiguration.cs` | Infrastructure | EF mapping for idempotency and correlation. |
| `experience/src/features/submissions/hooks/useSubmissionQuotePacket.ts` | Frontend | Query/update packet API. |
| `experience/src/features/submissions/hooks/useSubmissionApproval.ts` | Frontend | Grant/decline approval mutation. |
| `experience/src/features/submissions/hooks/useBindSubmission.ts` | Frontend | Bind requested/confirm bind mutation. |
| `experience/src/features/submissions/hooks/useArchiveSubmission.ts` | Frontend | Archive/reactivate mutation. |
| `experience/src/features/submissions/components/SubmissionQuotePacketPanel.tsx` | Frontend | Recorded packet facts, linked document refs, readiness, and edit UI. |
| `experience/src/features/submissions/components/SubmissionApprovalPanel.tsx` | Frontend | Approval state and grant/decline controls. |

---

## Step 1 — Activate Downstream Transition Boundary (F0019-S0001)

### Modified Files

| File | Change |
|------|--------|
| `engine/src/Nebula.Application/Services/SubmissionService.cs` | Keep `ReadyForUWReview -> InReview`; ensure downstream role matrix follows ADR-025 and does not allow not-yet-owned transitions without guards. |
| `engine/tests/Nebula.Tests/Unit/WorkflowStateMachineTests.cs` | Update/add boundary regression: authorized F0019 `ReadyForUWReview -> InReview` succeeds; `ReadyForUWReview -> Bound` still rejects. |
| `experience/src/pages/SubmissionDetailPage.tsx` | Show downstream transition action only when available from backend and role-eligible. |

### Logic Flow

```text
SubmissionService.TransitionAsync(submissionId, dto, rowVersion, user)
```

1. Load submission with account/broker/program/assignee; return `404` if absent or unreadable.
2. Verify `rowVersion`; stale returns `precondition_failed` / HTTP 412.
3. Verify `WorkflowStateMachine.IsValidTransition("Submission", current, dto.ToState)`.
4. Verify per-transition role guard:
   - `ReadyForUWReview -> InReview`: Underwriter/Admin.
   - `InReview -> Quoted`: Underwriter/Admin and packet readiness.
   - `Quoted -> BindRequested`: Underwriter/Admin and granted approval + approved packet.
   - `BindRequested -> Bound`: handled by bind method, not generic transition unless idempotency context is present.
   - terminal transitions: use Step 5 reason-code guard.
5. Append `WorkflowTransition` + `ActivityTimelineEvent` and update `Submission.CurrentStatus` in one transaction.

### Mutation Traceability

| Screen / Entry Point | User Action | Endpoint | Service Method | Entity / Carrier | Authorization | Concurrency | Validation Failure | Audit / Timeline | Test Expectation |
|----------------------|-------------|----------|----------------|------------------|---------------|-------------|--------------------|------------------|------------------|
| Submission Detail workflow panel | Move to InReview | `POST /submissions/<built-in function id>/transitions` | `SubmissionService.TransitionAsync` | `Submission.CurrentStatus` | `submission:transition` + Underwriter/Admin | `If-Match` rowVersion | `403`, `409 invalid_transition`, `412` | `SubmissionTransitioned` + `WorkflowTransition` | API and UI reload show `InReview`; old F0006 reject test is updated with F0019 comment. |

## Step 2 — Quote/Proposal Packet Lifecycle (F0019-S0002)

### Entity / DTO / Code

```csharp
public class SubmissionQuotePacket : BaseEntity
{
    public Guid SubmissionId { get; set; }
    public string Status { get; set; } = "Draft";
    public string LinkedDocumentRefsJson { get; set; } = "[]";
    public decimal? RecordedPremiumAmount { get; set; }
    public string? RecordedLimits { get; set; }
    public string? RecordedDeductibles { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string? CarrierMarket { get; set; }
    public string ReadinessState { get; set; } = "Draft";
    public Submission Submission { get; set; } = default!;
}

public record SubmissionQuotePacketUpdateDto(
    IReadOnlyList<Guid> LinkedDocumentRefs,
    decimal? RecordedPremiumAmount,
    string? RecordedLimits,
    string? RecordedDeductibles,
    DateTime? EffectiveDate,
    string? CarrierMarket,
    bool MarkReady);
```

### Logic Flow

1. `GetQuotePacketAsync` returns existing packet or a default Draft projection.
2. `UpdateQuotePacketAsync` verifies `submission:update`, rowVersion, status in `InReview`/`Quoted`, and not `Bound`/archived.
3. Validate recorded value presence/format only; do not calculate or derive premium/rating.
4. Validate linked documents are submission-parented F0020 refs; mark-ready requires completeness signal.
5. Persist packet fields; if `MarkReady && submission.CurrentStatus == "InReview"`, set packet `ReadyForApproval` and transition submission to `Quoted`.
6. Append `SubmissionPacketUpdated` and, when quoted, `SubmissionTransitioned`; commit atomically.

### Mutation Traceability

| Screen / Entry Point | User Action | Endpoint | Service Method | Entity / Carrier | Authorization | Concurrency | Validation Failure | Audit / Timeline | Test Expectation |
|----------------------|-------------|----------|----------------|------------------|---------------|-------------|--------------------|------------------|------------------|
| Submission Detail packet panel | Save packet | `PUT /submissions/<built-in function id>/quote-packet` | `SubmissionService.UpdateQuotePacketAsync` | `SubmissionQuotePacket` | `submission:update` + Underwriter/Admin | `If-Match` rowVersion | `400`, `403`, `409`, `412` | `SubmissionPacketUpdated` | Reload shows recorded facts and document refs; no computed-pricing field exists. |
| Submission Detail packet panel | Mark ready | `PUT /submissions/<built-in function id>/quote-packet` | `SubmissionService.UpdateQuotePacketAsync` | packet + `Submission.CurrentStatus` | `submission:update` + `submission:transition` guard | `If-Match` rowVersion | `409 missing documents` | `SubmissionPacketUpdated` + `SubmissionTransitioned` | Reload shows `Quoted` and ready packet. |

## Step 3 — Underwriting Approval Checkpoint (F0019-S0003)

```csharp
public class SubmissionApprovalDecision : BaseEntity
{
    public Guid SubmissionId { get; set; }
    public string Decision { get; set; } = default!;
    public Guid ApproverUserId { get; set; }
    public string Reason { get; set; } = default!;
    public string AuthorityContextJson { get; set; } = "{}";
    public DateTime DecidedAt { get; set; }
    public string BlockingConditionsJson { get; set; } = "[]";
    public Submission Submission { get; set; } = default!;
}

public record SubmissionApprovalRequestDto(string Decision, string Reason, IReadOnlyList<string>? BlockingConditions);
```

1. `ApproveSubmissionAsync` requires `submission:approve`, `Quoted`, packet `ReadyForApproval`, and no effective granted approval for the cycle.
2. Decline requires a non-empty reason; grant requires authority context from current role/assignment.
3. Append `SubmissionApprovalDecision`; grant sets packet `Approved`; decline leaves bind blocked.
4. Append `SubmissionApprovalGranted` or `SubmissionApprovalDeclined`; commit atomically.

## Step 4 — Bind Decision and Policy Handoff (F0019-S0004)

```csharp
public class SubmissionBindHandoff : BaseEntity
{
    public Guid SubmissionId { get; set; }
    public string IdempotencyKey { get; set; } = default!;
    public string Status { get; set; } = "Pending";
    public Guid CorrelationId { get; set; }
    public string PayloadSnapshotJson { get; set; } = default!;
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public record SubmissionBindRequestDto(string? IdempotencyKey);
```

1. Request bind moves `Quoted -> BindRequested` when packet `Approved` and approval `Granted`.
2. Confirm bind moves `BindRequested -> Bound`; idempotency key coalesces retries.
3. Create/affirm one `SubmissionBindHandoff` with recorded packet facts and F0020 document refs for F0018 `/policies/from-bind`.
4. Handoff failure records pending/retryable state and does not roll back `Bound`.

## Step 5 — Terminal Decisions and Archive (F0019-S0005, F0019-S0006)

1. Decline/withdraw require `reasonCode`; `reasonDetail` is required when `Other`.
2. `Declined` allowed from `InReview`/`Quoted`; `Withdrawn` from `Quoted`/`BindRequested`; terminal states reject forward transitions.
3. Archive/reactivate use `submission:archive`; archive only from `Bound`/`Declined`/`Withdrawn`.
4. `Submission.IsArchived` is separate from `IsDeleted`; default list excludes archived and direct reads/list with `includeArchived=true` remain available.
5. No generic delete route is added.

## Step 6 — Downstream List and Timeline (F0019-S0007, F0019-S0008)

1. Add list query fields: `approvalPending`, `stuckOnly`, `includeArchived`.
2. Add list item fields: `ageDaysInState`, `approvalStatus`, `isArchived`, `stuckFlag`.
3. Frontend list adds status multi-select, approval pending toggle, stuck toggle, include archived toggle, approval chip, and archived flag.
4. Timeline renders F0019 event types from `ActivityTimelineEvent` in stable order.

## Scope Breakdown

| Layer | Required Work | Owner | Status |
|------|----------------|-------|--------|
| Backend (`{PRODUCT_ROOT}/engine/`) | Entities, DTOs, repositories, service methods, endpoint routes, EF configurations, migrations, API contract and schemas. | Backend Developer | Planned |
| Frontend (`{PRODUCT_ROOT}/experience/`) | Submission detail packet/approval/bind/archive controls, downstream list filters/chips, hooks/types/tests. | Frontend Developer | Planned |
| AI (`{PRODUCT_ROOT}/neuron/`) | No AI scope. | AI Engineer | Not required |
| Quality | Unit, integration, contract, and frontend component/integration coverage for all eight stories. | Quality Engineer | Planned |
| DevOps/Runtime | Runtime preflight and deployability check; migration review if EF migration is generated. | DevOps | Conditional |

## Dependency Order

```text
Step 0 (Architect):   feature-local assembly plan + G0 evidence
Step 1 (Backend):     downstream transition guard and boundary regression
Step 2 (Backend):     packet entity/service/API + schema updates
Step 3 (Backend):     approval entity/service/API + policy verification
Step 4 (Backend):     bind handoff + idempotency
Step 5 (Backend):     terminal outcomes + archive/reactivate + list fields
  ---- Backend checkpoint: API integration tests green for S0001-S0006 ----
Step 6 (Frontend):    detail panels/actions + list filters/chips + timeline surfacing
  ---- Frontend checkpoint: component/integration tests green for S0002-S0008 ----
Step 7 (QE):          full lifecycle and security-sensitive validation evidence
```

## Integration Checkpoints

### After Backend Step 1

- [ ] `ReadyForUWReview -> InReview` succeeds for Underwriter/Admin and appends transition/timeline.
- [ ] `ReadyForUWReview -> Bound` still returns `409 invalid_transition`.

### After Backend Steps 2-4

- [ ] Packet save persists recorded values and document refs without computation.
- [ ] Mark-ready creates `Quoted` transition only when completeness passes.
- [ ] Approval grant/decline writes append-only decision records.
- [ ] Bind is idempotent and produces exactly one handoff record.

### After Backend Step 5

- [ ] Decline/withdraw require reason code and terminal states reject forward movement.
- [ ] Archive excludes default list, include-archived finds the record, and reactivate is audited.
- [ ] No submission delete endpoint exists.

### Cross-Story Verification

- [ ] Full lifecycle: `ReadyForUWReview -> InReview -> Quoted -> BindRequested -> Bound -> Archive`.
- [ ] Alternate lifecycle: `InReview/Quoted -> Declined` and `Quoted/BindRequested -> Withdrawn`.
- [ ] All Casbin policies enforced (`submission:transition`, `submission:update`, `submission:approve`, `submission:archive`; BrokerUser denied).
- [ ] Timeline events for full lifecycle are correct and ordered.
- [ ] ProblemDetails format consistent with existing endpoints.
- [ ] Boundary regression proves no rating/pricing/scoring endpoint or computed-pricing field was introduced.

## Integration Checklist

- [ ] API contract compatibility validated.
- [ ] Frontend contract compatibility validated.
- [ ] AI contract compatibility validated: N/A.
- [ ] Test cases mapped to acceptance criteria.
- [ ] Developer-owned fast-test responsibilities identified by layer.
- [ ] Required runtime evidence artifacts identified under run `2026-06-03-7e8e0ddc`.
- [ ] Framework vs solution boundary reviewed; no `agents/**` changes in feature scope.
- [ ] Mutation traceability tables completed for every state-changing story.
- [ ] Render-only tests are not used to close mutation stories.
- [ ] Run/deploy instructions updated if migration changes require it.

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Packet could drift into rating or computed pricing. | High | Keep DTO names `recorded*`, validate presence/format only, add no-computation regression. | Backend + Code Reviewer |
| Approval and bind can duplicate under retry. | High | Use append-only approval rules plus bind idempotency key and unique handoff constraint. | Backend |
| Archive can be confused with delete. | Medium | Separate `IsArchived` from `IsDeleted`, no delete route, audit archive/reactivate. | Backend |
| Frontend action availability could diverge from backend guards. | Medium | Backend `availableTransitions` remains source of truth; UI hides/blocks only as ergonomic mirror. | Frontend |

## Knowledge-Graph Binding Plan

- Baseline nodes from lookup: `workflow:submission`, `entity:submission`, `entity:submission-quote-packet`, `entity:submission-approval-decision`, `entity:policy`, `entity:document`, `entity:workflow-transition`, `entity:activity-timeline-event`, `capability:submission-workflow`, downstream submission states, `policy_rule:submission-transition`, `policy_rule:submission-approve`, `policy_rule:submission-archive`, `api:nebula-rest`, schemas `submission`, `submission-transition-request`, `line-of-business`, and `activity-event-payloads`.
- Expected code-index binding delta at G7: confirm or add globs for `engine/src/Nebula.Domain/Entities/Submission*.cs`, `engine/src/Nebula.Application/Services/SubmissionService.cs`, `engine/src/Nebula.Application/Services/WorkflowStateMachine.cs`, `engine/src/Nebula.Api/Endpoints/SubmissionEndpoints.cs`, `engine/src/Nebula.Infrastructure/Repositories/Submission*.cs`, `engine/src/Nebula.Infrastructure/Persistence/Configurations/Submission*.cs`, and `experience/src/features/submissions/**`.
- Expected canonical node delta: none for the main entity/workflow/policy nodes if existing Phase B nodes remain adequate; add endpoint/event nodes only if the as-built source introduces new semantic surfaces not already represented.
- G7 must diff this baseline against as-built code and run `validate.py --regenerate-symbols --check-symbols` plus `validate.py --check-drift`.

## JSON Serialization Convention

OpenAPI remains 3.0.3 nullable syntax; JSON Schema uses Draft-07 type arrays. C# uses nullable reference types and TypeScript uses `T | null`. Packet linked-document refs and authority/blocking-condition collections are serialized as JSON arrays in persistence but exposed as typed arrays in DTOs.

## DI Registration Changes

Register new repositories in the existing infrastructure/application DI extension: `ISubmissionQuotePacketRepository`, `ISubmissionApprovalDecisionRepository`, and `ISubmissionBindHandoffRepository`. No new hosted worker is expected for MVP; bind handoff retry is recorded as pending/retryable.

## Casbin Policy Sync

`submission:approve` and `submission:archive` rows already exist in `planning-mds/security/policies/policy.csv` and `authorization-matrix.md`. If implementation changes policy actions, copy updates to the embedded policy resource and re-run policy/security validation.
