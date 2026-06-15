# Context Loading Optimization Report

Date: 2026-06-08

## Summary

This change adds a product-local prompt context strategy for `nebula-insurance-crm` without changing product runtime behavior, business logic, authorization, validation, safety rules, or public interfaces.

New controls:
- `planning-mds/context-map.yaml` defines default and on-demand context layers.
- `.agentignore` blocks high-token/generated/history-heavy paths from broad retrieval.
- `scripts/validate-context-map.py` validates the context-loading policy.
- README and planning docs explain how agents should load context during planning, implementation, review, validation, and evidence audit runs.

## Files Changed

- `.agentignore`
- `README.md`
- `planning-mds/README.md`
- `planning-mds/context-map.yaml`
- `planning-mds/architecture/context-loading-optimization-report.md`
- `scripts/validate-context-map.py`
- `scripts/tests/test_validate_context_map.py`

## Measured High-Token Surfaces Before Optimization

Measured with a local file inventory on 2026-06-08. Token estimates use a rough 4 text characters per token heuristic.

| Surface | Files | Text Size | Lines | Estimated Tokens | Notes |
|---------|------:|----------:|------:|-----------------:|-------|
| `experience/**` | 33,920 | 181.48 MiB | 4,213,759 | 47,572,594 | Dominated by `node_modules`; should never be broad prompt context |
| `engine/**` | 407 | 3.02 MiB | 72,010 | 791,635 | Use KG/changed-path/exact-file routing |
| `planning-mds/knowledge-graph/**` | 9 | 2.19 MiB | 58,260 | 574,052 | Prefer lookup/hint output before raw YAML |
| `planning-mds/features/archive/**` | 257 | 2.12 MiB | 38,680 | 555,957 | Historical archive; exact-file only |
| `experience/src/**` | 311 | 1.24 MiB | 37,810 | 324,010 | Use feature slice, changed path, or failing tests |
| `planning-mds/operations/evidence/**` | 197 | 0.85 MiB text | 11,174 | 223,795 | Plus images/logs; manifest-routed only |
| `planning-mds/architecture/**` | 43 | 0.61 MiB | 11,783 | 161,093 | Load task-matched files only |
| `planning-mds/api/**` | 1 | 0.22 MiB | 6,745 | 58,026 | Full OpenAPI is on-demand |
| `planning-mds/schemas/**` | 85 | 0.14 MiB | 4,365 | 36,666 | Exact schema only |
| `planning-mds/screens/**` | 27 | 0.02 MiB text, 14.14 MiB binary | 486 | 5,740 text | Screenshots are exact-file only |
| `neuron/**` | 22 | 0.01 MiB | 266 | 2,185 | Load only for AI/neuron scope |
| `planning-mds/lob-schemas/**` | 5 | 0.00 MiB | 128 | 1,032 | Exact schema bundle only |

## Estimated Before/After Prompt Consumption

| Scenario | Before | After | Expected Reduction |
|----------|-------:|------:|-------------------:|
| Accidental broad `experience/**` load | ~47.6M tokens | ~5k-40k tokens from exact changed files/config | 99%+ |
| Broad source + planning read (`engine/**`, `experience/src/**`, architecture, API, KG) | ~1.9M tokens | ~40k-120k tokens via target feature, KG output, exact files | 94-98% |
| Feature review with archive/evidence loaded broadly | ~780k tokens | ~20k-80k tokens via latest-run/manifest/exact evidence | 90-97% |
| Planning run with all architecture/API/schema files loaded | ~256k tokens | ~20k-70k tokens using context map and task-matched files | 70-90% |

The largest reduction comes from removing dependency folders, full source trees, old evidence runs, screenshots, logs, and archived feature folders from default retrieval.

## Default Context After Optimization

Default context is limited to:
- `README.md`
- `lifecycle-stage.yaml`
- `.agentignore`
- `planning-mds/context-map.yaml`
- `planning-mds/features/REGISTRY.md`
- `planning-mds/features/ROADMAP.md`
- Target feature folder only
- KG lookup/hint output when available
- Exact changed files or exact files named by the task

## On-Demand Only Context

These surfaces require exact-file, KG, changed-path, manifest, or explicit-user routing:
- `planning-mds/features/archive/**`
- `planning-mds/operations/evidence/**`
- screenshots, images, visual artifacts, PDFs
- old run logs and generated outputs
- full API specs
- full JSON Schema and LOB schema bundles
- full backend/frontend/neuron source trees
- full test trees
- examples and historical feature docs

## Assumptions

- Token estimates are approximate and use text bytes divided by 4.
- Agents and operators honor `.agentignore` for broad searches and use exact paths for bypasses.
- KG tools remain the preferred compressed routing layer for source and planning context.
- Runtime source files are still readable by exact path; this change only controls default/broad prompt loading.

## Risks

- Overly strict broad-search discipline can hide relevant files if agents skip KG/changed-path routing. Mitigation: context map explicitly allows exact-file, KG, changed-path, and explicit-user bypasses.
- Some tools may not automatically honor `.agentignore`. Mitigation: docs require scoped searches and exact paths when tool-level ignore support is unavailable.
- `planning-mds/BLUEPRINT.md` is on-demand, so planning runs must explicitly load it when baseline strategy is needed.

## Follow-Up Recommendations

- Add a lifecycle gate for `python3 scripts/validate-context-map.py` if the team wants automated enforcement in product validation.
- Consider adding a small `scripts/context-inventory.py` helper to refresh this report's measurements.
- Keep `planning-mds/context-map.yaml` updated when new large surfaces, generated artifacts, or agent evidence formats are introduced.
- Prefer adding new compressed indexes over expanding default prompt context.
