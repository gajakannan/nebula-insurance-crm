# Deployment Topology Guidance

**Status:** Active guidance
**Owner:** Architect / DevOps
**Applies to:** Future service, worker, and integration runtime planning
**Harness source:** `nebula-agents` microservices deployment patterns

## Current Baseline

Nebula CRM currently runs as a product stack centered on:

- `.NET` backend in `engine/`
- React frontend in `experience/`
- `neuron/` AI layer where applicable
- PostgreSQL, Redis, authentik, and supporting services through Docker Compose

This document does not create new runtimes. It defines what future architecture plans must specify when additional services or workers are approved.

## Required Topology Decisions

Every future service or worker must define:

| Area | Required Detail |
|------|-----------------|
| Runtime | API, worker, scheduler, relay, or projection |
| Container | Dockerfile ownership and runtime base |
| Configuration | Environment variables and secret sources |
| Network | Internal-only vs public routing |
| Persistence | Owned database/schema/store |
| Health | Live and ready checks |
| Observability | Logs, metrics, traces |
| Scaling | Horizontal scaling assumptions |
| Rollback | Feature flag, fallback path, or deploy rollback |

## Health and Readiness

- Liveness answers whether the process should stay running.
- Readiness answers whether the service can handle work.
- Readiness must check critical dependencies such as database, broker, cache, identity, and required upstream APIs.

## Observability

Future services must emit:

- structured JSON logs
- correlation IDs across inbound and outbound calls
- OpenTelemetry traces where distributed calls exist
- metrics for request volume, latency, errors, retries, queue depth, and worker failures

## Deployment Strategy

Use the least distributed topology that satisfies the architecture decision:

| Need | Preferred Topology |
|------|--------------------|
| Standard CRM feature | Existing modular monolith |
| Background work inside CRM boundary | Hosted worker or scheduled job |
| Independent processing runtime | Separate worker/service after ADR |
| External integration with replay | Connector plus landing zone/outbox after ADR |
| High-risk rollout | Feature flag plus fallback path |

## Prohibited Patterns

- Shipping an independently deployed runtime without health/readiness checks.
- Introducing secrets outside approved environment/secret-management paths.
- Requiring local developers to run unrelated optional services for core CRM flows.
- Adding a service that cannot be disabled or rolled back independently.

