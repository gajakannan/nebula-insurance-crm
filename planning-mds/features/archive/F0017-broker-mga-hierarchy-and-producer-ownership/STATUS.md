# F0017 — Broker/MGA Hierarchy, Producer Ownership & Territory Management — Status

**Overall Status:** Done / Archived — feature run `2026-06-07-771a5ef6` completed through G8 closeout and archive transition under the same run.
**Last Updated:** 2026-07-03
**Closeout Review Date:** 2026-07-03
**Archived:** 2026-07-03

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|
| F0017-S0001 | Model broker/MGA hierarchy (self-referencing, arbitrary depth) | Done |
| F0017-S0002 | Navigate and traverse the distribution hierarchy | Done |
| F0017-S0003 | Assign and maintain producer ownership (effective-dated) | Done |
| F0017-S0004 | Define and manage territories with effective-dated assignment | Done |
| F0017-S0005 | Audit and timeline for hierarchy, ownership, and territory changes | Done |

## Required Role Matrix

> Architect finalized in Phase B (ADR-026). Security Reviewer is **not forced** for this slice because hierarchy-aware access-control enforcement is deferred to F0037 (ADR-026 §6); no recursive access or hierarchy-based permissions are introduced here.

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Hierarchy traversal, effective-dated ownership/territory, overlap/cycle validation, and frontend feature evidence require test evidence. | Architect (Phase B, confirmed) | 2026-06-06 |
| Code Reviewer | Yes | Self-referencing model, effective-dating, territory overlap semantics, and audit payload semantics require independent review. | Architect (Phase B, confirmed) | 2026-06-06 |
| Security Reviewer | No | Access-control enforcement deferred to F0037 (ADR-026 §6); no recursive access or hierarchy-based permissions introduced in this slice. | Architect (Phase B, confirmed) | 2026-06-06 |
| DevOps | Yes | EF Core migration and harness path classes make the slice deployment-config-bearing even though no new topology is introduced. | Product Manager (G2/G5 evidence) | 2026-07-02 |
| Architect | Yes | Self-referencing hierarchy + cached ancestry + effective-dated relationships (ADR-026) warrant assembly-plan validation and KG reconciliation. | Architect (Phase B) | 2026-06-06 |

## Plan-Review Repair Notes

- 2026-06-06: Addressed plan-review run `2026-06-06-aec58eee` findings by binding F0017 OpenAPI paths, JSON Schemas, and role-based mutation policy rules; clarifying F0023 as a downstream reporting substrate rather than a build prerequisite; and tightening deferred F0037 rollup/read-enforcement language.
- 2026-07-02: Plan-review rerun `2026-07-02-11251b53` returned READY; implementation evidence resumed the existing run `2026-06-07-771a5ef6`.

## Story Signoff Provenance

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0017-S0001 | Quality Engineer | Product Manager / QE | PASS | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/test-execution-report.md` | 2026-07-02 | Distribution hierarchy endpoint integration coverage included in 24/24 backend rerun. |
| F0017-S0001 | Code Reviewer | Code Reviewer | APPROVED | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/code-review-report.md` | 2026-07-02 | Reparent audit payload repair included. |
| F0017-S0001 | DevOps | DevOps | PASS | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/deployability-check.md` | 2026-07-02 | EF migration validated through integration-test startup. |
| F0017-S0001 | Architect | Architect | PASS | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/g0-assembly-plan-validation.md` | 2026-06-07 | ADR-026 assembly-plan alignment accepted. |
| F0017-S0002 | Quality Engineer | Product Manager / QE | PASS | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/test-execution-report.md` | 2026-07-02 | Ancestors/descendants backend coverage and hierarchy panel frontend test included. |
| F0017-S0002 | Code Reviewer | Code Reviewer | APPROVED | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/code-review-report.md` | 2026-07-02 | Navigation read paths reviewed. |
| F0017-S0002 | DevOps | DevOps | PASS | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/deployability-check.md` | 2026-07-02 | Frontend build and runtime status accepted with warnings. |
| F0017-S0002 | Architect | Architect | PASS | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/g0-assembly-plan-validation.md` | 2026-06-07 | Cached ancestry read model accepted. |
| F0017-S0003 | Quality Engineer | Product Manager / QE | PASS | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/test-execution-report.md` | 2026-07-02 | Producer ownership assign/reassign/as-of coverage included. |
| F0017-S0003 | Code Reviewer | Code Reviewer | APPROVED | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/code-review-report.md` | 2026-07-02 | Effective-dated ownership semantics reviewed. |
| F0017-S0003 | DevOps | DevOps | PASS | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/deployability-check.md` | 2026-07-02 | Producer ownership table/index migration covered. |
| F0017-S0003 | Architect | Architect | PASS | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/g0-assembly-plan-validation.md` | 2026-06-07 | Effective-dated relationship design accepted. |
| F0017-S0004 | Quality Engineer | Product Manager / QE | PASS | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/test-execution-report.md` | 2026-07-02 | Territory assignment coverage includes G3 cross-territory reassignment regression. |
| F0017-S0004 | Code Reviewer | Code Reviewer | APPROVED | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/code-review-report.md` | 2026-07-02 | G3 territory overlap blocker repaired and approved. |
| F0017-S0004 | DevOps | DevOps | PASS | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/deployability-check.md` | 2026-07-02 | Territory tables/index migration covered. |
| F0017-S0004 | Architect | Architect | PASS | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/g0-assembly-plan-validation.md` | 2026-06-07 | Territory overlap-prevention design accepted. |
| F0017-S0005 | Quality Engineer | Product Manager / QE | PASS | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/test-execution-report.md` | 2026-07-02 | Audit/timeline emission covered through structural mutation paths. |
| F0017-S0005 | Code Reviewer | Code Reviewer | APPROVED | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/code-review-report.md` | 2026-07-02 | Descendant audit payload repair reviewed. |
| F0017-S0005 | DevOps | DevOps | PASS | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/deployability-check.md` | 2026-07-02 | Timeline/audit changes deploy with existing runtime. |
| F0017-S0005 | Architect | Architect | PASS | `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/g0-assembly-plan-validation.md` | 2026-06-07 | Audit/timeline requirement accepted in assembly plan. |

## Implementation Progress (feature run 2026-06-07-771a5ef6)

> Feature implementation and evidence are complete through G8. The feature folder was archived on 2026-07-03 as a PM-owned closeout correction under the same run.

| Slice | Status | Evidence |
|-------|--------|----------|
| Entities + EF configs + migration (S0001/S0003/S0004) | Done | `20260608033854_F0017_DistributionHierarchyOwnershipTerritory.cs`; backend integration startup applies schema path. |
| DistributionNode service + endpoints + Casbin + DI (S0001/S0002) | Done, tested | `DistributionEndpointTests`; G3 audit-depth repair included. |
| ProducerOwnership service + endpoints + Casbin + DI (S0003) | Done, tested | `ProducerOwnershipEndpointTests`. |
| Territory + TerritoryAssignment service + endpoints + Casbin + DI (S0004) | Done, tested | `TerritoryEndpointTests`; G3 cross-territory reassignment regression added. |
| Audit/timeline on structural mutations (S0005) | Done, reviewed | timeline events on reparent/ownership-assign/territory-create/member-assign. |
| Frontend distribution slice (S0002/S0003/S0004) | Done, validated | `experience/src/features/distribution/**`; Vitest feature test, lint, and build pass. |

Test evidence:
- `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/artifacts/test-results/f0017-backend-after-g3.trx` — 24/24 backend integration tests passed.
- `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/artifacts/test-results/f0017-frontend-vitest.txt` — 2/2 feature-level frontend tests passed.
- `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/artifacts/test-results/frontend-lint.txt` — frontend lint exit 0.
- `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/artifacts/test-results/frontend-build.txt` — frontend production build exit 0.

## Deferred Non-Blocking Follow-ups

- OpenAPI advisory: remediate or explicitly accept `Microsoft.OpenApi` GHSA-v5pm-xwqc-g5wc before release.
- Frontend bundle size: Vite reports a large JS chunk; track route-level splitting or chunk policy.
- Frontend panel coverage: add broader tests for `OwnershipPanel` and `TerritoriesPanel` if those are release-critical surfaces before final production rollout.
- Migration snapshot drift: prior evidence states the F0017 migration is scoped to its four tables while broader branch snapshot drift is pre-existing.
