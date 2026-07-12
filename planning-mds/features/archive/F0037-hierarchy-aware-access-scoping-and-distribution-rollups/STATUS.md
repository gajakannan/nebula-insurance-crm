# F0037 - Hierarchy-Aware Access Scoping & Distribution Rollups - Status

**Overall Status:** Done - archived; PRD alignment rerun approved
**Last Updated:** 2026-07-06
**Plan Run:** `2026-07-06-6e3851ab`

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|
| F0037-S0001 | Resolve current user distribution scope | Done |
| F0037-S0002 | Enforce hierarchy-aware read scoping | Done |
| F0037-S0003 | Apply visibility to search, saved views, insights, and reports | Done |
| F0037-S0004 | Add distribution rollup reporting | Done |
| F0037-S0005 | Add rollup filters, panels, drilldowns, and no-leak states | Done |
| F0037-S0006 | Add security evidence and reconciliation checks | Done |

## Required Role Matrix

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Access-scoping, no-leak behavior, saved-view reapplication, and rollup reconciliation require independent validation evidence. | Product Manager | 2026-07-06 |
| Code Reviewer | Yes | Authorization, projection visibility, and aggregation flow require independent implementation review. | Product Manager | 2026-07-06 |
| Security Reviewer | Yes | F0037 introduces hierarchy/territory/producer-based access enforcement and hidden-record no-leak requirements. | Product Manager | 2026-07-06 |
| Architect | Yes | Phase B must approve the distribution-scope service, API/schema/security deltas, ontology bindings, and implementation direction before build. | Product Manager | 2026-07-06 |
| DevOps | Conditional | Required only if Phase B introduces materialized rollup jobs, background recomputation, new runtime infrastructure, or deployability changes. | Product Manager | 2026-07-06 |

## Story Signoff Provenance

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0037-S0001 | Architect | Architect | APPROVED | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/g0-assembly-plan-validation.md | 2026-07-06 | E2E rerun confirms existing architecture remains authoritative. |
| F0037-S0001 | Quality Engineer | Quality Engineer | PASS | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/test-execution-report.md | 2026-07-06 | E2E rerun validation covers sidebar access to the F0037 rollups UI. |
| F0037-S0001 | Code Reviewer | Code Reviewer | PASS | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/code-review-report.md | 2026-07-06 | E2E rerun review confirms no data-access behavior changed. |
| F0037-S0001 | Security Reviewer | Security Reviewer | PASS | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/security-review-report.md | 2026-07-06 | E2E rerun review confirms navigation does not widen access. |
| F0037-S0002 | Architect | Architect | APPROVED | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/g0-assembly-plan-validation.md | 2026-07-06 | E2E rerun confirms direct-read and projection scope remain unchanged. |
| F0037-S0002 | Quality Engineer | Quality Engineer | PASS | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/test-execution-report.md | 2026-07-06 | E2E rerun validation confirms route discoverability only. |
| F0037-S0002 | Code Reviewer | Code Reviewer | PASS | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/code-review-report.md | 2026-07-06 | E2E rerun review confirms no direct-read code changed. |
| F0037-S0002 | Security Reviewer | Security Reviewer | PASS | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/security-review-report.md | 2026-07-06 | E2E rerun review confirms no hidden-record behavior changed. |
| F0037-S0003 | Architect | Architect | APPROVED | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/g0-assembly-plan-validation.md | 2026-07-06 | E2E rerun confirms search/report/insight scoping remains unchanged. |
| F0037-S0003 | Quality Engineer | Quality Engineer | PASS | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/test-execution-report.md | 2026-07-06 | E2E rerun validation covers Operational Reports landing behavior. |
| F0037-S0003 | Code Reviewer | Code Reviewer | PASS | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/code-review-report.md | 2026-07-06 | E2E rerun review confirms predicate-first flow untouched. |
| F0037-S0003 | Security Reviewer | Security Reviewer | PASS | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/security-review-report.md | 2026-07-06 | E2E rerun review confirms no access-control changes. |
| F0037-S0004 | Architect | Architect | APPROVED | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/g0-assembly-plan-validation.md | 2026-07-06 | E2E rerun confirms distribution rollup API/schema remain unchanged. |
| F0037-S0004 | Quality Engineer | Quality Engineer | PASS | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/test-execution-report.md | 2026-07-06 | E2E rerun validation covers default Distribution rollups landing query. |
| F0037-S0004 | Code Reviewer | Code Reviewer | PASS | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/code-review-report.md | 2026-07-06 | E2E rerun review confirms rollup service code untouched. |
| F0037-S0004 | Security Reviewer | Security Reviewer | PASS | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/security-review-report.md | 2026-07-06 | E2E rerun review confirms scoped aggregation unchanged. |
| F0037-S0005 | Architect | Architect | APPROVED | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/g0-assembly-plan-validation.md | 2026-07-06 | E2E rerun is specifically tied to UI discoverability for this story. |
| F0037-S0005 | Quality Engineer | Quality Engineer | PASS | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/test-execution-report.md | 2026-07-06 | E2E rerun validation confirms E2E validation reaches the rollups tab. |
| F0037-S0005 | Code Reviewer | Code Reviewer | PASS | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/code-review-report.md | 2026-07-06 | E2E rerun review accepts the sidebar-only UI wiring. |
| F0037-S0005 | Security Reviewer | Security Reviewer | PASS | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/security-review-report.md | 2026-07-06 | E2E rerun review confirms no leak-prone UI copy added. |
| F0037-S0006 | Architect | Architect | APPROVED | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/g0-assembly-plan-validation.md | 2026-07-06 | E2E rerun confirms KG semantics remain stable. |
| F0037-S0006 | Quality Engineer | Quality Engineer | PASS | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/test-execution-report.md | 2026-07-06 | E2E rerun harness validation is recorded in this run. |
| F0037-S0006 | Code Reviewer | Code Reviewer | PASS | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/code-review-report.md | 2026-07-06 | E2E rerun review confirms change is limited to navigation. |
| F0037-S0006 | Security Reviewer | Security Reviewer | PASS | planning-mds/operations/evidence/runs/2026-07-06-2e7e606d/security-review-report.md | 2026-07-06 | E2E rerun security review confirms no policy or data-access drift. |

## Phase B Architecture Status

| Artifact | Status | Notes |
|----------|--------|-------|
| `feature-assembly-plan.md` | Drafted | Implementation execution plan for scope resolver, predicate-first projection visibility, distribution rollups, UI states, and evidence. |
| `planning-mds/api/nebula-api.yaml` | Updated | Adds hierarchy filters and `GET /operational-reports/distribution-rollups`. |
| `planning-mds/schemas/distribution-rollup-report.schema.json` | Created | Shared JSON Schema for F0037 rollup response. |
| `authorization-matrix.md` / `policy.csv` | Updated | Adds `distribution_rollup:read` role gate and no-leak constraints. |
| KG canonical/mapping/code-index | Updated | Adds Phase B semantic nodes and planning bindings. |

## Closeout Summary

F0037 is archived and approved. PRD alignment rerun `2026-07-06-2e7e606d` confirmed and corrected the Operational Reports Distribution Rollups UI at `/operational-reports?report=rollups`.

**Current gate:** Approved via PRD alignment rerun 2026-07-06-2e7e606d.
