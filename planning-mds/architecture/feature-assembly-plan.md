# Feature Assembly Plan (F0001 + F0002 + F0006 + F0007 + F0009 + F0010 + F0012 + F0013 + F0014 + F0015 + F0033)

**Owner:** Architect
**Status:** Approved
**Last Updated:** 2026-03-28

## Goal

Define the build order, role handoffs, and integration checkpoints for implemented and planned features.

**Note:** F0010 frontend (Pipeline Board) and F0011 are abandoned — superseded by F0012, then F0013. F0012 is archived — superseded by F0013. Backend endpoints from F0010 and F0012 carry forward into F0013. See individual feature sections for details.

---

## F0006 — Submission Intake Workflow

**Added:** 2026-03-27 — Architecture review complete; data model, API, workflow, Casbin, schemas, error codes, and timeline events finalized.
**Updated:** 2026-03-31 — Concurrency, completeness, and soft-dependency contracts reconciled across OpenAPI, JSON Schema, feature stories, and Casbin references.

> **Implementation Execution Plan:** [`feature-assembly-plan.md`](../features/F0006-submission-intake-workflow/feature-assembly-plan.md) — detailed per-step file paths, C# code, logic flows, Casbin enforcement, timeline events, HTTP responses, and integration checkpoints for the backend/frontend developer agents.

### Dependencies

| Dependency | Source | What F0006 Needs | Status |
|------------|--------|------------------|--------|
| Account entity lookup | F0016 | AccountId, Name, Region, Industry point lookups for validation and display | **Entity exists; add targeted lookup methods if missing** |
| Broker entity | F0002 | BrokerId, LegalName, BrokerRegion set | **Done** |
| User search API | F0004-S0002 | Assignee picker for ownership | **Done** |
| Task linked entity | F0003/F0004 | LinkedEntityType=Submission on Task | **Done** |
| WorkflowSlaThreshold | ADR-009 | Stale thresholds per intake state | **Seed required** |
| Document metadata | F0020 | Document completeness checks via adapter, not hardcoded service branching | **Soft dependency — null-object adapter until available** |

### Architecture Notes

- **Data model:** Submission entity expanded with Description, ExpirationDate; PremiumEstimate made nullable. UserProfile navigation added for assignee denormalization.
- **Workflow:** 4 intake states (Received→Triaging→WaitingOnBroker/ReadyForUWReview) with completeness and assignment guards on →ReadyForUWReview. WorkflowStateMachine rewritten to 10-state model (F0006 intake + F0019 downstream).
- **Stale detection:** Query-time computation from WorkflowSlaThreshold + last WorkflowTransition.OccurredAt.
- **Completeness:** Field checks (including "assigned user has Underwriter role") + document checks through an `ISubmissionDocumentChecklistReader` adapter. Used as read-side projection and transition guard.
- **Casbin:** Existing §2.3 policies extended with `create`, `update`, `assign` actions (already in policy.csv).
- **API:** 7 submission endpoints (list, create, detail, update, transition, assignment, timeline). State-changing mutations require `If-Match` / `rowVersion` and return HTTP 412 on stale versions.

### Backend Assembly Steps

**Step 1 — Migration: add columns, seed data (Backend Developer)**
1. Add Description (varchar 2000 NULL), ExpirationDate (date NULL) columns
2. Alter PremiumEstimate to nullable
3. Add AccountId, BrokerId, EffectiveDate indexes
4. Seed WorkflowSlaThreshold for intake states (Received=48h, Triaging=48h, WaitingOnBroker=72h)
5. Re-seed ReferenceSubmissionStatus (10 states)
6. Seed sample submissions for dev/test

**Step 2 — Domain and application services (Backend Developer)**
1. Submission entity + UserProfile nav prop
2. OpportunityStatusCatalog: align to 10 submission statuses
3. WorkflowStateMachine: rewrite submission transitions
4. Expand reference-data lookups for account/program validation and add the default F0020 document-checklist adapter
5. SubmissionService: Create (region validation, auto-assignment, timeline event), Update (changed fields tracking + `If-Match`), List (ABAC-scoped, stale), Transition (completeness+role guards + `If-Match`), Assign (role validation + `If-Match`), Completeness evaluation
6. DTOs: rewrite SubmissionDto, SubmissionCreateDto, SubmissionUpdateDto; new list-item, assignment, completeness, list-query DTOs
7. Validators: rewrite create/update; new assignment validator

**Step 3 — API endpoints (Backend Developer)**
1. `GET /submissions` — paginated list with filters (S0001)
2. `POST /submissions` — create with region validation (S0002)
3. `GET /submissions/{id}` — detail with completeness + transitions (S0003)
4. `PUT /submissions/{id}` — update mutable fields (S0003)
5. `POST /submissions/{id}/transitions` — transition with guards (S0004)
6. `PUT /submissions/{id}/assignment` — assign/reassign (S0006)
7. `GET /submissions/{id}/timeline` — paginated timeline (S0007)

**Step 4 — Integration testing (Backend Developer + QE)**
1. Smoke test: full intake lifecycle (Create → Triage → WaitOnBroker → ReadyForUWReview)
2. Region alignment validation
3. Completeness guard enforcement
4. Role-gated transition enforcement
5. Assignment validation (underwriter requirement in ReadyForUWReview)
6. Stale detection with configured thresholds
7. Stale `If-Match` handling returns HTTP 412 across update, transition, and assignment

### Frontend Assembly Steps

**Step 5 — Submission pipeline list (Frontend Developer)**
1. Feature slice: `experience/src/features/submissions/`
2. API hooks: `useSubmissions()`, `useSubmissionDetail()`, `useSubmissionCreate()`, `useSubmissionTransition()`, `useSubmissionAssignment()`, `useSubmissionTimeline()`
3. Pipeline list page with status/broker/LOB/owner/stale filters, sort, pagination
4. Stale indicator badges on list rows
5. Row click → detail navigation

**Step 6 — Submission detail view (Frontend Developer)**
1. Submission header (status badge, account, broker, LOB, effective date, assigned user)
2. Completeness panel (field checks + document checks with status indicators)
3. Timeline section with paginated feed
4. Action bar: transition buttons filtered by user role + current state
5. Assignment picker (reuse F0004 user search pattern)

**Step 7 — Create submission form (Frontend Developer)**
1. Account picker, Broker picker with region validation feedback
2. Effective date, LOB dropdown, optional fields
3. Region mismatch error shown inline
4. On success → navigate to detail view

**Step 8 — Dashboard integration (Frontend Developer)**
1. Stale submission nudge card with count and link to filtered pipeline list
2. Extend existing nudge framework (F0001-S0005 pattern)

### QA Assembly Steps

**Step 9 — Test coverage (Quality Engineer)**
1. Backend: workflow transition matrix (all valid + invalid pairs)
2. Backend: completeness guard (missing fields, missing underwriter)
3. Backend: region alignment validation
4. Backend: stale detection with configured thresholds
5. Backend: ABAC scoping (distribution user sees own, manager sees region, admin sees all)
6. Frontend: pipeline list filtering and sorting
7. Frontend: detail view completeness panel and action bar state
8. E2E: full intake lifecycle from creation through ReadyForUWReview handoff

### Dependency Order

```
Step 0 (Architect):   architecture review + spec finalization ← DONE
Step 1 (Backend):     migration, seed data, Account stub if needed
Step 2 (Backend):     domain entity + catalog + state machine + services + DTOs + validators
Step 3 (Backend):     API endpoints with Casbin enforcement
Step 4 (Backend+QE):  integration testing + smoke tests
  ──── Backend checkpoint: all 7 API endpoints passing ────
Step 5 (Frontend):    pipeline list
Step 6 (Frontend):    detail view + transitions + assignment + completeness
Step 7 (Frontend):    create submission form
Step 8 (Frontend):    dashboard nudge card
  ──── Frontend checkpoint: full UI flow verified ────
Step 9 (QE):          comprehensive test coverage
```

### Integration Checkpoints

| Checkpoint | Gate | Owner | Criteria |
|------------|------|-------|----------|
| F0006-A | Backend API ready | Backend Dev | All 7 endpoints pass smoke tests; intake transitions validated; Casbin enforcement verified; completeness guard tested |
| F0006-B | Frontend pipeline ready | Frontend Dev | Pipeline list renders with filters; detail view shows completeness and timeline; create form validates region |
| F0006-C | Full integration | QE | E2E intake lifecycle passes; stale detection verified; role-based access confirmed |

### Signoff Role Matrix

| Role | Required | Rationale |
|------|----------|-----------|
| Quality Engineer | Yes | Workflow transitions, completeness guards, stale detection, and ABAC scoping require structured validation |
| Code Reviewer | Yes | State machine rewrite, region validation, and completeness logic require independent review |
| Security Reviewer | Yes | Cross-role visibility, assignment authorization, and ABAC policy extensions |
| DevOps | No | No new infrastructure in MVP |
| Architect | Yes | Workflow design, completeness policy, state machine alignment |

### Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Account/program validation still depends on list-oriented reference-data APIs | Medium | Add targeted point lookups so submission create/update flows do not load full cached lists just to validate IDs | Backend |
| WorkflowStateMachine has old states not in F0006+F0019 model | Medium | Step 2 rewrites to 10-state model; migration maps existing records | Backend |
| Stale detection N+1 performance on list endpoint | Medium | Batch-load thresholds; use window function for last transition per submission | Backend |
| F0020 (Document Management) not available | Low | Document completeness shows "unavailable"; field completeness enforced; transition guard soft-skips documents | Backend |

---

## F0007 — Renewal Pipeline

**Added:** 2026-03-26 — Architecture review complete; data model, API, workflow, Casbin, and ADRs finalized.

> **Implementation Execution Plan:** [`feature-assembly-plan.md`](../features/F0007-renewal-pipeline/feature-assembly-plan.md) — detailed per-step file paths, C# code, logic flows, Casbin enforcement, timeline events, HTTP responses, and integration checkpoints for the backend/frontend developer agents.

### Dependencies

| Dependency | Source | What F0007 Needs | Status |
|------------|--------|------------------|--------|
| Policy entity (stub) | F0018 | PolicyId, ExpirationDate, LOB, AccountId, BrokerId, PolicyNumber, Carrier | **Must implement before F0007** |
| Account entity | F0016 | AccountId, Name, Industry, PrimaryState | Assumed available (placeholder or stub) |
| Broker entity | F0002 | BrokerId, LegalName, LicenseNumber, State | **Done** |
| User search API | F0004-S0002 | Assignee picker for ownership | **Done** |
| Task linked entity | F0003/F0004 | LinkedEntityType=Renewal on Task | **Done** |
| WorkflowSlaThreshold | ADR-009 + ADR-014 | Per-LOB renewal timing thresholds | **Migration required** |

### F0018 Dependency Stub Strategy

F0018 (Policy Lifecycle) is sequenced before F0007 in the release plan. F0007 requires a minimum Policy entity (see data-model.md §6.2). Two approaches:

1. **Preferred:** F0018 implements its full entity before F0007 starts. F0007 consumes the Policy read service.
2. **Fallback:** If F0018 is delayed, implement a minimal Policy stub (Migration 006 in data-model.md) with the fields F0007 needs. F0018 extends this stub when it lands.

### Architecture Notes

- **Data model:** Renewal entity redesigned (data-model.md §6). PolicyId required, one active renewal per policy (filtered unique index). 6-status lifecycle replaces old 8-status model.
- **Workflow:** State machine with role-gated transitions (F0007 README). Transitions are atomic (renewal update + WorkflowTransition + ActivityTimelineEvent in single transaction per ADR-011).
- **Timing:** Overdue/approaching detection is query-time computation from stored dates and WorkflowSlaThreshold (ADR-014). No Temporal dependency in MVP.
- **Casbin:** New `create` and `assign` actions added to policy.csv §2.4. Per-transition role checks at application layer.
- **API:** 6 renewal endpoints (list, create, detail, transition, assignment, timeline). WorkflowTransitionRequest extended with conditional fields.

### Backend Assembly Steps

**Step 1 — Migrations and seed data (Backend Developer)**
1. Migration 006: Create Policy table (stub) if F0018 not yet landed
2. Migration 007: Restructure Renewals table (drop old columns, add new), re-seed ReferenceRenewalStatus (6 values)
3. Migration 008: Extend WorkflowSlaThreshold with LineOfBusiness column, seed renewal timing thresholds
4. Seed sample policies and renewals for dev/test

**Step 2 — Domain and application services (Backend Developer)**
1. Renewal entity + repository with filtered unique index enforcement
2. RenewalService: create from policy (inherits fields, computes TargetOutreachDate, validates one-active-per-policy)
3. RenewalTransitionService: validate allowed transitions, role guards, conditional field checks, atomic transition+audit
4. RenewalAssignmentService: role-based assignment rules, timeline event creation
5. RenewalQueryService: list with due-window filtering, urgency computation, ABAC scoping
6. Casbin policy enforcement for create/assign actions

**Step 3 — API endpoints (Backend Developer)**
1. `GET /renewals` — list with filters (S0001)
2. `POST /renewals` — create from policy (S0006)
3. `GET /renewals/{id}` — detail with policy context, available transitions (S0002)
4. `POST /renewals/{id}/transitions` — transition with conditional fields (S0003)
5. `PUT /renewals/{id}/assignment` — assign/reassign (S0004)
6. `GET /renewals/{id}/timeline` — paginated timeline (S0007)

**Step 4 — Integration testing (Backend Developer + QE)**
1. Smoke test: full lifecycle (create → Outreach → InReview → Quoted → Completed)
2. Overdue detection with LOB-specific thresholds
3. Duplicate renewal rejection
4. Role-gated transition enforcement
5. Assignment validation (role compatibility, terminal state rejection)

### Frontend Assembly Steps

**Step 5 — Renewal pipeline list (Frontend Developer)**
1. Feature slice: `experience/src/features/renewals/`
2. API hooks: `useRenewals()`, `useRenewalDetail()`, `useRenewalTransition()`, `useRenewalAssignment()`, `useRenewalTimeline()`
3. Pipeline list page with due-window filters, status/LOB/owner filters, sort, pagination
4. Overdue/approaching badges on list rows
5. Row click → detail navigation

**Step 6 — Renewal detail view (Frontend Developer)**
1. Renewal header (status, owner, LOB, urgency badge)
2. Policy section (reads from F0018 API)
3. Account/broker context sections
4. Timeline section with paginated feed
5. Action bar: transition buttons filtered by user role + current state
6. Assignment picker (reuse F0004 user search pattern)

**Step 7 — Dashboard integration (Frontend Developer)**
1. Renewal nudge card: overdue + approaching counts with link to filtered pipeline list
2. Extend existing nudge framework (F0001-S0005 pattern)

### QA Assembly Steps

**Step 8 — Test coverage (Quality Engineer)**
1. Backend: workflow transition matrix (all valid + invalid pairs)
2. Backend: conditional field validation (Lost reason, Completed policy link)
3. Backend: one-active-renewal-per-policy constraint
4. Backend: overdue/approaching computation with different LOB thresholds
5. Backend: ABAC scoping (distribution user sees own, manager sees region, underwriter sees assigned)
6. Frontend: pipeline list filtering and sorting
7. Frontend: detail view data display and action bar state
8. E2E: full lifecycle from creation through completion or loss

### Dependency Order

```
Step 0 (Architect):   architecture review + spec finalization ← DONE
Step 1 (Backend):     migrations, seed data, Policy stub if needed
Step 2 (Backend):     domain services + Casbin enforcement
Step 3 (Backend):     API endpoints
Step 4 (Backend+QE):  integration testing + smoke tests
  ──── Backend checkpoint: all API endpoints passing ────
Step 5 (Frontend):    pipeline list
Step 6 (Frontend):    detail view + transitions + assignment
Step 7 (Frontend):    dashboard nudge card
  ──── Frontend checkpoint: full UI flow verified ────
Step 8 (QE):          comprehensive test coverage
```

### Integration Checkpoints

| Checkpoint | Gate | Owner | Criteria |
|------------|------|-------|----------|
| F0007-A | Backend API ready | Backend Dev | All 6 endpoints pass smoke tests; workflow transitions validated; Casbin enforcement verified |
| F0007-B | Frontend pipeline ready | Frontend Dev | Pipeline list renders with filters; detail view shows policy context and timeline |
| F0007-C | Full integration | QE | E2E lifecycle test passes; overdue detection verified; role-based access confirmed |

### F0022 Integration Surface (Future)

F0022 (Work Queues) will add rule-based queue routing for renewals. F0007 MVP uses manual assignment only. The integration surface:

- F0022 consumes `RenewalCreated` events to route new renewals to queues
- F0022 may call `PUT /renewals/{id}/assignment` to auto-assign from queue
- F0007's `assign` Casbin action provides the authorization foundation for F0022's automated assignment

No code changes needed in F0007 for F0022 — the API and authorization model are already extensible.

### Signoff Role Matrix

| Role | Required | Rationale |
|------|----------|-----------|
| Quality Engineer | Yes | Renewal timing, workflow transitions, and overdue detection require structured validation |
| Code Reviewer | Yes | Workflow state machine, timing logic, and API behavior require independent review |
| Security Reviewer | Yes | Cross-role visibility, handoff authorization, and ABAC policy extensions |
| DevOps | No | No new infrastructure in MVP (Temporal is future phase) |
| Architect | Yes | Workflow orchestration, state machine design, and ADR-009/014 extensions |

### Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| F0018 (Policy) not ready before F0007 starts | High | Implement Policy stub migration (006) with minimum fields; F0018 extends later | Architect + Backend |
| Overdue computation performance at scale (>10K renewals) | Medium | Partial index on TargetOutreachDate WHERE status=Identified; consider materialized view if needed | Backend |
| WorkflowSlaThreshold LOB extension breaks existing submission thresholds | Low | Existing entries have NULL LineOfBusiness; migration is additive only | Backend |
| ReferenceRenewalStatus seed data change breaks existing renewal records | Medium | Migration 007 must update any existing renewal records to map old→new statuses | Backend |

---

## F0015 — Frontend Quality Gates + Test Infrastructure

**Updated:** 2026-03-21 — Completed; solution-side frontend validation feature implemented with lifecycle evidence enforcement

### Dependencies

- Existing frontend runtime and tests under `experience/`
- Existing strategy baseline in `planning-mds/architecture/TESTING-STRATEGY.md`
- Existing lifecycle gate mechanism in `lifecycle-stage.yaml`
- Existing evidence governance under `planning-mds/operations/evidence/`
- Proven containerized Playwright/frontend rerun path established during F0013 remediation

### Architecture Notes

#### Boundary Split

F0015 is a Nebula solution feature. It may consume stronger generic framework contracts if they land separately, but its implementation scope is solution-specific:

| Scope | Lives In | Notes |
|---|---|---|
| Generic role/action/template changes | External agent framework | Separate framework track; not owned by F0015 |
| Nebula lifecycle, evidence, and tracker activation | `planning-mds/**`, `lifecycle-stage.yaml` | In scope for F0015 |
| Nebula frontend tooling and tests | `experience/**` | In scope for F0015 |

#### Runtime Execution Rule

Frontend proof for this feature must follow the same runtime execution principle used elsewhere in the repo: validation evidence comes from the approved application runtime path, not from informal local assertions. Because host-side dependency behavior on this Windows-mounted workspace has been unreliable, F0015 should treat the containerized frontend runtime as a first-class execution path rather than a fallback of last resort.

### Frontend Assembly Steps

1. Add first-class frontend commands for integration, accessibility, and coverage validation.
2. Configure shared frontend test setup for API mocking and accessibility assertions.
3. Emit machine-readable frontend coverage artifacts from the standard runtime path.
4. Backfill representative critical-path tests on auth and dashboard/brokers surfaces to prove the new tooling.

### QA Assembly Steps

1. Produce story-to-suite mapping for F0015 validation layers.
2. Require evidence that distinguishes component/integration/a11y/coverage/visual checks.
3. Execute one full frontend validation run in the approved runtime path and record the evidence package.

### DevOps Assembly Steps

1. Wire frontend quality checks into Nebula's lifecycle gates at the appropriate stages.
2. Preserve a repeatable containerized execution path for frontend validation.
3. Record lifecycle-gate and command output in the standard evidence package.

### Dependency Order

```text
Step 0 (Architect):  feature scope + tracker sync + solution-side gate plan
Step 1 (Frontend):   scripts/config + shared harness + representative backfill tests
Step 2 (QE):         full frontend validation run + evidence capture
Step 2 (DevOps):     lifecycle gate activation + repeatable runtime execution proof
```

### Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Host `node_modules` / optional native package instability on mounted workspace | High | Keep containerized frontend runtime first-class for proof and CI parity | DevOps + QE |
| Pre-existing auth/frontend test failures reduce confidence in the initial baseline | High | Backfill/stabilize critical auth and dashboard slices before broadening scope | Frontend + QE |
| Visual smoke remains treated as sufficient proof by habit | Medium | Require distinct component/integration/coverage evidence in lifecycle/signoff | QE + Code Review |
| Framework/solution responsibilities blur during implementation | Medium | Keep generic agent-framework updates separate from F0015 solution changes | Architect |

### Signoff Role Matrix

| Role | Required | Rationale |
|------|----------|-----------|
| Quality Engineer | Yes | Validate full frontend run and evidence quality |
| Code Reviewer | Yes | Validate test adequacy and gate behavior |
| DevOps | Yes | Validate runtime execution path and lifecycle activation |
| Architect | Yes | Accept solution-side gate architecture and boundary application |
| Security Reviewer | No | No new authz/data-boundary contract is expected in scope |

**Checkpoint F0015-A:** Achieved on 2026-03-21. Nebula frontend validation is enforceable, evidence-backed, and repeatable in the approved runtime path.

---

## F0013 — Dashboard Framed Storytelling Canvas

**Updated:** 2026-03-14 — Architecture review complete; backend scope confirmed (LOB, SLA, breakdown endpoint — ADR-009)

### Dependencies

**Carries forward from F0012 (all implemented and deployed):**
- `periodDays` parameter on `GET /dashboard/kpis`
- `avgDwellDays` + `emphasis` on `OpportunityFlowNode` (flow endpoint)
- `GET /dashboard/opportunities/aging` — per-status aging bucket matrix (implemented; missing from OpenAPI spec — backfill in Step F)
- `GET /dashboard/opportunities/hierarchy` — entity type → color group → status tree (implemented; missing from OpenAPI spec — backfill in Step F)

**Carries forward from F0010 (all implemented and deployed):**
- `GET /dashboard/opportunities` — stage summary counts
- `GET /dashboard/opportunities/flow` — flow nodes and links
- `GET /dashboard/opportunities/outcomes` — terminal outcome aggregates
- `GET /dashboard/opportunities/{entityType}/{status}/items` — mini-card drilldowns
- `GET /dashboard/opportunities/outcomes/{outcomeKey}/items` — outcome mini-card drilldowns

**Existing infrastructure (unchanged):**
- ABAC policy: `dashboard_kpi`, `dashboard_pipeline`, `dashboard_nudge` — no changes
- `GET /dashboard/nudges`, `GET /my/tasks`, `GET /my/timeline` — unchanged
- TanStack Query hooks in `experience/src/features/opportunities/hooks/` — 7 hooks, all carry forward
- TypeScript DTOs in `experience/src/features/opportunities/types.ts` — carry forward, extend

**New backend dependencies (ADR-009):**
- `LineOfBusiness` field on Submission and Renewal entities (EF Core migration)
- `WorkflowSlaThreshold` entity and seed data (EF Core migration)
- `GET /dashboard/opportunities/{entityType}/{status}/breakdown` — new endpoint
- Aging endpoint enhancement: SLA band computation per status

### Architecture Notes

#### What F0012 Got Wrong and F0013 Corrects

F0012 over-interpreted "infographic" as "flatten everything," stripping borders, depth, and glow from ALL dashboard components — including operational panels (nudges, activity, tasks) that need those identity elements. F0013 restores a three-layer visual hierarchy: app chrome as frame, operational panels with glass-card depth, and story canvas zone as the flat infographic surface.

| F0012 Concept | F0013 Change |
|---|---|
| Flat canvas for everything | Three-layer hierarchy: frame + glass-card operational panels + flat story zone |
| Horizontal connected flow | Vertical timeline, stops alternating left-right |
| 5 chapters (Flow/Friction/Outcomes/Aging/Mix) | 3 chapters (Flow/Friction/Outcomes); Aging/Mix become per-stop alternates |
| Lazy-load Aging/Mix on chapter switch | All chapter data eager; breakdown data lazy per-stop |
| Violet/fuchsia palette | Muted coral/steel blue editorial palette |
| Activity/Tasks as flat stacked sections | Activity/Tasks restored to glass-card raised panels |

#### Backend Changes

**1. Line of Business Classification (ADR-009)**

Add `LineOfBusiness` (string, nullable) to both Submission and Renewal entities. Nullable for backward compatibility — existing records retain `null`.

Known values (commercial P&C NAIC/ISO standard):

| Value | Display Label |
|---|---|
| `Property` | Property |
| `GeneralLiability` | General Liability |
| `CommercialAuto` | Commercial Auto |
| `WorkersCompensation` | Workers' Compensation |
| `ProfessionalLiability` | Professional Liability / E&O |
| `Marine` | Marine / Inland Marine |
| `Umbrella` | Umbrella / Excess |
| `Surety` | Surety / Bond |
| `Cyber` | Cyber Liability |
| `DirectorsOfficers` | Directors & Officers |

Stored as string (not DB enum) for extensibility. Validated at API layer against the known set. Requires EF Core migration, DTO updates, OpenAPI spec update, JSON schema updates, and seed data population.

**2. SLA Configuration Table (ADR-009)**

New entity `WorkflowSlaThreshold` with per-entity-type, per-status SLA thresholds.

| Field | Type | Description |
|---|---|---|
| `Id` | uuid (PK) | |
| `EntityType` | string | `submission` or `renewal` |
| `Status` | string | Workflow status |
| `WarningDays` | int | Days threshold for "approaching" state |
| `TargetDays` | int | SLA target — breach if exceeded |
| `CreatedAt` | datetime | Audit |
| `UpdatedAt` | datetime | Audit |

Constraint: `(EntityType, Status)` unique. `WarningDays < TargetDays`.

SLA band computation:
- **On time:** dwell ≤ `WarningDays`
- **Approaching:** `WarningDays` < dwell ≤ `TargetDays`
- **Overdue:** dwell > `TargetDays`

Seed defaults (submission):

| Status | WarningDays | TargetDays | Rationale |
|---|---|---|---|
| Received | 1 | 2 | Fast intake expected |
| Triaging | 2 | 5 | Triage within a week |
| WaitingOnBroker | 5 | 10 | External dependency, more tolerance |
| ReadyForUWReview | 3 | 7 | Queue shouldn't stall |
| InReview | 5 | 14 | Complex analysis |
| Quoted | 7 | 21 | Decision window |
| BindRequested | 2 | 5 | Final step, should be quick |

Seed defaults (renewal — same statuses, slightly relaxed where applicable):

| Status | WarningDays | TargetDays |
|---|---|---|
| Received | 1 | 3 |
| Triaging | 2 | 5 |
| WaitingOnBroker | 5 | 10 |
| ReadyForUWReview | 3 | 7 |
| InReview | 5 | 14 |
| Quoted | 7 | 21 |
| BindRequested | 2 | 5 |

No CRUD API at MVP — seed-only via migration.

**3. Per-Stage Breakdown Endpoint (new)**

```
GET /dashboard/opportunities/{entityType}/{status}/breakdown
  ?groupBy=assignedUser|broker|program|lineOfBusiness|brokerState
  &periodDays=180
```

Response shape:
```json
{
  "entityType": "submission",
  "status": "Received",
  "groupBy": "lineOfBusiness",
  "periodDays": 180,
  "groups": [
    { "key": "Property", "label": "Property", "count": 42 },
    { "key": "GeneralLiability", "label": "General Liability", "count": 28 },
    { "key": null, "label": "Unknown", "count": 3 }
  ],
  "total": 73
}
```

Authorization: `dashboard_pipeline` (same as existing stage endpoints).

groupBy join paths:
- `assignedUser`: Submission/Renewal → UserProfile (display name)
- `broker`: Submission/Renewal → Broker (legal name)
- `program`: Submission/Renewal → Program (name)
- `lineOfBusiness`: Submission/Renewal `.LineOfBusiness`
- `brokerState`: Submission/Renewal → Broker `.State`

Null groups: items where the groupBy field is null are collected into `{ key: null, label: "Unknown" }`.

**4. Aging Endpoint Enhancement (SLA bands)**

Extend `GET /dashboard/opportunities/aging` response to include SLA context per status:

```json
{
  "statuses": [{
    "status": "Received",
    "label": "Received",
    "colorGroup": "intake",
    "displayOrder": 1,
    "sla": {
      "warningDays": 1,
      "targetDays": 2,
      "onTimeCount": 15,
      "approachingCount": 3,
      "overdueCount": 1
    },
    "buckets": [
      { "key": "0-2", "label": "0–2 days", "count": 8 },
      ...
    ],
    "total": 19
  }]
}
```

The `sla` object is pre-computed server-side from `WorkflowSlaThreshold`. Existing `buckets` remain for the aging heatmap. Frontend uses `sla` for SLA gauge visualizations at each stage.

**5. OpenAPI Spec Backfill**

The aging and hierarchy endpoints are implemented in backend code but missing from `nebula-api.yaml`. Backfill both endpoint definitions and response schemas into the spec as part of this feature.

#### Frontend Changes

**Component Decomposition:**

```
experience/src/features/opportunities/components/
├── StoryCanvas.tsx                     ← MODIFY: vertical timeline; reduce chapters to 3
├── VerticalTimeline.tsx                ← NEW: SVG vertical spine with alternating stage nodes
├── TimelineStageNode.tsx              ← NEW: individual node (mini-vis + callout areas)
├── MiniVisualization.tsx              ← NEW: contextual chart dispatcher per stage
├── NarrativeCallout.tsx               ← NEW: 2-3 data-driven bullet points per stage
├── TerminalOutcomesBranch.tsx         ← NEW: terminal branches at timeline bottom
├── ChapterOverlayManager.tsx          ← MODIFY: reduce to Flow/Friction/Outcomes only
├── overlays/
│   ├── FrictionOverlay.tsx            ← KEEP: dwell time donuts + emphasis rings
│   └── OutcomesOverlay.tsx            ← KEEP: terminal branch highlighting
├── charts/                             ← NEW directory: SVG mini-chart components
│   ├── IconGridChart.tsx              ← Waffle chart (LOB icons at Received)
│   ├── SlaGaugeChart.tsx              ← SLA arc gauge (Triaging, etc.)
│   ├── ProgressRingChart.tsx          ← Progress ring (WaitingOnBroker)
│   ├── DonutChart.tsx                 ← Donut (UW Review, Quoted, Friction uniform)
│   ├── StackedBarChart.tsx            ← Stacked bar (Quote Prep)
│   ├── CountBadge.tsx                 ← Count badge (BindRequested)
│   └── BarChart.tsx                   ← Bar chart (alternates: top brokers, etc.)
├── ConnectedFlow.tsx                   ← REMOVE after VerticalTimeline complete
├── TerminalOutcomesRail.tsx           ← REMOVE after TerminalOutcomesBranch complete
├── overlays/AgingOverlay.tsx          ← REMOVE (aging is now per-stop alternate)
└── overlays/MixOverlay.tsx            ← REMOVE (mix is now per-stop alternate)
```

**Data Loading Strategy:**

| Data | When Loaded | Hook | Budget |
|------|-------------|------|--------|
| Nudges | Eager (mount) | `useDashboardNudges()` | Parallel |
| KPIs | Eager (mount) | `useDashboardKpis(periodDays)` | Parallel |
| Flow + Friction | Eager (mount) | `useOpportunityFlow(entityType, periodDays)` | Parallel |
| Outcomes | Eager (mount) | `useOpportunityOutcomes(periodDays)` | Parallel |
| Aging + SLA | Eager (mount) | `useOpportunityAging(entityType, periodDays)` | Parallel |
| Tasks | Eager (mount) | `useMyTasks()` | Parallel (deferred render) |
| Activity | Eager (mount) | `useTimelineEvents(...)` | Parallel (deferred render) |
| Breakdown | **Lazy** (per-stop toggle) | `useOpportunityBreakdown(entityType, status, groupBy, periodDays, { enabled })` | <250ms on toggle |

Initial load: 7 parallel queries. Breakdown requests lazy-load on per-stop alternate toggle.

### Backend Assembly Steps

1. **(A) EF Migration: Add LineOfBusiness to Submission and Renewal**
   - Add nullable `string? LineOfBusiness` to `Submission` and `Renewal` entities
   - Property configuration: `maxLength: 50`
   - Generate and apply EF Core migration

2. **(B) EF Migration: Create WorkflowSlaThreshold table**
   - New entity with `(EntityType, Status)` unique constraint
   - Seed data for submission and renewal statuses per tables above
   - Generate and apply EF Core migration

3. **(C) Update Submission/Renewal DTOs and Request Schemas**
   - Add `LineOfBusiness` to DTOs, create requests, and update requests
   - API-layer validation: if provided, must be one of the 10 known values; null allowed
   - Update JSON schemas in `planning-mds/schemas/`

4. **(D) Implement Breakdown Endpoint**
   - Add `OpportunityBreakdownDto`, `OpportunityBreakdownGroupDto` DTOs
   - Add `GetOpportunityBreakdownAsync(entityType, status, groupBy, periodDays)` to `IDashboardRepository` + `DashboardRepository`
   - Implement groupBy join logic for all 5 dimensions (assignedUser, broker, program, lineOfBusiness, brokerState)
   - Add to `DashboardService`
   - Register `GET /dashboard/opportunities/{entityType}/{status}/breakdown` in `DashboardEndpoints.cs`
   - Authorization: `dashboard_pipeline`

5. **(E) Enhance Aging Endpoint with SLA Bands**
   - Add `WorkflowSlaThresholdDto` and `SlaStatusBandsDto` DTOs
   - Modify `GetOpportunityAgingAsync` to join `WorkflowSlaThreshold` and compute onTime/approaching/overdue counts per status
   - Add `sla` object to `OpportunityAgingStatusDto` response

6. **(F) Update OpenAPI Spec**
   - Backfill aging endpoint definition + response schema (existing tech debt)
   - Backfill hierarchy endpoint definition + response schema (existing tech debt)
   - Add breakdown endpoint definition + response schema
   - Add `lineOfBusiness` to Submission/Renewal schemas
   - Add `sla` to aging response schema
   - Update JSON schemas in `planning-mds/schemas/`

7. **(G) Update Dev Seed Data**
   - Add `LineOfBusiness` values to existing test submissions and renewals
   - Distribute across LOB types for realistic visualization testing

8. **(H) Unit Tests**
   - Breakdown groupBy logic for all 5 dimensions
   - SLA band computation (onTime/approaching/overdue boundary cases: exactly at warning, exactly at target, below, above)
   - LOB validation (valid values, null, invalid)

9. **(I) Integration Tests**
   - Breakdown endpoint: 200 OK for each groupBy value, 400 for invalid groupBy, 401, 403
   - Enhanced aging: SLA bands present in response, correct band assignment
   - LOB on create/update submission and renewal

### Frontend Assembly Steps

1. **(J) S0000: Editorial Palette Refresh**
   - Update CSS custom properties in `globals.css` (dark + light themes)
   - Update glass-card, glow, gradient utilities to coral/steel-blue
   - Add data visualization palette tokens (6 semantic colors: `--data-primary` through `--data-danger`)
   - Replace all hardcoded violet/fuchsia hex values in components with CSS custom properties
   - Verify WCAG AA contrast for all text/background pairs in both themes

2. **(K) S0001: Three-Layer Visual Hierarchy**
   - Restore `glass-card` + depth + glow on NudgeCardsSection, ActivityFeed, MyTasksPanel
   - Ensure story canvas zone (KPIs, flow, chapters) remains flat/borderless
   - Verify three-layer distinction in both dark and light themes

3. **(L) S0002: Vertical Timeline**
   - Create `VerticalTimeline.tsx` — SVG vertical spine with alternating left-right nodes
   - Create `TimelineStageNode.tsx` — individual node with mini-vis area + callout area
   - Create `TerminalOutcomesBranch.tsx` — fan-out branches at bottom
   - Flow ribbons along spine (thickness proportional to transition count)
   - Color progression follows `colorGroup` (warm-to-cool top-to-bottom)
   - Mini-visualization area scales proportionally with stage item count
   - Keyboard navigation (tab order follows displayOrder top-to-bottom)
   - Wire into `StoryCanvas.tsx` replacing `ConnectedFlow.tsx`

4. **(M) Add Breakdown Hook + Update Types**
   - Create `useOpportunityBreakdown.ts` — TanStack Query hook with `enabled` flag for lazy loading
   - Add breakdown DTOs to `types.ts`
   - Add SLA band types to aging DTOs in `types.ts`
   - Update `useOpportunityAging` return type to include SLA data

5. **(N) S0003: Mini-Visualizations + Callouts + Alternates**
   - Create SVG chart components: `IconGridChart`, `SlaGaugeChart`, `ProgressRingChart`, `DonutChart`, `StackedBarChart`, `CountBadge`, `BarChart`
   - Create `MiniVisualization.tsx` — dispatcher rendering the right chart per stage mapping
   - Create `NarrativeCallout.tsx` — 2-3 dynamically-computed bullet points per stage
   - Implement per-stop alternate toggle (dot/chevron indicator, 150ms crossfade)
   - Wire breakdown data into alternates needing groupBy data (LOB, broker, UW, program, brokerState)
   - Wire SLA band data from aging endpoint into SLA gauge at Triaging (and other stages)
   - Graceful degradation: count < 3 → count badge fallback; null LOB → "Unknown" group; missing data → skip alternate in cycle
   - Geographic alternates: use `brokerState` groupBy, render as simplified regional dot map or bar-by-state

6. **(O) S0004: Chapter Controls (3 chapters)**
   - Modify `StoryCanvas.tsx` chapter state to 3 values: `flow | friction | outcomes`
   - Modify `ChapterOverlayManager.tsx` to remove Aging/Mix overlay imports
   - Remove `AgingOverlay.tsx` and `MixOverlay.tsx`
   - Flow: contextual defaults + per-stop alternate toggles visible
   - Friction: all stops → uniform dwell-band donuts + emphasis rings; per-stop toggles hidden
   - Outcomes: terminal branches glow, stage visuals dim; per-stop toggles hidden
   - Switching back to Flow restores last-selected per-stop alternate (React state)

7. **(P) S0005: Responsive/Accessibility/Performance**
   - 4 breakpoints: desktop (1280+), tablet landscape (1024), tablet portrait (768), phone (375)
   - Desktop: vertical timeline full width, all nodes visible
   - Phone: timeline stays vertical (natural fit); radial popovers as bottom-sheets; chapter controls as scrollable pill group
   - Keyboard: Tab → chapter controls → timeline stage nodes → per-stop toggle → next node
   - ARIA: `role="tablist"` on chapters, `aria-label` on all chart SVGs, callout text readable by screen reader
   - `prefers-reduced-motion`: disable glow pulse animations, retain static shadows
   - Performance: LCP < 2.5s, timeline SVG < 200ms, chapter switch < 150ms, max 7 parallel mount queries

8. **(Q) Cleanup**
   - Remove `ConnectedFlow.tsx`, `TerminalOutcomesRail.tsx`
   - Remove `AgingOverlay.tsx`, `MixOverlay.tsx`

9. **(R) Tests**
   - Component tests for all new chart components (icon grid, gauge, donut, bar, etc.)
   - StoryCanvas integration: chapter switching, period sync
   - VerticalTimeline: node rendering, ribbon proportions, keyboard nav
   - MiniVisualization: stage-to-chart mapping, fallback/degradation
   - NarrativeCallout: dynamic bullet generation from data
   - Run lint, build, test, lint:theme

### QA Assembly Steps

1. **(S) Test Plan** — story-to-test mapping for all 6 stories
2. **(T) Backend Tests** — breakdown groupBy dimensions, SLA band boundaries, LOB validation, ABAC scoping
3. **(U) Frontend E2E**
   - Editorial palette in both themes
   - Three-layer visual hierarchy (glass-card on operational panels, flat on story zone)
   - Vertical timeline with correct stage ordering and alternating layout
   - Mini-visualizations render inline with correct chart types per stage
   - Narrative callouts display data-driven bullets
   - Per-stop alternate toggle cycles through views (breakdown data loads on toggle)
   - Chapter switching (Flow/Friction/Outcomes) changes all visuals uniformly
   - Per-stop toggles hidden during Friction/Outcomes, restored on Flow
   - Terminal outcome branches at timeline bottom
   - Period sync across all data sources
4. **(V) Responsive/Accessibility E2E**
   - All 4 breakpoints render correctly
   - Rail collapse states (4 combinations)
   - Keyboard navigation through timeline and chapters
   - Screen reader announces stage label + count
   - `prefers-reduced-motion` disables animations
5. **(W) Performance Validation** — LCP, SVG render, chapter switch, mount query count, breakdown lazy-load timing

### DevOps Assembly Steps

1. **(X) Migration Verification** — EF Core migrations (LOB + SLA) apply cleanly in containers; seed data populates
2. **(Y) Runtime Smoke** — dashboard loads with seeded LOB/SLA data; all endpoints respond; no new env vars or infra

### Dependency Order

```
Step 0 (Backend):   (A) LOB migration + (B) SLA migration [parallel, independent]
Step 1 (Backend):   (C) DTO/schema updates → (D) Breakdown endpoint → (E) Aging SLA enhancement [sequential]
Step 1 (Backend):   (F) OpenAPI spec + (G) seed data [parallel with Step 1]
Step 2 (Backend):   (H) unit tests + (I) integration tests

Step 0 (Frontend):  (J) S0000 editorial palette [independent — can start immediately]
Step 1 (Frontend):  (K) S0001 visual hierarchy [depends on J]
Step 2 (Frontend):  (L) S0002 vertical timeline [depends on K]
Step 2 (Frontend):  (M) breakdown hook + types [depends on backend D]
Step 3 (Frontend):  (N) S0003 mini-vis + callouts [depends on L + backend D + backend E]
Step 4 (Frontend):  (O) S0004 chapter controls [depends on N]
Step 5 (Frontend):  (P) S0005 responsive/a11y [depends on O]
Step 6 (Frontend):  (Q) cleanup + (R) tests

Step 3 (QA):        (S–W) [depends on all implementation]
Step 3 (DevOps):    (X–Y) [depends on backend migrations]
```

Backend Step 0–1 and Frontend Step 0–1 proceed in parallel. Frontend S0003+ depends on backend breakdown + aging SLA endpoints.

### Integration Checklist

- [ ] API contract: breakdown endpoint added to OpenAPI spec
- [ ] API contract: aging endpoint backfilled + SLA bands schema added
- [ ] API contract: hierarchy endpoint backfilled
- [ ] API contract: `lineOfBusiness` added to Submission/Renewal schemas
- [ ] JSON schemas created/updated: breakdown response, aging SLA bands, LOB on create/update
- [ ] Frontend types: breakdown DTO, SLA bands on aging DTO, LOB on submission/renewal
- [ ] Frontend hooks: `useOpportunityBreakdown`, enhanced `useOpportunityAging`
- [ ] Component decomposition: VerticalTimeline, TimelineStageNode, MiniVisualization, NarrativeCallout, 7 chart components
- [ ] Chapter reduction: 5 → 3 with removal of AgingOverlay + MixOverlay
- [ ] Test cases mapped to all 6 story acceptance criteria
- [ ] Run/deploy instructions updated in F0013 GETTING-STARTED.md
- [ ] ADR-009 written and filed

### Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| LOB migration + existing null data | High | Nullable field; seed data pre-populated; icon grid groups nulls as "Unknown" | Backend Dev |
| Breakdown endpoint query performance (GROUP BY across large tables) | Medium | Index on `(CurrentStatus, EntityType)` already exists for other queries; add composite index if profiling warrants | Backend Dev |
| SVG mini-chart rendering 7–10 charts simultaneously | Medium | SVG is lightweight; profile against LCP budget; virtualize off-screen stages if needed | Frontend Dev |
| Multiple interaction modes (per-stop toggles + chapter override) | Medium | Chapter override hides per-stop toggles — modes mutually exclusive; clear UI labeling | Frontend Dev + QE |
| Aging SLA thresholds require migration to change | Low | Seed-only at MVP; values are stable defaults; admin UI deferred | Architect |
| OpenAPI spec drift (aging/hierarchy not in spec) | Low | Backfill as part of Step F; resolves existing tech debt | Backend Dev |

### Signoff Role Matrix

| Role | Required | Rationale |
|------|----------|-----------|
| Quality Engineer | Yes | 6-story acceptance criteria, responsive/a11y, performance budgets |
| Code Reviewer | Yes | New entity, new endpoint, SVG component decomposition |
| Security Reviewer | Yes | Breakdown endpoint authorization, LOB data exposure |
| DevOps | Yes | EF Core migrations (LOB + SLA table), seed data |
| Architect | No | Patterns documented in ADR-009; no architecture exceptions |

**Checkpoint F0013-A:** Framed storytelling canvas dashboard with vertical timeline, contextual mini-visualizations (LOB icon grid, SLA gauge, etc.), per-stop alternates backed by breakdown endpoint, 3-chapter controls, and editorial palette. Backend LOB, SLA, and breakdown data serving correctly with ABAC scoping.

---

## F0012 — Dashboard Storytelling Infographic Canvas — ARCHIVED

**Updated:** 2026-03-14 — Archived, superseded by F0013

> **ARCHIVED:** F0013 supersedes F0012. F0012's backend contract changes (periodDays on KPIs, avgDwellDays + emphasis on flow nodes) are implemented and carry forward into F0013. F0012's frontend flat-canvas approach is replaced by F0013's three-layer visual hierarchy.

**Original Updated:** 2026-03-13 — Architecture review findings incorporated (H-ARCH-01/02, M-ARCH-01/02/03, L-ARCH-01/02/03)

### Dependencies

- F0010 opportunities refactor baseline (Pipeline Board + Heatmap/Treemap/Sunburst) — **Done**
- Existing dashboard aggregates and read endpoints:
  - `GET /dashboard/kpis` — **requires `periodDays` parameter addition (H-ARCH-01)**
  - `GET /dashboard/opportunities`
  - `GET /dashboard/opportunities/flow` — **requires `avgDwellDays` + `emphasis` DTO extension (H-ARCH-02)**
  - `GET /dashboard/opportunities/outcomes`
  - `GET /dashboard/opportunities/aging`
  - `GET /dashboard/opportunities/hierarchy`
  - `GET /dashboard/opportunities/{entityType}/{status}/items`
  - `GET /dashboard/opportunities/outcomes/{outcomeKey}/items`
  - `GET /dashboard/nudges`
  - `GET /my/tasks`
  - `GET /my/timeline`
- Existing ABAC policy coverage for `dashboard_kpi`, `dashboard_pipeline`, `dashboard_nudge`

### Architecture Notes

**This slice replaces the panelized dashboard with a continuous flat infographic canvas. All F0011 scope (connected flow, terminal outcomes, visual system, mini-view rebalancing) is self-contained here.**

No new domain entities, database migrations, or ABAC policy changes required. Focus is on:
- Flat infographic canvas with no panel borders, card wrappers, or divider lines
- Nudge bar integrated as top canvas section flowing into story controls
- Connected left-to-right opportunity flow with terminal outcome branches (from F0011)
- In-canvas chapter overlays replacing mode-separated visual tabs (chapter overlay composition pattern)
- Activity/My Tasks as flat canvas sections below story content (layout change: side-by-side → stacked full-width)
- Adaptive canvas width behavior with collapsible left nav and right Neuron rail

#### Backend Contract Changes (H-ARCH-01, H-ARCH-02)

**1. KPI Period Synchronization (H-ARCH-01):**
- Add optional `periodDays` query parameter to `GET /dashboard/kpis` (default: 90 for backward compatibility)
- `activeBrokers` and `openSubmissions` remain current counts (not windowed)
- `renewalRate` and `avgTurnaroundDays` become window-aware using the provided `periodDays`
- OpenAPI spec updated: `planning-mds/api/nebula-api.yaml` line ~553

**2. Flow Node Friction Data (H-ARCH-02):**
- Extend `OpportunityFlowNodeDto` with two optional fields:
  - `avgDwellDays: double?` — average calendar days items currently spend in this status (from WorkflowTransition dwell computation)
  - `emphasis: string?` — server-computed hint: `bottleneck` (highest count), `blocked` (highest dwell), `active` (rightmost non-zero), `normal` (default)
- Emphasis computation moves from frontend (`OpportunitiesSummary.tsx` currently derives blocked/active) to backend (authoritative source)
- OpenAPI spec updated: `planning-mds/api/nebula-api.yaml` OpportunityFlowNode schema
- JSON Schema created: `planning-mds/schemas/opportunity-flow-node.schema.json`

#### Frontend Scope

##### Component Decomposition (M-ARCH-01)

The current `OpportunitiesSummary.tsx` is monolithic (handles Pipeline/Heatmap/Treemap/Sunburst tabs). Decompose into:

```
experience/src/features/opportunities/components/
├── StoryCanvas.tsx                 ← Shell: period + chapter state, canvas surface container
├── ConnectedFlow.tsx               ← SVG connected left-to-right flow rendering
├── TerminalOutcomesRail.tsx        ← Outcome branch nodes (refactor from existing OpportunityOutcomesRail.tsx)
├── ChapterOverlayManager.tsx       ← Overlay layer switcher (renders active overlay based on chapter state)
├── overlays/
│   ├── FrictionOverlay.tsx         ← Dwell time / bottleneck annotations on flow nodes
│   ├── OutcomesOverlay.tsx         ← Terminal outcome emphasis mode (branch path highlighting)
│   ├── AgingOverlay.tsx            ← Heat intensity blocks on stage nodes (from existing OpportunityHeatmap.tsx)
│   └── MixOverlay.tsx              ← Composition blocks + radial inset (from existing Treemap + Sunburst)
└── [existing components preserved for reference during migration]
```

Each overlay is independently testable and can be lazy-loaded per M-ARCH-02 below.

**Dashboard-level layout changes:**
- `DashboardPage.tsx`: Replace 2-column `xl:grid-cols-[1fr_18.5rem]` grid for Activity/Tasks with stacked full-width sections (L-ARCH-03: deliberate layout change from side-by-side to stacked — reduces above-fold density on desktop but aligns with infographic narrative flow)
- `KpiCardsRow.tsx`: Restyle from bordered cards to inline flat band (strip `glass-card`, use `canvas-section` spacing)
- `NudgeCardsSection.tsx`: Restyle from bordered cards to flat nudge zone (strip card borders, remove gap between nudges and story controls)

##### Chapter Data Loading Strategy (M-ARCH-02)

Use hybrid eager/lazy loading aligned with ADR-002 per-widget pattern:

| Data | When Loaded | Hook Pattern | Budget |
|------|-------------|--------------|--------|
| Nudges | Eager (mount) | `useDashboardNudges()` | Parallel with others |
| KPIs | Eager (mount) | `useDashboardKpis(periodDays)` | Parallel with others |
| Flow + Friction | Eager (mount) | `useOpportunityFlow(entityType, periodDays)` | Parallel with others |
| Outcomes | Eager (mount) | `useOpportunityOutcomes(periodDays)` | Parallel with others |
| Aging | **Lazy** (chapter switch) | `useOpportunityAging(entityType, periodDays, { enabled: chapter === 'aging' })` | <250ms on switch |
| Hierarchy (Mix) | **Lazy** (chapter switch) | `useOpportunityHierarchy(periodDays, { enabled: chapter === 'mix' })` | <250ms on switch |
| Tasks | Eager (mount) | `useMyTasks()` | Deferred render (below fold) |
| Activity | Eager (mount) | `useTimelineEvents(...)` | Deferred render (below fold) |

Initial load fires 6 parallel queries (nudges + KPIs + flow + outcomes + tasks + activity) — within ADR-002's 500ms budget. Chapter-specific data (aging, hierarchy) lazy-loads on user interaction using TanStack Query `enabled` flag (pattern from ADR-004 popovers).

##### CSS Architecture for Flat Canvas (L-ARCH-01)

**New CSS utilities:**
- `canvas-section` — spacing-only section separation (no border, no elevation, no backdrop-blur)
- `canvas-zone-tight` — reduced vertical spacing for content zones that flow together (nudge → controls)
- `canvas-zone-break` — standard vertical spacing for content zone separation (story → activity → tasks)

**Token additions:** See `planning-mds/screens/design-tokens.md` for new canvas spacing tokens.

**Scope restrictions:**
- Do NOT remove `glass-card`, `surface-card`, `content-inset` globally — they are used in non-dashboard contexts (Brokers page, forms, modals)
- Only dashboard components should stop using panel-style classes and switch to `canvas-section`
- Theme tokens (`--surface-card`, `--border-default`) remain available for non-dashboard use

##### Nudge Bar Session Behavior

Nudge dismissals remain **session-scoped** per ADR-004 (React state, not persisted). The screen spec `S-DASH-001` documents this behavior.

### Backend Assembly Steps

1. **Add `periodDays` parameter to `GET /dashboard/kpis`** (H-ARCH-01): Update endpoint, service, and repository to accept optional period parameter (default 90). `activeBrokers` and `openSubmissions` remain current. `renewalRate` and `avgTurnaroundDays` use the provided window.
2. **Extend `OpportunityFlowNodeDto`** (H-ARCH-02): Add `avgDwellDays` (computed from WorkflowTransitions for entities currently in each status) and `emphasis` (computed from count concentration and dwell time ranking).
3. Validate existing aggregates support period-synchronized canvas rendering.
4. Add unit tests for emphasis computation logic (bottleneck/blocked/active/normal assignment).
5. Add unit tests for KPI window-aware computation.
6. Add integration tests for read authorization and partial-failure handling across all dashboard endpoints.

### Frontend Assembly Steps

1. **Create `canvas-section` CSS utilities** (L-ARCH-01): Add `canvas-section`, `canvas-zone-tight`, `canvas-zone-break` to `index.css`. Do not modify existing panel classes used elsewhere.
2. **Implement flat infographic canvas shell** in `DashboardPage.tsx`: Replace stacked panel layout with flat canvas container. Strip `glass-card` / `surface-card` from dashboard components only.
3. **Restyle NudgeCardsSection**: Remove card borders, apply `canvas-zone-tight` between nudge zone and story controls.
4. **Restyle KpiCardsRow**: Remove card borders, render as inline flat band with `canvas-section` spacing.
5. **Decompose OpportunitiesSummary** (M-ARCH-01): Create `StoryCanvas.tsx`, `ConnectedFlow.tsx`, `ChapterOverlayManager.tsx`, and individual overlay components per decomposition plan above.
6. **Build ConnectedFlow**: SVG left-to-right connected flow with ribbon links. Use `displayOrder` for deterministic stage ordering, `colorGroup` for warm-to-cool progression.
7. **Build chapter controls**: `[Flow] [Friction] [Outcomes] [Aging] [Mix]` selector. Default: Flow. State: React `useState`.
8. **Build overlay components**: FrictionOverlay (uses `avgDwellDays`/`emphasis` from flow nodes), OutcomesOverlay (emphasizes terminal branches), AgingOverlay (refactored from existing Heatmap), MixOverlay (refactored from existing Treemap + Sunburst).
9. **Implement lazy-load pattern** (M-ARCH-02): Aging and hierarchy queries use `enabled: chapter === 'aging'|'mix'` respectively.
10. **Reposition Activity/Tasks** (L-ARCH-03): Remove 2-column grid. Render as stacked full-width flat canvas sections with `canvas-zone-break` spacing. Strip panel borders.
11. **Update `useDashboardKpis` hook**: Accept and pass `periodDays` parameter.
12. **Implement rail-aware adaptive width**: Use existing CSS custom properties (`--sidebar-width`, `--chat-panel-width`) for canvas width calculation.
13. **Validate responsive behavior**: desktop (1280+), tablet landscape (1024), tablet portrait (768), phone (375).
14. **Visual regression check**: Confirm zero panel borders, card wrappers, or divider lines in rendered output across all breakpoints and rail states.
15. Add component/integration tests and run lint/build/test gates.

### QA Assembly Steps

1. Build test plan with story-to-test mapping across narrative and operational layers.
2. E2E: flat canvas render with nudge zone, KPI band, connected flow, terminal outcomes — no panel borders visible.
3. E2E: period switch synchronizes all canvas data (KPIs, flow, outcomes).
4. E2E: chapter switching and overlay fallback behaviors (including lazy-load timing).
5. E2E: left/right rail collapse combinations and width adaptation (4 states: both expanded, left collapsed, right collapsed, both collapsed).
6. E2E: Activity/My Tasks stacked sections below canvas with action handoff.
7. Validate keyboard/screen-reader flows and reduced-motion behavior.
8. **Visual regression**: No-panel-border validation across MacBook, iPad, iPhone snapshots with all rail-state variants.

### DevOps Assembly Steps

1. Confirm no new runtime services or environment variable contracts are introduced.
2. Run backend/frontend runtime smoke checks for dashboard startup and route stability.
3. Capture deployability evidence and record any runtime deviations.

### Dependency Order

```
Step 0 (Architect):  OpenAPI + schema updates ✓ (done in planning)
Step 1 (Backend):    KPI periodDays → flow DTO extension → emphasis computation → tests
Step 1 (Frontend):   CSS utilities → canvas shell → nudge/KPI restyle → component decomp → connected flow → chapters → overlays → lazy-load → activity/tasks reposition → rail adaptation → responsive → tests
Step 2 (QA):         E2E + accessibility + breakpoint parity + visual regression (no-border check)
Step 2 (DevOps):     runtime smoke checks and evidence capture
```

### Integration Checklist

- [x] API contract touchpoints identified and OpenAPI spec updated
- [x] JSON schemas created for flow response (`opportunity-flow*.schema.json`)
- [x] Frontend contract touchpoints identified
- [x] Frontend component decomposition strategy documented
- [x] Chapter data loading strategy (eager/lazy) documented
- [x] CSS architecture for flat canvas documented
- [ ] AI contract compatibility validated (if in scope) — N/A
- [x] Test cases mapped to acceptance criteria (planning test plan)
- [x] Run/deploy instructions drafted in feature getting-started docs
- [x] Screen specification created (`planning-mds/screens/S-DASH-001-infographic-canvas.md`)

### Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| KPI period synchronization (H-ARCH-01) | High | Add periodDays param with default 90 for backward compat | Backend Dev |
| Friction chapter data gap (H-ARCH-02) | High | Extend OpportunityFlowNodeDto with avgDwellDays + emphasis | Backend Dev |
| OpportunitiesSummary monolith decomposition | Medium | Follow documented component split; preserve existing files during migration | Frontend Dev |
| Story-canvas visual density impacts readability | Medium | Keep numeric labels persistent; enforce contrast/readability checks | Frontend + QE |
| ABAC leakage risk from merged aggregate views | Medium | Re-validate all overlay and label payloads under role scope tests | Backend + Security |
| Rail-collapse and responsive regression risk | Medium | Test all 4 rail-state × 4 breakpoint combinations | Frontend + QE |
| Activity/Tasks side-by-side → stacked layout change | Low | Deliberate UX decision; less above-fold density but better narrative flow | Product + Frontend |
| Chapter naming discoverability | Low | Validate naming in product review; allow content-label iteration | Product |

### Signoff Role Matrix

| Role | Required | Rationale |
|------|----------|-----------|
| Quality Engineer | Yes | Baseline acceptance criteria, cross-device parity, no-panel-border visual regression |
| Code Reviewer | Yes | Baseline independent implementation review, component decomposition validation |
| Security Reviewer | Yes | Aggregate scope and ABAC boundary verification across chapter overlays |
| DevOps | No | No expected infrastructure/env-contract changes |
| Architect | No | No architecture exceptions planned; patterns documented |

**Checkpoint F0012-A:** Flat infographic canvas dashboard (no panel borders visible, connected flow with terminal outcomes, chapter overlays with lazy-load, collapsible rails, flat activity/tasks sections) is implemented and validated with ABAC-preserving drilldowns.

---

## F0011 — Dashboard Opportunities Flow-First Modernization — ABANDONED

**Updated:** 2026-03-14 — Abandoned, superseded by F0012 then F0013

> **ABANDONED:** F0011 was first deprecated in favor of F0012, then F0012 was archived in favor of F0013. F0011 will not be implemented. Its connected flow concept influenced F0012 and F0013 but no F0011-specific code ships.

**Original Updated:** 2026-03-12 — Planning assembly pass

### Dependencies

- F0010 baseline opportunities implementation (Pipeline Board + Heatmap/Treemap/Sunburst)
- Existing opportunities endpoints:
  - `GET /dashboard/opportunities`
  - `GET /dashboard/opportunities/flow`
  - `GET /dashboard/opportunities/{entityType}/{status}/items`
- Existing ABAC policy coverage for `dashboard_pipeline`

### Architecture Notes

**This slice is a dashboard opportunities refactor with additive aggregate contract work.**

No workflow-state taxonomy changes are expected. Focus is on:
- Flow-first connected rendering model
- Terminal outcomes rail aggregates and drilldowns
- Visual hierarchy refresh and responsive interaction parity

#### Backend Scope (Expected)

- Extend opportunities payloads (or add dedicated aggregate endpoint) to support:
  - deterministic stage sequence metadata for connected rendering
  - terminal outcome summary nodes (count, percent, average days to exit)
  - stable drilldown target identifiers for stage and outcome nodes
- Preserve `dashboard_pipeline` authorization behavior for all opportunities routes.

#### Frontend Scope (Expected)

File placement remains under `experience/src/features/opportunities/`.

Primary additions/changes:
- `OpportunitiesSummary.tsx` -> promote connected flow canvas as default
- new flow canvas component(s) for stage node + ribbon rendering
- terminal outcomes rail component
- secondary mini-view strip (aging + radial-inspired summaries)
- responsive simplification for mobile layout (stacked stage cards + bottleneck/outcome list)

### Backend Assembly Steps

1. Define or update DTOs for stage sequence and outcome summaries.
2. Add/extend repository methods to compute terminal outcomes and stage ordering metadata.
3. Add/extend service methods for flow and outcome aggregates.
4. Add/extend dashboard endpoints for outcomes aggregate access.
5. Add unit tests for aggregate calculations and ordering guarantees.
6. Add integration tests for endpoint behavior, authorization, and validation.

### Frontend Assembly Steps

1. Add/update opportunities types for stage/outcome aggregate contracts.
2. Add/update hooks for connected flow and outcomes rail data.
3. Implement connected flow canvas default view.
4. Implement outcomes rail and outcome drilldown interactions.
5. Apply visual system updates (reduced border noise, warm-to-cool rhythm, stage emphasis).
6. Implement secondary mini-view strip and contextual expand interactions.
7. Apply responsive and accessibility behavior across breakpoints.
8. Add component and interaction tests; run lint/build/test gates.

### QA Assembly Steps

1. Create test plan with story-to-test mapping.
2. E2E: flow-default render, period switching, stage/outcome drilldowns.
3. E2E: mini-view expand behavior and return-to-flow context.
4. E2E: breakpoint parity (desktop/tablet/phone) and error isolation.
5. Validate accessibility requirements for keyboard/screen-reader flows.

### DevOps Assembly Steps

1. Confirm no new runtime services or env-var contracts are introduced.
2. Run backend/frontend runtime smoke checks in application runtime containers.
3. Capture deployability evidence and document deviations if discovered.

### Dependency Order

```
Step 1 (Backend):  DTO/contract updates -> repository aggregates -> service -> endpoint -> tests
Step 1 (Frontend): types/hooks -> connected flow -> outcomes rail -> visual system -> mini-views -> responsive/a11y -> tests
Step 2 (QA):       E2E + accessibility validation after backend/frontend merge
Step 2 (DevOps):   deployability smoke checks and evidence capture
```

### Integration Checklist

- [x] API contract touchpoints identified
- [x] Frontend contract touchpoints identified
- [ ] AI contract compatibility validated (if in scope) — N/A
- [x] Test cases mapped to acceptance criteria (planning test plan)
- [x] Run/deploy instructions drafted in feature getting-started docs

### Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Terminal outcome category mapping ambiguity | Medium | Confirm mapping before implementation; provide fallback grouping in contract | Product + Backend |
| Stage emphasis rule source ambiguity | Medium | Decide backend flag vs frontend-derived rule before build | Product + Frontend |
| Breakpoint complexity for connected flow | Medium | Explicitly test desktop/tablet/phone parity in QE plan | Frontend + QE |
| Visual refresh impacts readability | Low | Keep labels/counts first; enforce contrast checks | Frontend + QE |

### Signoff Role Matrix

| Role | Required | Rationale |
|------|----------|-----------|
| Quality Engineer | Yes | Baseline acceptance criteria and cross-device parity coverage |
| Code Reviewer | Yes | Baseline independent implementation review |
| Security Reviewer | Yes | Opportunities aggregate and drilldown authorization verification |
| DevOps | No | No expected infrastructure/env-contract changes |
| Architect | No | No planned architecture exceptions |

**Checkpoint F0011-A:** Connected opportunities flow default + outcomes rail + responsive accessibility parity are implemented and validated with ABAC-preserving drilldowns.

---

## F0010 — Dashboard Opportunities Refactor (Pipeline Board + Insight Views) — FRONTEND SUPERSEDED

**Updated:** 2026-03-14 — Frontend Pipeline Board superseded by F0013 vertical timeline; backend endpoints (aging, hierarchy) carry forward

> **FRONTEND SUPERSEDED:** F0013 replaces the Pipeline Board default view with a vertical timeline. F0010's backend endpoints (`/dashboard/opportunities/aging`, `/dashboard/opportunities/hierarchy`) remain implemented and are consumed by F0013. The Heatmap, Treemap, and Sunburst frontend views from F0010 are replaced by F0013's per-stop alternate visualizations.

**Original Updated:** 2026-03-11 — Detailed implementation assembly plan (F0010 build pass)

### Dependencies
- Existing dashboard opportunities widget shell and period controls (F0001)
- Opportunities summary data (`GET /dashboard/opportunities`)
- Opportunities flow data (`GET /dashboard/opportunities/flow`)
- Opportunities drilldown mini-cards (`GET /dashboard/opportunities/{entityType}/{status}/items`)
- ABAC policy coverage for `dashboard_pipeline` resource

### Architecture Notes

**This is primarily a frontend refactor with two new backend aggregation endpoints.**

No new domain entities, no EF Core migrations, no Casbin policy changes. All data is read-only aggregation from existing Submission/Renewal/WorkflowTransition tables.

#### New Backend Endpoints

**`GET /dashboard/opportunities/aging`** (S0002)
- Query params: `entityType` (submission|renewal, required), `periodDays` (1–730, default 180)
- Returns: status × aging-bucket matrix. Fixed buckets: `0-2`, `3-5`, `6-10`, `11-20`, `21+`
- Response shape:
  ```json
  {
    "entityType": "submission",
    "periodDays": 180,
    "statuses": [{
      "status": "Received", "label": "Received", "colorGroup": "intake",
      "displayOrder": 1,
      "buckets": [
        { "key": "0-2", "label": "0–2 days", "count": 5 },
        { "key": "3-5", "label": "3–5 days", "count": 3 },
        { "key": "6-10", "label": "6–10 days", "count": 8 },
        { "key": "11-20", "label": "11–20 days", "count": 2 },
        { "key": "21+", "label": "21+ days", "count": 1 }
      ],
      "total": 19
    }]
  }
  ```
- Authorization: `dashboard_pipeline` (same as existing)
- Implementation: Query non-terminal entities, compute `daysInStatus` (same logic as mini-card items — last WorkflowTransition to current status, fallback to CreatedAt), group by status × bucket.

**`GET /dashboard/opportunities/hierarchy`** (S0003/S0004)
- Query params: `periodDays` (1–730, default 180)
- Returns: tree-structured data for Treemap and Sunburst views
- Response shape:
  ```json
  {
    "periodDays": 180,
    "root": {
      "id": "root", "label": "All Opportunities", "count": 142,
      "children": [{
        "id": "submission", "label": "Submissions", "count": 85,
        "levelType": "entityType",
        "children": [{
          "id": "submission:intake", "label": "Intake", "count": 20,
          "levelType": "colorGroup", "colorGroup": "intake",
          "children": [{
            "id": "submission:intake:Received", "label": "Received",
            "count": 20, "levelType": "status", "colorGroup": "intake"
          }]
        }]
      }]
    }
  }
  ```
- Authorization: `dashboard_pipeline`
- Implementation: Query non-terminal submissions + renewals, group by EntityType → ColorGroup → Status. Reuses reference status tables for labels and display ordering.

#### Frontend Component Architecture

File placement: All new files in `experience/src/features/opportunities/`.

```
features/opportunities/
├── components/
│   ├── OpportunitiesSummary.tsx       ← MODIFY: add view mode state, render active view
│   ├── OpportunityChart.tsx           ← KEEP (Sankey, remove from default path)
│   ├── OpportunityPipelineBoard.tsx   ← NEW: Pipeline Board default (S0001)
│   ├── OpportunityHeatmap.tsx         ← NEW: Aging Heatmap (S0002)
│   ├── OpportunityTreemap.tsx         ← NEW: Composition Treemap (S0003)
│   ├── OpportunitySunburst.tsx        ← NEW: Hierarchy Sunburst (S0004)
│   ├── OpportunityViewSwitcher.tsx    ← NEW: View mode toggle bar
│   ├── OpportunityDrilldown.tsx       ← NEW: Unified drilldown (S0005)
│   ├── OpportunityPopover.tsx         ← KEEP (reused by drilldown)
│   ├── OpportunityMiniCard.tsx        ← KEEP
│   └── OpportunityPill.tsx            ← KEEP (reuse in Pipeline Board)
├── hooks/
│   ├── useDashboardOpportunities.ts   ← KEEP
│   ├── useOpportunityFlow.ts          ← KEEP
│   ├── useOpportunityItems.ts         ← KEEP
│   ├── useOpportunityAging.ts         ← NEW (S0002)
│   └── useOpportunityHierarchy.ts     ← NEW (S0003/S0004)
├── lib/
│   └── opportunity-colors.ts          ← KEEP
├── types.ts                           ← MODIFY: add new DTOs
└── index.ts                           ← KEEP
```

View mode state: `useState<'pipeline' | 'heatmap' | 'treemap' | 'sunburst'>('pipeline')`.

Responsive strategy (S0005):
- Desktop (≥1280px): Full visualization, side-by-side entity sections
- Tablet (768–1279px): Stacked entity sections, reduced chart heights
- Mobile (<768px): Single entity tab, horizontally scrollable board, compact charts

Accessibility (S0005):
- View switcher: `role="tablist"` / `role="tab"` with `aria-selected`
- SVG visualizations: `aria-label` on containers, text summary fallback
- Keyboard: Tab for view navigation, Enter/Space for target selection, Escape for drilldown close
- `prefers-reduced-motion`: disable chart transitions

Charting approach: Use custom SVG implementations. Heatmap = HTML table with intensity backgrounds. Treemap and Sunburst use `d3-hierarchy` for layout math with custom SVG rendering. Avoid heavy charting libraries.

### Backend Assembly Steps

1. **(A)** Add `OpportunityAgingDto`, `OpportunityAgingStatusDto`, `OpportunityAgingBucketDto` DTOs
2. **(B)** Add `OpportunityHierarchyDto`, `OpportunityHierarchyNodeDto` DTOs
3. **(C)** Add `GetOpportunityAgingAsync(entityType, periodDays)` to `IDashboardRepository` and implement in `DashboardRepository`
4. **(D)** Add `GetOpportunityHierarchyAsync(periodDays)` to `IDashboardRepository` and implement in `DashboardRepository`
5. **(E)** Add `GetOpportunityAgingAsync` and `GetOpportunityHierarchyAsync` to `DashboardService`
6. **(F)** Register `GET /dashboard/opportunities/aging` and `GET /dashboard/opportunities/hierarchy` endpoints in `DashboardEndpoints.cs`
7. **(G)** Add unit tests for aging bucket calculation and hierarchy tree construction
8. **(H)** Add integration tests for both new endpoints (200 OK, 401, 403, validation)

### Frontend Assembly Steps

1. **(I)** Add new types to `types.ts` (aging DTOs, hierarchy DTOs, view mode type)
2. **(J)** Create `useOpportunityAging.ts` and `useOpportunityHierarchy.ts` hooks
3. **(K)** Create `OpportunityViewSwitcher.tsx` — view mode toggle bar
4. **(L)** Create `OpportunityPipelineBoard.tsx` — Pipeline Board default view (S0001)
5. **(M)** Modify `OpportunitiesSummary.tsx` — add view mode state, render active view component
6. **(N)** Create `OpportunityHeatmap.tsx` — Aging Heatmap view (S0002)
7. **(O)** Create `OpportunityTreemap.tsx` — Composition Treemap view (S0003)
8. **(P)** Create `OpportunitySunburst.tsx` — Hierarchy Sunburst view (S0004)
9. **(Q)** Create `OpportunityDrilldown.tsx` — Unified drilldown popover (S0005)
10. **(R)** Apply responsive layout and accessibility across all views (S0005)
11. **(S)** Add component tests for all new components
12. **(T)** Run lint, build, test, lint:theme

### QA Assembly Steps

1. **(U)** Create test plan covering all 5 stories
2. **(V)** E2E test: default Pipeline Board load, period switching
3. **(W)** E2E test: view mode switching (all 4 views)
4. **(X)** E2E test: drilldown from Pipeline Board and Heatmap
5. **(Y)** E2E test: empty/error states
6. **(Z)** Validate ABAC scope preservation

### DevOps Assembly Steps

1. **(AA)** Verify no new infra/env-var dependencies
2. **(AB)** Run backend build + test in container
3. **(AC)** Run frontend build + test
4. **(AD)** Record deployability evidence

### Dependency Order

```
Step 1 (Backend):  (A,B) DTOs → (C,D) Repository [parallel] → (E) Service → (F) Endpoints → (G,H) Tests
Step 1 (Frontend): (I) Types → (J) Hooks → (K) ViewSwitcher → (L) PipelineBoard → (M) Summary refactor
                   → (N) Heatmap [depends on backend C] → (O) Treemap → (P) Sunburst [depends on backend D]
                   → (Q) Drilldown → (R) Responsive/A11y → (S,T) Tests
Step 2 (QA):       (U–Z) [depends on all implementation]
Step 2 (DevOps):   (AA–AD) [depends on all implementation]
```

Backend and Frontend (S0001/view switcher) can proceed in parallel. Frontend S0002-S0004 depend on their backend endpoints.

### Integration Checklist

- [x] API contract compatibility validated — existing endpoints unchanged, new endpoints follow REST patterns
- [x] Frontend contract compatibility validated — new hooks for new endpoints, existing hooks unchanged
- [ ] AI contract compatibility validated (if in scope) — N/A
- [ ] Test cases mapped to acceptance criteria
- [ ] Run/deploy instructions updated

### Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Charting library for Treemap/Sunburst | Medium | Use d3-hierarchy for layout math + custom SVG rendering. Minimal new dependency. | Frontend Developer |
| Aging bucket query performance | Low | Aggregate in DB query. Use same patterns as existing flow query. | Backend Developer |
| Responsive complexity (4 views × 3 breakpoints) | Medium | Test each combination explicitly in E2E. | Frontend Developer + QE |
| Sankey removal surprise | Low | Keep OpportunityChart.tsx in codebase, just not default. Can restore later. | Architect |

### Signoff Role Matrix

| Role | Required | Rationale |
|------|----------|-----------|
| Quality Engineer | Yes | Baseline: acceptance criteria coverage for 5 stories, E2E workflows |
| Code Reviewer | Yes | Baseline: independent implementation review |
| Security Reviewer | Yes | New backend endpoints require authorization verification |
| DevOps | No | No new infra, env vars, or deployment changes |
| Architect | No | Standard patterns, no architecture exceptions |

**Checkpoint F0010-A:** Opportunities widget defaults to Pipeline Board, optional insights render correctly, and drilldowns remain scoped and usable across breakpoints.

---

## F0001 — Dashboard

### Dependencies
- Dashboard endpoints (`/dashboard/*`, `/my/tasks`, `/timeline/events`)
- Task entity + indexes (`planning-mds/architecture/data-model.md`)
- Timeline event query support (ActivityTimelineEvent)
- ABAC enforcement for dashboard queries

### Backend Assembly Steps
1. Implement Task entity + repository (Tasks table, indexes per data-model.md).
2. Implement ActivityTimelineEvent read query with ABAC scoping.
3. Implement dashboard aggregation endpoints:
   - `/dashboard/kpis`
   - `/dashboard/pipeline`
   - `/dashboard/pipeline/{entityType}/{status}/items`
   - `/dashboard/nudges`
   - `/my/tasks`
   - `/timeline/events`
4. Enforce request/response schema validation for dashboard payloads.

### Frontend Assembly Steps
1. Build Dashboard shell and five widgets (KPI, Pipeline, Tasks, Activity Feed, Nudges).
2. Integrate API calls and empty/error states per stories.
3. Ensure role‑aware rendering and degrade gracefully on unavailable widgets.

### QA/Integration
- Validate p95 targets for endpoints.
- Verify ABAC scope filtering across widgets.
- Verify edge cases (empty states, unknown actor, partial data).

**Checkpoint F0001‑A:** Dashboard loads with real data for authorized user.

---

## F0002 — Broker Relationship Management

### Dependencies
- Broker + Contact entities, soft delete rules
- ActivityTimelineEvent write on mutations
- ABAC enforcement per authorization matrix
- Broker/Contact OpenAPI + JSON Schemas

### Backend Assembly Steps
1. Implement Broker CRUD endpoints per OpenAPI (create/read/update/delete).
2. Enforce license immutability + global uniqueness; 409 on conflict.
3. Implement deactivation guard: block broker deactivation if active submissions/renewals exist (409 `active_dependencies_exist`). Implement reactivation endpoint (F0002-S0008) — restore Status to Active; emit BrokerReactivated timeline event; reject already-Active brokers with 409 `already_active`.
4. Implement Contact CRUD endpoints per OpenAPI (list/create/read/update/delete).
5. Enforce required email/phone and validation rules; return ProblemDetails on validation error.
6. Emit ActivityTimelineEvent for broker/contact create/update/delete.
7. Mask broker/contact email/phone on **all** broker and contact API responses (`GET /brokers`, `GET /brokers/{id}`, `GET /contacts`, `GET /contacts/{id}`) when `Broker.Status = Inactive`. Return `null` as the masking sentinel; see Broker and Contact schema descriptions in `nebula-api.yaml`.

### Frontend Assembly Steps
1. Broker List screen with search, filters, and status badges.
2. Broker 360 view with profile, contacts, timeline panel.
3. Contact create/update/delete flows within Broker 360.
4. Edit broker, deactivate broker flows with confirmation and error handling.

### QA/Integration
- Verify license immutability enforcement.
- Verify deactivation guard (`active_dependencies_exist`) when active submissions/renewals exist.
- Verify reactivation (F0002-S0008): Active→reject, Inactive→Active, unauthorized→403, not found→404.
- Verify masking behavior for inactive brokers on both list and detail endpoints (brokers and contacts).
- Verify ABAC scope on broker/contact reads and mutations.

**Checkpoint F0002‑A:** Broker 360 flow complete end‑to‑end.

---

## F0009 — Authentication + Role-Based Login

**Updated:** 2026-03-05 — Detailed implementation assembly plan (F0009 build pass)

### Dependencies
- F0005 authentik baseline and claim normalization (complete)
- F0009 implementation contract and broker visibility matrix:
  - `planning-mds/features/F0009-authentication-and-role-based-login/IMPLEMENTATION-CONTRACT.md`
  - `planning-mds/features/F0009-authentication-and-role-based-login/BROKER-VISIBILITY-MATRIX.md`
- BrokerUser matrix rules in `planning-mds/security/authorization-matrix.md` section 2.10
- BrokerUser policy rows in `planning-mds/security/policies/policy.csv`

### Pre-Existing Artifacts (Do Not Re-Implement)

The following are already implemented and correct as of the F0009 planning pass:

| Artifact | Location | Notes |
|----------|----------|-------|
| Auth event bus | `experience/src/features/auth/authEvents.ts` | `session_expired`, `broker_scope_unresolvable` |
| Session teardown hook | `experience/src/features/auth/useSessionTeardown.ts` | §2.1 teardown contract |
| Auth event handler | `experience/src/features/auth/useAuthEventHandler.ts` | Mounted in AppInner |
| OIDC UserManager singleton | `experience/src/features/auth/oidcUserManager.ts` | oidc-client-ts |
| Auth feature index | `experience/src/features/auth/index.ts` | Public surface |
| UnauthorizedPage | `experience/src/pages/UnauthorizedPage.tsx` | reason param support |
| App.tsx auth wiring | `experience/src/App.tsx` | useAuthEventHandler, /unauthorized route |
| API 401/403 interceptor | `experience/src/services/api.ts` | emits auth events |
| Vite auth-mode guard plugin | `experience/vite.config.ts` | §13 build guard |
| authModeGuard unit tests | `experience/src/features/auth/tests/authModeGuard.test.ts` | §13 coverage |
| POST /auth/logout | `engine/src/Nebula.Api/Endpoints/AuthEndpoints.cs` | §2.1 |
| ICurrentUserService.BrokerTenantId | `engine/src/Nebula.Application/Common/ICurrentUserService.cs` | Interface |
| HttpCurrentUserService.BrokerTenantId | `engine/src/Nebula.Api/Services/HttpCurrentUserService.cs` | broker_tenant_id claim |
| policy.csv §2.10 | `planning-mds/security/policies/policy.csv` | BrokerUser policy rows |
| AuditBrokerUserRead helpers | BrokerService, DashboardService, TimelineService, TaskService | Audit logging |

### Backend Assembly Steps

1. **(A) ActivityTimelineEvent.BrokerDescription migration**
   - Add nullable `string? BrokerDescription` to `ActivityTimelineEvent` entity
   - Generate and apply EF Core migration `20260305_F0009_BrokerDescription`
   - Update `ActivityTimelineEventConfiguration.cs` if needed

2. **(B) Broker scope resolution infrastructure**
   - Add `GetIdByBrokerTenantIdAsync(string tenantId)` to `IBrokerRepository` + `BrokerRepository`
   - Create `BrokerScopeUnresolvableException` in `Nebula.Application`
   - Register global exception middleware mapping to `broker_scope_unresolvable` ProblemDetails (§6.1)
   - Create `BrokerScopeResolver` service that reads `ICurrentUserService.BrokerTenantId` and calls the new repo method

3. **(C, D) BrokerService scope + DTO filtering**
   - `ListAsync`: if `user.Roles.Contains("BrokerUser")`, scope query to `BrokerTenantId`-resolved broker only
   - `GetByIdAsync`: verify resolved broker ID matches requested broker ID; throw `BrokerScopeUnresolvableException` if not
   - Create `BrokerBrokerUserDto` (excludes `RowVersion`, `IsDeactivated`) for BrokerUser responses

4. **(E) ContactService BrokerUser scope + DTO**
   - Scope contact reads to broker scope resolved from `BrokerTenantId`
   - Create `ContactBrokerUserDto` (excludes `RowVersion`)

5. **(F, G) TimelineService + BrokerDescription population**
   - `BrokerService` mutations: populate `BrokerDescription` using templates from BROKER-VISIBILITY-MATRIX.md for approved event types
   - `TimelineService.ListEventsAsync` for BrokerUser: filter to approved event types; return `BrokerDescription` instead of `EventDescription` in response DTO

6. **(H) TaskService BrokerUser scope filter**
   - For BrokerUser: filter tasks where `LinkedEntityType='Broker'` AND `LinkedEntityId` = resolved broker ID
   - Return task DTO subset: `id`, `title`, `status`, `priority`, `dueDate`, `linkedEntityType`, `linkedEntityId` (omit `assignedToUserId`, audit timestamps)

7. **(I) DashboardService/Repository nudge BrokerUser scope filter**
   - For BrokerUser: filter nudges to `nudgeType='OverdueTask'` AND `linkedEntityType='Broker'` AND `linkedEntityId IN resolved broker scope`
   - Empty result → return empty array (not 403); 403 only if scope resolution fails

8. **(J) DevSeedData broker tenant mapping**
   - Add seed row linking `broker001@example.local`'s `broker_tenant_id` to an existing test Broker entity

### Frontend Assembly Steps

1. **(K) LoginPage.tsx** at `/login`
   - Sign-in button triggers `oidcUserManager.signinRedirect()` (PKCE)
   - If OIDC config is missing (empty authority/clientId/redirectUri): disable button, show deterministic error
   - If IdP unavailable (signinRedirect throws): show deterministic retry guidance
   - Under `VITE_AUTH_MODE=dev`: redirect to `/` immediately (preserve existing dev workflow)

2. **(L) AuthCallbackPage.tsx** at `/auth/callback`
   - Calls `oidcUserManager.signinRedirectCallback()`
   - On success: resolve role from `nebula_roles` claim, redirect to role landing route
   - On failure (state/nonce/code validation error): clear stale state, redirect to `/login?error=callback_failed`
   - Missing/unsupported `nebula_roles`: redirect to `/unauthorized`
   - BrokerUser without `broker_tenant_id`: redirect to `/unauthorized`

3. **(M) ProtectedRoute component**
   - If no valid OIDC session: redirect to `/login`
   - If session exists but role not in allowedRoles: redirect to `/unauthorized`
   - Renders `<Outlet />` on success

4. **(N) useCurrentUser hook**
   - Reads OIDC user from `oidcUserManager.getUser()`
   - Returns `{ user, roles, isBrokerUser, isAuthenticated }`

5. **(O) api.ts resolveToken update**
   - Branch on `import.meta.env.VITE_AUTH_MODE`:
     - `'oidc'` or unset: `(await oidcUserManager.getUser())?.access_token ?? ''`
     - `'dev'`: `getDevToken()` (existing path unchanged)

6. **(P) App.tsx route wiring**
   - Add `/login` → `<LoginPage />`
   - Add `/auth/callback` → `<AuthCallbackPage />`
   - Wrap protected routes in `<ProtectedRoute>`
   - `/login` and `/auth/callback` are public (no ProtectedRoute wrapper)

### Infra Assembly Steps

1. **(Q) authentik blueprint update** (`docker/authentik/blueprints/nebula-dev.yaml`)
   - Add `BrokerUser` group
   - Add `broker_tenant_id` scope mapping expression
   - Add `lisa.wong@nebula.local` → DistributionUser group
   - Add `john.miller@nebula.local` → Underwriter group
   - Add `broker001@example.local` → BrokerUser group (with `broker_tenant_id` attribute)
   - Add `broker_tenant_id` scope mapping to the OAuth2 provider's property_mappings
   - All entries idempotent (use `identifiers:` correctly)

2. **(R) CI assertion** (`.github/workflows/frontend-ui.yml`)
   - Add step BEFORE `Build frontend` step: assert `VITE_AUTH_MODE != 'dev'` (per §13)

3. **(S) Env templates**
   - `experience/.env.example` — add `VITE_AUTH_MODE=oidc` with comment
   - `experience/.env.staging` — `VITE_AUTH_MODE=oidc`
   - `experience/.env.production` — `VITE_AUTH_MODE=oidc`
   - `experience/.env.development.local.example` — `VITE_AUTH_MODE=dev`

### QA Integration Steps

1. **(T) Test plan document** — `planning-mds/features/F0009-authentication-and-role-based-login/TEST-PLAN.md`
2. **(U) Backend unit tests**: scope resolver, BrokerDescription templates, policy deny
3. **(V) Frontend component tests**: LoginPage error states, AuthCallbackPage failure paths, ProtectedRoute guard behavior
4. **(W) Backend integration tests**: BrokerUser field exclusion, cross-broker deny, timeline event type filter
5. **(X) E2E tests** (Playwright): happy path login for all 3 seeded users, session expiry, 403 in-context

### Dependency Order

```
Step 1 (Backend): (A) migration → (B) scope resolver → (C–I) service layer [parallel]
Step 2 (Backend): (J) DevSeedData
Step 1 (Frontend): (N) useCurrentUser → (K) LoginPage → (L) AuthCallbackPage
                   (O) api.ts → (M) ProtectedRoute → (P) App.tsx wiring [sequential]
Step 1 (Infra):   (Q) blueprint + (R) CI + (S) env templates [parallel, independent]
Step 2 (QA):      (T–X) tests [depends on all above]
```

Backend, Frontend, and Infra steps proceed in parallel.

### QA/Integration Validation (from IMPLEMENTATION-CONTRACT.md §10)

- [ ] Login redirect/callback happy path for `lisa.wong`, `john.miller`, `broker001`
- [ ] Session-expired redirect + stale-state cleanup
- [ ] Route guard: 401 → teardown → `/login`; 403 → in-context error
- [ ] BrokerUser cross-scope denial (list + detail)
- [ ] BrokerUser field filtering (no `InternalOnly` fields in responses)
- [ ] Matrix vs policy parity check for BrokerUser actions
- [ ] Missing/invalid `broker_tenant_id` claim deny

**Checkpoint F0009‑A:** End-to-end login + broker boundary enforcement passes for all required seeded users.

---

## MVP Navigation Constraints

Several F0001 dashboard widgets reference click-through navigation to screens that are not in F0001/F0002 scope. The table below defines which targets are available and how unavailable targets degrade.

### Target Screen Availability

| Target Screen | In Scope? | Source | Notes |
|---------------|-----------|--------|-------|
| Broker 360 | Yes | F0002-S0003 | Fully available for click-through |
| Submission Detail | No | Future feature | Not in F0001/F0002 MVP |
| Renewal Detail | No | Future feature | Not in F0001/F0002 MVP |
| Submission List | No | Future feature | Not in F0001/F0002 MVP |
| Renewal List | No | Future feature | Not in F0001/F0002 MVP |
| Task Center | No | Future (F0003) | Not in F0001/F0002 MVP |

### Degradation Rules

When a navigation target is unavailable, the frontend must degrade gracefully:

1. **Links to unavailable screens render as plain text** — no `<a>` tag, no click handler, no pointer cursor. The entity name or label is still displayed for context but is not interactive.
2. **CTA buttons for unavailable targets are hidden** — if a nudge card's CTA would navigate to an unavailable screen, the CTA button is omitted; the card still displays its title, description, and urgency indicator.
3. **"View all" links to unavailable screens are hidden** — do not render "View all N" when the target list screen does not exist.
4. **No disabled/greyed-out links** — avoid confusing users with interactive-looking elements that do nothing. Omit rather than disable.
5. **No route stubs or placeholder pages** — do not create empty `/submissions` or `/renewals` routes. Routes are added when their feature is implemented.

### Per-Story Impact

| Story | Element | Target | Degradation |
|-------|---------|--------|-------------|
| F0001-S0002 | Mini-card click | Submission/Renewal Detail | Render entity name as plain text (not clickable) |
| F0001-S0002 | "View all N" link | Submission/Renewal List | Hide the link entirely |
| F0001-S0003 | Task row click (Broker) | Broker 360 | Works — F0002-S0003 in scope |
| F0001-S0003 | Task row click (Submission/Renewal/Account) | Detail screens | Render entity name as plain text (not clickable) |
| F0001-S0003 | Task row click (no linked entity) | Task Center | No navigation; row is informational only |
| F0001-S0003 | "View all tasks" link | Task Center | Hide the link entirely |
| F0001-S0004 | Feed item click | Broker 360 | Works — F0002-S0003 in scope |
| F0001-S0005 | CTA "Review Now" (Broker-linked task) | Broker 360 | Works — F0002-S0003 in scope |
| F0001-S0005 | CTA "Review Now" (non-Broker task) | Task Center / Detail | Hide CTA button |
| F0001-S0005 | CTA "Take Action" | Submission Detail | Hide CTA button |
| F0001-S0005 | CTA "Start Outreach" | Renewal Detail | Hide CTA button |

### Implementation Note

Navigation availability should be driven by a route registry check (e.g., `canNavigateTo(entityType)`) rather than hardcoded booleans. When the relevant future features are implemented and their routes registered, dashboard click-through will automatically activate without modifying F0001 code.

---

## F0003 Scope Decision (Task Write Endpoints)

**Decision:** F0003 task write endpoints are **out of scope** for the F0001/F0002 implementation pass.

**Rationale:** F0001 dashboard widgets (My Tasks, Nudge Cards) only *read* task data. No F0001 or F0002 story requires creating, updating, or deleting tasks via API. Task data for dashboard testing will be provided via a dev seed migration alongside Submission and Renewal seed data.

**Impact:**
- `POST /tasks`, `PUT /tasks/{taskId}`, `DELETE /tasks/{taskId}` — routes not registered, return 404.
- `GET /my/tasks`, `GET /tasks/{taskId}` — implemented as part of F0001.
- Task entity, table, and indexes — created in Phase 1 (Data Model + Migrations) since F0001 queries depend on them.
- F0003-S0001, F0003-S0002, and F0003-S0003 stories remain in the story index at MVP priority for future activation.

---

## Cross‑Feature Integration

- Dashboard broker activity feed must surface broker mutations from F0002.
- Timeline events must be consistent across dashboard and Broker 360 view.
- Ensure consistent ProblemDetails error codes for conflicts (invalid_transition, missing_transition_prerequisite, active_dependencies_exist, already_active, concurrency_conflict). See `planning-mds/architecture/error-codes.md` for the authoritative list.

## Exit Criteria

- F0001, F0002, and F0009 stories pass acceptance criteria.
- API contract validation passes.
- ABAC policy enforcement verified for all roles in matrix (including BrokerUser phase-1 delta).

---

## F0001-S0005 Completion Pass — Nudge Cards Remaining Work

**Date:** 2026-03-07
**Owner:** Backend Developer + Frontend Developer + Quality Engineer
**Scope:** Fix the 5 open gaps in F0001-S0005 only. No schema migrations. No new routes. No AI scope.

### Scope Breakdown

| Layer | Required Work | Owner |
|-------|---------------|-------|
| Backend (`engine/`) | (1) Add `AssignedToUserId` scope filter to stale submission + upcoming renewal queries. (2) Replace `UpdatedAt`-based staleness with last `WorkflowTransition` date for submissions. (3) Raise nudge return cap from 3 to 10. | Backend Developer |
| Frontend (`experience/`) | (4) Add `role="alert"` to nudge card container div in `NudgeCard.tsx`. | Frontend Developer |
| Quality | (5) Add integration test asserting nudge priority ordering: overdue tasks fill before stale submissions, stale before upcoming renewals; cap at 10. | Quality Engineer |
| AI (`neuron/`) | Not in scope. | — |
| DevOps/Runtime | No new infra, no migration, no env-var changes. Confirm build + tests pass. | DevOps |

### Dependency Order

1. **Backend** — fix `DashboardRepository.GetNudgesAsync` (all three backend items are in the same method; implement together).
2. **Frontend** — add `role="alert"` (independent, can run in parallel with backend).
3. **Quality** — add integration test (depends on backend fix being in place).
4. **Self-review + CI** — lint, build, test all pass.

### Integration Checkpoints

- [ ] `DashboardRepository.GetNudgesAsync`: stale submissions filtered by `AssignedToUserId == userId`
- [ ] `DashboardRepository.GetNudgesAsync`: upcoming renewals filtered by `AssignedToUserId == userId`
- [ ] `DashboardRepository.GetNudgesAsync`: staleness days computed from last `WorkflowTransition.OccurredAt` where `ToState = submission.CurrentStatus`, not from `UpdatedAt`
- [ ] Backend returns up to 10 nudges total (overdue tasks fill first, then stale, then upcoming)
- [ ] `NudgeCard.tsx` card container has `role="alert"`
- [ ] Integration test asserts priority ordering and 10-item cap
- [ ] `dotnet test` passes
- [ ] `pnpm --dir experience lint && pnpm --dir experience build && pnpm --dir experience test` pass

### Implementation Notes

**WorkflowTransition-based staleness (backend):**
The canonical pattern already exists in `DashboardRepository.GetOpportunityItemsAsync` (lines 228–232). For nudge computation:
1. Fetch candidate submissions: non-terminal, `AssignedToUserId == userId`.
2. For each candidate, find the max `WorkflowTransition.OccurredAt` where `WorkflowType = "Submission"` AND `ToState = submission.CurrentStatus`. Fall back to `submission.CreatedAt` if no matching transition exists (new submission never transitioned).
3. Filter to candidates where `(DateTime.UtcNow - transitionDate).TotalDays > 5`.
4. Sort by most stale first. Take up to `(10 - nudges.Count)`.

**Scope filter pattern:**
Tasks already use `AssignedToUserId == userId`. Apply the same pattern to submissions and renewals.

**10-item cap pattern:**
Replace all `Take(3)` → `Take(10)` and `Take(3 - nudges.Count)` → `Take(10 - nudges.Count)`. Final return: `nudges.Take(10).ToList()`. Remove the intermediate early-return guards (or update them to `>= 10`).

**Frontend `role="alert"`:**
The card container `<div>` in `NudgeCard.tsx` receives `role="alert"` so screen readers announce new/updated nudge cards. This is the outer div, not the dismiss button.

### Risks and Blockers

| Item | Severity | Mitigation |
|------|----------|------------|
| WorkflowTransition staleness query: submissions without any transitions use `CreatedAt` as fallback — may produce inaccurate staleness for very new submissions | Low | Acceptable for MVP; documented in code comment |
| ABAC-scope for stale/upcoming: using `AssignedToUserId == userId` as the scope proxy rather than full Casbin per-row check | Medium | Per-row Casbin check is too expensive for a nudge aggregation query; `AssignedToUserId` is the established ownership pattern for tasks and is the correct approximation here |

---

## F0002-S0009 — Native Casbin Enforcer Adoption

**Date:** 2026-03-08
**Owner:** Backend Developer Agent
**ADR:** `planning-mds/architecture/decisions/ADR-008-casbin-enforcer-adoption.md`

### Context

Stories S0001–S0008 are **Done**. The remaining work is S0009: replace the hand-rolled `PolicyAuthorizationService` with a native Casbin enforcer backed by `model.conf` + `policy.csv`. All existing endpoint authorization call sites (`HasAccessAsync` → `IAuthorizationService.AuthorizeAsync`) must continue to work unchanged. Frontend is unaffected — S0009 is a backend-internal change behind the existing `IAuthorizationService` interface.

### Scope Breakdown

| Layer | Required Work | Owner | Status |
|-------|---------------|-------|--------|
| Backend (`engine/`) | Replace `PolicyAuthorizationService` with `CasbinAuthorizationService`; add `Casbin.NET` NuGet; update DI; add startup validation; keep interface stable | Backend Developer | Planned |
| Frontend (`experience/`) | **None** — authorization change is behind existing API; no contract changes | — | N/A |
| AI (`neuron/`) | **None** | — | N/A |
| Quality | Unit tests for Casbin service; integration tests for policy matrix parity; startup failure tests | Backend Developer + Quality Engineer | Planned |
| DevOps/Runtime | Verify `policy.csv` + `model.conf` embedded resources resolve; no new infra dependencies | DevOps | Planned |

### Implementation Slices (Dependency Order)

#### Slice A — Safety Net (Baseline Tests)
1. Review existing `BrokerAuthorizationTests` — currently tests no-role 403 only.
2. Add positive authorization tests per role/action from `policy.csv` for F0002 resources:
   - Broker: create, read, search, update, delete, reactivate — per role matrix.
   - Contact: create, read, update, delete — per role matrix.
   - Timeline: read — per role matrix.
3. Add negative tests for denied actions (e.g., Underwriter cannot search brokers, RelationshipManager cannot delete brokers).
4. These tests lock the **current expected behavior** before the switch.

#### Slice B — Casbin Enforcer Implementation
1. Add `Casbin.NET` NuGet package to `Nebula.Infrastructure.csproj`.
2. Add `model.conf` as embedded resource in `Nebula.Infrastructure.csproj`.
3. Create `CasbinAuthorizationService : IAuthorizationService` in `Nebula.Infrastructure/Authorization/`.
4. Initialize Casbin `Enforcer` from embedded `model.conf` + `policy.csv` streams.
5. Map `AuthorizeAsync(role, resourceType, action, attrs?)` to `Enforcer.Enforce(subObj, objObj, action)`:
   - `subObj` = record with `Role = role`, `Id = attrs?["subjectId"]` (defaults to empty string if absent).
   - `objObj` = record with `Type = resourceType`, `Assignee = attrs?["assignee"]` (defaults to empty string if absent).
6. Fail fast on policy/model loading errors (throw `InvalidOperationException` at construction, not at first request).

#### Slice C — DI Switch + Cleanup
1. Update `DependencyInjection.cs`: replace `PolicyAuthorizationService` → `CasbinAuthorizationService`.
2. Delete or rename `PolicyAuthorizationService.cs` (no runtime references remain).
3. `IAuthorizationService` interface is **unchanged** — zero endpoint code changes.

#### Slice D — Verification
1. Run all existing integration tests to confirm behavioral parity.
2. Run new positive/negative authorization matrix tests (Slice A).
3. Add unit test for `CasbinAuthorizationService` directly — verify condition-based policies.
4. Add startup failure test: corrupt model/policy → deterministic `InvalidOperationException`.
5. Verify BrokerUser scope-isolation path is unaffected (remains query-layer; Casbin not consulted for BrokerUser fast-path).

#### Slice E — Documentation + Status
1. Update `F0002/STATUS.md` — mark S0009 Done with implementation evidence.
2. Update `F0002/README.md` — mark S0009 Done.
3. Update `ADR-008` status from Proposed → Accepted.

### Key Design Decisions

**Casbin Request Object Mapping:**
The `model.conf` matcher uses structured sub-request access (`r.sub.role`, `r.obj.type`, `r.sub.id`, `r.obj.assignee`). The `CasbinAuthorizationService` passes C# objects to `Enforce()`:

```
Request:  Enforce({ Role: "Admin", Id: "" }, { Type: "broker", Assignee: "" }, "read")
Matcher:  r.sub.role == p.sub && r.obj.type == p.obj && r.act == p.act && eval(p.cond)
Policy:   p, Admin, broker, read, true
Result:   true (condition "true" evals to true)
```

For condition-based policies (task ownership):
```
Request:  Enforce({ Role: "DistributionUser", Id: "abc-123" }, { Type: "task", Assignee: "abc-123" }, "read")
Policy:   p, DistributionUser, task, read, r.obj.assignee == r.sub.id
eval():   r.obj.assignee ("abc-123") == r.sub.id ("abc-123") → true
```

**Interface Stability:** `IAuthorizationService.AuthorizeAsync` signature is unchanged. Endpoint code requires zero modifications.

**Singleton Lifecycle:** `CasbinAuthorizationService` remains singleton. The Casbin `Enforcer` is thread-safe for `Enforce()` calls.

### Integration Checklist

- [x] API contract compatibility validated — `IAuthorizationService` interface unchanged
- [x] Frontend contract compatibility validated — no contract changes (backend-internal)
- [ ] Test cases mapped to acceptance criteria — Slice A + D
- [ ] Run/deploy instructions updated — no new infra; embedded resources only

### Risks and Blockers

| Item | Severity | Mitigation |
|------|----------|------------|
| Casbin .NET `eval()` behavior for condition expressions may differ from hand-rolled evaluator | Medium | Pre-switch matrix tests lock expected behavior; run both before and after switch |
| Model.conf attribute access (`r.sub.role`) requires passing C# objects — Casbin .NET reflection may require specific property casing | Medium | Use concrete record types with matching property names; unit test verifies attribute access |
| Embedded resource resolution for `model.conf` | Low | Same pattern as existing `policy.csv`; tested at startup with fail-fast |
| WSL integration test path resolution (known limitation) | Low | Tests run from Windows or container; no new limitation |

### Checkpoint

**F0002-S0009-A:** All integration tests pass with native Casbin enforcer. Hand-rolled policy parser removed from runtime path. Behavioral parity confirmed.

---

## F0014 — DevOps Smoke Test Automation

**Added:** 2026-03-27 — Architecture review complete; no new entities, APIs, workflows, or Casbin policies. Infrastructure/scripts feature only.

> **Implementation Execution Plan:** [`feature-assembly-plan.md`](../features/F0014-devops-smoke-test-automation/feature-assembly-plan.md) — per-step implementation spec for S0002 (multi-role `--all-users` mode) and S0003 (CI workflow). Includes corrected BrokerUser assertion expectations (S0002 AC was wrong — BrokerUser has `task:read` ALLOW, not deny-all).

### Dependencies

| Dependency | Source | What F0014 Needs | Status |
|------------|--------|------------------|--------|
| Task CRUD API | F0003 | `/tasks`, `/my/tasks` endpoints | **Done** |
| authentik blueprint | F0005 | ROPC + app-password tokens | **Done** (fixed in S0001) |
| Casbin policy | F0005/platform | Role-based access boundaries | **Done** |
| S0001 scripts | F0014-S0001 | `smoke-test.sh`, `dev-reset.sh` | **Done** |

### Architecture Notes

- **No application code changes** — all work is shell scripts and CI workflow configuration.
- **Critical finding:** S0002 acceptance criteria for broker001 (BrokerUser) is incorrect. BrokerUser has `task:read` ALLOW in policy.csv (§2.10). Corrected expectations: GET /my/tasks → 200, POST/PUT/DELETE → 403.
- **CI resource concern (S0003):** GitHub Actions `ubuntu-latest` has 7 GB RAM. Recommend starting only `db`, `authentik-server`, `authentik-worker`, `api` — skipping `temporal` and `temporal-ui` which smoke tests do not exercise.

### DevOps Assembly Steps

**Step 1 — S0002: Multi-role smoke test enhancement (DevOps Agent)**
1. Add `--all-users` flag to `smoke-test.sh`
2. Define role expectation matrix (4 users × expected role × crud mode)
3. Per-user JWT claims verification (`aud`, `nebula_roles`, `sub`)
4. Conditional test routing: full 9-test suite for internal roles, 4-test read-only suite for BrokerUser
5. Continue-on-failure: one user failure does not abort others
6. Unified multi-user summary with per-user pass/fail counts
7. Update README.md and GETTING-STARTED.md with `--all-users` usage

**Step 2 — S0003: CI smoke test workflow (DevOps Agent — Future)**
1. Create `.github/workflows/smoke-test.yml`
2. Trigger on PR + push to main, with concurrency group
3. Start selective docker compose services (skip temporal)
4. Health-check polling + blueprint wait
5. Run `smoke-test.sh --all-users`
6. Upload compose logs as artifact on failure
7. Tear down stack with `if: always()`

### Checkpoint

**S0002:** `./scripts/smoke-test.sh --all-users` runs all 4 dev users with correct role-appropriate assertions. Single-user mode not regressed. Exit code 0 only if all pass.

**S0003:** GitHub Actions workflow passes on `ubuntu-latest`. Stack starts, smoke tests run, failure logs uploaded, stack torn down.

---

## F0033 — Structured Logging and QE Toolchain Activation

**Added:** 2026-03-28 — Architecture review complete; solution activation feature for observability and QE stack already approved in solution patterns.

> **Implementation Execution Plan:** [`feature-assembly-plan.md`](../features/F0033-structured-logging-and-qe-toolchain-activation/feature-assembly-plan.md) — per-step implementation spec for Serilog, Bruno, Lighthouse CI, Pact, and SonarQube Community activation.

### Dependencies

| Dependency | Source | What F0033 Needs | Status |
|------------|--------|------------------|--------|
| API runtime | `engine/src/Nebula.Api` | Logging bootstrap point and representative endpoints | **Done** |
| Frontend quality foundation | F0015 | Existing frontend validation and coverage baseline | **Done** |
| Smoke-test auth/bootstrap | F0014 / F0005 / F0009 | Seeded users, dev tokens, and stable local stack assumptions | **Done** |
| Solution patterns | `planning-mds/architecture/SOLUTION-PATTERNS.md` | Approved stack decisions for Serilog, Bruno, Lighthouse, Pact, Sonar | **Done** |

### Architecture Notes

- **Solution activation, not framework drift:** The stack decisions already exist. F0033 makes them executable in the repo.
- **Opt-in QE services:** Pact Broker and SonarQube must live in `docker-compose.qe.yml`, not the default always-on stack.
- **Representative-first rollout:** Broker list is the first Pact slice. Bruno and Lighthouse also start with intentionally narrow, reviewable route/resource coverage.
- **Auth guardrail preserved:** Lighthouse on protected routes must use an explicit perf-only runtime profile. The production `VITE_AUTH_MODE=dev` guard remains intact.

### Assembly Steps

**Step 1 — S0001: Serilog baseline (Backend Developer)**
1. Add Serilog packages and appsettings configuration
2. Bootstrap `UseSerilog()` in `Program.cs`
3. Add request/user context enrichment middleware
4. Add logging verification tests

**Step 2 — S0002: Bruno collections (Quality Engineer + DevOps)**
1. Commit Bruno collection/env layout
2. Add `run-bruno.sh`
3. Add representative requests (`/healthz`, `/brokers`, `/my/tasks`)
4. Add CI workflow and stable artifact path

**Step 3 — S0003: Lighthouse CI (Frontend Developer + DevOps)**
1. Commit `lighthouserc.json`
2. Add `run-lhci.sh` and package scripts
3. Define approved route set and thresholds
4. Add CI workflow and artifact upload path

**Step 4 — S0004: Pact contract slice (Frontend Developer + Backend Developer)**
1. Add frontend consumer contract for broker list
2. Add backend provider verification
3. Add optional broker publication path and QE overlay service
4. Add CI verification workflow

**Step 5 — S0005: SonarQube Community (DevOps + Quality Engineer)**
1. Add optional SonarQube service to QE overlay
2. Add `run-sonar.sh`
3. Wire backend/frontend coverage import paths
4. Add CI analysis workflow and documented quality gate

### Signoff Role Matrix

| Role | Required | Rationale |
|------|----------|-----------|
| Quality Engineer | Yes | The feature activates QE tooling itself and needs execution evidence. |
| Code Reviewer | Yes | Runtime logging and cross-tool workflow changes need independent review. |
| Security Reviewer | Yes | Log redaction and new tool/service exposure boundaries must be reviewed. |
| DevOps | Yes | Compose overlays, workflows, and runtime entry points are core scope. |
| Architect | Yes | Cross-cutting activation sequencing and guardrails require architectural signoff. |

### Checkpoint

**F0033-A:** API emits trace-correlated structured logs through Serilog.

**F0033-B:** Bruno and Lighthouse both produce stable local/CI artifacts.

**F0033-C:** Broker list Pact contract is generated and provider-verified.

**F0033-D:** SonarQube Community ingests backend + frontend coverage in one analysis flow.
