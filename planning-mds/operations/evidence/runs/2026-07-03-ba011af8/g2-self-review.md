# G2 Self Review — F0024 Claims And Service Case Tracking

**Result:** PASS WITH RECOMMENDATIONS

## Scope Review

Implemented the approved F0024 vertical slice for CRM-owned service cases. The slice includes backend service-case aggregate entities, claim-reference context, communication/task bridge entities, append-only transition history, EF configuration, migration, repository, application service, API endpoints, validators, frontend typed hooks, workspace list/detail pages, navigation, and account/policy context panels.

Out of scope items remain excluded: claim adjudication, reserves, payments, coverage determination, carrier sync, external self-service, and reopen behavior for closed service cases.

## Acceptance Criteria Review

- Service cases can be created from account or policy context through `POST /service-cases` and contextual frontend panels.
- Service cases can be listed and opened through `/service-cases` and `/service-cases/:serviceCaseId`.
- Ownership, priority, due date, follow-up summary, claim-reference context, transitions, communication links, task links, and timeline projections are represented in backend code.
- Closed service cases reject mutations in the application service.
- Follow-up task creation links a new `TaskItem` to the service case and emits task plus service-case timeline projections.
- Policy/account context is read-only; F0024 does not mutate policy lifecycle or claim adjudication state.

## Validation Evidence

- Backend build passed with compiler server disabled: `dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore --nologo -m:1 -p:UseSharedCompilation=false`.
- Frontend production build passed: `corepack pnpm --dir experience build`.
- Frontend semantic theme guard passed: `corepack pnpm --dir experience lint:theme`.
- Frontend lint passed with six pre-existing warnings outside `features/service-cases`.
- Filtered backend test passed with escalated VSTest socket permissions: 5 passed, 0 failed.

## Implementation Risks

- The EF migration was hand-authored because local `dotnet ef`/MSBuild probes initially hung unless compiler server sharing was disabled.
- The current F0024 test run is build/lint plus an existing backend validator slice; F0024-specific API integration and frontend component tests are still recommended before closeout.
- Existing dependency advisory remains: `Microsoft.OpenApi 2.0.0` high severity advisory `GHSA-v5pm-xwqc-g5wc`.
- Existing nullable/lint warnings remain in non-F0024 files and were not introduced by this feature.

## Recommendations

- [high] Add F0024-specific backend integration coverage for create, update, transition, claim reference update, communication link, follow-up task, and closed-case rejection — owner: Quality Engineer; follow-up: required-before-closeout.
- [medium] Add frontend mocked component/integration tests for service-case create and detail mutations — owner: Frontend Developer; follow-up: required-before-closeout.
- [medium] Review the hand-authored F0024 migration before final signoff — owner: Code Reviewer; follow-up: required-before-closeout.
