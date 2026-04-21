# Feature Roadmap (Now / Next / Later)

This document is the working prioritization view for feature sequencing.

## Purpose

- Provide a current planning dashboard for sequencing decisions.
- Keep prioritization separate from feature metadata in `REGISTRY.md`.
- Avoid constant churn in `BLUEPRINT.md`, which should remain baseline strategy.

## Update Rules

- Update whenever feature priority or sequence changes.
- Keep entries at feature level (not story-level unless explicitly needed).
- Link each item to a feature folder and include rationale.

## Now

| Feature | Phase | Why Now |
|---------|-------|---------|
| [F0020 — Document Management & ACORD Intake](./F0020-document-management-and-acord-intake/README.md) | CRM Release MVP | Removes document chasing and supports completeness checks for submissions and underwriting review. |

## Next

| Feature | Phase | Why Next |
|---------|-------|----------|
| [F0018 — Policy Lifecycle & Policy 360](./F0018-policy-lifecycle-and-policy-360/README.md) | CRM Release MVP | Introduces policy truth needed for account context, servicing, and renewal workflows. |
| [F0019 — Submission Quoting, Proposal & Approval Workflow](./F0019-submission-quoting-proposal-and-approval/README.md) | CRM Release MVP | Completes intake-to-quote-to-bind operations; intake alone is insufficient for a commercial P&C CRM. |
| [F0021 — Communication Hub & Activity Capture](./F0021-communication-hub-and-activity-capture/README.md) | CRM Release MVP | Creates the communication system of record for broker interactions, underwriting follow-up, and audit history. |
| [F0022 — Work Queues, Assignment Rules & Coverage Management](./F0022-work-queues-assignment-rules-and-coverage-management/README.md) | CRM Release MVP | Adds operational routing, backup coverage, and workload balancing beyond personal task lists. |
| [F0023 — Global Search, Saved Views & Operational Reporting](./F0023-global-search-saved-views-and-operational-reporting/README.md) | CRM Release MVP | Provides cross-object findability, operational visibility, and daily management reporting required for adoption. |
| [F0031 — Data Import, Deduplication & Go-Live Migration](./F0031-data-import-deduplication-and-go-live-migration/README.md) | Release Enablement | Required for production rollout even though it is not the most visible product module. |

## Later

| Feature | Phase | Why Later |
|---------|-------|-----------|
| [F0008 — Broker Insights](./F0008-broker-insights/README.md) | CRM Release MVP+ | Higher-value after submissions, policies, renewals, and operational reporting are live and trustworthy. |
| [F0017 — Broker/MGA Hierarchy, Producer Ownership & Territory Management](./F0017-broker-mga-hierarchy-and-producer-ownership/README.md) | CRM Release MVP+ | Important for more advanced distribution models, producer governance, and territorial accountability. |
| [F0024 — Claims & Service Case Tracking](./F0024-claims-and-service-case-tracking/README.md) | CRM Release MVP+ | Extends Nebula into post-bind servicing and improves account and policy context. |
| [F0025 — Commission, Producer Splits & Revenue Tracking](./F0025-commission-producer-splits-and-revenue-tracking/README.md) | Brokerage Platform Expansion | Moves Nebula from CRM into brokerage economics and compensation operations. |
| [F0026 — Billing, Invoicing & Reconciliation](./F0026-billing-invoicing-and-reconciliation/README.md) | Brokerage Platform Expansion | Pushes the product deeper into agency management and finance operations. |
| [F0027 — COI, ACORD & Outbound Document Generation](./F0027-coi-acord-and-outbound-document-generation/README.md) | CRM Release MVP+ | Strong insurance-specific parity feature once policy and document foundations exist. |
| [F0028 — Carrier & Market Relationship Management](./F0028-carrier-and-market-relationship-management/README.md) | CRM Release MVP+ | Supports appetite, appointments, market access, and carrier relationship strategy. |
| [F0029 — External Broker Collaboration Portal](./F0029-external-broker-collaboration-portal/README.md) | Future Platform | External collaboration remains intentionally post-MVP until internal workflows are mature. |
| [F0030 — Integration Hub & Data Exchange](./F0030-integration-hub-and-data-exchange/README.md) | Future Platform | Needed for scalable connectivity across email, carriers, accounting, and document systems. |
| [F0032 — Admin Configuration & Reference Data Console](./F0032-admin-configuration-and-reference-data-console/README.md) | Platform Operations | Needed once queues, templates, rules, and reference data become more configurable. |

## Abandoned

| Feature | Superseded By | Rationale |
|---------|---------------|-----------|
| [F0010 — Dashboard Opportunities Refactor](./archive/F0010-dashboard-opportunities-refactor/README.md) | F0013 | F0010's Pipeline Board, Heatmap, Treemap, and Sunburst views are replaced by F0013's vertical timeline with contextual mini-visualizations. The insight views no longer fit the storytelling canvas direction. |
| [F0011 — Dashboard Opportunities Flow-First Modernization](./archive/F0011-dashboard-opportunities-flow-modernization/README.md) | F0013 | F0011's connected flow and terminal outcomes concepts live on in F0013 but with a fundamentally different visual approach (vertical timeline + narrative callouts instead of connected flow cells). |

## Completed

| Feature | Phase | Completion State |
|---------|-------|------------------|
| [F0016 — Account 360 & Insured Management](./archive/F0016-account-360-and-insured-management/README.md) | CRM Release MVP | Done and archived (2026-04-14) — 11 stories: list, create, profile edit, 360 workspace, contacts, relationships, lifecycle, merge, fallback contract, timeline, summary projection |
| [F0007 — Renewal Pipeline](./archive/F0007-renewal-pipeline/README.md) | CRM Release MVP | Done and archived (2026-04-12) — 7 stories: pipeline list, detail view, transitions, assignment, overdue visibility, create from policy, timeline |
| [F0006 — Submission Intake Workflow](./archive/F0006-submission-intake-workflow/README.md) | CRM Release MVP | Done and archived (2026-04-04) — 8 stories: pipeline list, create flow, detail workspace, intake transitions, completeness, assignment, timeline, stale visibility |
| [F0033 — Structured Logging and QE Toolchain Activation](./archive/F0033-structured-logging-and-qe-toolchain-activation/README.md) | Infrastructure | Done and archived (2026-03-30) — 5 stories: Serilog baseline, Bruno API validation, Lighthouse CI, Pact contract testing, SonarQube Community |
| [F0014 — DevOps Smoke Test Automation](./archive/F0014-devops-smoke-test-automation/PRD.md) | Infrastructure | Done and archived (2026-03-28) — 3 stories: blueprint fixes, multi-role smoke test, CI workflow |
| [F0004 — Task Center UI + Manager Assignment](./archive/F0004-task-center-ui-and-assignment/README.md) | Phase 1 | Done and archived (2026-03-23) |
| [F0015 — Frontend Quality Gates + Test Infrastructure](./archive/F0015-frontend-quality-gates-and-test-infrastructure/README.md) | Infrastructure | Done and archived (2026-03-21) |
| [F0003 — Task Center + Reminders (API-only MVP)](./archive/F0003-task-center/README.md) | MVP | Done and archived |
| [F0013 — Dashboard Framed Storytelling Canvas](./archive/F0013-dashboard-framed-storytelling-canvas/README.md) | MVP | Done and archived |
| [F0001 — Dashboard](./archive/F0001-dashboard/README.md) | MVP | Done and archived |
| [F0002 — Broker & MGA Relationship Management](./archive/F0002-broker-relationship-management/README.md) | MVP | Done and archived (post-MVP hardening follow-ups tracked) |
| [F0005 — IdP Migration: Keycloak → authentik](./archive/F0005-idp-migration/README.md) | Foundation | Done and archived |
| [F0009 — Authentication + Role-Based Login](./archive/F0009-authentication-and-role-based-login/README.md) | Phase 1 | Done and archived |
| [F0012 — Dashboard Storytelling Infographic Canvas (Flat Canvas + Collapsible Rails)](./archive/F0012-dashboard-storytelling-infographic-canvas/README.md) | MVP | Done and archived |

## Notes

- This roadmap is the authoritative Now/Next/Later view.
- The proposed Commercial P&C CRM release MVP spans the `Now` and `Next` buckets together; `Later` captures MVP+ and platform-expansion scope.
- `REGISTRY.md` remains the authoritative feature inventory and ID tracker.
- `BLUEPRINT.md` remains the baseline product/architecture source of truth.
- Tracker sync policy is defined in `TRACKER-GOVERNANCE.md`.
- Archived features are tracked under `planning-mds/features/archive/`.
