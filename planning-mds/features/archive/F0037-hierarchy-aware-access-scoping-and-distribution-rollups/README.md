# F0037 - Hierarchy-Aware Access Scoping & Distribution Rollups

**Status:** Phase B architecture drafted - pending G5 approval
**Priority:** High
**Phase:** CRM Release MVP+
**Plan Run:** `2026-07-06-6e3851ab`

## Overview

F0037 turns the completed F0017 distribution structure and F0023 projection/reporting substrate into enforced visibility and rolled-up distribution insight. The feature resolves a current user's hierarchy, territory, producer, role, and `asOf` scope, applies that scope before any search/report/count/facet/drilldown/rollup response is materialized, and adds distribution rollups for production, workflow, and activity.

## Documents

| Document | Purpose |
|----------|---------|
| [PRD.md](./PRD.md) | Phase A product requirements, G1 clarifications, scope, personas, and success criteria |
| [STATUS.md](./STATUS.md) | Planning status, story checklist, and required signoff roles |
| [GETTING-STARTED.md](./GETTING-STARTED.md) | Setup, prerequisites, validation, and implementation handoff notes |
| [feature-assembly-plan.md](./feature-assembly-plan.md) | Phase B architecture and implementation execution plan |

## Stories

| ID | Title | Status |
|----|-------|--------|
| [F0037-S0001](./F0037-S0001-resolve-current-user-distribution-scope.md) | Resolve current user distribution scope | Draft |
| [F0037-S0002](./F0037-S0002-enforce-hierarchy-aware-read-scoping.md) | Enforce hierarchy-aware read scoping | Draft |
| [F0037-S0003](./F0037-S0003-apply-visibility-to-search-views-insights-reports.md) | Apply visibility to search, saved views, insights, and reports | Draft |
| [F0037-S0004](./F0037-S0004-add-distribution-rollup-reporting.md) | Add distribution rollup reporting | Draft |
| [F0037-S0005](./F0037-S0005-add-rollup-filters-panels-and-no-leak-states.md) | Add rollup filters, panels, drilldowns, and no-leak states | Draft |
| [F0037-S0006](./F0037-S0006-add-security-evidence-and-reconciliation.md) | Add security evidence and reconciliation checks | Draft |

**Total Stories:** 6
**Completed:** 0 / 6

## Relationships

- **Depends on:** F0017 (structural hierarchy/territory/ownership model), F0023 (search/saved-view/reporting substrate), F0008 (broker insight projections and permission-safe analytics).
- **Originates from:** F0017 G1 deferral in plan run `2026-06-06-5fb353e9`.
- **Roadmap position:** Promoted to `Now` on 2026-07-06 by operator decision; still requires G3 and G5 approvals before any feature action or implementation.

## Architecture Review

**Phase B status:** Drafted - pending G5 approval
**Execution Plan:** [feature-assembly-plan.md](./feature-assembly-plan.md)

Phase B keeps F0037 inside the existing modular monolith and extends F0023/F0008 projection visibility instead of adding a new reporting store. New public contract: `GET /operational-reports/distribution-rollups`, response schema `distribution-rollup-report.schema.json`, policy rule `distribution_rollup:read`, and KG capability `distribution-rollup-reporting`.
