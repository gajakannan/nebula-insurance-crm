# Insurance CRM — Single Source of Truth Master Build Spec (Blueprint Prompt)

You are an AI development partner helping me create a production-grade Commercial P&C Insurance CRM.
This document is the ONLY source of truth. Do not rely on any other spec packages unless explicitly told to.
If information is missing, ask questions or mark TODOs — do NOT invent business rules.

## 0) How we will work (Process + Roles)

We will proceed in three explicit phases. You must stay within the current phase.

### Phase A — Product Manager Mode (PM/BA)
Goal: define product requirements (vision, users, epics, features, stories, acceptance criteria).
Output: a complete PM-ready spec section with minimal technical assumptions.

### Phase B — Architect/Tech Lead Mode (Dev/Arch)
Goal: define technical approach (stack, architecture, data model, workflows, APIs, security, NFRs).
Output: a complete build-ready technical spec section that maps to Phase A.

### Phase C — Implementation Mode
Goal: generate the actual repository and code in incremental vertical slices with tests.
Output: production-quality code + migrations + OpenAPI + tests + run instructions.

IMPORTANT RULES:

- Single source of truth is THIS document.
- If a requirement isn’t written here, do not implement it.
- If there is ambiguity, list questions and propose minimal default assumptions labeled clearly.
- No scope creep. Build only what’s specified for the current phase.

### Tracker Governance (Mandatory)

Planning trackers must stay in sync at all times. Treat stale tracker state as a process defect.

- Governance contract: `planning-mds/features/TRACKER-GOVERNANCE.md`
- Required validations before declaring planning or feature execution complete are defined by the external agent framework's product-manager role (tracker + story validators). Run those validators against this repo's `planning-mds/` before any planning/build/feature gate is marked complete.
- Do not mark any planning/build/feature gate complete while tracker validation errors remain.

---

## 1) Product Context

### 1.1 What we’re building

Name: Nebula

Domain: Commercial Property & Casualty Insurance CRM

Purpose: Manage broker/MGA relationships, accounts, submissions, renewals, activities, reminders, and broker insights.


### 1.2 Target users

- Distribution & Marketing (primary users)
- Underwriters (workflow updates + collaboration)
- Broker Relationship Managers
- MGA Program Managers
- Admin

External users (future): MGA users with limited access (not in Phase 0 MVP unless explicitly stated).

### 1.3 Core entities (baseline)

- Account (insured business)
- Broker
- MGA
- Program
- Contact
- Submission
- Renewal
- Document (versioned)
- ActivityTimelineEvent (immutable audit/timeline)
- WorkflowTransition (immutable append-only transitions)
- UserProfile (internal profile; maps IdP `(iss, sub)` to a stable internal `UserId (uuid)` — IdP-agnostic per ADR-006)
- UserPreference (separate table)

### 1.4 Critical workflows (baseline)

Submission: Received → Triaging → WaitingOnBroker → ReadyForUWReview → InReview → Quoted → BindRequested → Bound (or Declined/Withdrawn)
Renewal: Identified → Outreach → InReview → Quoted → Completed (or Lost)

Non-negotiables:

- Audit logging and timeline events are mandatory for every mutation and every workflow transition.
- Role-based visibility is mandatory: InternalOnly vs BrokerVisible content separation.

---

## 2) Technology and Platform (baseline decisions)

These are locked unless explicitly changed later:

- Frontend: React 18 + TypeScript + Vite + Tailwind + shadcn/ui
- State: TanStack Query, React Hook Form, AJV (JSON Schema validation)
- Backend: C# / .NET 10 Minimal APIs
- Database: PostgreSQL (dev + prod)
- ORM: EF Core 10
- AuthN: authentik (OIDC/JWT) — replaces Keycloak per ADR-006
- AuthZ: Casbin ABAC enforced server-side
- Workflow engine: Temporal (included in Phase 0)
- Deploy: Docker + docker-compose
- Agentic ops: Python MCP server (later, secondary interface; never source of truth)
- Testing:
  - Frontend: Vitest (unit/component), Playwright (E2E browser), @axe-core/playwright (a11y), Lighthouse CI (performance)
  - Backend: xUnit (unit/integration), Testcontainers (database), Bruno CLI (API collections), Coverlet (coverage), k6 (load)
  - AI/Neuron: pytest (unit/integration/evaluation), pytest-benchmark (performance), custom evaluation metrics
  - Cross-cutting: Pact.NET (contract testing), OWASP ZAP (security), Trivy (vulnerability scanning)

Architecture constraints:

- Clean Architecture: Domain → Application → Infrastructure → API
- Application depends on repository interfaces; Infrastructure implements with EF.
- Audit/timeline/transition tables are append-only (immutable).
- Reference data uses tables + deterministic seed data (not hardcoded enums when configurable).
- API error contract must be consistent across all services.

---

## 3) Phase A — Product Manager Spec (Current Baseline)

Status: Phase C implementation is complete for F0001 (Dashboard), F0002 (Broker Relationship Management), F0003 (Task Center API-only MVP), F0004 (Task Center UI + Manager Assignment), F0005 (IdP Migration), F0009 (Authentication + Role-Based Login), F0012 (Dashboard Storytelling Infographic Canvas), F0013 (Dashboard Framed Storytelling Canvas), and F0015 (Frontend Quality Gates + Test Infrastructure). Phase A remains the baseline spec and Phase B is approved.

### 3.1 Vision + Non-Goals

- Vision:
  - Provide a single operating system for commercial P&C distribution teams to manage broker/MGA relationships, accounts, submissions, renewals, and activity history with strong auditability.
  - Replace spreadsheet/email-driven processes with structured workflows, permission-aware collaboration, and traceable transitions.
  - Deliver a modular foundation that supports AI-assisted workflows later without changing the source-of-truth system.
  - Add an AI-assisted, permission-aware, **CRM-scoped** conversational companion (**Neuron**) over existing CRM data — presented as a multi-zone Day-at-a-Glance shell with a bounded set of assisted writes — without changing the .NET engine as the source of truth (Neuron is a secondary interface). See the Neuron Companion epic in §3.3.

- Non-goals (explicit):
  - No external broker/MGA self-service portal in MVP.
  - No advanced analytics dashboards beyond basic broker insight summaries in MVP.
  - No document OCR/intelligence workflows in MVP.
  - No claims management module in MVP.
  - No multi-region regulatory rules engine in MVP.
  - Neuron is a **CRM-scoped assistant, not a general-purpose chatbot** — off-topic queries are politely redirected, not answered.
  - **No real outbound email/communication send from Neuron** in its first slice (mock-send only); real send + Communication Hub (F0021) draft home are deferred.

### 3.2 Personas

- Persona 1: Distribution user
  - Primary job: intake and triage submissions, manage broker interactions, track pipeline movement.
  - Success metric: reduced intake turnaround and fewer handoff delays.

- Persona 2: Underwriter
  - Primary job: review triaged submissions, provide quote/bind decisions, maintain decision traceability.
  - Success metric: faster, consistent movement from ReadyForUWReview to Quoted/Bound or Declined.
  - Neuron companion: **primary user** of the Neuron Day-at-a-Glance companion as the **renewal owner** (commercial P&C); drafts renewal outreach (the only role with `renewal:draft_outreach` in v1). Distribution / Relationship Manager (Persona 1/3) assists.

- Persona 3: Relationship Manager
  - Primary job: maintain broker/account relationships, contacts, and timeline context.
  - Success metric: complete broker/account context available in one place.

- Persona 4: Program Manager
  - Primary job: oversee MGA/program-level relationships and program performance signals.
  - Success metric: program-level visibility with clear ownership and activity traces.

### 3.3 Features

**Note:** Features are organized as self-contained folders in `planning-mds/features/F{NNNN}-{slug}/` using the feature templates. Each folder includes `PRD.md`, `README.md`, `STATUS.md`, `GETTING-STARTED.md`, and colocated story files.

**Planning Views:**
- Feature inventory & ID tracker: `planning-mds/features/REGISTRY.md`
- Roadmap sequencing (Now / Next / Later): `planning-mds/features/ROADMAP.md`
- Story rollup index: `planning-mds/features/STORY-INDEX.md`
- Governance contract: `planning-mds/features/TRACKER-GOVERNANCE.md`

**MVP Features:**
- [F0001: Dashboard](features/archive/F0001-dashboard/PRD.md) - Done (Archived)
- [F0002: Broker & MGA Relationship Management](features/archive/F0002-broker-relationship-management/PRD.md) - Done (Archived)
- [F0003: Task Center + Reminders](features/archive/F0003-task-center/PRD.md) - Done (API-only MVP, archived 2026-03-20)
- [F0005: IdP Migration: Keycloak → authentik](features/archive/F0005-idp-migration/PRD.md) - Done (Archived)
- [F0006: Submission Intake Workflow](features/archive/F0006-submission-intake-workflow/PRD.md) - Done (Archived 2026-04-04)
- [F0007: Renewal Pipeline](features/archive/F0007-renewal-pipeline/PRD.md) - Done (Archived 2026-04-12; 7 stories: pipeline list, detail, transitions, assignment, overdue visibility, create from policy, timeline)
- [F0009: Authentication + Role-Based Login](features/archive/F0009-authentication-and-role-based-login/PRD.md) - Done (Archived; Phase 1)
- [F0004: Task Center UI + Manager Assignment](features/archive/F0004-task-center-ui-and-assignment/PRD.md) - Done (Archived 2026-03-23; Phase 1)
- [F0014: DevOps Smoke Test Automation](features/archive/F0014-devops-smoke-test-automation/PRD.md) - Done (Archived 2026-03-28; Infrastructure)
- [F0015: Frontend Quality Gates + Test Infrastructure](features/archive/F0015-frontend-quality-gates-and-test-infrastructure/PRD.md) - Done (Archived)
- [F0010: Dashboard Opportunities Refactor (Pipeline Board + Insight Views)](features/archive/F0010-dashboard-opportunities-refactor/PRD.md) - Abandoned (Superseded by F0013)
- [F0011: Dashboard Opportunities Flow-First Modernization (Connected Pipeline + Terminal Outcomes)](features/archive/F0011-dashboard-opportunities-flow-modernization/PRD.md) - Abandoned (Superseded by F0013)
- [F0012: Dashboard Storytelling Infographic Refactor (Unified Canvas + Collapsible Rails)](features/archive/F0012-dashboard-storytelling-infographic-canvas/PRD.md) - Done (Archived)
- [F0013: Dashboard Framed Storytelling Canvas](features/archive/F0013-dashboard-framed-storytelling-canvas/PRD.md) - Done (Archived)

**CRM Release MVP (Planned):**
- [F0016: Account 360 & Insured Management](features/archive/F0016-account-360-and-insured-management/PRD.md) - Done (Archived 2026-04-14; 11 stories: list, create, profile edit, 360 composition, contacts, relationships, lifecycle, merge, deleted/merged fallback contract, timeline, summary projection)
- [F0018: Policy Lifecycle & Policy 360](features/archive/F0018-policy-lifecycle-and-policy-360/PRD.md) - Done (Archived 2026-04-22; 11 stories: list, create, profile edit, 360 composition, versions, endorsements, cancellation, reinstatement, renewal linkage, timeline, summary projection)
- [F0034: Product Schema Registry and Dynamic LOB Attributes](features/archive/F0034-product-schema-registry-and-dynamic-lob-attributes/PRD.md) - Done (Archived 2026-05-07; Platform Foundation / CRM Release MVP Enabler; 7 stories: decision lock, registry foundation, lifecycle carrier pinning, validator parity, dynamic panel, Cyber bundle, lifecycle/F0019 handoff)
- [F0036: Form Engine and Form-State Preservation (RHF + AJV + Widget Registry)](features/archive/F0036-dynamic-product-attribute-form-engine/PRD.md) - Done (Archived 2026-05-30; Platform Foundation / CRM Release MVP Enabler; 8 stories: engine skeleton + deps, MVP widget vocabulary, schema render + AJV parity, pin-during-edit, replace Cyber panel, attr-form preservation, controlled-form dirty-tracker + shared helper, CRUD preservation/restore)
- [F0019: Submission Quoting, Proposal & Approval Workflow](features/archive/F0019-submission-quoting-proposal-and-approval/PRD.md) - Done (Archived 2026-06-03; 8 stories: downstream workflow activation, quote/proposal packet, underwriting approval, bind handoff, decline/withdraw, archive/reactivate, pipeline visibility, timeline/audit)
- [F0020: Document Management & ACORD Intake](features/archive/F0020-document-management-and-acord-intake/PRD.md) - Done (Archived 2026-05-05; 12 stories: single upload, bulk upload, quarantine promote, classification-filtered list, detail/provenance, downloads, immutable replace, metadata update, classification ABAC, completeness signal, retention cleanup, templates library)
- [F0021: Communication Hub & Activity Capture](features/archive/F0021-communication-hub-and-activity-capture/PRD.md) - Done (Archived 2026-07-02; 5 stories: structured communication capture, contextual history, related-record/participant links, follow-up task linkage, correction/redaction audit)
- F0022: Work Queues, Assignment Rules & Coverage Management - Planned

- F0021: Communication Hub & Activity Capture - Planned
- [F0022: Work Queues, Assignment Rules & Coverage Management](features/archive/F0022-work-queues-assignment-rules-and-coverage-management/PRD.md) - Done (Archived 2026-07-03; 7 stories: queues/memberships, assignment rules, task/submission/renewal routing, coverage windows, queue worklists/aging, reassignment/rebalance, routing audit/permissions)
- [F0023: Global Search, Saved Views & Operational Reporting](features/archive/F0023-global-search-saved-views-and-operational-reporting/PRD.md) - Done (archived 2026-06-20; Phase A refined 2026-06-19; 7 stories: global search, filter/open results, personal saved views, team saved views, daily workload report, workflow aging/backlog report, permission-safe behavior)

**CRM Release MVP+ (Planned):**
- [F0008: Broker Insights](features/archive/F0008-broker-insights/PRD.md) - Done (Archived 2026-07-03; Phase A refined 2026-07-03; 5 stories: broker scorecard, trend drilldown, authorized benchmark comparison, review snapshot, permission-safe behavior)
- [F0017: Broker/MGA Hierarchy, Producer Ownership & Territory Management](features/archive/F0017-broker-mga-hierarchy-and-producer-ownership/PRD.md) - Done (Archived 2026-07-03; 5 stories: broker/MGA hierarchy, hierarchy traversal, producer ownership, territory assignment, audit/timeline)
- [F0024: Claims & Service Case Tracking](features/archive/F0024-claims-and-service-case-tracking/PRD.md) - Done (Archived 2026-07-03; 6 stories: service case intake, contextual visibility, ownership/follow-up, status transitions, claim-reference context, permission-safe audit history)
- F0027: COI, ACORD & Outbound Document Generation - Planned
- [F0028: Carrier & Market Relationship Management](features/archive/F0028-carrier-and-market-relationship-management/PRD.md) - Done (Archived 2026-07-03; feature run 2026-07-02-736e7854; 6 stories: market directory search, carrier/market profile management, underwriter contact management, appetite note capture, appointment context, market activity/related work)

- F0017: Broker/MGA Hierarchy, Producer Ownership & Territory Management - Planned
- F0024: Claims & Service Case Tracking - Planned
- [F0027: COI, ACORD & Outbound Document Generation](features/archive/F0027-coi-acord-and-outbound-document-generation/PRD.md) - Done (Archived 2026-07-03; feature run `2026-07-02-b9316621`; 5 stories: template library governance, preview generated document, issue generated artifact, regenerate/retrieve artifacts, render proposal from submission packet context)
- F0028: Carrier & Market Relationship Management - Planned
- [F0037: Hierarchy-Aware Access Scoping & Distribution Rollups](features/archive/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/PRD.md) - Done and archived (2026-07-06, feature run `2026-07-06-76799554`; 6 stories: current-user distribution scope, hierarchy-aware read scoping, scoped search/views/insights/reports, distribution rollups, rollup filters/drilldowns/no-leak states, security evidence and reconciliation)

**Brokerage Platform Expansion (Planned):**
- [F0025: Commission, Producer Splits & Revenue Tracking](features/archive/F0025-commission-producer-splits-and-revenue-tracking/PRD.md) - Done (Archived 2026-07-07; feature run `2026-07-07-9859bad4`; 6 stories: commission workspace search, schedule maintenance, producer split assignment, expected commission review, adjustment approval, revenue rollups)
- F0026: Billing, Invoicing & Reconciliation - Planned
- F0029: External Broker Collaboration Portal - Planned
- F0030: Integration Hub & Data Exchange - Planned

**Release Enablement / Platform Operations (Planned):**
- F0031: Data Import, Deduplication & Go-Live Migration - Planned
- [F0032: Admin Configuration & Reference Data Console](features/archive/F0032-admin-configuration-and-reference-data-console/PRD.md) - Planned (Phase A refined 2026-07-06; 6 draft stories: admin configuration catalog, reference/SLA drafts, queue/routing drafts, validate/compare, publish/rollback, audit/permission-safe behavior)
- [F0033: Structured Logging and QE Toolchain Activation](features/archive/F0033-structured-logging-and-qe-toolchain-activation/PRD.md) - Done (Archived)
- [F0035: Session Continuity & Token Refresh](features/archive/F0035-session-continuity-and-token-refresh/PRD.md) - Done (Archived 2026-05-24; 5 stories: silent renewal, idle warning modal, forced re-auth restore, auth error semantics, session telemetry)

**Neuron Companion — AI Conversational Layer (Planned):**

The first epic since the harness adopted the action/KG flow. A conversational companion embedded in the CRM (`experience/src/features/neuron`) that chats about CRM work and renders rich, registered components inline. The Python `neuron/` backend classifies intent and routes to specialist mini-orchestrators; the .NET engine stays the single source of truth (Neuron is secondary, calls the engine as the user via forwarded token, Casbin ABAC enforced unchanged). Sequenced Now → Next → Later; the reusable platform is extracted on the second live head (F0040), not on stubs. Source intake: `planning-mds/features/F0038-neuron-day-at-a-glance-shell/intake-brief.md`.

- F0038: Neuron Day-at-a-Glance Shell (Renewals live + draft outreach + mock-send) - Planned (Now)
- F0039: Neuron Multi-Thread Conversations - Planned (Next, provisional)
- F0040: Neuron Second Specialist Head - Planned (Next, provisional)
- _Later (unreserved):_ Day-at-a-Glance **brain** (cross-zone composition/ranking) · more specialist heads · real outbound send + Communication Hub (F0021) draft home · MCP-UI for external hosts · writes beyond drafting · thread search/share niceties.

### 3.4 MVP Features and Stories (vertical-slice friendly)

**Note:** User stories are written as separate markdown files organized by feature in `planning-mds/features/{feature-name}/` directories using the story template supplied by the external agent framework. Each story includes: description, acceptance criteria, edge cases, roles, and audit/timeline requirements.

**MVP Stories (Feature F0001: Dashboard):**
- [F0001-S0001: View Key Metrics Cards](features/archive/F0001-dashboard/F0001-S0001-view-key-metrics-cards.md) - Done (Archived)
- [F0001-S0002: View Pipeline Summary (Sankey Opportunities)](features/archive/F0001-dashboard/F0001-S0002-view-pipeline-summary.md) - Done (Archived)
- [F0001-S0003: View My Tasks and Reminders](features/archive/F0001-dashboard/F0001-S0003-view-my-tasks-and-reminders.md) - Done (Archived)
- [F0001-S0004: View Broker Activity Feed](features/archive/F0001-dashboard/F0001-S0004-view-broker-activity-feed.md) - Done (Archived)
- [F0001-S0005: View Nudge Cards](features/archive/F0001-dashboard/F0001-S0005-view-nudge-cards.md) - Done (Archived)

**MVP Stories (Feature F0002: Broker Relationship Management):**
- [F0002-S0001: Create Broker](features/archive/F0002-broker-relationship-management/F0002-S0001-create-broker.md) - Done (Archived)
- [F0002-S0002: Search Brokers](features/archive/F0002-broker-relationship-management/F0002-S0002-search-brokers.md) - Done (Archived)
- [F0002-S0003: Read Broker (Broker 360 View)](features/archive/F0002-broker-relationship-management/F0002-S0003-read-broker.md) - Done (Archived)
- [F0002-S0004: Update Broker](features/archive/F0002-broker-relationship-management/F0002-S0004-update-broker.md) - Done (Archived)
- [F0002-S0005: Delete Broker](features/archive/F0002-broker-relationship-management/F0002-S0005-delete-broker.md) - Done (Archived)
- [F0002-S0006: Manage Broker Contacts](features/archive/F0002-broker-relationship-management/F0002-S0006-manage-broker-contacts.md) - Done (Archived)
- [F0002-S0007: View Broker Activity Timeline](features/archive/F0002-broker-relationship-management/F0002-S0007-view-broker-activity-timeline.md) - Done (Archived)
- [F0002-S0008: Reactivate Broker](features/archive/F0002-broker-relationship-management/F0002-S0008-reactivate-broker.md) - Done (Archived)
- [F0002-S0009: Adopt Native Casbin Enforcer](features/archive/F0002-broker-relationship-management/F0002-S0009-adopt-native-casbin-enforcer.md) - Done (Archived)

**MVP Stories (Feature F0003: Task Center + Reminders — API-only):**
- [F0003-S0001: Create Task](features/archive/F0003-task-center/F0003-S0001-create-task.md) - ✅ Done
- [F0003-S0002: Update Task](features/archive/F0003-task-center/F0003-S0002-update-task.md) - ✅ Done
- [F0003-S0003: Delete Task](features/archive/F0003-task-center/F0003-S0003-delete-task.md) - ✅ Done

**Phase 1 Stories (Feature F0009: Authentication + Role-Based Login):**
- [F0009-S0001: Login Screen and OIDC Redirect](features/archive/F0009-authentication-and-role-based-login/F0009-S0001-login-screen-and-oidc-redirect.md) - Done (Archived)
- [F0009-S0002: OIDC Callback and Session Bootstrap](features/archive/F0009-authentication-and-role-based-login/F0009-S0002-oidc-callback-and-session-bootstrap.md) - Done (Archived)
- [F0009-S0003: Role-Based Entry and Protected Navigation](features/archive/F0009-authentication-and-role-based-login/F0009-S0003-role-based-entry-and-protected-navigation.md) - Done (Archived)
- [F0009-S0004: BrokerUser Access Boundaries](features/archive/F0009-authentication-and-role-based-login/F0009-S0004-broker-user-access-boundaries.md) - Done (Archived)
- [F0009-S0005: Seeded User Access Validation Matrix](features/archive/F0009-authentication-and-role-based-login/F0009-S0005-seeded-user-access-validation-matrix.md) - Done (Archived)

**Infrastructure Stories (Feature F0015: Frontend Quality Gates + Test Infrastructure):**
- [F0015-S0001: Establish frontend test infrastructure and commands](features/archive/F0015-frontend-quality-gates-and-test-infrastructure/F0015-S0001-establish-frontend-test-infrastructure-and-commands.md) - Done (Archived)
- [F0015-S0002: Activate Nebula frontend quality gates and evidence](features/archive/F0015-frontend-quality-gates-and-test-infrastructure/F0015-S0002-activate-nebula-frontend-quality-gates-and-evidence.md) - Done (Archived)
- [F0015-S0003: Backfill critical frontend coverage and record one full validation run](features/archive/F0015-frontend-quality-gates-and-test-infrastructure/F0015-S0003-backfill-critical-frontend-coverage-and-record-full-validation-run.md) - Done (Archived)

**Infrastructure Stories (Feature F0014: DevOps Smoke Test Automation — Archived):**
- [F0014-S0001: Blueprint ROPC fixes and smoke test scripts](features/archive/F0014-devops-smoke-test-automation/F0014-S0001-blueprint-ropc-fixes-and-smoke-scripts.md) - Done (Archived)
- [F0014-S0002: Multi-role smoke test verification](features/archive/F0014-devops-smoke-test-automation/F0014-S0002-multi-role-smoke-test-verification.md) - Done (Archived)
- [F0014-S0003: CI smoke test integration](features/archive/F0014-devops-smoke-test-automation/F0014-S0003-ci-smoke-test-integration.md) - Done (Archived)

**Infrastructure Stories (Feature F0033: Structured Logging and QE Toolchain Activation):**
- [F0033-S0001: Establish Serilog structured logging baseline](features/archive/F0033-structured-logging-and-qe-toolchain-activation/F0033-S0001-establish-serilog-structured-logging-baseline.md) - Done (Archived)
- [F0033-S0002: Activate Bruno API validation path](features/archive/F0033-structured-logging-and-qe-toolchain-activation/F0033-S0002-activate-bruno-api-validation-path.md) - Done (Archived)
- [F0033-S0003: Activate Lighthouse CI performance gate](features/archive/F0033-structured-logging-and-qe-toolchain-activation/F0033-S0003-activate-lighthouse-ci-performance-gate.md) - Done (Archived)
- [F0033-S0004: Establish broker list contract testing with Pact](features/archive/F0033-structured-logging-and-qe-toolchain-activation/F0033-S0004-establish-broker-list-contract-testing-with-pact.md) - Done (Archived)
- [F0033-S0005: Activate SonarQube Community quality reporting](features/archive/F0033-structured-logging-and-qe-toolchain-activation/F0033-S0005-activate-sonarqube-community-quality-reporting.md) - Done (Archived)

**Platform Foundation Stories (Feature F0034: Product Schema Registry and Dynamic LOB Attributes):**
- [F0034-S0001: Lock product-attribute decision set](features/archive/F0034-product-schema-registry-and-dynamic-lob-attributes/F0034-S0001-lock-product-attribute-decision-set.md) - Done (Archived)
- [F0034-S0002: Establish product schema registry foundation](features/archive/F0034-product-schema-registry-and-dynamic-lob-attributes/F0034-S0002-establish-product-schema-registry-foundation.md) - Done (Archived)
- [F0034-S0003: Pin attributes on lifecycle carriers](features/archive/F0034-product-schema-registry-and-dynamic-lob-attributes/F0034-S0003-pin-attributes-on-lifecycle-carriers.md) - Done (Archived)
- [F0034-S0004: Prove frontend and backend validator equivalence](features/archive/F0034-product-schema-registry-and-dynamic-lob-attributes/F0034-S0004-prove-validator-equivalence.md) - Done (Archived)
- [F0034-S0005: Render dynamic attribute panel from schema metadata](features/archive/F0034-product-schema-registry-and-dynamic-lob-attributes/F0034-S0005-render-dynamic-attribute-panel.md) - Done (Archived)
- [F0034-S0006: Activate Cyber product bundle](features/archive/F0034-product-schema-registry-and-dynamic-lob-attributes/F0034-S0006-activate-cyber-product-bundle.md) - Done (Archived)
- [F0034-S0007: Prove lifecycle integration and F0019 handoff](features/archive/F0034-product-schema-registry-and-dynamic-lob-attributes/F0034-S0007-prove-lifecycle-and-f0019-handoff.md) - Done (Archived)

**Release Enablement / Platform Operations Stories (Feature F0035: Session Continuity & Token Refresh):**
- [F0035-S0001: Silent Token Renewal with Concurrent Request Coalescing](features/archive/F0035-session-continuity-and-token-refresh/F0035-S0001-silent-token-renewal.md) - Done (Archived)
- [F0035-S0002: Idle Warning Modal with Grace Period](features/archive/F0035-session-continuity-and-token-refresh/F0035-S0002-idle-warning-modal.md) - Done (Archived)
- [F0035-S0003: Forced Re-Auth with Route and Form State Preservation](features/archive/F0035-session-continuity-and-token-refresh/F0035-S0003-forced-reauth-context-restore.md) - Done (Archived)
- [F0035-S0004: Auth Error Semantic Distinction](features/archive/F0035-session-continuity-and-token-refresh/F0035-S0004-auth-error-semantics.md) - Done (Archived)
- [F0035-S0005: Session Continuity Telemetry Events](features/archive/F0035-session-continuity-and-token-refresh/F0035-S0005-session-telemetry-events.md) - Done (Archived)

**CRM Release MVP Stories (Feature F0023: Global Search, Saved Views & Operational Reporting):**
- [F0023-S0001: Global search returns grouped CRM results](features/archive/F0023-global-search-saved-views-and-operational-reporting/F0023-S0001-global-search-results.md) - Done
- [F0023-S0002: Filter, sort, and open search results](features/archive/F0023-global-search-saved-views-and-operational-reporting/F0023-S0002-filter-and-open-search-results.md) - Done
- [F0023-S0003: Personal saved views](features/archive/F0023-global-search-saved-views-and-operational-reporting/F0023-S0003-personal-saved-views.md) - Done
- [F0023-S0004: Team saved views and defaults](features/archive/F0023-global-search-saved-views-and-operational-reporting/F0023-S0004-team-saved-views.md) - Done
- [F0023-S0005: Daily operational workload report](features/archive/F0023-global-search-saved-views-and-operational-reporting/F0023-S0005-daily-operational-workload-report.md) - Done
- [F0023-S0006: Workflow aging and backlog drilldowns](features/archive/F0023-global-search-saved-views-and-operational-reporting/F0023-S0006-workflow-aging-and-backlog-report.md) - Done
- [F0023-S0007: Permission-safe search and reporting behavior](features/archive/F0023-global-search-saved-views-and-operational-reporting/F0023-S0007-permission-safe-search-and-reporting.md) - Done

**CRM Release MVP+ Stories (Feature F0024: Claims & Service Case Tracking):**
- [F0024-S0001: Create a service case from account or policy context](features/archive/F0024-claims-and-service-case-tracking/F0024-S0001-create-service-case-from-context.md) - Done
- [F0024-S0002: View service cases in workspace and 360 context](features/archive/F0024-claims-and-service-case-tracking/F0024-S0002-view-service-cases-in-context.md) - Done
- [F0024-S0003: Manage service case ownership, priority, and follow-up](features/archive/F0024-claims-and-service-case-tracking/F0024-S0003-manage-service-case-ownership-and-follow-up.md) - Done
- [F0024-S0004: Transition a service case through servicing statuses](features/archive/F0024-claims-and-service-case-tracking/F0024-S0004-transition-service-case-status.md) - Done
- [F0024-S0005: Capture claim-reference context on a service case](features/archive/F0024-claims-and-service-case-tracking/F0024-S0005-capture-claim-reference-context.md) - Done
- [F0024-S0006: Audit and permission-safe service case history](features/archive/F0024-claims-and-service-case-tracking/F0024-S0006-audit-and-permission-safe-service-case-history.md) - Done

**Platform Operations Stories (Feature F0032: Admin Configuration & Reference Data Console):**
- [F0032-S0001: Admin configuration catalog](features/archive/F0032-admin-configuration-and-reference-data-console/F0032-S0001-admin-configuration-catalog.md) - Draft
- [F0032-S0002: Draft reference data and workflow SLA configuration](features/archive/F0032-admin-configuration-and-reference-data-console/F0032-S0002-draft-reference-and-sla-configuration.md) - Draft
- [F0032-S0003: Govern queue and routing configuration drafts](features/archive/F0032-admin-configuration-and-reference-data-console/F0032-S0003-govern-queue-routing-configuration.md) - Draft
- [F0032-S0004: Validate and compare configuration before publish](features/archive/F0032-admin-configuration-and-reference-data-console/F0032-S0004-validate-and-compare-configuration.md) - Draft
- [F0032-S0005: Publish and roll back configuration sets](features/archive/F0032-admin-configuration-and-reference-data-console/F0032-S0005-publish-and-rollback-configuration.md) - Draft
- [F0032-S0006: Audit and permission-safe configuration behavior](features/archive/F0032-admin-configuration-and-reference-data-console/F0032-S0006-audit-and-permission-safe-admin-configuration.md) - Draft

**MVP+ Stories (Feature F0028: Carrier & Market Relationship Management):**
- [F0028-S0001: Market directory search and open](features/archive/F0028-carrier-and-market-relationship-management/F0028-S0001-market-directory-search.md) - Done (Archived)
- [F0028-S0002: Carrier and market profile management](features/archive/F0028-carrier-and-market-relationship-management/F0028-S0002-carrier-market-profile-management.md) - Done (Archived)
- [F0028-S0003: Underwriter and market contact management](features/archive/F0028-carrier-and-market-relationship-management/F0028-S0003-underwriter-contact-management.md) - Done (Archived)
- [F0028-S0004: Appetite note capture and freshness](features/archive/F0028-carrier-and-market-relationship-management/F0028-S0004-appetite-note-capture.md) - Done (Archived)
- [F0028-S0005: Appointment context management](features/archive/F0028-carrier-and-market-relationship-management/F0028-S0005-appointment-context-management.md) - Done (Archived)
- [F0028-S0006: Market activity and related work visibility](features/archive/F0028-carrier-and-market-relationship-management/F0028-S0006-market-activity-and-related-work.md) - Done (Archived)

**CRM Release MVP+ Stories (Feature F0008: Broker Insights):**
- [F0008-S0001: Broker scorecard overview](features/archive/F0008-broker-insights/F0008-S0001-broker-scorecard-overview.md) - Done (Archived)
- [F0008-S0002: Trend drilldown and source record navigation](features/archive/F0008-broker-insights/F0008-S0002-trend-drilldown-source-records.md) - Done (Archived)
- [F0008-S0003: Authorized benchmark comparison](features/archive/F0008-broker-insights/F0008-S0003-authorized-benchmark-comparison.md) - Done (Archived)
- [F0008-S0004: Broker review snapshot](features/archive/F0008-broker-insights/F0008-S0004-review-snapshot.md) - Done (Archived)
- [F0008-S0005: Permission-safe broker insight behavior](features/archive/F0008-broker-insights/F0008-S0005-permission-safe-insights.md) - Done (Archived)

**MVP Stories (Feature F0010: Dashboard Opportunities Refactor):**
- [F0010-S0001: Replace Sankey default with Pipeline Board](features/archive/F0010-dashboard-opportunities-refactor/F0010-S0001-replace-sankey-with-pipeline-board-default.md) - Done (Historical; superseded by F0013)
- [F0010-S0002: Add Opportunities Aging Heatmap view](features/archive/F0010-dashboard-opportunities-refactor/F0010-S0002-add-opportunity-aging-heatmap-view.md) - Done (Historical; superseded by F0013)
- [F0010-S0003: Add Opportunities Composition Treemap view](features/archive/F0010-dashboard-opportunities-refactor/F0010-S0003-add-opportunity-composition-treemap-view.md) - Done (Historical; superseded by F0013)
- [F0010-S0004: Add Opportunities Hierarchy Sunburst view](features/archive/F0010-dashboard-opportunities-refactor/F0010-S0004-add-opportunity-hierarchy-sunburst-view.md) - Done (Historical; superseded by F0013)
- [F0010-S0005: Unify drilldown, responsive layout, and accessibility](features/archive/F0010-dashboard-opportunities-refactor/F0010-S0005-unify-drilldown-responsive-and-accessibility.md) - Done (Historical; superseded by F0013)

**MVP Stories (Feature F0011: Dashboard Opportunities Flow-First Modernization):**
- [F0011-S0001: Replace Pipeline Board tiles with connected flow-first canvas default](features/archive/F0011-dashboard-opportunities-flow-modernization/F0011-S0001-replace-pipeline-board-with-connected-flow-default.md) - Abandoned (Not implemented; superseded by F0013)
- [F0011-S0002: Add terminal outcomes rail and outcome drilldowns](features/archive/F0011-dashboard-opportunities-flow-modernization/F0011-S0002-add-terminal-outcomes-rail-and-drilldowns.md) - Abandoned (Not implemented; superseded by F0013)
- [F0011-S0003: Apply modern opportunities visual system](features/archive/F0011-dashboard-opportunities-flow-modernization/F0011-S0003-apply-modern-opportunities-visual-system.md) - Abandoned (Not implemented; superseded by F0013)
- [F0011-S0004: Rebalance secondary insights as mini-views](features/archive/F0011-dashboard-opportunities-flow-modernization/F0011-S0004-rebalance-secondary-insights-as-mini-views.md) - Abandoned (Not implemented; superseded by F0013)
- [F0011-S0005: Ensure responsive and accessibility parity](features/archive/F0011-dashboard-opportunities-flow-modernization/F0011-S0005-ensure-responsive-and-accessibility-parity.md) - Abandoned (Not implemented; superseded by F0013)

**MVP Stories (Feature F0012: Dashboard Storytelling Infographic Refactor):**
- [F0012-S0001: Unify KPI strip and opportunities into one interactive story canvas](features/archive/F0012-dashboard-storytelling-infographic-canvas/F0012-S0001-unify-kpi-and-opportunities-into-single-story-canvas.md) - Done (Archived)
- [F0012-S0002: Add interactive story chapters and in-canvas analytical overlays](features/archive/F0012-dashboard-storytelling-infographic-canvas/F0012-S0002-build-interactive-opportunities-story-chapters-and-overlays.md) - Done (Archived)
- [F0012-S0003: Move Activity and My Tasks below the story canvas as traditional panels](features/archive/F0012-dashboard-storytelling-infographic-canvas/F0012-S0003-reflow-dashboard-layout-with-activity-and-tasks-below-canvas.md) - Done (Archived)
- [F0012-S0004: Preserve collapsible left nav and right Neuron rail with adaptive canvas width](features/archive/F0012-dashboard-storytelling-infographic-canvas/F0012-S0004-support-collapsible-nav-and-neuron-rails-with-adaptive-canvas-width.md) - Done (Archived)
- [F0012-S0005: Ensure responsive, accessibility, and performance parity for storytelling dashboard](features/archive/F0012-dashboard-storytelling-infographic-canvas/F0012-S0005-ensure-responsive-accessibility-and-performance-parity-for-story-canvas.md) - Done (Archived)

**MVP Stories (Feature F0013: Dashboard Framed Storytelling Canvas):**
- [F0013-S0000: Editorial palette refresh — dark & light themes](features/archive/F0013-dashboard-framed-storytelling-canvas/F0013-S0000-editorial-palette-refresh-dark-and-light-themes.md) - Done (Archived)
- [F0013-S0001: Restore framed canvas identity with three-layer visual hierarchy](features/archive/F0013-dashboard-framed-storytelling-canvas/F0013-S0001-restore-framed-canvas-identity-with-three-layer-visual-hierarchy.md) - Done (Archived)
- [F0013-S0002: Build vertical timeline with connected stage nodes and terminal outcome branches](features/archive/F0013-dashboard-framed-storytelling-canvas/F0013-S0002-build-timeline-bar-with-connected-stage-nodes-and-terminal-branches.md) - Done (Archived)
- [F0013-S0003: Add contextual mini-visualizations at each timeline stage node](features/archive/F0013-dashboard-framed-storytelling-canvas/F0013-S0003-add-radial-donut-chart-popovers-at-each-timeline-stage-node.md) - Done (Archived)
- [F0013-S0004: Connect chapter controls as uniform override for timeline visualizations](features/archive/F0013-dashboard-framed-storytelling-canvas/F0013-S0004-connect-chapter-controls-to-radial-popover-data-layers.md) - Done (Archived)
- [F0013-S0005: Ensure responsive, accessibility, and performance parity for framed storytelling canvas](features/archive/F0013-dashboard-framed-storytelling-canvas/F0013-S0005-ensure-responsive-accessibility-and-performance-parity.md) - Done (Archived)

**CRM Release MVP Stories (Feature F0006: Submission Intake Workflow):**
- [F0006-S0001: Submission pipeline list with intake status filtering](features/archive/F0006-submission-intake-workflow/F0006-S0001-submission-pipeline-list-with-intake-status-filtering.md) - Done (Archived)
- [F0006-S0002: Create submission for new business intake](features/archive/F0006-submission-intake-workflow/F0006-S0002-create-submission-for-new-business-intake.md) - Done (Archived)
- [F0006-S0003: Submission detail view with intake context](features/archive/F0006-submission-intake-workflow/F0006-S0003-submission-detail-view-with-intake-context.md) - Done (Archived)
- [F0006-S0004: Submission intake status transitions](features/archive/F0006-submission-intake-workflow/F0006-S0004-submission-intake-status-transitions.md) - Done (Archived)
- [F0006-S0005: Submission completeness evaluation](features/archive/F0006-submission-intake-workflow/F0006-S0005-submission-completeness-evaluation.md) - Done (Archived)
- [F0006-S0006: Submission ownership assignment and underwriting handoff](features/archive/F0006-submission-intake-workflow/F0006-S0006-submission-ownership-assignment-and-underwriting-handoff.md) - Done (Archived)
- [F0006-S0007: Submission activity timeline and audit trail](features/archive/F0006-submission-intake-workflow/F0006-S0007-submission-activity-timeline-and-audit-trail.md) - Done (Archived)
- [F0006-S0008: Stale submission visibility and follow-up flags](features/archive/F0006-submission-intake-workflow/F0006-S0008-stale-submission-visibility-and-follow-up-flags.md) - Done (Archived)

**CRM Release MVP Stories (Feature F0007: Renewal Pipeline):**
- [F0007-S0001: Renewal pipeline list with due-window filtering](features/archive/F0007-renewal-pipeline/F0007-S0001-renewal-pipeline-list-with-due-window-filtering.md) - Done (Archived)
- [F0007-S0002: Renewal detail view with policy context and outreach history](features/archive/F0007-renewal-pipeline/F0007-S0002-renewal-detail-view-with-policy-context.md) - Done (Archived)
- [F0007-S0003: Renewal status transitions](features/archive/F0007-renewal-pipeline/F0007-S0003-renewal-status-transitions.md) - Done (Archived)
- [F0007-S0004: Renewal ownership assignment and handoff](features/archive/F0007-renewal-pipeline/F0007-S0004-renewal-ownership-assignment-and-handoff.md) - Done (Archived)
- [F0007-S0005: Overdue renewal visibility and escalation flags](features/archive/F0007-renewal-pipeline/F0007-S0005-overdue-renewal-visibility-and-escalation-flags.md) - Done (Archived)
- [F0007-S0006: Create renewal from expiring policy](features/archive/F0007-renewal-pipeline/F0007-S0006-create-renewal-from-expiring-policy.md) - Done (Archived)
- [F0007-S0007: Renewal activity timeline and audit trail](features/archive/F0007-renewal-pipeline/F0007-S0007-renewal-activity-timeline-and-audit-trail.md) - Done (Archived)

**Story Index:** See `planning-mds/features/STORY-INDEX.md` for auto-generated summary of all stories (if generated).

Reference examples also live under `planning-mds/examples/stories/`.

### 3.5 Screen list (MVP)

- Navigation Shell
- Dashboard
- Broker List
- Broker 360
- Task Center (optional MVP)
- Submission Pipeline List (F0006)
- Submission Detail (F0006)
- Create Submission (F0006)
- Renewal Pipeline List (F0007)
- Renewal Detail (F0007)
- Dynamic Attribute Panel (F0034)
- Global Search Overlay (F0023)
- Search Results Workspace (F0023)
- Saved Views Drawer (F0023)
- Operational Reports Workspace (F0023)
- Admin Configuration Catalog (F0032)
- Configuration Domain Detail (F0032)
- Configuration Draft Editor (F0032)
- Validation and Compare Drawer (F0032)
- Configuration Audit Workspace (F0032)
- Broker Insights Workspace (F0008)
- Broker Scorecard Panel (F0008)
- Trend Drilldown Drawer (F0008)
- Benchmark Comparison View (F0008)
- Review Snapshot View (F0008)
- Admin minimal (roles/policies optional MVP)

Screen baseline details:
- Navigation Shell: authenticated app shell, role-aware navigation, global search entry, notifications placeholder.
- Dashboard: role-aware landing screen with five widgets — nudge cards (dismissible action prompts for time-sensitive items), KPI metrics cards, pipeline summary (mini-Kanban with status pills and expandable card previews), my tasks & reminders, and broker activity feed. All widgets enforce ABAC scope and degrade gracefully when upstream entities are unavailable.
- Broker List: sortable/filterable list, quick search, create action, status tags.
- Broker 360: profile header, contacts, hierarchy/program links, immutable timeline panel.
- Task Center: assigned tasks, due dates, simple status states, reminder hooks.
- Dynamic Attribute Panel: schema-pinned product attributes, normalized validation errors, legacy read-only state, and Cyber pilot fields embedded in submission, policy, endorsement, and renewal surfaces.
- Global Search Overlay: authenticated shell entry for permission-scoped cross-object search across broker, MGA/program, account, policy, submission, renewal, and task records.
- Search Results Workspace: deep-linkable search criteria, filters, grouped result lists, pagination, and source-record navigation.
- Saved Views Drawer: personal and team view list, apply/save/update/delete controls, and manager/admin team defaults.
- Operational Reports Workspace: due-work, workload by owner/status, and workflow aging/backlog reports with source-record drilldowns.
- Admin Configuration Catalog: internal-only configuration entry point showing supported domains, published version, draft state, validation status, and unsupported-domain boundaries.
- Configuration Domain Detail: domain-level view of published configuration, drafts, validation status, compare summary, publish/rollback actions, and downstream refresh state.
- Configuration Draft Editor: supported field editing for first-release configuration domains without changing published runtime behavior until publish.
- Validation and Compare Drawer: review validation errors, warnings, version deltas, and downstream impact notes before publish.
- Configuration Audit Workspace: permission-safe audit review for create, update, validate, publish, rollback, and failed configuration attempts.
- Admin minimal: role assignment visibility and policy diagnostics (read-focused in MVP).

---

## 4) Phase B — Architect Spec (Public Baseline)

**Status: APPROVED (2026-02-14)** — Dashboard-first architecture approved as the planning baseline. Phase C implementation is complete for F0001/F0002/F0003/F0004/F0005/F0006/F0007/F0009/F0012/F0013/F0014/F0015/F0033.

This section defines the build-ready technical baseline for the reference implementation.

**Architecture Decision Records:** See `planning-mds/architecture/decisions/` for detailed ADRs:
- [ADR-001](architecture/decisions/ADR-001-json-schema-validation.md) — JSON Schema Validation
- [ADR-Auth](architecture/decisions/ADR-Authentication-Strategy.md) — Authentication Strategy (Keycloak — **superseded**)
- [ADR-006](architecture/decisions/ADR-006-authentik-idp-migration.md) — Authentication Strategy (authentik — **current**)
- [ADR-Token](architecture/decisions/ADR-Auth-Token-Storage.md) — Auth Token Storage (Hybrid)
- [ADR-002](architecture/decisions/ADR-002-dashboard-data-aggregation.md) — Dashboard Data Aggregation (per-widget endpoints)
- [ADR-003](architecture/decisions/ADR-003-task-entity-nudge-engine.md) — Task Entity & Nudge Engine
- [ADR-004](architecture/decisions/ADR-004-frontend-dashboard-widget-architecture.md) — Frontend Dashboard Widget Architecture
- [ADR-012](architecture/decisions/ADR-012-shared-document-storage-and-metadata-architecture.md) — Shared Document Storage and Metadata Architecture (F0020 — finalised 2026-05-04)
- [ADR-019](architecture/decisions/ADR-019-mock-quarantine-then-promote-ingest-pipeline.md) — Mock-quarantine-then-promote ingest pipeline (F0020)
- [ADR-020](architecture/decisions/ADR-020-lob-extensible-attribute-architecture.md) — LOB extensible attribute architecture (F0034)
- [ADR-021](architecture/decisions/ADR-021-form-engine-rhf-ajv-shadcn-registry.md) — Dynamic form engine with RHF, AJV, and shadcn widget registry (F0034)
- [ADR-022](architecture/decisions/ADR-022-validator-equivalence-restricted-profile.md) — Validator equivalence and restricted JSON Schema profile (F0034)
- [ADR-023](architecture/decisions/ADR-023-rules-governance-jsonlogic.md) — JsonLogic rules governance (F0034)
- [ADR-014](architecture/decisions/ADR-014-search-index-and-saved-view-architecture.md) — Search index, saved views, and operational reporting projections (F0023)
- [ADR-032](architecture/decisions/ADR-032-admin-configuration-console-contract.md) — Admin configuration console contract (F0032)
- [ADR-033](architecture/decisions/ADR-033-commission-producer-splits-and-revenue-tracking.md) — Commission, producer splits, and revenue tracking (F0025)

**F0023 addendum status:** Approved 2026-06-19 in plan run `2026-06-19-2f180001`.

**Data Model Supplement:** See `planning-mds/architecture/data-model.md` for Task entity, dashboard indexes, query patterns, F0017 hierarchy/territory, F0034 product schema registry, F0023 search/reporting read models, and F0032 admin configuration draft/publish/audit records. F0020 documents are filesystem-first (no relational entity in MVP); see `planning-mds/features/archive/F0020-document-management-and-acord-intake/README.md` for the on-disk layout and the `IDocumentRepository` boundary.

### 4.1 Service boundaries

- Architecture shape: modular monolith (single deployable) with clean module boundaries and internal APIs.
- **Dashboard module (F0001):**
  - Owns dashboard-specific read endpoints (KPIs, pipeline summary, nudges).
  - Reads across BrokerRelationship, Submission, Renewal, TaskManagement, and TimelineAudit modules.
  - No owned entities — purely a query/aggregation layer.
  - See [ADR-002](architecture/decisions/ADR-002-dashboard-data-aggregation.md) for per-widget endpoint design.
- **TaskManagement module (F0001 + F0003):**
  - Owns the Task entity (CRUD + status transitions).
  - Provides `GET /my/tasks` for dashboard and Task Center.
  - All Task mutations generate ActivityTimelineEvent records.
  - See [ADR-003](architecture/decisions/ADR-003-task-entity-nudge-engine.md) for entity design.
- BrokerRelationship module:
  - Owns Broker, Contact, MGA, Program relationship mappings.
  - Handles broker/contact CRUD, hierarchy links, broker search.
- Account module:
  - Owns Account profile and account-level relationship views.
  - Provides account context for submissions and renewals.
- Submission module:
  - Owns Submission aggregate and Submission workflow operations.
  - Enforces transition gates/checklists before status moves.
- Renewal module:
  - Owns Renewal aggregate and Renewal workflow operations.
  - Tracks outreach and renewal-specific lifecycle.
- TimelineAudit module:
  - Owns ActivityTimelineEvent and WorkflowTransition append-only records.
  - Provides timeline query/read APIs (including `GET /timeline/events` for dashboard activity feed).
- ProductSchemaRegistry module (F0034):
  - Owns LobProduct, LobProductVersion, LobSchemaBundle, and LobBundleActivationEvent.
  - Serves active and direct schema-bundle reads for dynamic product attributes.
  - Enforces bundle signatures, deterministic product-version ids, restricted schema profile, OpenAPI projection compatibility, and activation audit.
  - Does not own lifecycle rows; Submission, Renewal, PolicyVersion, and PolicyEndorsement pin product versions and carry attributes.
- SearchReporting module (F0023):
  - Owns SearchDocument, SavedView, SavedViewAuditEvent, and OperationalReportProjection read-side records.
  - Reads across BrokerRelationship, Account, Policy, Submission, Renewal, TaskManagement, TimelineAudit, and F0017 distribution structure where available.
  - Provides global search, saved-view CRUD/defaults, and operational-report endpoints.
  - Enforces high-level Casbin feature actions and mandatory query-layer source-record authorization before returning rows, snippets, facets, counts, or drilldowns.
  - Uses PostgreSQL full-text search and read-side projections for MVP; no external search engine is introduced.
- CommissionRevenue module (F0025):
  - Owns CommissionSchedule, ProducerSplitAssignment, ProducerSplitParticipant, ExpectedCommission, CommissionAdjustment, and RevenueAttributionProjection.
  - Reads policy premium/version context from Policy, carrier/market context from CarrierMarkets, and producer/territory attribution from F0017 modules.
  - Persists expected commission as CRM review data with source snapshots; it does not own ledger, payment, payout, billing, reconciliation, tax, or statement records.
  - Emits ActivityTimelineEvent records for every schedule, split, calculation, adjustment, and projection mutation.
  - Filters rows, totals, counts, facets, and drilldowns through source-record authorization before returning economic data.
- OperationsRouting module (F0022):
  - Owns WorkQueue, WorkQueueMember, AssignmentRule, CoverageWindow, QueueWorkItem, and RoutingDecisionLog.
  - Routes tasks, submissions, and renewals through deterministic rule precedence: manual override, coverage, territory/ownership, workload, fallback.
  - Reads source work through narrow adapters and updates source assignment only through existing source assignment ports.
  - Provides WorkQueues API endpoints for manager/admin queue operations before F0032 centralizes governance.
  - Enforces queue-level Casbin actions plus source-record ABAC before exposing queue work item details.
- AdminConfiguration module (F0032):
  - Owns ConfigurationDomain, ConfigurationDraft, ConfigurationValidationResult, PublishedOperationalConfigurationSet, ConfigurationRefreshStatus, and ConfigurationAuditEvent.
  - Provides a governed facade over F0022 queue/routing configuration, workflow SLA thresholds, F0023 saved-view/report defaults, and F0027 template metadata.
  - Calls source-module adapters for validation and publication; source modules remain authoritative for queue execution, report/search behavior, document generation, and product schema activation.
  - Enforces Admin-only publish/rollback, delegated steward draft/validate access, and permission-safe audit redaction.
- IdentityAuthorization module:
  - Validates authentik JWT tokens (JWKS from `Authentication__Authority/.well-known/openid-configuration`).
  - Normalizes `(iss, sub)` claims to internal `NebulaPrincipal { UserId, Roles, Regions }` via `IClaimsPrincipalNormalizer`.
  - Enforces Casbin ABAC policies at API/application boundaries.

### 4.2 Data model (detailed)

Define tables/fields for:

- Broker, Contact, UserProfile, UserPreference, ActivityTimelineEvent, WorkflowTransition
- F0022 OperationsRouting entities: WorkQueue, WorkQueueMember, AssignmentRule, CoverageWindow, QueueWorkItem, RoutingDecisionLog
- Reference tables + seed strategy

Core entities (minimum baseline):
- Account
  - Id (uuid), Name, Industry, PrimaryState, Region, Status
  - CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted
- Broker
  - Id (uuid), LegalName, LicenseNumber, State, Status, ManagedByUserId (uuid?, FK → UserProfile.UserId)
  - MgaId (nullable), PrimaryProgramId (nullable)
  - CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted
- BrokerRegion (new — multi-region broker scope)
  - BrokerId (uuid), Region (string)
  - Composite PK (BrokerId, Region)
- MGA
  - Id (uuid), Name, ExternalCode, Status
  - CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted
- Program
  - Id (uuid), Name, ProgramCode, MgaId, ManagedByUserId (uuid?, FK → UserProfile.UserId)
  - CreatedAt, CreatedByUserId (uuid), UpdatedAt, UpdatedByUserId (uuid?), IsDeleted
- Contact
  - Id (uuid), BrokerId (nullable), AccountId (nullable), FullName, Email, Phone, Role
  - CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted
- Submission
  - Id (uuid), AccountId, BrokerId, ProgramId (nullable), CurrentStatus, EffectiveDate, PremiumEstimate, AssignedToUserId (uuid, FK → UserProfile.UserId)
  - CreatedAt, CreatedByUserId (uuid), UpdatedAt, UpdatedByUserId (uuid?), IsDeleted
- Renewal
  - Id (uuid), AccountId, BrokerId, PolicyId, CurrentStatus, PolicyExpirationDate, TargetOutreachDate, AssignedToUserId (uuid, FK → UserProfile.UserId)
  - LineOfBusiness (nullable), LostReasonCode (nullable), LostReasonDetail (nullable), BoundPolicyId (nullable), RenewalSubmissionId (nullable)
  - CreatedAt, CreatedByUserId (uuid), UpdatedAt, UpdatedByUserId (uuid?), IsDeleted
- **Task** (new — required by Dashboard F0001 and Task Center F0003)
  - Id (uuid), Title, Description (nullable), Status (Open/InProgress/Done), Priority (Low/Normal/High/Urgent)
  - DueDate (nullable), AssignedToUserId (uuid, FK → UserProfile.UserId)
  - LinkedEntityType (nullable), LinkedEntityId (nullable) — polymorphic link to Broker/Submission/Renewal/Account
  - CreatedAt, CreatedByUserId (uuid), UpdatedAt, UpdatedByUserId (uuid?), CompletedAt (nullable), IsDeleted
  - See [data-model.md](architecture/data-model.md) for full table definition, indexes, and audit requirements
- UserProfile
  - UserId (uuid, PK), IdpIssuer (varchar), IdpSubject (varchar) — UNIQUE(IdpIssuer, IdpSubject)
  - Email, DisplayName, Department, RegionsJson, RolesJson
  - CreatedAt, UpdatedAt
  - (No longer keyed by raw IdP sub — see ADR-006 for principal key design)
- UserPreference
  - Id (uuid), Subject, PreferenceKey, PreferenceValueJson
  - CreatedAt, UpdatedAt
- ActivityTimelineEvent (append-only)
  - Id (uuid), EntityType, EntityId, EventType, EventPayloadJson, ActorUserId (uuid, logical ref → UserProfile.UserId), OccurredAt
- WorkflowTransition (append-only)
  - Id (uuid), WorkflowType, EntityId, FromState, ToState, Reason, ActorUserId (uuid, logical ref → UserProfile.UserId), OccurredAt
- LobProduct (F0034)
  - Id (uuid), Code, ProductKind, LineOfBusiness, DisplayName, CreatedAt
- LobProductVersion (F0034)
  - Id (uuid), ProductId, Version, Status, Signature, SignatureKeyId, ActivatedAt, DeprecatedAt, RetiredAt
- LobSchemaBundle (F0034)
  - Id (uuid), ProductVersionId, Stage, SchemaHash, DataSchemaJson, UiSchemaJson, RulesJson, ProjectionsJson
- LobBundleActivationEvent (append-only, F0034)
  - Id (uuid), ProductVersionId, FromStatus, ToStatus, Reason, ActorUserId, OccurredAt
- SearchDocument (F0023)
  - Id (uuid), ObjectType, ObjectId, TargetUrl, Title, Subtitle (nullable), Status (nullable), OwnerUserId (nullable), OwnerDisplayName (nullable)
  - AccountId/BrokerId/PolicyId/SubmissionId/RenewalId/TaskId (nullable), LineOfBusiness (nullable), Region (nullable), ProgramId/TerritoryId (nullable)
  - SearchText, SearchVector, MatchedFieldHintsJson, SourceUpdatedAt, IndexedAt, LastProjectionError (nullable)
  - Unique `(ObjectType, ObjectId)`, GIN `SearchVector`, and source/filter indexes
- SavedView (F0023)
  - Id (uuid), Name, NormalizedName, Description (nullable), ViewType, Visibility, OwnerUserId
  - TeamScopeType/TeamScopeKey (required for team views), CriteriaJson, SortJson, IsDefault, ArchivedAt (nullable), LastEditedByUserId (nullable), RowVersion
  - Unique active personal/team names and one active default per personal/team scope and ViewType
- SavedViewAuditEvent (append-only, F0023)
  - Id (uuid), SavedViewId, EventType, ActorUserId, OccurredAt, BeforeJson (nullable), AfterJson (nullable)
- OperationalReportProjection (F0023)
  - Id (uuid), SourceObjectType, SourceObjectId, TargetUrl, WorkflowType (nullable), CurrentStatus (nullable), StatusEnteredAt (nullable), DaysInStatus (nullable)
  - OwnerUserId/OwnerDisplayName, DueDate, IsDueToday, IsOverdue, AgeBand, AccountId/BrokerId/PolicyId, LineOfBusiness, Region, ProgramId/TerritoryId
  - LastSourceUpdatedAt, ProjectedAt; unique `(SourceObjectType, SourceObjectId)` and report/filter indexes
- ConfigurationDomain (F0032)
  - DomainKey, DisplayName, OwningModule, Status, EditableSchemaRef, ValidationStrategy, RefreshStrategy, SupportsRollback
  - Seeded for queue-routing, workflow-sla-thresholds, saved-view-report-defaults, and template-metadata
- ConfigurationDraft (F0032)
  - Id, DomainKey, BasePublishedVersion, DraftVersion, Status, PayloadJson, PayloadHash, LatestValidationResultId, RowVersion
  - At most one active non-published draft per domain
- ConfigurationValidationResult (F0032)
  - Id, DraftId, Status, DraftPayloadHash, BlockingErrorsJson, WarningsJson, CompareSummaryJson, ValidatedAt, ValidatedByUserId
- PublishedOperationalConfigurationSet (F0032)
  - Id, DomainKey, PublishedVersion, PayloadSnapshotJson, PayloadHash, SourceDraftId, RollbackOfPublishedVersion, PublishedAt, PublishedByUserId
  - Latest published version is authoritative; rollback creates a new version
- ConfigurationRefreshStatus and ConfigurationAuditEvent (F0032)
  - RefreshStatus records consumer cache/refresh handshake
  - AuditEvent records Created, Updated, Validated, Published, RolledBack, RefreshSucceeded, and RefreshFailed actions
- CommissionSchedule (F0025)
  - Id, CarrierMarketId, LineOfBusiness, State/ProductCode optional scope, RateType, RateValue, Basis, EffectiveFrom/EffectiveTo, SourceNote, RowVersion
  - Active schedules must not overlap for the same carrier/LOB/state/product scope.
- ProducerSplitAssignment and ProducerSplitParticipant (F0025)
  - Assignment: Id, PolicyId, PolicyVersionId, EffectiveFrom/EffectiveTo, Basis, Status, RowVersion
  - Participant: SplitAssignmentId, ProducerNodeId, ProducerDisplayNameSnapshot, SplitPercent, Role
  - Active participant percentages must total exactly 100.0000.
- ExpectedCommission (F0025)
  - Id, PolicyId, PolicyVersionId, CarrierMarketId, CommissionScheduleId, ProducerSplitAssignmentId, PremiumBasisAmount, ExpectedGrossCommissionAmount, ApprovedAdjustmentAmount, AdjustedExpectedCommissionAmount, CalculationStatus, SourceSnapshotHash, CalculatedAt/By, RowVersion
  - Source policy, schedule, and split rows remain authoritative; expected commission is review output with provenance.
- CommissionAdjustment (F0025)
  - Id, ExpectedCommissionId, AdjustmentAmount, Reason, EffectiveDate, Status, RequestedBy/At, DecisionBy/At, DecisionNote, RowVersion
  - Pending -> Approved/Rejected single-step workflow; same-user request and approval is denied.
- RevenueAttributionProjection (F0025)
  - Id, ExpectedCommissionId, policy period dimensions, ProducerNodeId, BrokerId, TerritoryId, CarrierMarketId, LineOfBusiness, Region, expected/adjusted/allocation amounts, exception status, LastSourceUpdatedAt, ProjectedAt
  - Rollup APIs aggregate only after source-record authorization filtering.

F0034 attribute-carrier additions:
- Submission, Renewal, PolicyVersion, and PolicyEndorsement carry `LobProductVersionId` plus `AttributesJson`.
- PolicyVersion and PolicyEndorsement also carry immutable denormalized `LineOfBusiness`.
- Policy has no independent attributes source; current policy attributes resolve through `CurrentVersionId -> PolicyVersion`.

Reference tables and seed strategy:
- ReferenceState, ReferenceIndustry, ReferenceTaskStatus, ReferenceSubmissionStatus, ReferenceRenewalStatus
- See [data-model.md Section 1.2](architecture/data-model.md) for complete seed definitions including status descriptions, terminal flags, and display metadata.
- Deterministic EF seed/migration scripts with idempotent upsert semantics.
- Runtime writes to reference tables are restricted to admin-only actions.

### 4.3 Workflow rules

Define allowed transitions and gating validations (Submission and Renewal).

Submission workflow transitions:
- Received -> Triaging
- Triaging -> WaitingOnBroker or ReadyForUWReview
- WaitingOnBroker -> ReadyForUWReview
- ReadyForUWReview -> InReview
- InReview -> Quoted or Declined
- Quoted -> BindRequested or Withdrawn
- BindRequested -> Bound or Declined

Renewal workflow transitions:
- Identified -> Outreach
- Outreach -> InReview
- InReview -> Quoted or Lost
- Quoted -> Completed or Lost

Transition rules and validations:
- Invalid transition pairs return HTTP 409 with `ProblemDetails` (`code=invalid_transition`).
- Missing required checklist/data preconditions return HTTP 409 with `ProblemDetails` (`code=missing_transition_prerequisite`).
- Subject must have permission for the requested transition action (otherwise HTTP 403).
- Submission/renewal creation must validate region alignment: `Account.Region` must be included in the broker's `BrokerRegion` set; otherwise return HTTP 400 with `ProblemDetails` (`code=region_mismatch`).
- Every successful transition appends:
  - one WorkflowTransition record
  - one ActivityTimelineEvent record
- Transition records are immutable; corrections happen via compensating transitions.

F0032 configuration lifecycle:
- DraftCreated -> DraftUpdated -> ValidationPassed -> Published
- DraftCreated -> DraftUpdated -> ValidationFailed -> DraftUpdated
- Published -> RollbackRequested -> Published

F0032 publication rules:
- Draft payloads never affect runtime behavior.
- Publish requires the latest validation result to be `Passed` and to match the current draft `PayloadHash`.
- Publish returns HTTP 409 when the draft base version is no longer the current published version.
- Draft update requires `If-Match`; stale row versions return HTTP 412.
- Rollback creates a new published version from an eligible prior snapshot and preserves all intervening audit history.

### 4.4 Authorization model (ABAC)

Define subject attributes (from UserProfile), resource attributes, actions.
Define minimal policies for Phase 0 and Phase 1.

Subject attributes (from JWT + UserProfile):
- subjectId, roles, department, regions, internalUser flag

Resource attributes:
- resourceType, ownerAccountId, brokerId, programId, accountRegion, internalOnly flag, workflowState

Actions (examples):
- broker:create, broker:read, broker:update, broker:delete
- contact:create, contact:read, contact:update, contact:delete
- submission:transition, renewal:transition
- timeline:read

Policy baseline:
- Internal distribution and relationship roles can create/read/update Broker and Contact.
- Underwriters have read access to broker/account context and transition access within underwriting stages.
- Admin has broad management access including policy administration.
- InternalOnly resources are denied to non-internal subjects.
- Enforcement is server-side only via Casbin middleware and application guards.
- Product schema bundles are InternalOnly. Internal roles may read and resolve bundles needed for records they can access; Admin alone may activate, deprecate, or retire bundles in MVP.
- F0023 high-level resources:
  - `global_search:read` for internal roles only.
  - `saved_view:read` for internal roles; personal/team eligibility is filtered at query layer.
  - `saved_view:manage` and `saved_view:default` for personal owners and manager/admin team-scope administrators.
  - `operational_report:read` for internal roles only.
- F0023 query-layer authorization is mandatory for source rows, snippets, suggestions, facets, counts, report summaries, and drilldowns. BrokerUser and ExternalUser have no F0023 policy lines.
- F0032 high-level resources:
  - `admin_configuration:read` for Admin, OperationsManager, ConfigurationSteward, and ComplianceQualityLead.
  - `admin_configuration:draft` and `admin_configuration:validate` for Admin and delegated ConfigurationSteward.
  - `admin_configuration:publish` and `admin_configuration:rollback` for Admin only.
  - `admin_configuration:audit` for Admin and ComplianceQualityLead.
- F0032 audit queries must redact domain payload fragments when the caller has audit access but lacks the source module's read policy.
- F0025 high-level resources:
  - `commission:read` for authorized internal roles only.
  - `commission:schedule_manage` for Admin and RelationshipManager.
  - `commission:split_assign`, `commission:calculate`, and `commission:adjustment_approve` for DistributionManager and Admin.
  - `commission:adjustment_request` for DistributionUser, DistributionManager, and Admin.
  - `commission:rollup_read` for authorized internal management/reporting roles only.
- F0025 source-record authorization is mandatory for economic rows, totals, counts, facets, and drilldowns. BrokerUser and ExternalUser have no F0025 policy lines.

### 4.5 API Contracts

Define endpoints + request/response contracts + error contract.

Primary OpenAPI contract:
- `planning-mds/api/nebula-api.yaml`

Entity coverage in API surface:
- Account, Broker, MGA, Program, Contact, Submission, Renewal, ActivityTimelineEvent, WorkflowTransition
- WorkQueue, AssignmentRule, CoverageWindow, QueueWorkItem, RoutingEvent

MVP endpoint pattern examples:
- GET `/brokers`
- POST `/brokers`
- GET `/brokers/{brokerId}`
- PUT `/brokers/{brokerId}`
- DELETE `/brokers/{brokerId}`
- GET `/contacts`
- POST `/contacts`
- GET `/submissions/{submissionId}/transitions`
- POST `/submissions/{submissionId}/transitions`
- GET `/renewals/{renewalId}/transitions`
- POST `/renewals/{renewalId}/transitions`

Dashboard endpoints (F0001 — per-widget, see [ADR-002](architecture/decisions/ADR-002-dashboard-data-aggregation.md)):
- GET `/dashboard/kpis` — KPI metrics (active brokers, open subs, renewal rate, avg turnaround)
- GET `/dashboard/pipeline` — Pipeline summary counts by status
- GET `/dashboard/pipeline/{entityType}/{status}/items` — Lazy-loaded mini-cards (max 5)
- GET `/dashboard/nudges` — Prioritized nudge cards (max 3)
- GET `/my/tasks` — Tasks assigned to authenticated user
- GET `/timeline/events?entityType=Broker&limit=20` — Broker activity feed

Task CRUD endpoints (F0001 + F0003):
- POST `/tasks` — Create task
- GET `/tasks/{taskId}` — Get task
- PUT `/tasks/{taskId}` — Update task
- DELETE `/tasks/{taskId}` — Soft delete task

Product schema registry endpoints (F0034):
- GET `/lob-schemas/active` — active bundle bootstrap, tenant-filtered and excluding Internal sentinels
- GET `/lob-schemas/{productVersionId}/{stage}` — direct bundle resolve for active and historical pinned rows
- POST `/lob-schemas/{productVersionId}/activate` — Admin-only activation/deprecation/retirement command surface

F0023 search/reporting endpoints:
- GET `/search-results` — permission-filtered global search rows, facets, and counts
- GET `/saved-views` — visible personal/team saved views
- POST `/saved-views` — create personal or team saved view
- GET `/saved-views/{savedViewId}` — saved-view detail
- PATCH `/saved-views/{savedViewId}` — update saved-view metadata/criteria/archive state (requires `If-Match`)
- DELETE `/saved-views/{savedViewId}` — soft archive saved view (requires `If-Match`)
- PUT `/saved-views/{savedViewId}/default` — set personal/team default (requires `If-Match`)
- GET `/operational-reports/workload` — due-today, overdue, owner/status workload report
- GET `/operational-reports/workflow-aging` — workflow aging and backlog report

F0032 admin configuration endpoints:
- GET `/admin/configuration-domains` — supported domain catalog and current state
- GET `/admin/configuration-domains/{domainKey}` — domain detail with draft/published/refresh state
- POST `/admin/configuration-domains/{domainKey}/drafts` — create draft from current published version
- PATCH `/admin/configuration-drafts/{draftId}` — update draft payload (requires `If-Match`)
- POST `/admin/configuration-drafts/{draftId}/validation` — validate draft payload
- GET `/admin/configuration-drafts/{draftId}/comparison` — compare draft to current published version
- POST `/admin/configuration-drafts/{draftId}/publish` — publish validated draft
- POST `/admin/configuration-domains/{domainKey}/rollback` — publish rollback snapshot
- GET `/admin/configuration-audit-events` — permission-safe audit search
F0025 commission/revenue endpoints:
- GET/POST `/commission-schedules` and PUT `/commission-schedules/{scheduleId}` — effective-dated schedule management
- POST `/producer-splits` and GET `/policies/{policyId}/producer-splits` — policy split assignment and lookup
- GET `/expected-commissions` and POST `/expected-commissions/{expectedCommissionId}/calculate` — commission workspace search and recalculation
- GET/POST `/expected-commissions/{expectedCommissionId}/adjustments` and POST `/commission-adjustments/{adjustmentId}/decision` — adjustment request and decision
- GET `/revenue-attribution/rollups` — authorized revenue rollups by producer, broker, territory, carrier market, or policy period

Error contract:
- All non-success responses return RFC 7807 `ProblemDetails` with `type`, `title`, `status`, plus extension fields `code`, `traceId`, and optional `detail`/`errors`.
- Dynamic LOB validation returns `LobValidationProblemDetails` with `lobErrors[]`; it does not reuse the global `ProblemDetails.errors` map.

### 4.6 Observability + NFRs

Logging, tracing, metrics, performance, security.

Observability baseline:
- Structured logging with correlation id and subject id where available.
- Distributed traces for API request path and DB calls.
- Metrics: request latency, error rate, transition counts, authorization denials.
- F0023 metrics: search latency, result counts, saved-view mutation outcomes, projection refresh lag, projection refresh failures, and authorization-denied counts by F0023 resource.

Performance:
- API read endpoints: p95 < 300ms under nominal load.
- API write/transition endpoints: p95 < 500ms under nominal load.
- List endpoints support pagination and bounded query size.
- F0023 search/report endpoints: p95 < 500ms for bounded queries and p95 projection lag under 60 seconds after source-record commit.
- F0032 catalog/domain reads: p95 < 500ms for up to 25 supported configuration domains.
- F0032 validation: p95 < 2 seconds for first-release domains with up to 250 queue/routing rules or SLA rows per domain.
- F0032 audit search: p95 < 750ms for filtered first-page queries.
- F0025 commission workspace list/detail reads: p95 < 500ms for bounded queries.
- F0025 schedule/split/calculation/adjustment mutations: p95 < 700ms under nominal load.
- F0025 revenue rollups: p95 < 750ms for bounded filters and projection freshness visible through `lastSourceUpdatedAt`.

Security:
- OIDC JWT validation against authentik issuer/audience (see ADR-006).
- Casbin ABAC for all protected actions.
- Any secondary access channel (for example MCP/agent tools) must enforce the same ABAC policies and tenant filters as API endpoints; no raw SQL access paths.
- F0009 Phase 1: RLS is not required; compensating controls are mandatory (tenant-scoped queries, ABAC checks, server-side field filtering, audit logging).
- F0023 counts, facets, snippets, saved-view previews, reports, and drilldowns are computed after authorization filtering; unauthorized matches are not surfaced as hidden-record counts.
- F0025 economic totals, adjustment history, exception counts, rollups, and drilldowns are computed after authorization filtering; unauthorized source rows are not surfaced as hidden-record counts.
- Secrets via environment variables; no hardcoded credentials in code or config.
- Immutable audit trail for every mutation and transition.
- Saved-view mutations append immutable `SavedViewAuditEvent` rows.

Availability:
- Target service availability 99.9% for production environments.
- Health/readiness endpoints for orchestration.

Scalability:
- Horizontal API scaling behind stateless app instances.
- Database indexing on high-cardinality lookup fields (license number, status, foreign keys).
- Transition/timeline tables partition-ready for growth.

### 4.7 Neuron Companion architecture (F0038)

Authored at F0038 Phase B (plan run `2026-06-30-d1dd91f7`). Detailed decisions
live in the linked artifacts; this is the §4 summary with pointers.

**Service boundary & auth.** Neuron is a **stateless** Python/FastAPI service and a
**secondary** interface; the .NET engine stays the CRM source of truth. Neuron
calls the engine **as the user** (forwarded authentik token); Casbin ABAC is
enforced engine-side, unchanged — no authz is re-implemented in Python. C4 L2/L3
views: `architecture/c4-neuron-companion.md`. Governing decisions:
[ADR-027](architecture/decisions/ADR-027-neuron-companion-a2a-orchestration.md)
(A2A-aligned internal orchestration) and
[ADR-028](architecture/decisions/ADR-028-neuron-companion-persistence-and-outreach-authorization.md)
(persistence, cross-store consistency, outreach authorization).

**Orchestration.** A2A-aligned internal profile: CRM scope guard → intent
classifier → A2A-aware orchestrator → registered specialist heads (Renewals live;
Tasks/Pipeline/Broker-activity inert stubs) → goal agents → MCP/tool calls.
Versioned YAML plans (`schemas/neuron-orchestration-plan.schema.json`) and private
Agent Cards (`schemas/neuron-agent-card.schema.json`) are checked-in, schema-
validated assets; every referenced head/tool/terminal-state must resolve or the
service refuses the asset. Public A2A endpoints / Agent Cards are deferred.

**Persistence (two owners, no cross-runtime transaction).** Neuron owns and writes
its own `neuron.*` schema directly (threads, messages, message_parts, agent_runs,
tool_calls, provenance_events — data-model §11); the engine owns CRM business
state. For a gesture that writes both stores, the **engine write is authoritative
and commits first**; the Neuron record references the engine id and is written
idempotently (ADR-028 §2).

**Response contract.** Versioned multi-part message envelope
(`schemas/neuron-message-envelope.schema.json`: `text|app|status|sources|actions`)
keyed by `thread_id`; zone-dispatch results use
`schemas/neuron-zone-payload.schema.json`. The React host renders **only registered
components from validated props** — no model-generated markup or numbers.

**The one write (Underwriter-only).** Draft outreach persists an InternalOnly,
AI-generated `ActivityTimelineEvent` (no transition); mock-send commits the real
`Identified → Outreach` `WorkflowTransition` + a "sent (simulated)" event and
**fakes SMTP** (no real email). Both are gated by the new dedicated
`renewal:draft_outreach` action (Underwriter only). The engine state machine
permits `Identified → Outreach` for an Underwriter **only** on the mock-send path
(ADR-028 §3, authorization-matrix §2.9a) — the general F0007 transition ownership
is unchanged. Engine endpoints carry the `NeuronCompanion` tag in
`api/nebula-api.yaml`; the Neuron service surface is `api/neuron-api.yaml`.

**Telemetry & NFRs.** Companion telemetry establishes the baseline
"needs-attention surfaced → draft-ready" duration plus a minimal secondary set
(`schemas/neuron-companion-telemetry-event.schema.json`), ingested fire-and-forget
to Serilog (`POST /internal/telemetry/neuron-companion`, mirrors F0035). Engine
read endpoints inherit the §4.6 p95 budgets; provenance/telemetry carry ids,
versions, hashes, model, cost, latency — never raw prompts, raw responses, or PII.

---

## 5) Phase C — Implementation Plan (locked order)

Implementation should proceed in staged increments. Start Phase 0 foundation when lifecycle stage transitions to `implementation` in `lifecycle-stage.yaml`; track calendar commitments in project-specific execution plans rather than this baseline spec.

### Phase 0 Foundation — required components

Must include Postgres + Redis + authentik (server + worker) + Casbin in docker-compose. Temporal is included as infrastructure-only
(server + worker containers); no F0001/F0002 story uses Temporal workflows — it is provisioned now so that
Submission/Renewal workflow orchestration (F0006/F0007) can adopt it without docker-compose changes later.
Backend: Clean Architecture scaffold + auth + ABAC wiring + error contract + timeline append-only.
Frontend: authenticated shell.
Tests: auth test + timeline append test.

No scope creep in Phase 0:

- No submission/renewal UI
- No document upload implementation
- No analytics
- No external MGA portal
- No Python MCP server

Definition of Done for Phase 0:

- [ ] docker-compose includes Postgres, Redis, authentik (server + worker), Casbin policy source, Temporal (infrastructure-only; no app code depends on it in F0001/F0002)
- [ ] backend scaffolded with clean architecture boundaries and auth/ABAC wiring
- [ ] frontend authenticated shell with protected routes
- [ ] consistent error contract implemented across API endpoints
- [ ] append-only timeline and workflow transition persistence in place
- [ ] baseline tests passing: auth enforcement + timeline append + one transition flow
- [ ] run instructions and local setup documented

---

## 6) Next Step Guidance

Drive the next action from the declared lifecycle stage in `lifecycle-stage.yaml`:

- If stage is `framework-bootstrap` or `planning`: ask the smallest set of questions needed to complete sections 3.1–3.5, then propose a first-pass draft of Vision, Non-goals, Personas, Epics, and MVP stories.
- If stage is `implementation` or `release-readiness`: use approved planning and architecture artifacts to execute implementation/review actions and collect gate evidence.
