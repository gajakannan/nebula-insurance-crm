# Plan Run — F0036 Form Engine and Form-State Preservation — 2026-05-25-51ff2a92

> Base run evidence package per `feature-evidence-package-standardization-plan-v2.md` §8.
> Plan action runs BEFORE the feature evidence package exists — this folder is the non-feature base run path. No feature completion run package under `planning-mds/operations/evidence/runs/{RUN_ID}/` is created during plan; `feature.md` creates that run package later.

## Run Summary

- Action: `plan` (Phase A+B)
- Feature ID: F0036 (Form Engine and Form-State Preservation — RHF + AJV + Widget Registry)
- Phase: A (PM requirements) + B (Architect architecture), sequential
- Declared `FEATURE_MODE`: `existing` (folder + `PRD.md` + `STATUS.md` skeleton present — matches contract definition; no override)
- PLAN_RUN_ID: `2026-05-25-51ff2a92`
- PRODUCT_ROOT (absolute): `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm`
- Run start: 2026-05-25

## Status

- **COMPLETE (sealed 2026-05-25T20:07:00-04:00)** — All plan-action gates resolved; both user approvals granted (A1, B2); all four exit validators exit 0. F0036 planning (Phase A requirements + Phase B architecture) is ready for the feature action.

### Gate Summary

| Gate | Decision |
|------|----------|
| PRE-FLIGHT | PASS |
| INPUT-RECONCILIATION | PASS |
| A0 PM REQUIREMENTS DRAFT | PASS |
| G2 TRACKER SYNC (A) | PASS |
| A1 PM APPROVAL GATE | APPROVED |
| B0 ARCHITECT ARCHITECTURE | PASS |
| B1 ONTOLOGY SYNC GATE | PASS (after one repair cycle) |
| B2 ARCHITECT APPROVAL GATE | APPROVED |
| EXIT-VALIDATION | PASS |

## Evidence Index

Planning artifacts produced/updated by this run (all paths relative to `{PRODUCT_ROOT}`):

**Phase A (PM)** — `planning-mds/features/F0036-dynamic-product-attribute-form-engine/`:
- `PRD.md` — enriched (Phase A Clarification Resolution; finalized story map; Phase B Outcome added at B0)
- `STATUS.md` — Planning Checklist updated; Required Role Matrix (Architect-confirmed at B0); empty append-only Story Provenance table
- `acceptance-criteria-checklist.md` — created (per-story coverage matrix + 8 checklist dimensions)
- `F0036-S0001-engine-skeleton-and-dependencies.md`
- `F0036-S0002-mvp-widget-vocabulary.md`
- `F0036-S0003-schema-driven-rendering-ajv-parity.md`
- `F0036-S0004-pin-during-edit.md`
- `F0036-S0005-replace-cyber-panel-five-screen-regression.md`
- `F0036-S0006-product-attribute-form-preservation.md`
- `F0036-S0007-crud-rhf-migration-and-registration-helper.md`
- `F0036-S0008-crud-form-preservation-restore.md`

**Phase B (Architect)**:
- `planning-mds/architecture/decisions/ADR-021-form-engine-rhf-ajv-shadcn-registry.md` — amended (reconciliation section §1–§6); **no separate companion ADR** (decision recorded)
- `planning-mds/knowledge-graph/feature-mappings.yaml` — `feature:F0036` moved from `excluded_features` to `features[]` with full bindings
- `planning-mds/knowledge-graph/coverage-report.yaml` — refreshed (`--write-coverage-report`)
- `planning-mds/knowledge-graph/canonical-nodes.yaml` — **unchanged** (no new shared semantics; F0036 reuses existing nodes)
- `planning-mds/features/F0036-.../README.md`, `GETTING-STARTED.md` — Architecture section updated / skeleton created

**Trackers**:
- `planning-mds/features/STORY-INDEX.md` — regenerated (133 story files)
- `planning-mds/features/REGISTRY.md`, `ROADMAP.md` — already reflect F0036 (title broadened 2026-05-25); no change needed this run
- `planning-mds/BLUEPRINT.md` — F0036 addition under Platform Foundation deferred to feature-action closeout per F0036 STATUS tracker checklist (not required for plan closeout)

## Validation Summary

Recorded in `lifecycle-gates.log` and `gate-decisions.md`. Exit-validation suite (per action contract) at run close:

1. `python3 agents/product-manager/scripts/validate-trackers.py`
2. `python3 agents/product-manager/scripts/generate-story-index.py {PRODUCT_ROOT}/planning-mds/features/`
3. `python3 {PRODUCT_ROOT}/scripts/kg/validate.py --check-drift`
4. `python3 agents/scripts/validate_templates.py`

`validate-feature-evidence.py` is intentionally NOT called at plan (no feature evidence package exists yet).

## Open Follow-ups

- **For the feature action (build):** four design items are recorded in the amended ADR-021 and PRD as deferred-to-build, not unresolved planning gaps — (1) the data-schema→widget derivation implementation, (2) client `rules.json`/JsonLogic evaluation for parity, (3) the conditional-gating convention (MFA maturity), (4) the bundle-level conditional vocabulary as a future ADR candidate.
- **BLUEPRINT.md** F0036 line under Platform Foundation: deferred to feature-action closeout per F0036 STATUS tracker checklist (not required for plan closeout).
- **(Carried forward, pre-existing — predate F0036, out of scope):** low-confidence inferred edge (0.4) on `feature:F0028` in `feature:F0018.depends_on`; Casbin policy pair `(renewal, update)` in `policy.csv` has no `policy_rule` node in `canonical-nodes.yaml`.
- **Lesson logged:** when editing `feature-mappings.yaml`, an insertion before a top-level section key must preserve that key — the first B1 attempt dropped the `stories:` header (caught and repaired within the allowed single cycle).
