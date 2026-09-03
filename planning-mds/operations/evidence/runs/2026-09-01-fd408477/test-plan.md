# Test Plan — F0040

## Scenarios

- Engine Broker internal-only role/scope matrix, filtered-before-count, newest-20 ordering, legal name, stored description, and Unknown User fallback.
- Neuron Broker head fixed query, mapping, cap, empty/error/auth handling, digest-only provenance, shared executor timeout/isolation, component ownership/props, and outcome telemetry.
- Deterministic and structured direct routing for unqualified Broker activity; no-tool rejection for filters, writes, unknown/inactive/cross-domain routes; owner-scoped persistence/replay.
- Frontend registered component/schema drift, semantic list/detail/link rendering, safe fallback, live status, and Broker-zone retry.
- Runtime rebuild/readiness and existing Renewals/Tasks/Pipeline compatibility.

## Acceptance

All feature-scoped tests and runtime checks must pass. Unrelated full-suite/environment failures are recorded as recommendations and cannot be silently reclassified as passes.
