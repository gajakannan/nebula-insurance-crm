# Prompt Context Optimization Report

Date: 2026-06-08

## Summary

Added a product-local prompt-loading strategy for `nebula-insurance-crm` that
routes agent context by target feature, KG output, changed paths, exact
contracts, and evidence manifests. The change is documentation/tooling only and
does not alter runtime behavior, product logic, authorization, validation,
schemas, evidence retention, or public interfaces.

## Files Changed

- `.agentignore`
- `planning-mds/context-map.yaml`
- `planning-mds/CONTEXT-LOADING.md`
- `planning-mds/CONTEXT-OPTIMIZATION-REPORT.md`
- `scripts/validate-context-map.py`
- `scripts/measure-context-surfaces.py`
- `scripts/tests/test_validate_context_map.py`

## High-Token Surfaces Before Optimization

These surfaces were identified as high-token or history-heavy context risks:

| Surface | Why Expensive | New Default |
|---|---|---|
| `planning-mds/features/**` | Many feature folders, stories, status files, historical implementation notes | Load registry/roadmap plus target feature only |
| `planning-mds/features/archive/**` | Historical feature records not needed for normal runs | On-demand only |
| `planning-mds/operations/evidence/**` | Old run manifests, raw reports, logs, screenshots, generated artifacts | Manifest-first exact-file access |
| `planning-mds/architecture/**` | Large cross-cutting architecture corpus | Exact topic/file routing |
| `planning-mds/api/**` | Full OpenAPI specs can be large and duplicated by feature docs | Exact contract routing |
| `planning-mds/lob-schemas/**` | Large JSON schema/rules/UI schema payloads | Exact schema routing |
| `planning-mds/knowledge-graph/**` | Raw graph files can duplicate KG command output | KG command output first |
| `engine/**` | Full backend source and test tree | Changed path or KG routing |
| `experience/**` | Full frontend source, tests, build outputs, visual outputs | Changed path or KG routing |
| `neuron/**` | AI/agent source unrelated to ordinary backend/frontend runs | Changed path, KG, or AI scope |
| tests, screenshots, logs, generated artifacts | Verbose or binary/visual data unsuitable for default prompt context | On-demand only |

Measured with `python3 scripts/measure-context-surfaces.py --format markdown` from the product root:

| Surface | Files | Bytes | Est. Tokens | Binary/Visual/Log Files |
|---|---:|---:|---:|---:|
| features | 322 | 2425826 | 606456 | 0 |
| feature_archive | 257 | 2223831 | 555957 | 0 |
| evidence | 197 | 1805968 | 451492 | 48 |
| architecture | 43 | 644374 | 161093 | 0 |
| api | 1 | 232107 | 58026 | 0 |
| lob_schemas | 5 | 4130 | 1032 | 0 |
| knowledge_graph | 9 | 2296210 | 574052 | 0 |
| backend | 407 | 3473181 | 868295 | 0 |
| frontend | 33915 | 452784359 | 113196089 | 13 |
| neuron | 22 | 8740 | 2185 | 0 |
| tests | 56 | 952190 | 238047 | 0 |
| visual_generated_logs | 197 | 1805968 | 451492 | 48 |

## Estimated Token Impact

Because this change is a prompt-loading policy, exact savings depend on the
agent run and target feature. Use `bytes / 4` as a rough token estimate.

Typical before/after estimate:

| Run Type | Before Pattern | After Pattern | Expected Reduction |
|---|---|---|---|
| Feature implementation | Registry + broad features + broad source tree + contracts | Registry/roadmap + target feature + KG hint + exact source/contracts | 60-85% |
| Code review | Broad feature docs + broad backend/frontend tree + evidence | Changed files + target feature status + exact contracts + selected evidence manifest | 65-90% |
| Validation | Full evidence history + logs/screenshots | latest-run + evidence manifest + exact failing artifact | 80-95% |
| Planning | Blueprint + all features/archive + architecture | Blueprint when needed + registry/roadmap + target feature + exact architecture docs | 50-80% |

Assumptions:

- Agents honor `.agentignore` during broad retrieval.
- Agents consult `planning-mds/context-map.yaml` before loading product context.
- KG tools are available or exact changed paths are known.
- Binary/visual artifacts are not serialized into prompt context unless
  explicitly requested.

## Risks

- Over-filtering can hide useful context if agents skip KG lookup and target
  feature selection. Mitigation: the context map requires KG, changed-path,
  exact-file, manifest, or explicit-user routing for on-demand layers.
- Some audits legitimately need archives or evidence history. Mitigation:
  `.agentignore` blocks broad search only; exact-file access remains allowed.
- Full schemas and API specs are authoritative. Mitigation: they are on-demand,
  not ignored for exact contract validation.

## Follow-Up Recommendations

- Add a CI or preflight step that runs `python3 scripts/validate-context-map.py`.
- Have orchestration prompts load `planning-mds/context-map.yaml` immediately
  after `.agentignore`.
- When evidence packages are generated, keep `latest-run.json` and
  `evidence-manifest.json` small and pointer-oriented.
- Prefer KG hint/lookup output over broad source-tree reads during feature work.
