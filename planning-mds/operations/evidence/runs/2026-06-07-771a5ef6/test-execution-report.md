# Test Execution Report — F0017 run 2026-06-07-771a5ef6

## Summary

Verdict: PASS WITH RECOMMENDATIONS.

Current-session validation passed for the F0017 backend and frontend distribution slice. After G3 repair, the authoritative backend result is 24/24 passing.

Same-run UI continuation on 2026-07-03 resolved the reported F0017 browser integration gaps by wiring the existing distribution panels into Broker Detail and validating the Vite proxy route prefixes.

Same-run frontend runtime cleanup on 2026-07-03 resolved the local `/login` authentication-configuration screen by enabling ignored dev auth configuration, and repaired browser-style direct navigation for proxied SPA routes such as `/brokers`.

Same-run hierarchy load cleanup on 2026-07-03 resolved the reported Broker Detail → Distribution `"Unable to load hierarchy."` state by ensuring broker-backed `DistributionNode` rows exist and stay synced with broker lifecycle changes.

Same-run demo visibility cleanup on 2026-07-03 added concrete F0017 examples for the reported broker (`Anchor Advisors 015`) so the live application demonstrates hierarchy children, current/historical territory assignment, ownership, and broker timeline events without requiring the operator to create all sample data by hand.

## Backend Execution

Command:
`dotnet test Nebula.slnx --filter "DistributionEndpointTests|ProducerOwnershipEndpointTests|TerritoryEndpointTests" --logger "trx;LogFileName=f0017-backend.trx"`

Result:
exit_code=0; 24 passed, 0 failed, 0 skipped after G3 repair.

Artifact:
artifacts/test-results/f0017-backend-after-g3.trx

Notes:
- The first sandboxed .NET attempt failed before project execution due MSBuild named-pipe/socket permissions; see artifacts/test-results/dotnet-sandbox-msbuild-permission.txt
- The approved elevated rerun passed. A second approved elevated rerun after G3 repair passed with 24/24 tests and is the authoritative backend result.
- 2026-07-03 cleanup reran `dotnet restore Nebula.slnx` after pinning `Microsoft.OpenApi` to `2.9.0`; restore completed without the prior advisory warning.

## Frontend Execution

Command:
`pnpm exec vitest run src/features/distribution/tests/HierarchyPanel.test.tsx --reporter=verbose`

Result:
exit_code=0; 2 passed, 0 failed.

Artifact:
artifacts/test-results/f0017-frontend-vitest.txt

Feature-level frontend notes:
- The hierarchy panel renders the root-to-node breadcrumb and child list.
- The hierarchy panel exposes the set-parent control expected by F0017 UI scope.

Command:
`pnpm exec vitest run src/pages/tests/BrokerDetailPage.integration.test.tsx src/features/distribution/tests/HierarchyPanel.test.tsx --reporter=verbose -t "renders F0017|HierarchyPanel"`

Result:
exit_code=0; 3 passed, 0 failed for the F0017-filtered Broker Detail route/component coverage.

Artifact:
artifacts/test-results/f0017-broker-detail-distribution-vitest.txt

Feature-level frontend notes:
- Broker Detail now exposes a `Distribution` tab.
- The tab renders the existing F0017 `DistributionPanels`, including hierarchy, ownership, and territory surfaces.
- The F0017 route integration test verifies hierarchy breadcrumb/children, ownership owner display, and territory assignment display from the broker detail page.
- 2026-07-03 cleanup stabilized the broader Broker Detail integration test's contact-edit flow without changing product behavior; the full file now passes 8/8, including the F0017 Distribution tab test.
- 2026-07-03 frontend runtime cleanup reran the F0017-filtered Broker Detail/HierarchyPanel suite after the dev-auth and proxy navigation fixes; result remained 3/3 passing.
- 2026-07-03 hierarchy load cleanup reran the F0017-filtered Broker Detail/HierarchyPanel suite after broker-backed node repair; result remained 3/3 passing.

## Hierarchy Load Fix Execution

Command:
`dotnet test Nebula.slnx --filter "BrokerEndpointTests|DistributionEndpointTests|ProducerOwnershipEndpointTests|TerritoryEndpointTests"`

Result:
exit_code=0; 40 passed, 0 failed, 0 skipped after adding broker lifecycle hierarchy-sync coverage.

Command:
`Playwright browser smoke for /brokers/e2bb173c-ae3c-431b-bcd6-98f21f04448c → Distribution tab`

Result:
exit_code=0; `Unable to load hierarchy.` count was 0 and the visible breadcrumb was `(root) > Acme MGA > Anchor Advisors 015`.

Artifact:
artifacts/test-results/f0017-hierarchy-load-fix-smoke.txt

## Demo Visibility And Timeline Execution

Command:
`Playwright browser smoke for /brokers/e2bb173c-ae3c-431b-bcd6-98f21f04448c → Distribution and Timeline tabs`

Result:
exit_code=0; the live UI showed:
- Hierarchy breadcrumb including `Acme MGA` and `Anchor Advisors 015`.
- Children list including `Anchor Advisors 015 Producer A` and `Anchor Advisors 015 Producer B`.
- Current ownership using producer node `e2bb173c-ae3c-431b-bcd6-98f21f041701`.
- Current territory `F0017 Demo - Southeast`.
- Territory as-of lookup: `2026-02-01` -> `F0017 Demo - Northeast`; `2026-05-01` -> `F0017 Demo - Southeast`.
- Timeline events `ProducerOwnershipAssigned` and `TerritoryMemberReassigned` with actor `Dev Seed`.

Artifact:
artifacts/test-results/f0017-demo-visibility-smoke.txt

## UI/API Runtime Smoke

Commands:
- `curl -i http://127.0.0.1:5173/distribution-nodes/00000000-0000-0000-0000-000000170003/ancestors`
- `curl -i http://127.0.0.1:5173/producer-ownership`
- `curl -i http://127.0.0.1:5173/territories`
- `curl -i http://127.0.0.1:5173/territory-assignments`
- Authenticated dev-JWT fetch through Vite to `/distribution-nodes/{nodeId}/ancestors`

Result:
exit_code=0; Vite returned backend responses for all F0017 prefixes. Authenticated hierarchy read returned 200 `application/json`.

Artifact:
artifacts/test-results/f0017-vite-proxy-smoke.txt

Command:
`curl -i -H 'Accept: text/html' http://127.0.0.1:5173/brokers`

Result:
exit_code=0; Vite returned HTTP 200 `text/html` for browser-style direct navigation to the Brokers route.

Command:
`curl -i -H 'Accept: application/json' http://127.0.0.1:5173/distribution-nodes/test-node/ancestors`

Result:
exit_code=0; Vite forwarded the F0017 API request to Kestrel, which returned backend JSON.

Command:
`pnpm exec playwright screenshot --wait-for-timeout 2500 http://127.0.0.1:5173/login /tmp/nebula-login-dev-mode-after-proxy.png`

Result:
exit_code=0; the app shell rendered through local dev auth and the prior authentication-configuration error was gone.

Command:
`pnpm exec playwright screenshot --wait-for-timeout 2500 http://127.0.0.1:5173/brokers /tmp/nebula-brokers-direct-route-after-proxy.png`

Result:
exit_code=0; the Brokers page rendered through the SPA shell on direct navigation.

Artifact:
artifacts/test-results/f0017-frontend-runtime-fix-smoke.txt

## Build And Lint Execution

Command:
`pnpm lint`

Result:
exit_code=0; 0 errors, 5 warnings.

Artifact:
artifacts/test-results/frontend-lint.txt

Command:
`pnpm build`

Result:
exit_code=0; TypeScript and Vite production build succeeded.

Artifact:
artifacts/test-results/frontend-build.txt

## Recommendations

- Track the Vite large-chunk warning for release hardening or route-level code splitting.
