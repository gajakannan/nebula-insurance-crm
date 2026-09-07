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

## Framework Execution Boundary

The framework is a separate checkout. In a local workspace, commands commonly
use `../nebula-agents` because the product and framework repositories are
sibling directories. The scripts resolve their own framework root from
`__file__`; the product is supplied explicitly through `--product-root`:

```sh
PRODUCT_ROOT="$PWD"
FRAMEWORK_ROOT="$(cd ../nebula-agents && pwd)"
python3 "$FRAMEWORK_ROOT/agents/product-manager/scripts/validate-feature-evidence.py" \
  --product-root "$PRODUCT_ROOT" --json
```

The product CI workflow does not assume that sibling directory. Its
`evidence-durability` job runs the repository-local
`scripts/validate-evidence-package.py` guard. A CI job that needs the full
framework validator must check out `nebula-agents` at a pinned revision and
invoke it with an absolute path plus `--product-root "$GITHUB_WORKSPACE"`.
Historical `commands.log` entries containing `../nebula-agents` are preserved
provenance from their original workspace; they are not rewritten to make old
runs executable in a new workspace.

### Product-owned checks from the framework runner

Recent `nebula-agents` revisions support an optional product manifest at
`.nebula-project.yaml`. When a product opts in, the framework loads the
declared instructions and runs declared local Python checks with the same
product root used by the action:

```sh
PRODUCT_ROOT="$PWD"
FRAMEWORK_ROOT="$(cd ../nebula-agents && pwd)"
python3 "$FRAMEWORK_ROOT/agents/scripts/project_context.py" \
  --product-root "$PRODUCT_ROOT" --action plan-review
python3 "$FRAMEWORK_ROOT/agents/scripts/run-gate.py" \
  --product-root "$PRODUCT_ROOT" --action plan-review \
  --plan-scope feature --target F#### \
  --run-id YYYY-MM-DD-xxxxxxxx --stage PR2
```

The runner executes a declared `argv` array in the product directory, expands
`{PRODUCT_ROOT}` to an absolute path, sets `NEBULA_PRODUCT_ROOT`, and records
stdout, stderr, result metadata, hashes, and command-log entries under the
base run's `artifacts/project-checks/` directory. Checks must emit the strict
JSON result contract documented by `nebula-agents/agents/docs/PROJECT-EXTENSIONS.md`.
They are additive: framework validators and reviewer approvals still run.

The current framework action specification exposes product checks only at
`plan-review:PR2:before_stage_complete`. CRM now uses that point for a
repository-wide snapshot of the evidence concession guard through
`scripts/validation/validate_evidence_durability.py`. This is an early
structural guard; it does not replace feature closeout validation. The
repository CI job still runs `scripts/validate-evidence-package.py --base`
against changed runs. If a future framework revision adds a feature-closeout
point, wire the same guard there in the same change as the pinned framework
revision and continue using `run-gate.py` as the executor.

## Future Contract and Retention Controls

New feature runs must be initialized with `agents/scripts/init-run.py`. The
initializer stamps `contract_version` and `contract_effective_date` into the
manifest; do not hand-create or copy a manifest. The action context should also
retain the framework revision used for the run so a later validator can select
the matching immutable policy bundle under `agents/actions/spec/history/`.

Before G8 closeout, the feature action must pass the stage validators, record
the tracker-sync result in `lifecycle-gates.log`, and keep every raw output
under the run's durable `artifacts/` tree. `/tmp`, home-directory paths, `.env`
files, and unresolved artifact references are invalid. PM closeout must not
promote `latest-run.json` until the final validator exits zero.

The repository CI job `evidence-durability` runs
`scripts/validate-evidence-package.py` for changed runs. It checks contract
identity, append-only log shape, durable artifact paths, and referenced-file
existence without reapplying current rules to the grandfathered historical
boundary. The full framework validator remains required at feature gates.

CI or the agent runner must retain the complete canonical run directory,
including raw outputs, with a retention period independent of the source
checkout. If a run is reopened, its new manifest must reference the prior run
with `rerun_of`; the prior package is never overwritten.

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
