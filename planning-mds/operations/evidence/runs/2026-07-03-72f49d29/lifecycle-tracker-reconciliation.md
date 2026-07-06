# Lifecycle Tracker Reconciliation — F0024

## Reason

F0024 existed in both `planning-mds/features/F0024-claims-and-service-case-tracking` and `planning-mds/features/archive/F0024-claims-and-service-case-tracking` because run `2026-07-03-ba011af8` archived the feature, then run `2026-07-03-72f49d29` reopened it for PRD drift reconciliation.

## Decision

- Active folder remains the source of truth while run `2026-07-03-72f49d29` is in progress.
- Archive folder remains historical baseline evidence for run `2026-07-03-ba011af8`.
- Final G8 closeout must supersede the old archive and remove the duplicate active lifecycle state.

## Updates

- `REGISTRY.md` already points F0024 to the active folder.
- `feature-mappings.yaml` already points F0024 and story nodes to the active folder.
- `feature-mappings.yaml` excludes the archived F0024 folder as historical baseline coverage while active `feature:F0024` remains mapped to the drift-reconcile folder.
- `ROADMAP.md` now lists F0024 in Now for drift reconciliation and no longer lists it as current Completed scope.
- `STORY-INDEX.md` F0024 links now point to the active folder.
- Active `STATUS.md` now marks the prior closeout section as historical baseline provenance.

## Result

PASS for active-run tracker reconciliation. The duplicate folder is intentional only until G8 closeout.

## Validation

- `python3 scripts/kg/validate.py --write-coverage-report` PASS.
