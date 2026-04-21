# Changelog

The Nebula Insurance CRM is a continuously-deployed product and uses release-branch tagging rather than semantic versioning. This changelog records the split provenance and major product-wide milestones only; feature-level history lives in per-feature archive records under `planning-mds/features/archive/`.

---

## Initial release — 2026-04-20

Split from `gajakannan/nebula-crm` at commit `d2fa37c4216147b7a0be399e4133dac59ef75d9f` (recorded identically in `.split-baseline`).

- Application code preserved: `engine/`, `experience/`, `neuron/`, `scripts/kg/`, `bruno/`
- Historical planning and archive evidence preserved: `planning-mds/features/archive/`, `planning-mds/operations/evidence/`
- Framework layer removed: role/action assets, bootstrap assets, and the builder Docker image moved to the standalone `nebula-agents` framework repo
- Live operator-facing docs and validation wiring rewritten to remove any local framework-path dependency
- `lifecycle-stage.yaml` authored fresh with Phase A product-local gates (`knowledge_graph_sync`, `solution_contract`, `frontend_quality`); Phase B rehomes `api_contract`, `infra_strict`, and `security_planning_strict` as product-local validators before CI / branch protection is restored

No version tag applied at split time. The split is recorded by baseline hash, not by tag.
