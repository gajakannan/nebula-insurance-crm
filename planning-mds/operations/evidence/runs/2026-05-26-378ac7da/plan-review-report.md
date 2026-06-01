# Plan Review Report - F0036

## Scope

- Plan scope: `feature`
- Target: `F0036`
- Feature: Form Engine and Form-State Preservation (RHF + AJV + Widget Registry)
- Feature path: `planning-mds/features/F0036-dynamic-product-attribute-form-engine`
- Run ID: `2026-05-26-378ac7da`
- Date: `2026-05-26`
- DIFF_RANGE: not provided
- Review question: Is this plan ready to build?

## Decision

`CONDITIONALLY READY`

No critical finding was identified in the current raw artifacts, but multiple high-severity readiness gaps remain. The feature has an implementable architectural direction in ADR-021, colocated stories, and enough source-context detail to support a vertical slice. It is not cleanly ready because implementation-facing artifacts still contradict that direction, Workstream B preservation scope is inconsistent across stories, planning/KG state is stale, and required validators are not all passing.

## Rationale

Current artifacts are stronger than the prior `2026-05-26-aaa8bd7c` NOT READY review: ADR-021 now defines a buildable two-layer validation contract, and F0036 no longer depends on ADR-023 for client-side rule execution. However, several handoff artifacts still instruct implementers to evaluate `rules.json` client-side or to include ADR-023 parity, while other sections say cross-field rules are backend-authoritative. The CRUD/RHF preservation scope also still has old six-form language in places that conflicts with the expanded approximately eleven-component sweep. These contradictions can lead to a materially wrong build or under-tested preservation claim.

## Next Action

Run targeted planning rework before `feature.md` implementation, or record explicit risk acceptance for each high finding. Minimum repair set: remove stale `rules.json`/ADR-023 client-evaluation language, reconcile Workstream B form inventory and preservation acceptance criteria, synchronize F0036 status/tracker/KG state, regenerate stale KG coverage, and rerun the failed validators.

## Findings by Severity

### Critical

None.

### High

#### PR-H1 - Validation parity contract is still contradictory across implementation handoff artifacts

Owner: Architect readiness, with Product Manager and Code Reviewer concurrence.

Evidence:

- ADR-021 now says the client validates only the structural `data-schema.json` layer, cross-field rules are backend-authoritative, and F0036 does not depend on ADR-023 (`planning-mds/architecture/decisions/ADR-021-form-engine-rhf-ajv-shadcn-registry.md:86`, `:88`, `:90`, `:91`, `:93`, `:95`, `:97`).
- S0003 acceptance criteria repeat the corrected backend-authoritative rule contract (`planning-mds/features/F0036-dynamic-product-attribute-form-engine/F0036-S0003-schema-driven-rendering-ajv-parity.md:43`, `:44`, `:46`, `:47`).
- The same S0003 story still lists "AJV (data-schema + rules.json) on the client" and "Cross-field rules.json evaluated client-side for parity" (`F0036-S0003-schema-driven-rendering-ajv-parity.md:60`, `:62`, `:70`, `:72`).
- README still says the ADR amendment records parity scope as "AJV + rules.json per ADR-022/023" (`planning-mds/features/F0036-dynamic-product-attribute-form-engine/README.md:58`).
- GETTING-STARTED still says invalid `rules.json` errors block submit, the engine performs `rules.json` validation, and parity includes `rules.json` (`planning-mds/features/F0036-dynamic-product-attribute-form-engine/GETTING-STARTED.md:34`, `:42`, `:58`).
- PRD Phase B Outcome still says client evaluation of `rules.json` is part of the parity scope even though the following lines remove ADR-023 and make backend validation authoritative (`planning-mds/features/F0036-dynamic-product-attribute-form-engine/PRD.md:231`, `:233`, `:234`, `:241`).
- Acceptance checklist still marks S0003 data validation as "data-schema + rules.json" while the later architecture section says rules are not client-evaluated (`planning-mds/features/F0036-dynamic-product-attribute-form-engine/acceptance-criteria-checklist.md:11`, `:61`).

Impact: An implementation agent can reasonably follow stale story/checklist/getting-started text and build an out-of-scope client `rules.json` evaluator, or write tests that assert the wrong parity boundary. That is a build-readiness issue, not a cosmetic doc mismatch.

Required repair: Make every implementation-facing artifact match ADR-021 section 3: client AJV over `data-schema.json`, parity against the actual backend for the structural layer, cross-field rules displayed from backend `lobErrors[]`, optional client pre-check only if explicitly scoped and parity-guarded, and no ADR-023 dependency.

#### PR-H2 - Workstream B preservation scope is inconsistent between the expanded inventory and S0008 acceptance coverage

Owner: Product Manager readiness and Code Reviewer buildability.

Evidence:

- PRD scope expands Workstream B to broker create/edit, account create/edit including account-contact edit, contact, task create/inline edit, submission create/edit native fields, policy create native fields, and renewal create (`planning-mds/features/F0036-dynamic-product-attribute-form-engine/PRD.md:59`, `:61`, `:66`).
- S0007 records the corrected exhaustive sweep and lists the expanded in-scope create/edit surfaces (`planning-mds/features/F0036-dynamic-product-attribute-form-engine/F0036-S0007-crud-rhf-migration-and-registration-helper.md:22`, `:24`, `:28`, `:29`, `:33`, `:36`, `:37`, `:38`, `:46`).
- S0008's "All migrated forms preserved" acceptance criterion still names only the older subset: broker create/edit, account create, contact create/edit, task create, and submission native fields (`planning-mds/features/F0036-dynamic-product-attribute-form-engine/F0036-S0008-crud-form-preservation-restore.md:31`, `:32`, `:34`).
- S0008 UI notes point back to the approximately eleven-component S0007 inventory, which conflicts with the narrower acceptance criterion (`F0036-S0008-crud-form-preservation-restore.md:108`, `:110`).
- S0007 still has a resolved-question block saying the inventory is exactly six surfaces and `CreatePolicyPage` is not a CRUD target (`F0036-S0007-crud-rhf-migration-and-registration-helper.md:142`, `:143`).
- STATUS signoff rationale still references "RHF migration of six hand-rolled forms" (`planning-mds/features/F0036-dynamic-product-attribute-form-engine/STATUS.md:70`, `:71`).
- GETTING-STARTED key files still list only the older CRUD set and omit the later-added edit/create surfaces (`planning-mds/features/F0036-dynamic-product-attribute-form-engine/GETTING-STARTED.md:46`).

Impact: The feature can claim "all migrated forms preserved" while tests and implementation only cover the older subset. That directly threatens the stated objective of fully closing F0035's zero-registered-forms gap.

Required repair: Make S0008 acceptance criteria, S0007 questions/assumptions, STATUS signoff rationale, GETTING-STARTED key files, and PRD/README summaries all reference the same source-of-truth Workstream B inventory, including any explicit operator deferrals.

#### PR-H3 - Plan state, tracker state, and KG state do not agree on F0036 readiness

Owner: Product Manager readiness, with Architect ownership for KG status.

Evidence:

- STATUS says the plan is complete and all five prior findings are resolved (`planning-mds/features/F0036-dynamic-product-attribute-form-engine/STATUS.md:3`, `:44`, `:45`, `:46`, `:47`, `:49`).
- README says the same rework left PR-M1 and PR-L1 open (`planning-mds/features/F0036-dynamic-product-attribute-form-engine/README.md:3`).
- REGISTRY still marks F0036 as "Phase A draft pending approval" (`planning-mds/features/REGISTRY.md:14`).
- STATUS tracker checklist still marks STORY-INDEX and BLUEPRINT unchecked (`planning-mds/features/F0036-dynamic-product-attribute-form-engine/STATUS.md:99`, `:103`, `:104`), while STORY-INDEX already contains S0001 through S0008 (`planning-mds/features/STORY-INDEX.md:274`, `:278`, `:285`).
- BLUEPRINT's feature inventory section lists nearby MVP/platform features but has no F0036 entry (`planning-mds/BLUEPRINT.md:164`, `:180`, `:183`, `:207`), matching the unchecked STATUS item to add F0036.
- KG feature mapping still marks F0036 `status: draft` and has Workstream B notes naming the old subset rather than the expanded inventory (`planning-mds/knowledge-graph/feature-mappings.yaml:827`, `:829`, `:851`, `:853`).

Impact: The build handoff cannot distinguish final resolved scope from draft or stale scope. This increases the chance that feature action starts against stale status, stale inventory, or missing blueprint context.

Required repair: Reconcile README, STATUS, REGISTRY, BLUEPRINT, STORY-INDEX checklist state, and KG feature mapping to one current readiness state. Raw product artifacts should remain authoritative, and any KG drift should be updated after the raw artifacts are corrected.

#### PR-H4 - Required KG validators fail, so KG readiness cannot be claimed

Owner: Architect readiness.

Evidence:

- `python3 /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/scripts/kg/validate.py` exited 1 with `coverage-report.yaml is stale` (`planning-mds/operations/evidence/runs/2026-05-26-378ac7da/artifacts/kg-validate.txt:5`, `:6`, `:21`, `:22`).
- `python3 /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/scripts/kg/validate.py --check-drift` exited 1 with the same stale coverage error and a Casbin drift warning for `(renewal, update)` (`planning-mds/operations/evidence/runs/2026-05-26-378ac7da/artifacts/kg-validate-drift.txt:5`, `:6`, `:18`, `:20`, `:22`, `:23`).

Impact: The plan-review contract requires KG validation evidence. With KG validation failing, this run cannot certify F0036 as fully KG-ready even though the F0036 node is discoverable.

Required repair: Regenerate or repair KG coverage, resolve or explicitly classify drift warnings, and rerun both KG validators to a clean result before claiming READY.

### Medium

#### PR-M1 - Required agent template validator fails outside F0036 scope

Owner: Framework/template owner, not the F0036 feature owner.

Evidence:

- `python3 agents/scripts/validate_templates.py` exited 1 (`planning-mds/operations/evidence/runs/2026-05-26-378ac7da/artifacts/validate-templates.txt:5`, `:6`).
- Errors include missing plan gates and commands in both plan templates, missing feature gates/commands in feature templates, and implementation ownership-boundary drift (`planning-mds/operations/evidence/runs/2026-05-26-378ac7da/artifacts/validate-templates.txt:11`, `:12`, `:13`, `:18`, `:24`, `:25`, `:27`, `:29`, `:30`).

Impact: This is not a F0036 product-rule gap, but it is a required validator failure and weakens confidence that the current framework templates match the active action contracts.

Required repair: Repair the agent framework templates and rerun `python3 agents/scripts/validate_templates.py`.

### Low

#### PR-L1 - Story validator warnings remain but do not block build readiness

Owner: Product Manager readiness.

Evidence:

- Story validation passed with warnings for S0001, S0002, S0003, S0007, and S0008 (`planning-mds/operations/evidence/runs/2026-05-26-378ac7da/artifacts/validate-stories.txt:5`, `:6`, `:12`, `:20`, `:29`, `:52`, `:60`).

Impact: The warnings do not overturn readiness, but S0003 and S0007 size warnings reinforce the need for exact handoff language because those stories carry broad build scope.

Required repair: Triage warnings during targeted rework; do not let warning cleanup replace the high-severity scope/contract fixes above.

## Product Readiness

Product readiness is conditional. The requirements are mostly specific and testable, and the feature has eight colocated stories with clear acceptance structures. However, product-owned handoff artifacts still disagree on plan state and preservation scope:

- State conflict: STATUS says all findings are resolved while README says two remain open, REGISTRY says Phase A draft, and BLUEPRINT lacks F0036 (`STATUS.md:3`, `README.md:3`, `REGISTRY.md:14`, `BLUEPRINT.md:164`, `:207`).
- Mutation/preservation contract conflict: the expanded approximately eleven-component Workstream B inventory is not consistently carried into S0008 acceptance criteria, STATUS signoff rationale, or GETTING-STARTED (`PRD.md:61`, `F0036-S0007-*.md:22`, `F0036-S0008-*.md:31`, `STATUS.md:71`, `GETTING-STARTED.md:46`).

Product Manager next action: reconcile tracker/status/readiness wording and make the Workstream B inventory a single explicit source of truth, including any deferrals.

## Architecture Readiness

Architecture readiness is conditional. ADR-021 now contains the missing build-critical validation decision from the prior review: client owns data-schema AJV, backend owns cross-field rules, and F0036 does not depend on ADR-023. That is enough architectural direction to implement once stale handoff text is removed.

Architecture blockers to READY:

- Implementation-facing artifacts still contradict ADR-021's validation split (`ADR-021:86-97`, `F0036-S0003-*.md:62`, `:72`, `GETTING-STARTED.md:58`, `PRD.md:231`).
- KG validation fails because `coverage-report.yaml` is stale (`kg-validate.txt:21`, `:22`).
- KG mapping still has stale F0036 state and stale Workstream B inventory notes (`feature-mappings.yaml:829`, `:853`).

Architect next action: align F0036 artifacts with ADR-021 section 3, update KG coverage/mapping, and rerun KG validators.

## Buildability Challenge

Buildability is conditional. The feature can be built as a vertical slice if the implementation starts from ADR-021 plus S0007's expanded inventory, but the current handoff still exposes two high-risk fork points:

- Validation fork: build only data-schema AJV plus backend `lobErrors[]`, or build an out-of-scope `rules.json` evaluator.
- Preservation fork: preserve the expanded approximately eleven-component CRUD inventory, or preserve the older six-form subset.

The implementation handoff should not rely on reviewer interpretation to choose the correct branch. Fix the artifacts before assigning implementation, or record explicit risk acceptance that the feature action will treat ADR-021 and S0007's expanded inventory as overriding stale text.

## Validation Evidence

| Command | CWD | Exit | Result | Evidence |
|---------|-----|------|--------|----------|
| `python3 agents/product-manager/scripts/validate-stories.py /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/planning-mds/features/F0036-dynamic-product-attribute-form-engine` | `/mnt/c/Users/gajap/sandbox/nebula/nebula-agents` | 0 | PASS with warnings | `artifacts/validate-stories.txt` |
| `python3 agents/product-manager/scripts/validate-trackers.py --product-root /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm` | `/mnt/c/Users/gajap/sandbox/nebula/nebula-agents` | 0 | PASS | `artifacts/validate-trackers.txt` |
| `python3 /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/scripts/kg/validate.py` | `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm` | 1 | FAIL: stale coverage report | `artifacts/kg-validate.txt` |
| `python3 /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/scripts/kg/validate.py --check-drift` | `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm` | 1 | FAIL: stale coverage report; drift warning | `artifacts/kg-validate-drift.txt` |
| `python3 agents/scripts/validate_templates.py` | `/mnt/c/Users/gajap/sandbox/nebula/nebula-agents` | 1 | FAIL: framework template drift | `artifacts/validate-templates.txt` |

Every validator command is also recorded in `commands.log`.

## Artifact Trace

Primary product artifacts read:

- `planning-mds/features/F0036-dynamic-product-attribute-form-engine/**`
- `planning-mds/features/REGISTRY.md`
- `planning-mds/features/ROADMAP.md`
- `planning-mds/features/STORY-INDEX.md`
- `planning-mds/BLUEPRINT.md`
- `planning-mds/architecture/decisions/ADR-020-lob-extensible-attributes.md`
- `planning-mds/architecture/decisions/ADR-021-form-engine-rhf-ajv-shadcn-registry.md`
- `planning-mds/architecture/decisions/ADR-022-lob-validation-parity.md`
- `planning-mds/architecture/decisions/ADR-024-session-continuity-token-refresh.md`
- `planning-mds/knowledge-graph/feature-mappings.yaml`
- `planning-mds/knowledge-graph/canonical-nodes.yaml`
- `planning-mds/knowledge-graph/code-index.yaml`
- `planning-mds/knowledge-graph/coverage-report.yaml`
- `planning-mds/examples/personas/nebula-personas.md`
- `experience/src/**` searched directly for Workstream B mutation-form surfaces.

Routing aid used:

- `python3 /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm/scripts/kg/lookup.py F0036`

Evidence produced:

- `planning-mds/operations/evidence/runs/2026-05-26-378ac7da/README.md`
- `planning-mds/operations/evidence/runs/2026-05-26-378ac7da/action-context.md`
- `planning-mds/operations/evidence/runs/2026-05-26-378ac7da/artifact-trace.md`
- `planning-mds/operations/evidence/runs/2026-05-26-378ac7da/gate-decisions.md`
- `planning-mds/operations/evidence/runs/2026-05-26-378ac7da/commands.log`
- `planning-mds/operations/evidence/runs/2026-05-26-378ac7da/lifecycle-gates.log`
- `planning-mds/operations/evidence/runs/2026-05-26-378ac7da/plan-review-report.md`
- `planning-mds/operations/evidence/runs/2026-05-26-378ac7da/artifacts/*.txt`

## Self-Review

- Findings cite concrete raw artifacts and line-specific evidence.
- Severity reflects build-readiness impact: no critical gap remains because ADR-021 now contains an implementable decision, but stale contradictory handoff text and validator failures are high enough to block READY.
- Required validators were run and recorded; failures are captured in `artifacts/` and `commands.log`.
- No product, feature, story, tracker, schema, KG, ADR, or architecture artifact was edited by this plan-review action.
