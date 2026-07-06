# Security Review Report - F0008 Broker Insights

Result: PASS

## Review Scope

Reviewed permission-sensitive broker insights read paths, including endpoint authorization, Casbin resource/action use, projection visibility filtering, peer benchmark suppression, and frontend handling of query parameters.

## Security Checks

| Check | Result | Evidence |
| --- | --- | --- |
| Endpoint authentication | PASS | Broker insights group uses `RequireAuthorization`. |
| Resource permission | PASS | Each endpoint checks `broker_insight/read`. |
| Query-layer visibility | PASS | Service passes `ProjectionVisibilityResolver.For(user)` before repository query/aggregation. |
| Scoped repository filtering | PASS | Repository filters by allowed regions when `SeeAll` is false. |
| Benchmark privacy | PASS | Peer median/rank/percentile are suppressed below minimum visible peer count. |
| Secrets scan | PASS | artifacts/security/secrets-scan.md found no targeted secret matches. |
| Targeted SAST | PASS | artifacts/security/sast-scan.md found no blocking authorization findings. |

## Waivers

- Dependency audit: waived because external package registry access is restricted in this environment.
- Authenticated DAST: waived until API container is rebuilt/restarted and operator test credentials are available.

## Findings

No blocking security findings for G3.

## Decision

Approved for operator testing with the recorded scan waivers.
