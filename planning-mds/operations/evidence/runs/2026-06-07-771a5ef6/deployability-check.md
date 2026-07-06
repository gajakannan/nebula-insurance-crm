# Deployability Check — F0017 run 2026-06-07-771a5ef6

## Summary

Verdict: PASS WITH RECOMMENDATIONS.

F0017 is deployable at G2 based on current runtime status, backend integration tests, frontend production build, and migration-scoped evidence. The slice changes schema/runtime code and UI code, but does not introduce a new service, container, external dependency, or deployment topology.

Same-run UI continuation on 2026-07-03 added the missing Broker Detail route integration and Vite development proxy prefixes for F0017 APIs. This is a dev-server/runtime wiring correction inside the approved F0017 UI/API scope, not a topology change.

Same-run frontend runtime cleanup on 2026-07-03 added local dev-auth configuration and repaired Vite's SPA navigation behavior for proxied route prefixes. This keeps browser navigation and refresh on routes such as `/brokers` in the frontend while preserving backend proxying for F0017 API calls.

Same-run hierarchy load cleanup on 2026-07-03 added idempotent broker/MGA-backed hierarchy node repair and broker lifecycle sync. This repairs already-seeded development databases and prevents new broker records from opening Broker Detail with missing hierarchy data.

Same-run demo visibility cleanup on 2026-07-03 extended the idempotent development seed repair for the reported broker detail route. It adds sample producer children, effective-dated territory history, and broker timeline events for local verification only. This uses existing F0017 tables and APIs; no new schema, service, environment variable, or deployment topology is required.

## Runtime Status

`docker compose ps` succeeded in the current resume environment. Services observed up include:
- `nebula-api`
- `nebula-db`
- `nebula-authentik-server`
- `nebula-authentik-worker`
- `nebula-temporal`
- `nebula-temporal-ui`

Historical G1 runtime preflight remains recorded in `g1-runtime-preflight.md` and `artifacts/test-results/g1-docker-ps.txt`.

## Migration And Schema

The EF migration `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260608033854_F0017_DistributionHierarchyOwnershipTerritory.cs` makes the feature deployment-config-bearing under the harness path classes. Current backend integration tests apply the schema path through the test startup and pass 24/24 after the G3 territory overlap repair.

No new Dockerfile, compose file, environment variable, CI workflow, or topology change is required by F0017.

No new database migration is required for the hierarchy load repair. The existing F0017 `DistributionNodes` table already supports broker/MGA nodes; the fix adds application-level sync plus idempotent development seed repair.

## Development Proxy

`experience/vite.config.ts` now proxies all F0017 API prefixes to the configured backend target:
- `/distribution-nodes`
- `/producer-ownership`
- `/territories`
- `/territory-assignments`

Runtime smoke through `http://127.0.0.1:5173` confirmed the routes reach Kestrel/backend responses instead of the frontend HTML shell. Authenticated hierarchy read through Vite returned 200 JSON. Artifact: `artifacts/test-results/f0017-vite-proxy-smoke.txt`.

The proxy now bypasses backend forwarding for browser-style `GET` requests that accept `text/html`, returning the Vite SPA shell for direct navigation and refresh. Verification confirmed:
- `Accept: text/html` request to `/brokers` returned HTTP 200 `text/html`.
- `Accept: application/json` request to `/distribution-nodes/test-node/ancestors` returned backend JSON from Kestrel.
- Playwright screenshots confirmed `/login` renders the app via dev auth and `/brokers` renders the Brokers page on direct navigation.

Artifact: `artifacts/test-results/f0017-frontend-runtime-fix-smoke.txt`.

## Hierarchy Runtime Data

After restarting `Nebula.Api`, development seed repair increased live `DistributionNodes` coverage from demo-only rows to broker/MGA-backed rows. The screenshot broker `e2bb173c-ae3c-431b-bcd6-98f21f04448c` now exists as a `Broker` distribution node parented to Acme MGA, and Broker Detail → Distribution renders its hierarchy breadcrumb without the prior error.

Artifact: `artifacts/test-results/f0017-hierarchy-load-fix-smoke.txt`.

The same broker now also has two producer child nodes, ownership/territory sample data, closed prior territory assignment history, and broker-scoped timeline events. Browser smoke confirmed the Distribution and Timeline tabs show the examples end to end.

Artifact: `artifacts/test-results/f0017-demo-visibility-smoke.txt`.

## Frontend Build

`pnpm build` completed successfully. Artifact: `artifacts/test-results/frontend-build.txt`.

F0017 route integration is covered by `artifacts/test-results/f0017-broker-detail-distribution-vitest.txt`.

Warning:
- The generated JS chunk is larger than 500 kB after minification. This is not an F0017 G2 blocker but should be considered before production hardening.

## Dependency And Security Notes

2026-07-03 cleanup pinned `Microsoft.OpenApi` to `2.9.0` in `engine/src/Nebula.Api/Nebula.Api.csproj` and updated `Microsoft.AspNetCore.OpenApi` to `10.0.5`. `dotnet restore Nebula.slnx` now completes without the prior `Microsoft.OpenApi` 2.0.0 advisory warning, and `dotnet list Nebula.slnx package --include-transitive` resolves `Microsoft.OpenApi` as `2.9.0` for API and test projects.

Security Reviewer remains not forced for F0017 because hierarchy-aware access-control enforcement is deferred to F0037.

## Recommendations

- Track frontend bundle splitting or chunk limit adjustment as release-hardening work.
- Re-run full candidate validation at G6 before requesting final G8 closeout.
