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

- Non-goals (explicit):
  - No external broker/MGA self-service portal in MVP.
  - No advanced analytics dashboards beyond basic broker insight summaries in MVP.
  - No document OCR/intelligence workflows in MVP.
  - No claims management module in MVP.
  - No multi-region regulatory rules engine in MVP.

### 3.2 Personas

- Persona 1: Distribution user
  - Primary job: intake and triage submissions, manage broker interactions, track pipeline movement.
  - Success metric: reduced intake turnaround and fewer handoff delays.

- Persona 2: Underwriter
  - Primary job: review triaged submissions, provide quote/bind decisions, maintain decision traceability.
  - Success metric: faster, consistent movement from ReadyForUWReview to Quoted/Bound or Declined.

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
- F0019: Submission Quoting, Proposal & Approval Workflow - Planned
- F0020: Document Management & ACORD Intake - Planned
- F0021: Communication Hub & Activity Capture - Planned
- F0022: Work Queues, Assignment Rules & Coverage Management - Planned
- F0023: Global Search, Saved Views & Operational Reporting - Planned

**CRM Release MVP+ (Planned):**
- F0008: Broker Insights - Planned
- F0017: Broker/MGA Hierarchy, Producer Ownership & Territory Management - Planned
- F0024: Claims & Service Case Tracking - Planned
- F0027: COI, ACORD & Outbound Document Generation - Planned
- F0028: Carrier & Market Relationship Management - Planned

**Brokerage Platform Expansion (Planned):**
- F0025: Commission, Producer Splits & Revenue Tracking - Planned
- F0026: Billing, Invoicing & Reconciliation - Planned
- F0029: External Broker Collaboration Portal - Planned
- F0030: Integration Hub & Data Exchange - Planned

**Release Enablement / Platform Operations (Planned):**
- F0031: Data Import, Deduplication & Go-Live Migration - Planned
- F0032: Admin Configuration & Reference Data Console - Planned
- [F0033: Structured Logging and QE Toolchain Activation](features/archive/F0033-structured-logging-and-qe-toolchain-activation/PRD.md) - Done (Archived)

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
- Admin minimal (roles/policies optional MVP)

Screen baseline details:
- Navigation Shell: authenticated app shell, role-aware navigation, global search entry, notifications placeholder.
- Dashboard: role-aware landing screen with five widgets — nudge cards (dismissible action prompts for time-sensitive items), KPI metrics cards, pipeline summary (mini-Kanban with status pills and expandable card previews), my tasks & reminders, and broker activity feed. All widgets enforce ABAC scope and degrade gracefully when upstream entities are unavailable.
- Broker List: sortable/filterable list, quick search, create action, status tags.
- Broker 360: profile header, contacts, hierarchy/program links, immutable timeline panel.
- Task Center: assigned tasks, due dates, simple status states, reminder hooks.
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

**Data Model Supplement:** See `planning-mds/architecture/data-model.md` for Task entity, dashboard indexes, and query patterns. F0020 documents are filesystem-first (no relational entity in MVP); see `planning-mds/features/F0020-document-management-and-acord-intake/README.md` for the on-disk layout and the `IDocumentRepository` boundary.

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
- IdentityAuthorization module:
  - Validates authentik JWT tokens (JWKS from `Authentication__Authority/.well-known/openid-configuration`).
  - Normalizes `(iss, sub)` claims to internal `NebulaPrincipal { UserId, Roles, Regions }` via `IClaimsPrincipalNormalizer`.
  - Enforces Casbin ABAC policies at API/application boundaries.

### 4.2 Data model (detailed)

Define tables/fields for:

- Broker, Contact, UserProfile, UserPreference, ActivityTimelineEvent, WorkflowTransition
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

### 4.5 API Contracts

Define endpoints + request/response contracts + error contract.

Primary OpenAPI contract:
- `planning-mds/api/nebula-api.yaml`

Entity coverage in API surface:
- Account, Broker, MGA, Program, Contact, Submission, Renewal, ActivityTimelineEvent, WorkflowTransition

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

Error contract:
- All non-success responses return RFC 7807 `ProblemDetails` with `type`, `title`, `status`, plus extension fields `code`, `traceId`, and optional `detail`/`errors`.

### 4.6 Observability + NFRs

Logging, tracing, metrics, performance, security.

Observability baseline:
- Structured logging with correlation id and subject id where available.
- Distributed traces for API request path and DB calls.
- Metrics: request latency, error rate, transition counts, authorization denials.

Performance:
- API read endpoints: p95 < 300ms under nominal load.
- API write/transition endpoints: p95 < 500ms under nominal load.
- List endpoints support pagination and bounded query size.

Security:
- OIDC JWT validation against authentik issuer/audience (see ADR-006).
- Casbin ABAC for all protected actions.
- Any secondary access channel (for example MCP/agent tools) must enforce the same ABAC policies and tenant filters as API endpoints; no raw SQL access paths.
- F0009 Phase 1: RLS is not required; compensating controls are mandatory (tenant-scoped queries, ABAC checks, server-side field filtering, audit logging).
- Secrets via environment variables; no hardcoded credentials in code or config.
- Immutable audit trail for every mutation and transition.

Availability:
- Target service availability 99.9% for production environments.
- Health/readiness endpoints for orchestration.

Scalability:
- Horizontal API scaling behind stateless app instances.
- Database indexing on high-cardinality lookup fields (license number, status, foreign keys).
- Transition/timeline tables partition-ready for growth.

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
