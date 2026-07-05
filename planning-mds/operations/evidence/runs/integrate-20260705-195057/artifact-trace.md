# Artifact Trace — integrate dry run

## Read
- chore/merge-PRs, replay/pr-47, merge-base 103d59b (git)
- planning-mds/knowledge-graph/{canonical-nodes,code-index,feature-mappings}.yaml (3 refs each)
- planning-mds/features/{REGISTRY,ROADMAP}.md (3 refs each)

## Created (this run folder)
- integration-report.json — machine-readable outcome
- artifacts/merge3-*.json — 5 semantic merge reports (4 clean, 1 conflict)
- artifacts/code-index.git-clean-merge.yaml — git's textually clean union (discarded per contract)
- artifacts/code-index.poisoned-vs-semantic.diff — 5,198-line byte divergence from canonical merge

## Written to the integration worktree (discarded at dry-run cleanup)
- 4 canonical merged files (canonical-nodes, code-index, REGISTRY, ROADMAP)

## NOT written
- feature-mappings.yaml (all-or-nothing: 1 unresolved conflict)
- anything on chore/merge-PRs, main, or the contributor branch
