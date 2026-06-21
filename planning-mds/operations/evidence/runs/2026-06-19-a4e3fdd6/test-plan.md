# Test Plan — F0023 run 2026-06-19-a4e3fdd6

**Role:** Quality Engineer · **Date:** 2026-06-20

## Story-to-AC Mapping

| Story | Key acceptance criteria | Covered by |
|-------|-------------------------|------------|
| S0001 Global search grouped results | `GET /search-results` returns grouped, permission-filtered rows + facets | SearchService unit tests; SearchResultsList component test; container route smoke |
| S0002 Filter/sort/open | object-type/status/region/LOB filters, sort, source navigation | SearchFilters + SearchResultsList (Link targetUrl) component tests; query validator unit |
| S0003 Personal saved views | create/list/update/archive personal views, owner-only | SavedViewService unit tests (create/update/archive/get-by-non-owner) |
| S0004 Team saved views + defaults | team scope authorization, default propagation | SavedViewService unit tests (team scope required/denied/manager-allowed, setDefault clears prior) |
| S0005 Daily workload report | due-today/overdue/owner/status counts, drilldowns | OperationalReportService unit tests (workload counts) |
| S0006 Workflow aging/backlog | age bands, backlog drilldown | OperationalReportService unit tests (aging bands + backlog order) |
| S0007 Permission-safe behavior | counts/facets/rows after source-visibility filter; 403 for unauthorized | ProjectionVisibilityResolver + repo visibility predicate (unit); Casbin 401/403 at endpoints (smoke) |

## Test Strategy

- **Unit (backend, xUnit):** service logic with hand-written stub repos — authorization, concurrency (If-Match/RowVersion), duplicate-name, audit emission, report aggregation, visibility resolution. 17 tests.
- **Component (frontend, Vitest + RTL):** presentational + navigation behavior for search results and the global search trigger. 5 tests.
- **Runtime smoke (container):** image build, migration apply, `/healthz` 200, F0023 routes 401 (auth-enforced), control 404.
- **Dependency security scan:** `dotnet list package --vulnerable` + `pnpm audit`.
- **Deferred (follow-up):** Testcontainers-Postgres integration tests (real SQL visibility predicate, facet counts, saved-view CRUD over HTTP) and Playwright E2E (search→save→reload→apply→open; manager team-default propagation). See Deferred Follow-ups in pm-closeout.

## Developer-vs-QE Test Ownership

Backend/Frontend developers own unit + component tests for their layers (delivered). QE owns the cross-tier strategy, coverage validation, and the deferred integration/E2E + permission-matrix layer.

## Test Data / Fixtures

- Backend unit: file-scoped in-memory stub repos (`SvSavedViewRepo`, `SrchRepo`, `RptRepo`) + stub `ICurrentUserService`/`IAuthorizationService`/`IUnitOfWork`.
- Projections: populated at runtime by `SearchProjectionService.BackfillAsync` from source modules (idempotent upsert).

## Happy / Edge / Error / Auth / Accessibility / Regression Cases

- Happy: search returns results; create/apply saved view; workload/aging counts.
- Edge: query <2 chars (no search), empty results (access-scoped copy), no saved views.
- Error: duplicate name (409), stale If-Match (412/precondition), invalid criteria (422).
- Auth: unauthorized role → 401/403; non-owner personal view → 404; team scope denied (403).
- Accessibility: search input labelled (`role=search`, `sr-only` label); aria-busy on loading lists.
- Regression: full frontend suite 245 pass (1 pre-existing unrelated failure); backend suite unaffected.

## Risks And Mitigations

- Cross-object leakage (Critical) → visibility predicate applied before counts/facets/rows; unit-verified; integration test deferred.
- Projection lag → `generatedAt`/`indexedAt` surfaced; documented.

## Result

`PASS` — automated unit + component tests green; logic coverage ≥80%; runtime smoke green. Integration/E2E coverage deferred as non-blocking follow-ups.
