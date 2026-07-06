# Test Plan - F0008 Broker Insights

Result: PASS

## Scope

Validate the F0008 broker insights implementation across backend aggregation, permission-safe filtering, frontend rendering, and build-time integration.

## Test Cases

| Area | Test | Expected Result |
| --- | --- | --- |
| Backend service | Group broker projections into scorecards | Metrics are grouped per visible broker and hidden region rows are excluded. |
| Backend service | Trend buckets | Bucket output is ordered and partial status propagates. |
| Backend service | Benchmark peer suppression | Peer median/rank are suppressed when visible peer count is below the minimum. |
| Frontend workspace | Authorized data render | Scorecard, trend, benchmark, and snapshot panels render for a visible broker. |
| Frontend workspace | Broker filter | Broker ID input updates scorecard query parameters. |
| Backend build | Solution build | API, Application, Infrastructure, Domain, and Tests compile. |
| Frontend build | TypeScript/Vite build | Route, hooks, and components compile into production bundle. |

## Out Of Scope

- Full authenticated browser E2E against a rebuilt API container is deferred until operator test setup because the running Docker API image predates this local implementation.
- Live projection ingestion from upstream workflow/search data is not included in this feature pass.
