# SAST Scan - F0008 Broker Insights

Result: PASS

## Command

`rg -n "AllowAnonymous|RequireAuthorization|HasPermissionAsync|PolicyDenied|broker_insight|SeeAll|ProjectionVisibility" engine/src/Nebula.Api/Endpoints/BrokerInsightEndpoints.cs engine/src/Nebula.Application/Services/BrokerInsightService.cs engine/src/Nebula.Infrastructure/Repositories/BrokerInsightProjectionRepository.cs engine/tests/Nebula.Tests/Unit/BrokerInsights/BrokerInsightServiceTests.cs`

## Findings

- Endpoint group uses `RequireAuthorization`.
- Each endpoint checks `broker_insight/read` through `AuthzHelper.HasPermissionAsync`.
- Query/service path applies `ProjectionVisibilityResolver.For(user)` before aggregation.
- Repository filters rows by visibility regions when `SeeAll` is false.
- Unit test verifies non-`SeeAll` visibility reaches the repository.

## Result

No blocking SAST findings for G2.
