# F0008 Broker Insights - Complete Implementation Report

**Feature:** F0008 - Broker Insights  
**Product:** Nebula Commercial P&C Insurance CRM  
**Report Date:** 2026-07-04  
**Implementation Date:** 2026-07-03  
**Archive Date:** 2026-07-03  
**Final Status:** Built, archived, and tested end to end  

## Executive Summary

F0008 was delivered as a read-only Broker Insights MVP for internal distribution and relationship teams. The feature adds a Broker Insights workspace with broker scorecards, trend drilldowns, authorized peer benchmarks, review snapshots, and permission-safe aggregation behavior.

The implementation followed the `nebula-agents` harness process for planning, architecture, implementation gates, review evidence, KG reconciliation, and PM closeout. The feature is archived under:

`planning-mds/features/archive/F0008-broker-insights/`

The application route is:

`/broker-insights`

A visible sidebar navigation item named **Broker Insights** was added near **Brokers**, so users no longer need to manually type the URL.

## Harness And Evidence

The feature was handled through the `nebula-agents` feature lifecycle, including Phase A/B planning, feature assembly, implementation, review, validation, KG reconciliation, and closeout.

Primary evidence:

| Evidence | Path |
|---|---|
| Canonical feature run | `planning-mds/operations/evidence/runs/2026-07-03-fd732693` |
| Supplemental runtime test run | `planning-mds/operations/evidence/runs/2026-07-03-fbe8a385` |
| Plan run | `planning-mds/operations/evidence/runs/2026-07-03-4b9ca863` |
| Feature status | `planning-mds/features/archive/F0008-broker-insights/STATUS.md` |
| Feature assembly plan | `planning-mds/features/archive/F0008-broker-insights/feature-assembly-plan.md` |
| Governing ADR | `planning-mds/architecture/decisions/ADR-031-broker-insights-read-models.md` |

Story closeout:

| Story | Title | Delivered |
|---|---|---|
| F0008-S0001 | Broker scorecard overview | Yes |
| F0008-S0002 | Trend drilldown and source record navigation | Yes, with source rows limited/deferred as recorded |
| F0008-S0003 | Authorized benchmark comparison | Yes |
| F0008-S0004 | Broker review snapshot | Yes |
| F0008-S0005 | Permission-safe broker insight behavior | Yes |

Required signoffs recorded in `STATUS.md`:

- Quality Engineer: PASS
- Code Reviewer: PASS
- Security Reviewer: PASS
- DevOps: PASS
- Architect: PASS

## What Was Built

### Backend

Implemented the Broker Insights read-side model and API surface.

Key backend additions:

- `BrokerInsightProjection` read model.
- EF Core configuration and migration for `BrokerInsightProjections`.
- Query DTOs and response DTOs for scorecards, trends, benchmarks, and snapshots.
- Repository for permission-filtered projection queries.
- Application service for metric composition and benchmark logic.
- Validators for period, metric key, bucket, peer set, and paging.
- Minimal API endpoints under `/broker-insights`.

Backend endpoints delivered:

| Endpoint | Purpose |
|---|---|
| `GET /broker-insights/scorecards` | Paginated broker metric scorecards |
| `GET /broker-insights/{brokerId}/trends` | Period bucket trend drilldown |
| `GET /broker-insights/{brokerId}/benchmarks` | Authorized peer benchmark comparison |
| `GET /broker-insights/{brokerId}/snapshot` | Review-ready broker snapshot |

Important backend behavior:

- Aggregates are computed from `BrokerInsightProjection` rows.
- Visibility filtering is applied before aggregation.
- Benchmark rank, percentile, and median are suppressed unless visible peer count reaches the configured threshold.
- F0008 does not implement F0037 hierarchy-aware access enforcement or distribution rollups.

### Frontend

Implemented the Broker Insights workspace and route.

Key frontend additions:

- Protected route: `/broker-insights`.
- `BrokerInsightsPage` wrapper.
- Feature slice under `experience/src/features/broker-insights/`.
- Typed API hooks for scorecards, trends, benchmarks, and snapshots.
- Scorecard metric cards.
- Trend drilldown panel.
- Authorized benchmark panel.
- Review snapshot panel.
- Empty and partial-data states.
- Sidebar navigation item: **Broker Insights**, placed near **Brokers**.

User path now supported:

1. Open `http://localhost:5173/brokers`.
2. Click **Broker Insights** in the sidebar.
3. Land on `http://localhost:5173/broker-insights`.
4. Review scorecards, trend drilldown, benchmarks, and snapshot.

### Planning, Contracts, And Security Artifacts

F0008 planning and architecture artifacts were updated or added:

- PRD and story files.
- Feature assembly plan.
- ADR-031 for broker insight read models.
- OpenAPI contract additions in `planning-mds/api/nebula-api.yaml`.
- JSON schemas for scorecard, trend, benchmark, and snapshot responses.
- Data model documentation for `BrokerInsightProjection`.
- Authorization matrix and policy entries for `broker_insight:read`.
- Knowledge graph bindings and coverage report updates.
- Registry, roadmap, story index, and archive state updates.

## Runtime Fixes And Local Environment Work

Several local runtime issues were handled while making F0008 runnable for testing:

1. **Authentication configuration**
   - Created local `.env` from `.env.example`.
   - Set local development authentik secret/bootstrap values.
   - Added `experience/.env.development.local` with `VITE_AUTH_MODE=dev` so the local frontend can use the dev-auth token flow for feature review.

2. **Database/runtime recovery**
   - Reset local Docker volumes after migration drift caused `SavedViews already exists`.
   - Recreated the `authentik` database manually because the Postgres init script was not executable in the local environment.
   - Confirmed API, DB, authentik, Temporal, and Temporal UI were running.

3. **Frontend proxy correction**
   - Updated `experience/vite.config.ts` so the Vite API proxy defaults to `http://localhost:8080`.
   - Added `/broker-insights` to proxied API paths.
   - This fixed frontend-to-backend F0008 API calls during local development.

4. **Demo data for local testing**
   - Seeded local `BrokerInsightProjections` rows for five brokers across seven F0008 metrics.
   - This was local runtime data only, used so the UI could show populated scorecards and benchmarks.
   - The seeded peer count satisfies the minimum benchmark threshold.

5. **Navigation gap fix**
   - Added the **Broker Insights** sidebar item in `experience/src/components/layout/Sidebar.tsx`.
   - The item links to `/broker-insights` and appears in both desktop and mobile navigation because it uses the shared `NAV_ITEMS` array.

## Validation Performed

### Runtime Health

Docker Compose stack was verified:

- `nebula-api`: running on `http://localhost:8080`
- `nebula-db`: healthy on port `5433`
- `nebula-authentik-server`: healthy on `9000/9443`
- `nebula-authentik-worker`: healthy
- `nebula-temporal`: running
- `nebula-temporal-ui`: running on `8082`

Frontend dev server was running at:

`http://localhost:5173`

### Automated Tests

Focused backend tests:

```bash
dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter BrokerInsight --no-restore
```

Result:

- Passed: 3
- Failed: 0
- Skipped: 0

Focused frontend test:

```bash
pnpm --dir experience exec vitest run src/features/broker-insights/tests/BrokerInsightsWorkspace.test.tsx
```

Result:

- Passed: 2
- Failed: 0

Theme guard:

```bash
pnpm --dir experience lint:theme
```

Result:

- Passed

Frontend build after nav change:

```bash
pnpm --dir experience build
```

Result:

- Passed

Frontend lint after nav change:

```bash
pnpm --dir experience lint
```

Result:

- Passed with existing unrelated warnings.

### Live API Verification

The following F0008 live endpoint paths were verified through the frontend proxy with a dev token:

| Endpoint | Result |
|---|---|
| `/broker-insights/scorecards` | 200 |
| `/broker-insights/{brokerId}/trends` | 200 |
| `/broker-insights/{brokerId}/benchmarks` | 200 |
| `/broker-insights/{brokerId}/snapshot` | 200 |

Observed live data:

- Scorecards returned populated broker metrics.
- Trend endpoint returned bucketed trend data.
- Benchmark endpoint returned peer status `Available`.
- Snapshot endpoint returned broker snapshot data for Apex Brokerage.

### Browser E2E Smoke

Browser flow tested:

1. Navigated to `http://localhost:5173/brokers`.
2. Verified **Broker Insights** nav item is visible.
3. Clicked **Broker Insights**.
4. Verified URL changed to `http://localhost:5173/broker-insights`.
5. Verified the page rendered:
   - Broker Insights heading.
   - Apex Brokerage scorecard.
   - Quote count.
   - Bind count.
   - Quote-to-bind rate.
   - Retention rate.
   - Open pipeline.
   - Activity count.
   - Production.
   - Trend drilldown.
   - Authorized benchmark.
   - Review snapshot.
6. Verified all F0008 network calls returned `200`.
7. Verified no browser page errors were captured.

Screenshot produced:

`/private/tmp/f0008-broker-insights-e2e.png`

## Known Caveats And Deferrals

1. **Source row drilldown is limited**
   - The API shape includes `sourceRows` and `sourceLinks`.
   - Rich populated source-record drilldown/navigation is not fully fleshed out.
   - This was recorded in closeout evidence as deferred/limited, not discovered as a surprise.

2. **Projection ingestion/scheduling is deferred**
   - F0008 uses `BrokerInsightProjection` read models.
   - Automated production ingestion/scheduling remains future work.
   - Local demo projection rows were seeded only for runtime review.

3. **F0037 remains separate**
   - F0008 consumes authorized projection rows.
   - F0037 remains responsible for hierarchy-aware access enforcement and distribution rollups.

4. **EF migration metadata residual risk**
   - STATUS notes that EF migration designer/model snapshot should be regenerated through standard EF tooling before future migration authoring.

5. **Unrelated test failure observed**
   - A broad frontend test run surfaced an unrelated failure in `sessionTelemetry.test.ts`.
   - The focused F0008 frontend test passed.

6. **Sandbox-specific MSBuild issue**
   - A backend test attempt inside the sandbox hit MSBuild named-pipe permission errors.
   - The same focused backend test passed when rerun with the required permission context.

## Files And Areas Touched

High-level touched areas:

- `engine/src/Nebula.Domain/Entities/`
- `engine/src/Nebula.Application/DTOs/`
- `engine/src/Nebula.Application/Interfaces/`
- `engine/src/Nebula.Application/Services/`
- `engine/src/Nebula.Application/Validators/`
- `engine/src/Nebula.Infrastructure/Persistence/`
- `engine/src/Nebula.Infrastructure/Repositories/`
- `engine/src/Nebula.Api/Endpoints/`
- `engine/tests/Nebula.Tests/Unit/BrokerInsights/`
- `experience/src/features/broker-insights/`
- `experience/src/pages/BrokerInsightsPage.tsx`
- `experience/src/App.tsx`
- `experience/src/components/layout/Sidebar.tsx`
- `experience/vite.config.ts`
- `planning-mds/api/`
- `planning-mds/schemas/`
- `planning-mds/architecture/`
- `planning-mds/security/`
- `planning-mds/knowledge-graph/`
- `planning-mds/features/archive/F0008-broker-insights/`

## Final Assessment

F0008 is built end to end for the approved read-only MVP scope and is good to test in the running app.

Use:

`http://localhost:5173/broker-insights`

or navigate through:

`Brokers -> Broker Insights` from the sidebar.

The feature is functionally available with backend, frontend, local runtime data, proxy routing, sidebar navigation, and focused automated/browser validation completed.
