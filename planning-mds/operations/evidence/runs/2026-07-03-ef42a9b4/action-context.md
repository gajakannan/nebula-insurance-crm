# Action Context

## Run Identity

- Run ID: `2026-07-03-ef42a9b4`
- Action: `defect-bugfix`
- Product root: `/Users/wallstreet62/Desktop/nebula3/nebula-insurance-crm`
- Lifecycle Authority: none

## Defect Scope

- `DEFECT_SUMMARY`: Resolve PR #47 merge conflicts between F0021 Communication Hub and upstream Neuron/F0038 planning/KG changes.
- `OBSERVED_BEHAVIOR`: GitHub reports PR #47 conflicts; merge-tree identifies conflicts in `STORY-INDEX.md`, `canonical-nodes.yaml`, `coverage-report.yaml`, and `feature-mappings.yaml`.
- `EXPECTED_BEHAVIOR`: PR #47 merges upstream `main` cleanly while preserving F0021 runtime behavior and upstream F0038/Neuron planning and KG semantics.
- `REPRO_STEPS`: `git fetch upstream main refs/pull/47/head:refs/remotes/upstream/pr/47`; `git merge-tree --write-tree upstream/main upstream/pr/47`.
- `AFFECTED_PATHS`: `planning-mds/features/STORY-INDEX.md`, `planning-mds/knowledge-graph/canonical-nodes.yaml`, `planning-mds/knowledge-graph/coverage-report.yaml`, `planning-mds/knowledge-graph/feature-mappings.yaml`, `planning-mds/architecture/decisions/ADR-028-communication-activity-capture-and-redaction.md`.
- `AGENT_ROLES`: architect, product-manager, quality-engineer.
- `FEATURE_REFS`: F0021, F0038.
- `ALLOW_FEATURE_PROPOSAL`: false.

## Scope Boundaries

- Resolve merge conflicts only.
- Preserve closed F0021 feature evidence and `latest-run.json`.
- Do not create new feature evidence.
- Do not modify `nebula-agents` unless validation proves the harness itself is broken.
- Do not change F0021 runtime behavior unless validation uncovers a defect caused by the merge.
