# Feature Review Report

Feature: F0036-dynamic-product-attribute-form-engine
Feature Run ID: 2026-05-28-077b7b30
Review Run ID: 2026-05-30-6c8cd3ee
Date: 2026-05-30
Mode: closeout-audit
Review Question: Is this feature truly done?

## Decision
- Status: NOT DONE
- Rationale: Required closeout evidence validation fails on the published feature run. The review also found a concrete implementation gap in the account-contact edit preservation path, which contradicts the feature's core claim that all in-scope mutation forms survive forced re-auth.
- Next Action: Repair via owning-role rework, re-run feature closeout evidence validation to green, then rerun feature-review.

## Findings By Severity

### Critical
- [critical] Required closeout evidence validation fails for the approved feature run. Location: `planning-mds/operations/evidence/runs/2026-05-28-077b7b30/gate-decisions.md`; Evidence: `artifacts/validate-feature-evidence-closeout.log`; Impact: FR4 requires `NOT DONE` when required evidence validation fails. The validator reports missing closeout-required gate rows `G5`, `G6`, and `G8`, so the run cannot be treated as a complete terminal evidence package. Owner: Product Manager / feature-action evidence owner; Recommendation: reconcile gate numbering/schema or regenerate `gate-decisions.md` through the owning feature action, then rerun `validate-feature-evidence.py --stage closeout`.
- [critical] Account-contact edit snapshots cannot restore after a forced re-auth from a fresh page load. Location: `experience/src/pages/AccountDetailPage.tsx:91`, `experience/src/pages/AccountDetailPage.tsx:140`, `experience/src/features/forms/useRegisteredForm.ts:72`; Evidence: S0007 lists account-contact edit as in scope and S0008 requires all wired forms to restore, but `AccountDetailPage` initializes `editingContact` as `null`, registers `formKey=account-contact:<accountId>:new`, and `useRegisteredForm` consumes only the current registration key. A snapshot taken while editing an existing contact uses `account-contact:<accountId>:<contactId>`, so return from re-auth cannot identify/open that edit form. Impact: F0035 finding #1 is not fully closed for the account-contact edit surface. Owner: Frontend / Code Reviewer; Recommendation: persist enough modal/form identity in the route or snapshot metadata to re-open the edited contact and consume the matching form key, then add a restore test for AccountDetail account-contact edit.

### High
- [high] QE evidence does not prove all completion claims. Location: `planning-mds/operations/evidence/runs/2026-05-28-077b7b30/test-execution-report.md:50`; Evidence: `artifacts/test-results/g2-full-suite.log:3480` shows 5 failed tests and `COVERAGE_EXIT=1`; `artifacts/test-results/g2-integration-lane.log:710` shows 2 failed integration tests. The report classifies remaining reds as pre-existing, but the same report skips per-screen forced-re-auth Playwright E2E and live-backend AJV parity. Impact: acceptance evidence is incomplete for the exact forced-auth and live parity risks F0036 was created to close. Owner: Quality Engineer; Recommendation: either supply green feature-scoped runtime evidence for the required layers or record explicit owner/mitigation/target-date acceptance for each skipped layer.
- [high] Closeout state is inconsistent across feature documents. Location: `planning-mds/features/archive/F0036-dynamic-product-attribute-form-engine/README.md:3`, `planning-mds/features/archive/F0036-dynamic-product-attribute-form-engine/PRD.md:13`, `planning-mds/features/archive/F0036-dynamic-product-attribute-form-engine/feature-assembly-plan.md:5`; Evidence: `STATUS.md` says Done - Archived, but README and PRD still say plan complete/pending re-confirmation and the assembly plan still says Draft. Impact: Product Manager completion review cannot rely on the feature folder itself to prove terminal state. Owner: Product Manager / Architect for assembly-plan metadata; Recommendation: reconcile terminal status fields through the owning lifecycle action and rerun tracker/evidence validators.

### Medium
- [medium] Feature evidence README drifted from the manifest. Location: `planning-mds/operations/evidence/runs/2026-05-28-077b7b30/README.md:7`; Evidence: README says final state is `draft` and must agree with `evidence-manifest.json`, while the manifest and latest-run.json say `approved`. Impact: evidence readers get contradictory run-state guidance even before opening raw reports. Owner: Product Manager / feature-action evidence owner; Recommendation: regenerate the evidence README from the final approved state.

### Low
- [low] Account forms snapshot `taxId`/PII without `sensitiveFieldPaths`. Location: `experience/src/pages/CreateAccountPage.tsx:49`, `experience/src/pages/AccountDetailPage.tsx:127`; Evidence: `security-review-report.md` records the same low residual. Impact: within ADR-024 boundary but avoidable sensitive browser-local retention. Owner: Frontend / Security; Recommendation: pass `sensitiveFieldPaths: ['taxId']` at least for account create/edit trackers.
- [low] New dependency scan was deferred. Location: `security-review-report.md` Scan Disposition; Evidence: four new deps were added but dependency scan did not run because `security_sensitive_scope=false`. Impact: low package hygiene gap. Owner: DevOps / Security; Recommendation: run the repo standard dependency scan before release.

## Completion Checks
- Requirements satisfaction: FAIL. Core all-forms restore claim is contradicted by the account-contact edit path.
- Architecture and KG alignment: FAIL. Closeout evidence validation fails, and assembly-plan status remains Draft despite archive state.
- Code quality: FAIL. A changed source path leaves an in-scope restore scenario unreachable after re-auth return.
- Security: PASS WITH LOW RESIDUALS. No critical/high security issue found, but `taxId` exclusion and dependency scan remain open.
- Test evidence: FAIL. Raw test artifacts include failing commands and skipped high-value forced-auth/live-parity layers.
- Deployability: PASS WITH RESERVATION. No deploy config changed; build evidence is green, but dependency scan is deferred.
- Signoff and closeout: FAIL. Required closeout evidence validator fails and feature docs disagree on terminal state.
- Tracker sync: NOT RE-RUN AFTER STOP. Closeout evidence claims tracker pass; this review stopped at the required evidence validation failure.

## Validation Evidence
- `python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm --feature F0036 --stage closeout`: FAIL - closeout evidence validator reports missing `G5`, `G6`, and `G8` rows. Output captured at `artifacts/validate-feature-evidence-closeout.log`.
- `python3 agents/product-manager/scripts/validate-trackers.py`: SKIPPED - stop condition after required closeout evidence validation failed.
- `python3 agents/product-manager/scripts/generate-story-index.py /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/planning-mds/features/`: SKIPPED - command mutates `STORY-INDEX.md` and the review action is read-only outside the review folder; stop condition already reached.
- `python3 /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/scripts/kg/validate.py --check-symbols`: SKIPPED - stop condition after required closeout evidence validation failed.
- `python3 /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/scripts/kg/validate.py --check-drift`: SKIPPED - stop condition after required closeout evidence validation failed.
- `python3 agents/scripts/validate_templates.py`: SKIPPED - stop condition after required closeout evidence validation failed.

## Artifact Trace
- Feature path: `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/planning-mds/features/archive/F0036-dynamic-product-attribute-form-engine`
- Feature evidence run: `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/planning-mds/operations/evidence/runs/2026-05-28-077b7b30`
- latest-run.json: `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/planning-mds/operations/evidence/features/F0036-dynamic-product-attribute-form-engine/latest-run.json` resolves `run_id=2026-05-28-077b7b30`, `status=approved`.
- Changed-file set: `planning-mds/operations/evidence/runs/2026-05-28-077b7b30/artifacts/diffs/changed-files.txt` (80 paths).
- Runtime evidence: `g1-runtime-preflight.md`, `test-execution-report.md`, `coverage-report.md`, `deployability-check.md`, raw `artifacts/test-results/*.log`.
- Review artifacts: `action-context.md`, `commands.log`, `artifacts/validate-feature-evidence-closeout.log`, this report.

## Self-Review Gate
- Findings cite exact source lines, report lines, or evidence paths.
- Severity reflects done impact: required evidence validator failure and core AC violation are critical.
- No implementation, feature docs, trackers, KG files, or feature evidence artifacts were edited by this review.
- Skipped validators are justified by the stop condition and read-only constraint.

```json
{
  "gate": "feature_review",
  "question": "is_this_feature_truly_done",
  "status": "not_done",
  "mode": "closeout_audit",
  "findings": {"critical": 2, "high": 2, "medium": 1, "low": 2},
  "evidence_validation": "fail",
  "can_merge_or_release": false,
  "requires_risk_acceptance": false,
  "available_actions": ["fix_findings", "cancel"]
}
```
