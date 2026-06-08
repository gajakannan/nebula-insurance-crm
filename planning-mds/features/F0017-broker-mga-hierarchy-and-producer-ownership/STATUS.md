# F0017 — Broker/MGA Hierarchy, Producer Ownership & Territory Management — Status

**Overall Status:** Planned — plan complete (Phase A + B approved, G1–G5 PASS, plan run `2026-06-06-5fb353e9`); plan-review repair complete 2026-06-06; rerun plan-review before feature/build action
**Last Updated:** 2026-06-06

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|
| F0017-S0001 | Model broker/MGA hierarchy (self-referencing, arbitrary depth) | Planned |
| F0017-S0002 | Navigate and traverse the distribution hierarchy | Planned |
| F0017-S0003 | Assign and maintain producer ownership (effective-dated) | Planned |
| F0017-S0004 | Define and manage territories with effective-dated assignment | Planned |
| F0017-S0005 | Audit and timeline for hierarchy, ownership, and territory changes | Planned |

## Required Signoff Roles (Set in Planning)

> Architect finalized in Phase B (ADR-026). Security Reviewer is **not forced** for this slice because hierarchy-aware access-control enforcement is deferred to F0037 (ADR-026 §6); no recursive access or hierarchy-based permissions are introduced here.

## Plan-Review Repair Notes

- 2026-06-06: Addressed plan-review run `2026-06-06-aec58eee` findings by binding F0017 OpenAPI paths, JSON Schemas, and role-based mutation policy rules; clarifying F0023 as a downstream reporting substrate rather than a build prerequisite; and tightening deferred F0037 rollup/read-enforcement language.
- Next gate: rerun `plan-review` after validators pass so the prior NOT READY evidence is superseded by a fresh readiness report.

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Hierarchy traversal, effective-dated ownership/territory, and overlap/cycle validation require test evidence. | Architect (Phase B, confirmed) | 2026-06-06 |
| Code Reviewer | Yes | Self-referencing model, effective-dating, and audit semantics require independent review. | Architect (Phase B, confirmed) | 2026-06-06 |
| Security Reviewer | No | Access-control enforcement deferred to F0037 (ADR-026 §6); no recursive access or hierarchy-based permissions introduced in this slice. | Architect (Phase B, confirmed) | 2026-06-06 |
| DevOps | No | Standard EF Core migrations + indexes (data-model §9.5); no new deploy topology. Revisit at the feature action if runtime/deploy risk emerges. | Architect (Phase B) | 2026-06-06 |
| Architect | Yes | Self-referencing hierarchy + cached ancestry + effective-dated relationships (ADR-026) warrant G0 assembly-plan validation at build. | Architect (Phase B) | 2026-06-06 |

## Story Signoff Provenance

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0017-S0001 | Quality Engineer | - | N/A | - | - | Populate after story breakdown is created. |
| F0017-S0001 | Code Reviewer | - | N/A | - | - | Populate after story breakdown is created. |

## Backend Progress (feature run 2026-06-07-771a5ef6)

> In-progress build. Gates G0 (assembly plan) and G1 (runtime preflight) passed (validator exit 0). Step 1 implementation is partial: the **DistributionNode hierarchy vertical (S0001/S0002) is complete and tested**; producer ownership (S0003), territory (S0004), audit (S0005), and the frontend slice are deferred to the next session.

| Slice | Status | Evidence |
|-------|--------|----------|
| Entities + EF configs + migration (S0001/S0003/S0004) | Done (4 entities, migration `20260608033854_F0017_…` applies in postgres) | `dotnet build` 0 errors; migration applied by integration-test startup |
| DistributionNode service + 3 endpoints + Casbin + DI (S0001/S0002) | Done, tested | 8 integration tests green (reparent/cycle/ancestry/ancestors/descendants/403/404/428) |
| ProducerOwnership service + endpoints (S0003) | Pending (entity+config done) | — |
| Territory + TerritoryAssignment service + endpoints (S0004) | Pending (entity+config done) | — |
| Frontend distribution panels (S0002/S0003/S0004) | Pending (CI-validated) | — |

Test evidence: `planning-mds/operations/evidence/runs/2026-06-07-771a5ef6/artifacts/test-results/step1-hierarchy-integration-tests.txt` — `dotnet test … DistributionEndpointTests|BrokerEndpointTests` → 20/20 passed (sdk 10.0 + Testcontainers postgres:16).
