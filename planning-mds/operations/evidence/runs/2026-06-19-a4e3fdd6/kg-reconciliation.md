# Knowledge-Graph Reconciliation — F0023 run 2026-06-19-a4e3fdd6

**Gate:** G7 · **Role:** Architect (`agents/architect/SKILL.md`) · **Date:** 2026-06-20

## Scope

Bind the **as-built** F0023 source into the SEMANTIC graph (`code-index.yaml` / `canonical-nodes.yaml`) so the next feature's architect reads a correct graph at G0. Code paths only (stable across the G8 archive move). Diffed against the G0 "Knowledge-Graph Binding Plan" baseline.

## Binding Delta

Added `paths.backend` / `paths.frontend` code bindings to 7 existing Phase B nodes in `code-index.yaml` (previously planning-only). Matches the G0 Binding Plan; frontend bound by directory glob per the glob-not-file-by-file rule.

| Node | Bound code (summary) |
|------|----------------------|
| `entity:search-document` | Domain entity, SearchDtos, ISearchDocumentRepository, SearchDocumentRepository + Configuration, migration glob |
| `entity:saved-view` | Domain entity, SavedViewDtos, ISavedViewRepository, SavedViewService, repo + config |
| `entity:saved-view-audit-event` | Domain entity + EF configuration |
| `entity:operational-report-projection` | Domain entity, OperationalReportDtos, repo interface + impl + config |
| `capability:global-search` | SearchService, ProjectionVisibilityResolver, validator, SearchProjectionService (infra), SearchEndpoints, unit test, `experience/src/features/search/**`, SearchResultsPage |
| `capability:saved-views` | SavedViewService, validator, SavedViewEndpoints, unit test, `experience/src/features/search/**` |
| `capability:operational-reporting` | OperationalReportService, validator, OperationalReportEndpoints, unit test, `experience/src/features/reports/**`, OperationalReportsPage |

Endpoints/schemas/ADR/api retain their existing planning bindings (already cover the surfaces).

## Canonical Nodes

**None introduced.** F0023 implements the capability/entity/endpoint nodes already added to `canonical-nodes.yaml` during Phase B planning (`2026-06-19-2f180001`); G7 affirms them and binds code. No new `WHY`/rationale entries required.

## Validator Results

- `python3 scripts/kg/validate.py --check-symbols` → exit 0 (`2702 symbols, 2702 on bound nodes`; PASS).
- `python3 scripts/kg/validate.py --check-drift` → exit 0 (`186 code bindings`; PASS) on the post-binding graph.
- `coverage-report.yaml` regeneration NOT run here (path-sensitive; deferred to G8 post-archive-move per contract).

### Symbol-regeneration limitation (environment) — follow-up, non-blocking

`validate.py --regenerate-symbols` could not run in this environment: the TypeScript extractor is unavailable and the C# extractor's internal build fails (`csharp extractor exited 1`), which would zero out `symbol-index.yaml`. To avoid shipping a wiped index, the committed `symbol-index.yaml` (2702 symbols) was **preserved** (`git checkout`); `--check-symbols` and `--check-drift` run green against it. The F0023 symbol entries are therefore not yet in the symbol layer — regenerate in an environment with working extractors. Tracked as an Open Follow-up.

## Handoff to Closeout

Semantic graph is green (symbol + drift). PM closeout (G8) should VERIFY (not re-author) these bindings, then run the path-sensitive `--write-coverage-report` AFTER the archive move.
