# F0010 — Dashboard Opportunities Refactor (Pipeline Board + Insight Views) — Status

**Overall Status:** Abandoned — Superseded by F0013
**Superseded By:** F0013 — Dashboard Framed Storytelling Canvas
**Abandoned Date:** 2026-03-14
**Last Updated:** 2026-03-14

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|
| F0010-S0001 | Replace Sankey default with Pipeline Board | [x] Done |
| F0010-S0002 | Add Opportunities Aging Heatmap view | [x] Done |
| F0010-S0003 | Add Opportunities Composition Treemap view | [x] Done |
| F0010-S0004 | Add Opportunities Hierarchy Sunburst view | [x] Done |
| F0010-S0005 | Unify drilldown, responsive layout, and accessibility | [x] Done |

## Backend Progress

- [x] Opportunities summary contract updated for Pipeline Board default data needs
- [x] Opportunities insights contract defined for Heatmap/Treemap/Sunburst aggregates
- [x] Authorization coverage validated for all opportunities endpoints
- [x] Unit tests passing
- [x] Integration tests passing

## Frontend Progress

- [x] Opportunities widget layout updated to Pipeline Board default
- [x] View mode toggle implemented (Pipeline, Heatmap, Treemap, Sunburst)
- [x] Drilldown popovers wired consistently across views
- [x] Responsive layouts verified (MacBook, iPad, iPhone)
- [x] Component tests passing
- [x] Visual regression coverage updated

## Cross-Cutting

- [x] API documentation updated
- [x] Feature test plan executed
- [x] Deployability check evidence recorded
- [x] No TODOs remain in implementation code

## Required Signoff Roles (Set in Planning)

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Baseline acceptance and regression coverage for dashboard view-mode workflows. | Architect | 2026-03-11 |
| Code Reviewer | Yes | Baseline independent implementation review before completion/archive transition. | Architect | 2026-03-11 |
| Security Reviewer | Yes | New backend endpoints (`/aging`, `/hierarchy`) require authorization verification. | Architect | 2026-03-11 |
| DevOps | No | No new infra, env vars, or deployment changes expected. | Architect | 2026-03-11 |
| Architect | No | Standard patterns applied, no architecture exceptions. | Architect | 2026-03-11 |

## Story Signoff Provenance

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0010-S0001 | Quality Engineer | QE Agent | PASS | `experience/src/features/opportunities/tests/OpportunitiesSummary.test.tsx`, `engine/tests/Nebula.Tests/Integration/DashboardEndpointTests.cs` | 2026-03-11 | 8 component tests + 5 integration tests cover pipeline board default, view switching, period selector, loading/error states |
| F0010-S0001 | Code Reviewer | Code Review Agent | APPROVED | `planning-mds/features/archive/F0010-dashboard-opportunities-refactor/F0010-CODE-REVIEW-REPORT.md` | 2026-03-11 | APPROVED WITH RECOMMENDATIONS — 0 critical, 0 high |
| F0010-S0001 | Security Reviewer | Security Agent | PASS | `planning-mds/features/archive/F0010-dashboard-opportunities-refactor/F0010-SECURITY-REVIEW-REPORT.md` | 2026-03-11 | PASS — 0 critical, 0 high, 0 medium |
| F0010-S0002 | Quality Engineer | QE Agent | PASS | `engine/tests/Nebula.Tests/Integration/DashboardEndpointTests.cs` (aging shape + entity type tests) | 2026-03-11 | Integration tests validate 5 aging buckets + total rollup |
| F0010-S0002 | Code Reviewer | Code Review Agent | APPROVED | `planning-mds/features/archive/F0010-dashboard-opportunities-refactor/F0010-CODE-REVIEW-REPORT.md` | 2026-03-11 | Heatmap reviewed as part of full feature review |
| F0010-S0002 | Security Reviewer | Security Agent | PASS | `planning-mds/features/archive/F0010-dashboard-opportunities-refactor/F0010-SECURITY-REVIEW-REPORT.md` | 2026-03-11 | Aging endpoint authorization verified |
| F0010-S0003 | Quality Engineer | QE Agent | PASS | `experience/src/features/opportunities/tests/OpportunitiesSummary.test.tsx` | 2026-03-11 | Treemap rendering covered via view switching test |
| F0010-S0003 | Code Reviewer | Code Review Agent | APPROVED | `planning-mds/features/archive/F0010-dashboard-opportunities-refactor/F0010-CODE-REVIEW-REPORT.md` | 2026-03-11 | L-01: simple slice layout acceptable for MVP |
| F0010-S0003 | Security Reviewer | Security Agent | PASS | `planning-mds/features/archive/F0010-dashboard-opportunities-refactor/F0010-SECURITY-REVIEW-REPORT.md` | 2026-03-11 | Read-only SVG rendering, no new attack vectors |
| F0010-S0004 | Quality Engineer | QE Agent | PASS | `experience/src/features/opportunities/tests/OpportunitiesSummary.test.tsx` | 2026-03-11 | Sunburst rendering covered via view switching test |
| F0010-S0004 | Code Reviewer | Code Review Agent | APPROVED | `planning-mds/features/archive/F0010-dashboard-opportunities-refactor/F0010-CODE-REVIEW-REPORT.md` | 2026-03-11 | L-02: SVG text styling note, acceptable |
| F0010-S0004 | Security Reviewer | Security Agent | PASS | `planning-mds/features/archive/F0010-dashboard-opportunities-refactor/F0010-SECURITY-REVIEW-REPORT.md` | 2026-03-11 | Hierarchy endpoint authorization verified |
| F0010-S0005 | Quality Engineer | QE Agent | PASS | `experience/src/features/opportunities/tests/OpportunitiesSummary.test.tsx` | 2026-03-11 | Accessibility (aria-label, tablist, sr-only), period persistence, view switching all tested |
| F0010-S0005 | Code Reviewer | Code Review Agent | APPROVED | `planning-mds/features/archive/F0010-dashboard-opportunities-refactor/F0010-CODE-REVIEW-REPORT.md` | 2026-03-11 | Keyboard nav, screen reader support, ABAC scope verified |
| F0010-S0005 | Security Reviewer | Security Agent | PASS | `planning-mds/features/archive/F0010-dashboard-opportunities-refactor/F0010-SECURITY-REVIEW-REPORT.md` | 2026-03-11 | All endpoints enforce dashboard_pipeline authorization |

## Archival Criteria

All items above must be checked before moving this feature folder to `planning-mds/features/archive/`.
