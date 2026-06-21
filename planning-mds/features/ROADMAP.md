# Feature Roadmap (Now / Next / Later)

**Last Reviewed:** 2026-06-19

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
| [F0017 — Broker/MGA Hierarchy, Producer Ownership & Territory Management](./F0017-broker-mga-hierarchy-and-producer-ownership/README.md) | CRM Release MVP+ | Planned (plan run `2026-06-06-5fb353e9`); promoted to Now by operator decision. MVP slice (arbitrary-depth hierarchy + effective-dated producer ownership + effective-dated territory + change audit) depends only on F0002 (done), so it can start immediately. Establishes the structural distribution backbone that F0022 routing, F0023 reporting, F0008 insights, and F0037 (access scoping + rollups) all consume. Hierarchy-aware rollups and access-control enforcement were deferred to F0037 to keep this slice shippable. |

**Boundary Notes:** F0017 owns the **structural** distribution model + audit only; hierarchy-aware access enforcement and distribution rollups are owned by F0037. F0019 delivered the submission-bound quote/proposal packet workflow needed to move a submission through approval and bind. F0027 later owns reusable COI, ACORD, proposal-template, and outbound document generation capability.

## Next

| Feature | Phase | Why Next |
|---------|-------|----------|
| [F0037 — Hierarchy-Aware Access Scoping & Distribution Rollups](./F0037-hierarchy-aware-access-scoping-and-distribution-rollups/README.md) | CRM Release MVP+ | Promoted Later→Next (2026-06-06, operator decision); now first in `Next` after F0023's 2026-06-19 promotion to `Now`. Homes the scope deferred from F0017 (plan run `2026-06-06-5fb353e9`): hierarchy-aware access-control enforcement + distribution rollup reporting. **Placeholder — needs its own `plan` run before build**; can only start once F0017 (Now) is delivered and F0023 is underway. |
| [F0022 — Work Queues, Assignment Rules & Coverage Management](./F0022-work-queues-assignment-rules-and-coverage-management/README.md) | CRM Release MVP | Adds operational routing, backup coverage, and workload balancing beyond personal task lists; must ship durable queue/rule records plus minimal manager controls without waiting for F0032. Pulled forward (2026-06-06): consumes F0017's hierarchy/territory/ownership for routing now that F0017 is active. |
| [F0021 — Communication Hub & Activity Capture](./F0021-communication-hub-and-activity-capture/README.md) | CRM Release MVP | Creates the communication system of record for broker interactions, underwriting follow-up, and audit history. |
| [F0031 — Data Import, Deduplication & Go-Live Migration](./F0031-data-import-deduplication-and-go-live-migration/README.md) | Release Enablement | Required for production rollout and should start early enough to validate migration paths against the completed broker/account foundations while later workflow modules continue maturing. |
| [F0027 — COI, ACORD & Outbound Document Generation](./F0027-coi-acord-and-outbound-document-generation/README.md) | CRM Release MVP+ | Policy and document foundations are already complete, making outbound insurance document generation a natural near-term parity layer after the core quote workflow is underway. |
| [F0032 — Admin Configuration & Reference Data Console](./F0032-admin-configuration-and-reference-data-console/README.md) | Platform Operations | Lands after F0022/F0023 to centralize governance, validation, publish, rollback, and audit over module-owned configuration rather than becoming a prerequisite for queues. |

## Later

| Feature | Phase | Why Later |
|---------|-------|-----------|
| [F0008 — Broker Insights](./F0008-broker-insights/README.md) | CRM Release MVP+ | Higher-value after F0023 reporting substrate and F0017 hierarchy/producer dimensions are live; uses F0019 quote/bind outcomes for production metrics. |
| [F0028 — Carrier & Market Relationship Management](./F0028-carrier-and-market-relationship-management/README.md) | CRM Release MVP+ | Supports appetite, appointments, market access, and carrier relationship strategy before carrier-aware economics and production analysis deepen. |
| [F0024 — Claims & Service Case Tracking](./F0024-claims-and-service-case-tracking/README.md) | CRM Release MVP+ | Extends Nebula into post-bind servicing and improves account and policy context. |
| [F0025 — Commission, Producer Splits & Revenue Tracking](./F0025-commission-producer-splits-and-revenue-tracking/README.md) | Brokerage Platform Expansion | Moves Nebula from CRM into brokerage economics and compensation operations. |
| [F0026 — Billing, Invoicing & Reconciliation](./F0026-billing-invoicing-and-reconciliation/README.md) | Brokerage Platform Expansion | Pushes the product deeper into agency management and finance operations. |
| [F0030 — Integration Hub & Data Exchange](./F0030-integration-hub-and-data-exchange/README.md) | Brokerage Platform Expansion | Needed for scalable connectivity across email, carriers, accounting, and document systems. |
| [F0029 — External Broker Collaboration Portal](./F0029-external-broker-collaboration-portal/README.md) | Brokerage Platform Expansion | External collaboration remains intentionally post-MVP until internal workflows, integration boundaries, and broker-safe visibility controls are mature. |

## Abandoned

| Feature | Superseded By | Rationale |
|---------|---------------|-----------|
| [F0010 — Dashboard Opportunities Refactor](./archive/F0010-dashboard-opportunities-refactor/README.md) | F0013 | F0010's Pipeline Board, Heatmap, Treemap, and Sunburst views are replaced by F0013's vertical timeline with contextual mini-visualizations. The insight views no longer fit the storytelling canvas direction. |
| [F0011 — Dashboard Opportunities Flow-First Modernization](./archive/F0011-dashboard-opportunities-flow-modernization/README.md) | F0013 | F0011's connected flow and terminal outcomes concepts live on in F0013 but with a fundamentally different visual approach (vertical timeline + narrative callouts instead of connected flow cells). |

## Completed

| Feature | Phase | Completion State |
|---------|-------|------------------|
| [F0023 — Global Search, Saved Views & Operational Reporting](./archive/F0023-global-search-saved-views-and-operational-reporting/README.md) | CRM Release MVP | Done and archived (2026-06-20, feature run `2026-06-19-a4e3fdd6`) — 7 stories: global search, filter/open, personal saved views, team saved views + defaults, daily workload report, workflow aging/backlog, permission-safe behavior |
| [F0019 — Submission Quoting, Proposal & Approval Workflow](./archive/F0019-submission-quoting-proposal-and-approval/README.md) | CRM Release MVP | Done and archived (2026-06-03) — 8 stories: downstream workflow activation, quote/proposal packet, underwriting approval, bind handoff, decline/withdraw, archive/reactivate, pipeline visibility, timeline/audit |
| [F0036 — Form Engine and Form-State Preservation (RHF + AJV + Widget Registry)](./archive/F0036-dynamic-product-attribute-form-engine/README.md) | Platform Foundation / CRM Release MVP Enabler | Done and archived (2026-05-30) — 8 stories: engine skeleton + deps, MVP widget vocabulary, schema render + AJV parity, pin-during-edit, replace Cyber panel, attr-form preservation, controlled-form dirty-tracker + shared helper, CRUD preservation/restore |
| [F0035 — Session Continuity & Token Refresh](./archive/F0035-session-continuity-and-token-refresh/README.md) | Release Enablement / Platform Operations | Done and archived (2026-05-24) - 5 stories: silent renewal, idle warning modal, forced re-auth restore, auth error semantics, session telemetry |
| [F0034 — Product Schema Registry and Dynamic LOB Attributes](./archive/F0034-product-schema-registry-and-dynamic-lob-attributes/README.md) | Platform Foundation / CRM Release MVP Enabler | Done and archived (2026-05-07) — 7 stories: decision lock, registry foundation, lifecycle carrier pinning, validator parity, dynamic panel, Cyber bundle, lifecycle/F0019 handoff |
| [F0020 — Document Management & ACORD Intake](./archive/F0020-document-management-and-acord-intake/README.md) | CRM Release MVP | Done and archived (2026-05-05) — 12 stories: single upload, bulk upload, quarantine promote, classification-filtered list, detail/provenance, downloads, immutable replace, metadata update, classification ABAC, completeness signal, retention cleanup, templates library |
| [F0018 — Policy Lifecycle & Policy 360](./archive/F0018-policy-lifecycle-and-policy-360/README.md) | CRM Release MVP | Done and archived (2026-04-22) — 11 stories: list, create, profile edit, 360 composition, versions, endorsements, cancellation, reinstatement, renewal linkage, timeline, summary projection |
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
- Boundary guardrails: F0019 delivered submission-bound quote/proposal workflow while F0027 owns reusable outbound generation; F0022 owns usable queue/routing foundations while F0032 later governs shared configuration; F0008 remains separate but must land after F0023 and F0017.
- Reviewed 2026-06-19 (operator decision): promoted **F0023 — Global Search, Saved Views & Operational Reporting** from `Next` to `Now` to run in parallel with F0017 and pull the search/reporting substrate forward (F0008 insights and F0037 rollups depend on it). Resulting `Now`: **F0017 + F0023**. Resulting `Next` order: **F0037 → F0022 → F0021 → F0031 → F0027 → F0032**. Dependency note: F0023's core search/saved-views/reporting substrate can build in parallel with F0017, but its hierarchy/territory-aware report facets depend on F0017 landing. ROADMAP-only resequencing — `REGISTRY.md` status (`Planned`) and the `BLUEPRINT.md` status snapshot are unaffected (neither encodes Now/Next/Later sequence).
- Reviewed 2026-06-06 during F0017 plan run `2026-06-06-5fb353e9` (operator decision at the Phase A approval gate): F0017 promoted from `Later` to `Now` (its MVP slice depends only on completed F0002, so it can start immediately and unblocks F0022/F0023/F0008). F0037 (Hierarchy-Aware Access Scoping & Distribution Rollups) created to home the access-control-enforcement and hierarchy-aware-rollup scope deferred from F0017 at its G1 clarification gate. `Next` reordered (operator "pull substrate forward") and F0037 promoted from Later into `Next` immediately after F0023 (operator decision): resulting Next order is **F0023 → F0037 → F0022 → F0021 → F0031 → F0027 → F0032** (F0037 is a placeholder needing its own plan run before build; it depends on F0017 + F0023). `Later` now begins at F0008.
- Reviewed 2026-06-03 after F0019 closeout; moved F0019 from Now to Completed and archived it.
- Reviewed 2026-05-17 after roadmap sequencing review against completed feature foundations; F0019 moved into `Now`, F0031 moved earlier in `Next`, F0027/F0032 moved into `Next`, and `Later` reordered around dependency readiness.
- Reviewed 2026-05-17 after auth/session review identified disruptive active-session expiry behavior; F0035 added as a Now item.
- Reviewed 2026-05-25 after an F0035 review found its form-state preservation wired to zero forms, traced to ADR-021 drift (the accepted RHF/AJV/widget-registry form engine was never implemented; F0034 shipped a hardcoded Cyber panel). F0036 added as a Now item to realize ADR-021 for LOB product attributes (Cyber first) and connect those forms to F0035 preservation. Broadened the same day (operator decision) to also cover the hand-rolled CRUD forms; the 2026-05-27 scope refinement confirmed they stay controlled and register through a controlled-form dirty-tracker adapter. F0036 title updated to "Form Engine and Form-State Preservation."
- Reviewed 2026-05-07 against `REGISTRY.md`, current planned feature PRDs, archived feature dependencies, and F0034 closeout evidence. F0034 is completed and F0019 remains the next CRM Release MVP feature.
- The proposed Commercial P&C CRM release MVP spans the `Now` and `Next` buckets together; `Later` captures MVP+ and platform-expansion scope.
- `REGISTRY.md` remains the authoritative feature inventory and ID tracker.
- `BLUEPRINT.md` remains the baseline product/architecture source of truth.
- Tracker sync policy is defined in `TRACKER-GOVERNANCE.md`.
- Archived features are tracked under `planning-mds/features/archive/`.
- Reviewed 2026-06-20 after F0023 closeout (feature run `2026-06-19-a4e3fdd6`); moved F0023 from Now to Completed and archived it. Resulting `Now`: F0017.
