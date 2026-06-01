# Operations Evidence

This directory stores evidence packages produced by `nebula-agents` action runs against this product repo.

Effective `2026-05-19` (the Feature Evidence Package Standardization contract — see `nebula-agents/_private-plans/feature-evidence-package-standardization-plan-v2.md` and the framework `CONSUMER-CONTRACT.md`).

## Two Evidence Profiles

### Base run (§8) — non-feature / manual / validate-action runs

Path:

```text
planning-mds/operations/evidence/runs/{run-id}/
  README.md
  action-context.md
  artifact-trace.md
  gate-decisions.md
  commands.log
  lifecycle-gates.log
```

Used by `agents/actions/validate.md` and other operator-initiated runs. The validate-action additionally produces:

- `pm-validation-report.md`
- `architect-validation-report.md`
- `implementation-validation-report.md`

These live alongside the base files in the run folder. They do **not** require an `evidence-manifest.json`.

### Feature completion (§9, §10) — `feature.md` / `build.md` closeout

Path:

```text
planning-mds/operations/evidence/features/F####-{slug}/
  latest-run.json                       # only after PM closeout + supersession patch

planning-mds/operations/evidence/runs/{run-id}/
  <§8 base files>
  evidence-manifest.json                # §11 schema v1
  feature-action-execution.md
  g0-assembly-plan-validation.md
  g1-runtime-preflight.md               # when runtime_bearing = true
  g2-self-review.md
  test-plan.md
  test-execution-report.md
  coverage-report.md
  deployability-check.md
  code-review-report.md
  security-review-report.md             # when security_sensitive_scope or required
  signoff-ledger.md
  pm-closeout.md
  artifacts/{coverage,diffs,test-results,security,screenshots}/
```

Run ID format: `YYYY-MM-DD-XXXXXXXX` (`secrets.token_hex(4)` style 8-char suffix). Templates for each artifact live under `nebula-agents/agents/templates/`.

## Effective-Date Boundary (§6)

- Archived completed features with `Archived Date < 2026-05-19` are **skipped** by feature-evidence validation. They count as `features_skipped_pre_contract_archived`.
- Archived features with `Archived Date >= 2026-05-19` or an `Evidence Reentry Date >= 2026-05-19` require the canonical evidence package.
- Active terminal (`Done`/`Completed`/`Archived`) features whose `STATUS.md` `Closeout review date` is on or after `2026-05-19` require the canonical package.
- Retired features (`Terminal Status = Abandoned` or `Superseded`) are registry-only and never satisfy completion-evidence requirements.

## Path Class Extensions (§7)

The framework default path classes (in `nebula-agents/agents/product-manager/scripts/validate-feature-evidence.py` `DEFAULT_PATH_CLASSES`) cover `engine/**` and `experience/**`. This product extends them with `neuron/**`:

| Path class (glob) | Forces |
|-------------------|--------|
| `neuron/**` excluding migrations and test-only subtrees | `runtime_bearing = true` |
| `neuron/**/Migrations/**` | `runtime_bearing = true` and `deployment_config_changed = true` |

`neuron/` hosts AI runtime services (RAG, retrieval, prompt orchestration) that this product treats as runtime-bearing. Changes that touch `neuron/` source must force `runtime_bearing = true` in the manifest and produce `g1-runtime-preflight.md`.

The product extension is additive — framework defaults are not overridden. The validator's `path_class_extension_conflict_fails` rule enforces this; the broad-scan run below confirms no conflict.

## Frontend Global Lanes (§20)

- `planning-mds/operations/evidence/frontend-quality/` remains the global frontend quality lane. The lifecycle gate consumes its `latest-run.json` (§12 schema, with `feature_id` omitted).
- `planning-mds/operations/evidence/frontend-ux/` remains the rolling UX audit lane. Audit files use the `ux-audit-YYYY-MM-DD.md` naming convention.

Both lanes may be referenced from a feature evidence package via `manifest.global_evidence_refs`. They do not substitute for feature-level role reports.

## Validators

Run from the framework repo (`nebula-agents`):

```text
python3 agents/product-manager/scripts/validate-trackers.py --product-root /path/to/nebula-insurance-crm
python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root /path/to/nebula-insurance-crm --json
```

The first invocation also calls feature-evidence during tracker integration per §22. Closeout (`--stage closeout`) is run by the closeout action after tracker results are appended to `lifecycle-gates.log`.

## Phase 4 Baseline Acceptance (§27)

As of 2026-05-22 normalization:

| Item | Count | Validator behavior |
|------|------:|--------------------|
| Archived completed features with `Archived Date < 2026-05-19` | 17 | skipped (`features_skipped_pre_contract_archived`) |
| Archived completed features with `Archived Date >= 2026-05-19` | 0 | n/a |
| Retired superseded features (F0010, F0011 → F0013) | 2 | skipped (`features_skipped_retired_superseded`) |
| Retired abandoned features | 0 | n/a |
| Active Done/completed terminal features | 0 | n/a |

This table is the Phase 4 acceptance oracle. Update it before re-running validators if the registry changes.

## Legacy Evidence Folders

The directories `f0004/`, `f0006/`, `f0007/`, `f0013/`, `f0015/`, `f0018/`, `F0020/`, `F0034/`, and `plan-2026-02-08-preview-walkthrough/` predate the run-id consolidation contract. They are not validated against §10. Their evidence remains accessible for audit, and new validators ignore these root-level legacy folders instead of applying compatibility path rules.
