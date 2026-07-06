# Artifact Trace — F0017 plan run 2026-07-02-cdf3040f

## Framework Artifacts

- `nebula-agents/agents/templates/prompts/evidence-contract/plan-operator-friendly.md` — strict plan-run contract.
- `nebula-agents/agents/actions/plan.md` — action flow and gate contract.
- `nebula-agents/agents/ROUTER.md` — reference routing rules.
- `nebula-agents/agents/agent-map.yaml` — role/action wiring.
- `nebula-agents/agents/docs/AGENT-USE.md` — product-root resolution and retrieval guard.

## Product Planning Artifacts

- `planning-mds/features/REGISTRY.md`
- `planning-mds/features/ROADMAP.md`
- `planning-mds/BLUEPRINT.md`
- `planning-mds/features/F0017-broker-mga-hierarchy-and-producer-ownership/**`

## Product Architecture And Ontology Artifacts

- `planning-mds/architecture/decisions/ADR-026-broker-mga-hierarchy-producer-ownership-and-territory.md`
- `planning-mds/architecture/data-model.md`
- `planning-mds/api/nebula-api.yaml`
- `planning-mds/security/authorization-matrix.md`
- `planning-mds/security/policies/policy.csv`
- `planning-mds/knowledge-graph/solution-ontology.yaml`
- `planning-mds/knowledge-graph/canonical-nodes.yaml`
- `planning-mds/knowledge-graph/feature-mappings.yaml`
- `planning-mds/knowledge-graph/code-index.yaml`
- `planning-mds/knowledge-graph/coverage-report.yaml`

## Changes During This Run

- Created base run evidence files for strict `plan` action run `2026-07-02-cdf3040f`.
- Reviewed F0017 PRD, README, STATUS, ROADMAP/REGISTRY references, raw story files, ADR-026, data model, OpenAPI, schema/security references, and KG lookup output.
- Ran F0017 story validation successfully for S0001-S0005.
- Ran story-index generation successfully; no tracked `STORY-INDEX.md` diff resulted.
- Ran KG preflight, KG validation, and KG drift check successfully; existing KG warnings are non-blocking and unrelated to F0017 gate readiness.
- Ran template validation successfully.
- `validate-trackers.py` failed repository-wide due to legacy/archived evidence references to missing artifacts. This is recorded as the current G2 blocker.
