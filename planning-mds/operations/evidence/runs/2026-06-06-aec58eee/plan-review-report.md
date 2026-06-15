# Plan Review Report

Scope: feature `F0017` - Broker/MGA Hierarchy, Producer Ownership & Territory Management
Run ID: `2026-06-06-aec58eee`
Date: 2026-06-06
Review Question: Is this plan ready to build?

## Decision

- Status: NOT READY
- Rationale: The F0017 stories and core ADR are detailed, but PR2 did not pass because `python3 scripts/kg/validate.py` failed with stale `coverage-report.yaml`. A required readiness validator is red, and plan-review is read-only, so the graph cannot be repaired in this run. Additional high/medium handoff gaps remain around missing concrete API/schema contracts and dependency/scope ambiguity.
- Next Action: Return to `plan.md` or targeted Architect/PM rework. Regenerate and validate KG coverage, add or bind the F0017 OpenAPI/schema contracts before implementation handoff, resolve F0023 dependency wording, then rerun plan-review.

## Findings By Severity

### Critical

- [critical] Required KG validator failed: `coverage-report.yaml` is stale. Location: `planning-mds/operations/evidence/runs/2026-06-06-aec58eee/artifacts/validator-03-kg-validate.log`; Impact: PR2 Validator Pass is not green, and KG readiness cannot rely on the current coverage report. This blocks a readiness decision of READY or CONDITIONALLY READY under the plan-review gate. Owner: Architect. Recommendation: In owning rework, run `python3 scripts/kg/validate.py --write-coverage-report`, then rerun `python3 scripts/kg/validate.py` and `python3 scripts/kg/validate.py --check-drift` before rerunning plan-review.

### High

- [high] F0017 backend behavior has only design-level endpoint bullets; full OpenAPI and JSON schemas are explicitly deferred to the feature action. Location: `planning-mds/architecture/decisions/ADR-026-broker-mga-hierarchy-producer-ownership-and-territory.md` lines 83-89 and 135-138; `planning-mds/knowledge-graph/feature-mappings.yaml` lines 907-909. Impact: Stories S0001-S0005 require mutations, reads, ProblemDetails errors, request bodies, response bodies, concurrency behavior, and audit payloads, but implementers do not yet have bound OpenAPI paths or schemas. This can force invention during implementation or G0. Owner: Architect. Recommendation: Add/bind the F0017 OpenAPI paths and JSON schemas, or produce an explicit architect-owned contract artifact before implementation agents build against it.

### Medium

- [medium] Dependency wording conflicts on whether F0023 blocks F0017. Location: `planning-mds/features/F0017-broker-mga-hierarchy-and-producer-ownership/PRD.md` lines 71-74; `planning-mds/knowledge-graph/feature-mappings.yaml` lines 904-909; conflicts with `planning-mds/features/ROADMAP.md` lines 23-25, which says F0017 depends only on completed F0002 and can start immediately. Recommendation: PM should clarify F0023 as a downstream consumer/reporting substrate, not a prerequisite for F0017 build, and align KG dependency semantics.
- [medium] PRD solution-design prose still describes deferred rollup and access-enforcement work. Location: `planning-mds/features/F0017-broker-mga-hierarchy-and-producer-ownership/PRD.md` lines 42-44 and 80-103; ADR-026 lines 24-27 and 77-81 defer those concerns to F0037. Recommendation: PM/Architect should remove or qualify rollup-services, hierarchy-aware filters, and parent/child visibility language so implementation does not accidentally widen the slice.

### Low

- [low] `GETTING-STARTED.md` still says to refine the feature into stories and an implementation contract even though stories now exist and plan status says Phase A+B are approved. Location: `planning-mds/features/F0017-broker-mga-hierarchy-and-producer-ownership/GETTING-STARTED.md` lines 7-13. Recommendation: PM should refresh the handoff notes during plan rework so implementers see current setup and verification guidance.

## Product Readiness

- Requirements quality: Mostly strong. The PRD has clear MVP scope and non-goals in lines 33-64, and all five stories have concrete acceptance criteria. The residual PRD scope conflict around rollups/access enforcement needs PM cleanup.
- Story testability: PASS. `validate-stories.py` passed for S0001-S0005, and the stories include validation failures, persistence evidence, point-in-time reads, concurrency, and audit expectations.
- Mutation contracts: Strong. S0001, S0003, S0004, and S0005 name entry points, editable/read-only states, roles, save results, reload evidence, and error behavior.
- UI/screen readiness: Adequate for planning. PRD lines 116-174 include desktop and narrow ASCII layouts for hierarchy, ownership, territory, and timeline surfaces.
- Tracker state: Tracker validation passed, but dependency wording should be aligned because ROADMAP says F0017 can start while PRD/KG still list F0023 as a dependency.

## Architecture Readiness

- API/schema readiness: Not ready. ADR-026 provides endpoint candidates, but full OpenAPI and schemas are deferred and not bound in KG.
- Data/workflow readiness: Mostly ready. Data model section 9 and ADR-026 define self-reference, cached ancestry, effective dating, territory overlap rules, audit atomicity, migration order, and relevant indexes.
- Authorization readiness: Partially ready. ADR-026 clearly defers hierarchy-aware read scoping to F0037 and keeps F0017 to authenticated internal reads plus authorized mutations, but PRD prose still mentions parent-child visibility and territory scoping.
- ADR and NFR readiness: ADR-026 is accepted and stories define measurable performance/reliability targets. Main gap is contract-level API/schema detail.
- KG/ontology alignment: Not ready. Lookup resolves F0017 confidently and drift check passed, but base KG validation failed due stale coverage report.

## Buildability Challenge

- Vertical slice size: Large but bounded if scope is kept to structural hierarchy, ownership, territory, and audit. It is not build-ready until KG and API/schema handoffs are repaired.
- Role handoffs: STATUS requires Architect, Quality Engineer, and Code Reviewer; Security and DevOps are not forced because enforcement/deploy topology are deferred. This is coherent with ADR-026 once PRD wording is cleaned up.
- Testability: Good story-level coverage targets exist: cycle/self-parent, overlap, backdating, point-in-time reads, concurrency, atomic audit, immutable events, read-only/error states. API/schema gaps weaken implementation-test mapping.
- Dependency and sequencing clarity: Needs PM/KG alignment for F0023 semantics before build starts.
- Risk hotspots: Self-referencing reparent with descendant ancestry recompute, effective-dated close/open transactions, territory overlap detection, audit atomicity, and deferred authorization boundaries require focused G0 and QE coverage.

## Validation Evidence

- `python3 agents/product-manager/scripts/validate-stories.py /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/planning-mds/features/F0017-broker-mga-hierarchy-and-producer-ownership`: PASS - see `artifacts/validator-01-validate-stories.log`.
- `python3 agents/product-manager/scripts/validate-trackers.py`: PASS - see `artifacts/validator-02-validate-trackers.log`.
- `python3 /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/scripts/kg/validate.py`: FAIL - stale `coverage-report.yaml`; see `artifacts/validator-03-kg-validate.log`.
- `python3 /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/scripts/kg/validate.py --check-drift`: PASS - see `artifacts/validator-04-kg-check-drift.log`.
- `python3 agents/scripts/validate_templates.py`: PASS - see `artifacts/validator-05-validate-templates.log`.
- `python3 /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/scripts/kg/lookup.py F0017`: PASS routing aid - see `artifacts/kg-lookup-F0017.log`.

## Artifact Trace

- `planning-mds/features/F0017-broker-mga-hierarchy-and-producer-ownership/PRD.md`: reviewed for scope, dependencies, screen/UI readiness, and product handoff.
- `planning-mds/features/F0017-broker-mga-hierarchy-and-producer-ownership/F0017-S0001-*.md` through `F0017-S0005-*.md`: reviewed for acceptance criteria, mutation contracts, role constraints, validation failures, and testability.
- `planning-mds/features/F0017-broker-mga-hierarchy-and-producer-ownership/README.md`, `STATUS.md`, `GETTING-STARTED.md`: reviewed for feature index, signoff, implementation handoff, and stale notes.
- `planning-mds/features/REGISTRY.md`, `ROADMAP.md`, `BLUEPRINT.md`: reviewed for tracker and sequencing state.
- `planning-mds/architecture/decisions/ADR-026-broker-mga-hierarchy-producer-ownership-and-territory.md`: reviewed for accepted architecture decisions, API posture, authorization posture, and follow-ups.
- `planning-mds/architecture/data-model.md` section 9: reviewed for entity fields, invariants, audit, indexes, and migration order.
- `planning-mds/api/nebula-api.yaml`, `planning-mds/schemas/**`, `planning-mds/security/authorization-matrix.md`, `planning-mds/security/policies/policy.csv`: searched for F0017 endpoint/schema/policy coverage.
- `planning-mds/knowledge-graph/feature-mappings.yaml`, `canonical-nodes.yaml`, `code-index.yaml`, `solution-ontology.yaml`, `coverage-report.yaml`: reviewed for feature mappings, canonical nodes, and KG validator status.

## PR3 Self-Review Gate

- Product Manager self-review: Findings cite concrete files/sections; severity reflects handoff impact; no product fixes made.
- Architect self-review: API/schema and KG findings cite raw ADR/KG/validator evidence; no KG, API, schema, policy, or architecture edits made.
- Code Reviewer self-review: Buildability findings are tied to vertical-slice handoff and test mapping; no implementation or planning edits made.

## PR4 Readiness Gate

```json
{
  "gate": "plan_review",
  "question": "is_this_plan_ready_to_build",
  "status": "not_ready",
  "findings": {
    "critical": 1,
    "high": 1,
    "medium": 2,
    "low": 1
  },
  "can_start_feature_action": false,
  "requires_risk_acceptance": false,
  "available_actions": ["fix_findings", "cancel"]
}
```
