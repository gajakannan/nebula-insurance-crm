# F0040 — Neuron Second Specialist Head (Broker Activity) — Status

**Overall Status:** In Progress — implementation complete, G2 passed with recommendations
**Last Updated:** 2026-09-02

> G1 selected `broker_activity` as the second live specialist domain. Phase A
> defines three read/platform slices and was explicitly approved at G3 on
> 2026-08-31. Phase B architecture, contracts, KG source, and final role matrix
> were approved by the operator at G5 on 2026-08-31.

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|
| F0040-S0001 | Live Broker activity Day-at-a-Glance zone with Broker 360 drill-through | Implemented — G2 |
| F0040-S0002 | Direct recent-broker-activity routing with durable replay | Implemented — G2 |
| F0040-S0003 | Two-live-head platform hardening, failure isolation, and telemetry | Implemented — G2 |

## Story × Role Progress

Cell states: `not-started` · `in-progress` · `done` · `not-in-scope`; review columns resolve to `PASS` / `FAIL` during the feature action.

| Story | Backend | Frontend | AI | QA | DevOps | Code Review | Security | Overall |
|-------|---------|----------|----|----|--------|-------------|----------|---------|
| F0040-S0001 | done | done | done | done | not-in-scope | not-started | not-started | in-progress |
| F0040-S0002 | not-in-scope | done | done | done | not-in-scope | not-started | not-started | in-progress |
| F0040-S0003 | done | not-in-scope | done | done | done | not-started | not-started | in-progress |

## Required Role Matrix

This matrix is final for the approved architecture and governs the future feature action.

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Authorization matrix, newest-20 feed contract, two-head failure isolation, durable replay, performance, and telemetry acceptance coverage. | Architect | 2026-08-31 |
| Code Reviewer | Yes | Independent review across engine, Neuron, and frontend contract changes. | Architect | 2026-08-31 |
| Security Reviewer | Yes | Query-before-count broker scope, internal-only principal boundary, catalog/registry fail-closed behavior, cross-owner replay, and telemetry minimization. | Architect | 2026-08-31 |
| AI Engineer | Yes | Shared executor, specialist head, intent catalog/routing fixtures, component contracts, provenance, and evaluation coverage under `neuron/`. | Architect | 2026-08-31 |
| Architect | Yes | ADR-037 conformance and replay-compatible two-live-head contract require as-built reconciliation. | Architect | 2026-08-31 |
| DevOps | No | The proposed design adds no service, port, secret, environment variable, migration, or deployment topology. | Architect | 2026-08-31 |

## Story Signoff Provenance

Rows are appended during the future feature action after the Phase B matrix is final. No implementation evidence exists during this plan run.

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|

## Deferred Non-Blocking Follow-ups

| Follow-up | Why deferred | Tracking link | Owner |
|-----------|--------------|---------------|-------|
| Cross-zone ranking/composition (Day-at-a-Glance brain) | Requires more than independent head assembly and remains Later by epic decision. | BLUEPRINT.md §3.3 Neuron Companion | Product Manager |
| Additional live Tasks/Pipeline heads | F0040 is limited to the approved second head. | Future feature allocation | Product Manager |
| Broker filters, summaries, recommendations, and writes | Not required to activate the established read-only broker feed. | Future feature allocation | Product Manager |

## Tracker Sync Checklist

- [x] `REGISTRY.md` status/path aligned after Phase B KG compile
- [x] `ROADMAP.md` rationale aligned after Phase B KG compile
- [x] `STORY-INDEX.md` regenerated for S0001–S0003
- [x] `BLUEPRINT.md` F0040 scope and story links aligned
- [x] Story validation passes
- [x] Tracker validation passes with feature-evidence validation skipped for this plan run

## Closeout Summary

Not applicable during planning. This section is completed only after implementation and required signoffs.

## Notes

- G1 decision: `broker_activity` approved by the operator for plan run `2026-08-30-af27c9c1`.
- Existing feed semantics come from archived F0001-S0004 and F0002-S0007; F0040 does not redefine broker events or authorization.
- Dependency features F0038, F0039, F0001, and F0002 are archived as completed. Evidence verification is audit-pending for this base-run-only plan and no repo-wide evidence audit substitutes for it.
- Phase B owns `feature-assembly-plan.md`, ADR/API/schema decisions, final required roles, and all `kg-source/**` updates.
- Feature run `2026-09-01-fd408477` G0 re-affirmed the approved assembly-plan ordering and Required Role Matrix against the current engine, Neuron, and frontend source surfaces.
