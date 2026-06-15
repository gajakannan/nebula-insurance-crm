# Artifact Trace — F0017-broker-mga-hierarchy-and-producer-ownership run 2026-06-06-5fb353e9

> Captures what was read, written, generated, referenced externally, and explicitly omitted/waived for this base run.

## Artifacts Read

Framework (nebula-agents):
- `agents/ROUTER.md`
- `agents/agent-map.yaml`
- `agents/docs/AGENT-USE.md`
- `agents/actions/plan.md`
- `agents/docs/AGENT-OPS.md` (base run §8 layout + commands.log §13 schema — live successor to the standardization plan doc)
- `agents/templates/{commands-log,artifact-trace,gate-decisions,lifecycle-gates-log,story}-template.md`

Product (nebula-insurance-crm):
- `planning-mds/features/F0017-broker-mga-hierarchy-and-producer-ownership/{PRD.md,STATUS.md,README.md,GETTING-STARTED.md}`
- `planning-mds/features/REGISTRY.md` (F0017 row), `planning-mds/features/ROADMAP.md` (F0017 placement)
- `.agentignore` (presence confirmed; honored for broad discovery)

## Artifacts Created Or Updated

Base run files (this folder) — created:
- `README.md`, `action-context.md`, `artifact-trace.md`, `gate-decisions.md`, `commands.log` (empty init), `lifecycle-gates.log` (empty init)

Planning artifacts — Phase A (PM) created/updated:
- `planning-mds/features/F0017-broker-mga-hierarchy-and-producer-ownership/PRD.md` — refined: MVP boundary, resolved-clarifications block, ASCII screen layouts, real story list
- `.../F0017-S0001-model-broker-mga-hierarchy.md` — created
- `.../F0017-S0002-navigate-hierarchy.md` — created
- `.../F0017-S0003-producer-ownership-effective-dated.md` — created
- `.../F0017-S0004-territory-management-effective-dated.md` — created
- `.../F0017-S0005-hierarchy-ownership-territory-audit.md` — created
- `.../STATUS.md` — Story Checklist + signoff recommendation updated (provenance rows untouched/append-only)
- `.../README.md` — stories table updated (5 stories)
- `planning-mds/knowledge-graph/feature-mappings.yaml` — F0017 moved from `excluded_features` to a minimal PM `features:` stub (id/path/status/depends_on + note); canonical bindings deferred to Phase B
- `planning-mds/knowledge-graph/coverage-report.yaml` — regenerated (generated artifact) to reflect F0017 now mapped
- `planning-mds/features/STORY-INDEX.md` — regenerated (146 stories)

Planning artifacts — Phase A re-scope iteration (operator request at G3):
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/{PRD,README,STATUS,GETTING-STARTED}.md` — created (placeholder homing F0017 deferred scope)
- `planning-mds/features/ROADMAP.md` — F0017 moved Later→Now; F0037 added to Later; Last Reviewed + Notes updated
- `planning-mds/features/REGISTRY.md` — next ID F0037→F0038; F0037 added to Planned
- `planning-mds/knowledge-graph/feature-mappings.yaml` — F0037 added to `excluded_features`; `coverage-report.yaml` regenerated

Planning artifacts — Phase B (Architect) created/updated (after G3 approval):
- `planning-mds/architecture/decisions/ADR-026-broker-mga-hierarchy-producer-ownership-and-territory.md` — created (governing ADR; Status: Proposed pending G5)
- `planning-mds/architecture/data-model.md` — added §9 (F0017 entities, invariants, migration order) + Version 5.0 note
- `planning-mds/knowledge-graph/canonical-nodes.yaml` — added entities (producer, territory, producer-ownership, territory-assignment), capabilities (distribution-hierarchy-management, producer-ownership-management, territory-management), adr:026
- `planning-mds/knowledge-graph/feature-mappings.yaml` — F0017 stub upgraded to full bindings (affects/governed_by/depends_on); 5 F0017 story mappings added
- `planning-mds/knowledge-graph/coverage-report.yaml` — regenerated (G4)
- `.../F0017-.../README.md` — added Architecture section + ERD (Mermaid + ASCII)
- `.../F0017-.../STATUS.md` — signoff matrix finalized by Architect (QE/CR/Architect = Yes; Security/DevOps = No, with rationale)

> Reconciliation (per plan.md Deliverables Contract): `feature-assembly-plan.md` is **not** a plan deliverable — it belongs to `agents/actions/feature.md` Step 0, so it was intentionally not created here (the operator-friendly prompt lists it under Phase B outputs; the authoritative action doc overrides). Full OpenAPI (`nebula-api.yaml`) and JSON schemas are likewise authored during the feature action; Phase B captured the API surface at design level in ADR-026 §7. BLUEPRINT Section 4 not edited — no baseline-architecture change beyond the feature-scoped ADR; revisit if a shared pattern emerges.

> Note: the initial Phase A draft required no F0017 tracker-row edits (REGISTRY/ROADMAP already carried it). The operator-requested re-scope iteration then edited ROADMAP (F0017→Now, +F0037) and REGISTRY (+F0037), both re-validated. BLUEPRINT Section 3 review deferred — no F0017 gaps surfaced by validation; revisit in Phase B if architecture adds detail.

## Generated Evidence

- Baseline `scripts/kg/validate.py` output (PASS, exit 0) — recorded in `README.md` → Validation Summary and `commands.log`.
- Exit-validation tool output — _pending at run close_.

## External Or Global Evidence References

None for this base run. (No frontend-quality / frontend-ux global lane dependencies at planning time.)

## Omissions And Waivers

- No feature evidence package, `evidence-manifest.json`, `latest-run.json`, or role reports — **correct for a base run** (`plan` action); these belong to the later `feature.md` run, not an omission requiring a waiver.
- No coverage/test/security reports — not applicable to planning.

## Run Environment (conditional)

`commands.log` uses repo-relative / `{PRODUCT_ROOT}`-relative `cwd` values, so no
absolute-path justification is required. Commands were executed with the shell
working directory at the product root (`/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm`).
