# Nebula CRM — Dual-Stack Implementation

## What Was Built

### Phase 0 Foundation

Phase 0 introduced a Python/FastAPI sidecar service named `neuron` alongside the existing C# .NET CRM backend. The service provides health endpoints, structured JSON logging, and an async SQLAlchemy read-only database connection for analytics-oriented workloads.

The sidecar runs as a separate Docker service on the internal Nebula network. A C# bridge was added so the .NET backend can call Python capabilities through an internal `NeuronClient` without changing existing public API contracts.

Key capabilities delivered:

- FastAPI service foundation for Python-owned analytical features.
- Async SQLAlchemy connectivity for safe read-only database access.
- Structlog-based JSON logging for operational consistency.
- Health endpoints for container and service readiness checks.
- Internal C# client bridge for controlled .NET-to-Python communication.

### Phase 1 F0008 Broker Insights

Phase 1 implemented the F0008 Broker Insights feature on top of the Python sidecar. It added three analytics endpoints for broker scorecards, time-windowed trends, and production leaderboard ranking.

The feature also added a React user experience for broker analytics. Users can open a dedicated Broker Insights leaderboard page, navigate into broker detail, and inspect broker performance from a new Insights tab.

Broker analytics delivered:

- Broker scorecard metrics for quote rate, bind rate, retention rate, premium, and activity.
- Time-windowed weekly and monthly trend aggregation.
- Production leaderboard ranking active brokers by performance.
- React leaderboard page at `/broker-insights`.
- Broker detail Insights tab.
- Pure SVG trends chart.
- Metric cards aligned to the existing Nebula UI patterns.

## Why Dual-Stack?

| Task | .NET Approach | Python Approach |
| --- | --- | --- |
| Analytical aggregations | EF Core LINQ can become verbose across multiple related tables. | SQLAlchemy and Python domain code express aggregations compactly. |
| Broker scorecards | Requires more boilerplate for grouped counts, rates, and premium totals. | Metrics can be computed directly with focused analytics functions. |
| Weekly trends | ISO week grouping is awkward and easy to get subtly wrong. | Python tooling handles date windows and grouping naturally. |
| Monthly trends | Requires custom date bucketing and projection logic. | Month bucketing is concise and readable. |
| Leaderboards | Cross-table ranking logic adds query and DTO complexity. | Ranking can be composed cleanly in the analytics service. |
| Future PDF parsing | Usually requires additional commercial or heavy .NET libraries. | Python has mature open-source PDF parsing libraries. |
| Fuzzy deduplication | Implementable in .NET, but less ergonomic for rapid matching workflows. | Python has strong fuzzy matching and record linkage tooling. |
| AI and LLM integration | Possible, but tends to require more adapter code. | Python is the primary ecosystem for AI orchestration and model tooling. |
| Operational isolation | Analytics runs inside the CRM backend process unless separated. | The sidecar scales, restarts, or fails independently. |
| CRM stability | New analytical workloads increase pressure on the core service. | The core C# CRM remains focused on transactional workflows. |

## Where the Changes Were Made

### Python Layer

The new Python layer lives under `neuron/` and follows a clean architecture layout with API, application, domain, and infrastructure boundaries. This location owns Python-native analytics while keeping the existing .NET CRM modules intact.

The Python service was added to Docker Compose as an independent container on the Nebula network. This allows the sidecar to be deployed, restarted, monitored, and scaled separately from the C# API.

### C# Bridge

The C# bridge was added in the application and infrastructure layers of the existing backend. The application layer defines the internal interface and DTOs, while the infrastructure layer provides the HTTP client implementation that calls the Python service.

This keeps the dependency direction clean: .NET consumers depend on an application contract, not on FastAPI implementation details. It also preserves the existing public API surface while enabling internal dual-stack execution.

### React Frontend

The React frontend received a new feature module for Broker Insights and two page-level integrations. The dedicated leaderboard page gives users a production overview, while the broker detail tab keeps broker-specific analytics close to existing broker workflows.

The app route, sidebar navigation, and Vite proxy were updated to expose the feature and route Python endpoint calls during local development. Existing React pages remain unchanged except for the broker detail extension point.

## What Was NOT Changed

- Existing C# domain ownership remains unchanged.
- Existing EF Core migrations were not modified.
- Existing database schema was not changed.
- Existing Authentik and Casbin security behavior remains intact.
- Existing public CRM API contracts remain stable.
- Existing React pages were not redesigned.
- Existing submission, policy, renewal, workflow, and timeline behavior remains untouched.
- Python does not become the system of record for C# owned aggregates.

## Validation Summary

| Area | Checks | Result |
| --- | --- | --- |
| Phase 0 foundation | Docker service startup, health checks, internal service availability | Passed |
| Phase 0 bridge | C# application contract and HTTP bridge compilation | Passed |
| Phase 0 logging | Structured JSON logging available in the sidecar | Passed |
| Phase 0 database access | Async read-only SQLAlchemy connectivity verified | Passed |
| Phase 1 APIs | Scorecard, trends, and leaderboard endpoints return live data | Passed |
| Phase 1 frontend route | `/broker-insights` renders successfully | Passed |
| Phase 1 proxy | Vite forwards Broker Insights API calls to the Python sidecar | Passed |
| Phase 1 broker detail | Insights tab renders scorecard and trend content | Passed |
| Phase 1 charting | Pure SVG trend chart renders without feature-owned console errors | Passed |
| Phase 1 type safety | TypeScript validation completed successfully | Passed |
