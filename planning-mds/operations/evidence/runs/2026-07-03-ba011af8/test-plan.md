# Test Plan — F0024 Claims And Service Case Tracking

**Result:** PASS WITH RECOMMENDATIONS

## Planned Coverage

| Area | Coverage Target | Status |
|---|---|---|
| Backend compile | API, Application, Infrastructure, Domain compile together | Covered |
| Backend API behavior | Create/list/detail/update/transition/claim-reference/communication-link/follow-up endpoints | Recommended before closeout |
| Backend workflow | Allowed transitions, resolved summary requirement, closed-case mutation rejection | Recommended before closeout |
| Persistence | EF mapping and migration for five service-case tables | Covered by build; migration review recommended |
| Frontend compile | TypeScript and Vite production build | Covered |
| Frontend theme/lint | Semantic theme guard and ESLint | Covered |
| Frontend behavior | Create modal, list panel, detail transition/claim/task flows | Recommended before closeout |
| Security | Service-case authorization and claim-reference data handling | Carry to G3 security review |

## Commands

- `perl -e 'alarm 60; exec @ARGV' dotnet build engine/src/Nebula.Api/Nebula.Api.csproj --no-restore --nologo -m:1 -p:UseSharedCompilation=false`
- `corepack pnpm --dir experience build`
- `corepack pnpm --dir experience lint:theme`
- `corepack pnpm --dir experience lint`
- `perl -e 'alarm 90; exec @ARGV' dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --no-restore --nologo -m:1 -p:UseSharedCompilation=false --filter FullyQualifiedName~CommunicationValidatorsTests`

## Recommendations

- [high] Add F0024-specific API integration tests before closeout — owner: Quality Engineer; follow-up: required-before-closeout.
- [medium] Add service-case frontend tests before closeout — owner: Frontend Developer; follow-up: required-before-closeout.
