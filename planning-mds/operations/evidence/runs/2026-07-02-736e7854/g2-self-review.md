# G2 Self Review — F0028

## Scope Review

F0028 implementation remained inside the approved CRM-side carrier and market relationship management scope. The slice adds carrier market profiles, underwriter contacts, appetite notes, appointments, related activity links, API endpoints, authorization checks, search projection participation, frontend route/navigation, and focused tests.

Out-of-scope items remain excluded: external carrier synchronization, rating/pricing, quote comparison, reinsurance workflows, and external broker visibility.

## Acceptance Criteria Review

- F0028-S0001: directory/search/list behavior is implemented through `/carrier-markets`, query filters, policy-protected API list endpoints, and frontend directory controls.
- F0028-S0002: profile create/update/archive behavior is implemented through DTO validation, service methods, repository persistence, audit fields, and frontend profile form.
- F0028-S0003: contact capture and removal behavior is implemented through contact child endpoints, persistence, and frontend contact entry controls.
- F0028-S0004: appetite note capture/removal behavior is implemented through child endpoints, structured classes/region fields, and frontend note entry controls.
- F0028-S0005: appointment capture/removal behavior is implemented through child endpoints, persistence, and frontend appointment controls.
- F0028-S0006: related activity link capture/removal behavior is implemented through child endpoints and persisted relationship metadata.

## Implementation Risks

- Full-suite regression was not run during this local closeout; focused F0028 and authorization tests passed.
- `Microsoft.OpenApi 2.0.0` carries an existing NU1903 advisory surfaced by build; not introduced by this feature.
- Frontend test execution in local Node requires `NODE_OPTIONS=--localstorage-file=/private/tmp/nebula-vitest-localstorage` because jsdom localStorage is otherwise unavailable.

## Validation Evidence

- `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore -m:1 /p:UseSharedCompilation=false` passed.
- `dotnet build engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-restore -m:1 /p:UseSharedCompilation=false` passed.
- Focused backend tests for `CarrierMarketEndpointTests` and `CasbinAuthorizationServiceTests` passed: 111 tests.
- `npm run build` in `experience` passed.
- `NODE_OPTIONS=--localstorage-file=/private/tmp/nebula-vitest-localstorage npm run test -- App.test.tsx` passed: 15 tests.
- Runtime smoke checks passed: `/healthz` returned `200 Healthy`; unauthenticated `/carrier-markets` returned `401`.
