# PM Closeout - F0025-commission-producer-splits-and-revenue-tracking run 2026-07-07-9859bad4

> Required at G8/closeout per the feature evidence contract. PM-owned final approval artifact.

## Final Story Status

| Story | Final Status | Evidence | Notes |
|-------|--------------|----------|-------|
| F0025-S0001 | Done | signoff-ledger.md / test-execution-report.md / code-review-report.md / security-review-report.md | Commission workspace search and policy context delivered. |
| F0025-S0002 | Done | signoff-ledger.md / test-execution-report.md / code-review-report.md / security-review-report.md | Commission schedule maintenance delivered. |
| F0025-S0003 | Done | signoff-ledger.md / test-execution-report.md / code-review-report.md / security-review-report.md | Producer split assignment delivered. |
| F0025-S0004 | Done | signoff-ledger.md / test-execution-report.md / code-review-report.md / security-review-report.md | Expected commission calculation review delivered. |
| F0025-S0005 | Done | signoff-ledger.md / test-execution-report.md / code-review-report.md / security-review-report.md | Commission adjustment request and approval delivered. |
| F0025-S0006 | Done | signoff-ledger.md / test-execution-report.md / code-review-report.md / security-review-report.md | Revenue attribution rollups delivered. |

## Archive Decision

F0025 is Done and archived on 2026-07-07.

- Active path at run start: `planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking`
- Archived path at closeout: `planning-mds/features/archive/F0025-commission-producer-splits-and-revenue-tracking`
- Approved run: `2026-07-07-9859bad4`

## Deferred Follow-ups

| Follow-up | Owner | Target |
|-----------|-------|--------|
| Add dedicated source-scope regression tests for cross-policy expected commissions, adjustments, splits, and rollup totals. | Quality Engineer + Backend Developer | Before production release hardening |
| Rerun dependency, secrets, SAST, and DAST scanner classes in CI/staging. | Security Reviewer + DevOps | Before production release hardening |
| Confirm whether multi-participant revenue attribution needs one projection row per participant rather than one row per expected commission. | Product Manager + Architect | Before expanding compensation analytics |
| Rerun staging DAST with deployed frontend/API and authenticated paths where feasible. | Security Reviewer + DevOps | Before production release hardening |
| Track unrelated broad frontend localStorage suite failures outside F0025. | Quality Engineer | Separate test-environment cleanup |
| Confirm whether schedule-list reads need finer carrier-market scoping beyond `commission:read`. | Product Manager + Security Reviewer | Future hardening story if required |

## Recommendation Acceptances

- Accepted: F0025-S0001 - PM accepts the Code Reviewer and Security Reviewer `WITH RECOMMENDATIONS` signoff for this story because no Critical or High findings remain; deferred follow-ups are listed above.
- Accepted: F0025-S0002 - PM accepts the Code Reviewer and Security Reviewer `WITH RECOMMENDATIONS` signoff for this story because no Critical or High findings remain; deferred follow-ups are listed above.
- Accepted: F0025-S0003 - PM accepts the Code Reviewer and Security Reviewer `WITH RECOMMENDATIONS` signoff for this story because no Critical or High findings remain; deferred follow-ups are listed above.
- Accepted: F0025-S0004 - PM accepts the Code Reviewer and Security Reviewer `WITH RECOMMENDATIONS` signoff for this story because no Critical or High findings remain; deferred follow-ups are listed above.
- Accepted: F0025-S0005 - PM accepts the Code Reviewer and Security Reviewer `WITH RECOMMENDATIONS` signoff for this story because no Critical or High findings remain; deferred follow-ups are listed above.
- Accepted: F0025-S0006 - PM accepts the Code Reviewer and Security Reviewer `WITH RECOMMENDATIONS` signoff for this story because no Critical or High findings remain; deferred follow-ups are listed above.

## Tracker Updates

- `REGISTRY.md`: F0025 moved from Planned to Archived Features with archive date 2026-07-07.
- `ROADMAP.md`: F0025 moved from Now to Completed.
- `BLUEPRINT.md`: F0025 baseline status updated to Done with archived path.
- `STORY-INDEX.md`: regenerated after the archive move.
- `feature-mappings.yaml`: F0025 feature/story paths updated to archived paths and status set to `archived-done`.
- Scoped tracker validation: `validate-trackers.py --product-root /Users/msig4/Documents/NEBULA/nebula-insurance-crm --feature F0025 --run-id 2026-07-07-9859bad4` passed at G6 and will be rerun after closeout publication.

## Validator Results

| Gate / Check | Command | Exit Code | Result |
|--------------|---------|-----------|--------|
| G0 evidence | `validate-feature-evidence.py --stage G0` | 0 | PASS |
| G1 evidence | `validate-feature-evidence.py --stage G1` | 0 | PASS |
| G2 evidence | `validate-feature-evidence.py --stage G2` | 0 | PASS |
| G3 evidence | `validate-feature-evidence.py --stage G3` | 0 | PASS |
| G4 approval | `validate-feature-evidence.py --stage G4` | 0 | PASS |
| G5 signoff | `validate-feature-evidence.py --stage G5` | 0 | PASS |
| G6 candidate | `validate-feature-evidence.py --stage G6` | 0 | PASS |
| G6 trackers | `validate-trackers.py --feature F0025 --run-id 2026-07-07-9859bad4` | 0 | PASS |
| G7 KG symbol/decision regeneration | `.venv/bin/python scripts/kg/validate.py --regenerate-symbols --check-symbols --regenerate-decisions --check-decisions` | 0 | PASS |
| G7 KG drift | `.venv/bin/python scripts/kg/validate.py --check-drift` | 0 | PASS |
| G8 story index | `generate-story-index.py /Users/msig4/Documents/NEBULA/nebula-insurance-crm/planning-mds/features/` | 0 | PASS |
| G8 prior manifest patch | `patch-prior-manifest.py --feature F0025 --new-run-id 2026-07-07-9859bad4` | 0 | PASS; no prior approved manifests |
| G8 closeout evidence | `validate-feature-evidence.py --feature F0025 --stage closeout` | 1 then 0 | PASS after repairing archived artifact paths and PM acceptance identifiers |
| G8 tracker validation | `validate-trackers.py --feature F0025 --run-id 2026-07-07-9859bad4` | 1 then 0 | PASS after appending final accepted STATUS signoff rows |
| G8 KG coverage | `.venv/bin/python scripts/kg/validate.py --write-coverage-report` | 1 then 0 | PASS after updating stale F0025 PRD source path in `canonical-nodes.yaml` |
| G8 KG drift | `.venv/bin/python scripts/kg/validate.py --check-drift` | 0 | PASS |
| G8 template validation | `/Users/msig4/Documents/NEBULA/nebula-insurance-crm/.venv/bin/python agents/scripts/validate_templates.py` | 0 | PASS |

Final closeout validator results are mirrored in `commands.log` and `lifecycle-gates.log`.
