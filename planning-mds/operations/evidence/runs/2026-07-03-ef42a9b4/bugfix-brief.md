# Bugfix Brief

## Impact

GitHub PR #47 could not merge because generated tracker/KG artifacts diverged from upstream Neuron/F0038 planning work.

## Affected Users

- PR reviewer: blocked by GitHub conflict UI.
- Future Nebula agents: at risk of reading duplicate `adr:028` semantics if unresolved.
- F0021 testers: not blocked by runtime conflict, but PR integration could not proceed.

## Acceptance Checks

- PR branch has no unresolved merge conflicts.
- F0021 Communication KG mappings remain present.
- Upstream F0038/Neuron KG mappings remain present.
- F0021 Communication ADR is uniquely `ADR-029`.
- Nebula harness validations pass.
- Focused F0021 frontend regression passes.

## Non-Goals

- No new F0021 product behavior.
- No changes to closed F0021 feature evidence or `latest-run.json`.
- No changes to `nebula-agents` harness code.

## Feature Promotion Recommendation

No feature promotion required. This is an integration defect fix.
