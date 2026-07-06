# KG Reconciliation — F0017 run 2026-06-07-771a5ef6

## Summary

Verdict: PASS WITH RECOMMENDATIONS.

G7 KG reconciliation ran after promoting F0017 to Active/Done and regenerating `planning-mds/knowledge-graph/coverage-report.yaml`.

## Commands

- `python scripts/kg/validate.py` initially failed because `coverage-report.yaml` was stale.
- `python scripts/kg/validate.py --write-coverage-report` regenerated the coverage report and exited 0.
- `python scripts/kg/validate.py` exited 0 after regeneration.
- `python scripts/kg/cochange.py --coverage-gaps` exited 0.

## KG Validation Result

Knowledge graph validation after regeneration:
- Features mapped: 28
- Stories mapped: 137
- Feature coverage: 28 mapped, 12 excluded, 0 uncovered
- Code bindings: 186
- Symbol index: 2702 symbols, 2702 on bound nodes
- Result: PASS

Warnings are pre-existing graph hygiene warnings, mainly unknown `entity-renewal:stub-*` symbols and one low-confidence inferred edge on `feature:F0028` in `feature:F0018.depends_on`. No F0017-specific KG blocker was found.

## Binding Delta

F0017 tracker promotion changed the evidence validation surface from Planned/skip to Active/Done/full validation. KG coverage was regenerated to reflect the current graph state. No F0017-specific binding gap remains after regeneration.

## Canonical Nodes

Canonical F0017 nodes remain the feature, five stories, ADR-026, distribution hierarchy entities/endpoints, producer ownership entities/endpoints, territory entities/endpoints, frontend distribution slice, and related evidence artifacts. The implemented scope stays aligned to ADR-026: structural/effective-dated model and audit events, with hierarchy-aware enforcement and rollups deferred to F0037.

## Validator Results

`scripts/kg/validate.py --write-coverage-report` exited 0 and refreshed `planning-mds/knowledge-graph/coverage-report.yaml`. A follow-up `scripts/kg/validate.py` exited 0. `scripts/kg/cochange.py --coverage-gaps` exited 0.

## Cochange Result

`scripts/kg/cochange.py --coverage-gaps` completed with exit 0. The output reports repository-wide co-change coverage gaps such as shared infrastructure/config files; these are broad graph-hygiene signals and not blockers for F0017 closeout.

## Reconciliation Decision

F0017 can proceed to G8 closeout. KG state is valid after coverage report regeneration, with non-blocking repository-wide warnings carried forward.

## Handoff to Closeout

Carry the repository-wide KG warnings into PM closeout as non-blocking graph hygiene. No F0017-specific KG action blocks G8.
