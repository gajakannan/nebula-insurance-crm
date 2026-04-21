# Application Assembly Plan (Phase C)

**Owner:** Architect
**Status:** Approved
**Last Updated:** 2026-02-21

## Purpose

Provide a sequenced, cross‑role plan to assemble the Nebula CRM implementation without blocking dependencies, while keeping F0001 (Dashboard), F0002 (Broker Relationship Management), and F0009 (Authentication + Role-Based Login) aligned to the approved architecture and contracts.

## Scope

- Modules: BrokerRelationship, Submission, Renewal, TaskManagement, TimelineAudit, IdentityAuthorization, Dashboard
- Features in scope: F0001, F0002 (MVP), F0009 (Phase 1 auth hardening + broker boundary pilot)
- Out of scope: External portal, analytics/insights beyond F0001/F0002, deployment hardening
- **F0003 Task Write Endpoints:** Out of scope for this implementation pass. Task write endpoints (`POST /tasks`, `PUT /tasks/{taskId}`, `DELETE /tasks/{taskId}`) are not registered and return HTTP 404. Dashboard (F0001) only reads tasks. Test data for the My Tasks and Nudge widgets will be provided via a dev seed migration. The story index lists F0003-S0001, F0003-S0002, and F0003-S0003 as MVP priority for future activation, not for this pass.

## Required Inputs (Must Exist)

- `planning-mds/BLUEPRINT.md` (Phase A complete)
- `planning-mds/api/nebula-api.yaml` (OpenAPI contract)
- `planning-mds/security/authorization-matrix.md` and `planning-mds/security/policies/policy.csv`
- `planning-mds/features/F0009-authentication-and-role-based-login/IMPLEMENTATION-CONTRACT.md`
- `planning-mds/features/F0009-authentication-and-role-based-login/BROKER-VISIBILITY-MATRIX.md`
- `planning-mds/architecture/SOLUTION-PATTERNS.md`
- JSON Schemas in `planning-mds/schemas/`
- ADRs in `planning-mds/architecture/decisions/`

## Assembly Order (High Level)

### 1) Foundation (Shared Infrastructure)

**Backend**
- Set up .NET solution structure per module boundaries.
- Wire ProblemDetails error contract and consistent error codes.
- Implement Casbin ABAC enforcement middleware and policy loading.
- Implement schema validation layer (NJsonSchema) using `planning-mds/schemas/`.

**Frontend**
- Scaffold app shell, routing, auth gate, error boundaries.
- Apply design tokens and base layout components.
- Add auth route shell (`/login`, `/auth/callback`, `/unauthorized`).

**QA/DevOps**
- Baseline test harness (unit + integration) and CI validation gate alignment.

**Checkpoint A:**
- Auth middleware in place, basic request validation, and API skeleton running with health endpoint.

### 2) Data Model + Migrations

**Backend**
- Implement core entities (Broker, Contact, Submission, Renewal, ActivityTimelineEvent, WorkflowTransition, Task) with audit fields and soft delete.
- Apply indexes per `planning-mds/architecture/data-model.md` and `planning-mds/architecture/SOLUTION-PATTERNS.md`.

**Checkpoint B:**
- Migrations applied and seed/reference data ready for local dev.

### 3) Contract‑First API Implementation

**Backend**
- Implement only endpoints defined in `planning-mds/api/nebula-api.yaml` that are in scope for F0001 and F0002.
- For out-of-scope endpoints and schemas (see `planning-mds/schemas/README.md`), do not register routes; callers should receive HTTP 404.
- Until F0003 is activated, task write endpoints (`POST /tasks`, `PUT /tasks/{taskId}`, `DELETE /tasks/{taskId}`) are treated as out-of-scope and must return 404.
- Enforce schema validation on request payloads.
- Ensure policy checks for every endpoint (ABAC + role action).

**Frontend**
- Generate client types from OpenAPI (if used) or align TS types to schemas.

**Checkpoint C:**
- Contract validation passes; all endpoints return correct shape for dummy data.

### 3.5) F0009 Auth and Boundary Delta

**Backend**
- Align BrokerUser rows in `policy.csv` with matrix section 2.10.
- Implement broker scope resolver (`email` => exactly one active broker).
- Enforce BrokerVisible/InternalOnly response shaping for BrokerUser.

**Frontend**
- Implement OIDC code + PKCE flow (`oidc-client-ts`).
- Enforce deterministic route guard behavior for unauthenticated, 401, and 403 outcomes.
- Implement role-based landing resolution and precedence.

**Checkpoint C‑F0009:**
- End-to-end login + callback + BrokerUser boundary validations pass.
- Security review checklist evidence complete (`planning-mds/security/F0009-security-review-checklist.md`).

### 4) Feature Assembly (F0001 + F0002)

Follow the feature assembly plan to build complete vertical slices.

### 5) System Hardening

- Logging, tracing, and metrics wiring.
- Performance checks (p95 targets in stories).
- Security checks: verify deny‑by‑default behavior for missing policies.

**Checkpoint D:**
- End‑to‑end smoke tests across F0001/F0002 succeed.

## Handoff Contracts

- Backend → Frontend: stable API endpoints and response schemas.
- Backend → QA: test data seeding and deterministic error codes.
- Architect → All: updated contracts + schemas are authoritative.

## Exit Criteria (Phase C)

- All F0001, F0002, and F0009 stories pass acceptance criteria.
- API contract validation passes.
- ABAC enforcement verified with integration tests, including BrokerUser phase-1 rules.
- Phase 1 no-RLS compensating controls verified in security review evidence.
- UI renders core flows without fallback errors.
