# Polyglot Service Governance

**Status:** Active guidance
**Owner:** Architect
**Applies to:** Future backend architecture planning
**Harness source:** `nebula-agents` Architect and Backend Developer dual-stack guidance

## Purpose

Nebula CRM is a .NET-first product that may later add Python bounded services when an ADR proves the service boundary and stack choice. This document makes the product ready for future .NET/Python planning without creating a service, feature, migration, or runtime change.

## Current Baseline

- Default backend runtime: `.NET / ASP.NET Core / EF Core` in `engine/`.
- Current architecture style: modular monolith with strict module boundaries.
- Existing Python surfaces: `neuron/` and repo tooling, not product backend services.
- Future Python backend services are allowed only after architecture approval.

## Stack Assignment Rules

Every future backend service or extracted bounded context must declare:

| Field | Required Decision |
|-------|-------------------|
| Service name | Stable runtime/service identifier |
| Bounded context | Business capability owned by the service |
| Stack | `.NET`, `Python`, or other approved stack |
| Storage owner | Database/schema/store owned exclusively by the service |
| Public contracts | REST/OpenAPI, events, datasets, or gRPC contracts |
| Authorization model | Whether the service enforces or consumes CRM authorization |
| Audit owner | Which component creates immutable timeline/audit records |
| Observability | Logs, traces, metrics, health, readiness |
| ADR reference | Accepted ADR documenting the decision |

## Stack Selection Criteria

Use `.NET` by default when:

- The service owns core CRM aggregates or transactional workflows.
- Existing domain model and EF Core persistence patterns dominate.
- The implementation depends on current CRM authorization/audit machinery.
- Team delivery speed favors existing `engine/` patterns.

Use Python only when the ADR demonstrates one or more of:

- Data processing, parsing, ML, AI, ETL, or scientific-library advantage.
- Async/batch processing that is naturally isolated from CRM aggregates.
- Independent deployability or scaling requirements.
- A bounded context that can own its data without shared database access.

## Required ADR Sections

Any non-default stack decision must include:

- why the modular monolith is insufficient
- why the selected stack is materially better
- service ownership and data sovereignty
- contract shape and compatibility strategy
- authorization and audit boundaries
- failure, replay, and rollback behavior
- test and observability requirements

## Prohibited Patterns

- Creating a Python service without ADR-backed stack assignment.
- Sharing CRM database tables across service boundaries.
- Moving aggregate ownership by implementation convenience.
- Allowing a service to bypass CRM authorization or audit rules.
- Publishing domain events directly without an outbox or equivalent reliable delivery pattern.

