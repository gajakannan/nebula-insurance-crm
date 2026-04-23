# F0018 G3 Code Review Evidence

Date: 2026-04-22  
Reviewer: Codex feature runner  
Verdict: PASS

## Reviewed Surfaces

- `engine/src/Nebula.Api/Endpoints/PolicyEndpoints.cs`
- `engine/src/Nebula.Application/Services/PolicyService.cs`
- `engine/src/Nebula.Application/DTOs/PolicyDtos.cs`
- `engine/src/Nebula.Application/Validators/PolicyValidators.cs`
- `engine/src/Nebula.Infrastructure/Repositories/PolicyRepository.cs`
- `engine/src/Nebula.Infrastructure/Persistence/Configurations/PolicyConfiguration.cs`
- `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260422021000_F0018_PolicyLifecycleAggregate.cs`
- `experience/src/features/policies/**`
- `experience/src/pages/PoliciesPage.tsx`
- `experience/src/pages/PolicyDetailPage.tsx`
- `experience/src/pages/CreatePolicyPage.tsx`
- `experience/src/pages/PolicyImportPage.tsx`
- `experience/src/pages/AccountDetailPage.tsx`

## Findings

| Severity | Finding | Resolution |
|----------|---------|------------|
| Critical | None remaining. | N/A |
| High | None remaining. | N/A |
| Medium | `/policies/from-bind` was still a 501 placeholder despite the OpenAPI contract. | Fixed by adding `PolicyFromBindRequestDto`, validator, and endpoint mapping through the policy create path. |
| Medium | Dashboard renewal aging assumed one SLA threshold per status and threw when valid LOB-specific rows existed. | Fixed by grouping thresholds by status and preferring the default threshold row. |

## Residual Risk

- Policy-number generation is count-based with a unique index as the final guard. A dedicated sequence row remains the stronger concurrency design from the assembly plan.
- Policy-specific endpoint integration tests should be added for from-bind and lifecycle action edge cases; current regression coverage is through existing backend suite and account/renewal compatibility tests.

## Verification

- `dotnet test engine/Nebula.slnx` => PASS, 395 passed, 1 skipped
- `CI=true pnpm --dir experience build` => PASS
