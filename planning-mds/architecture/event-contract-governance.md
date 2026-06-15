# Event Contract Governance

**Status:** Active guidance
**Owner:** Architect
**Applies to:** Future events, projections, imports, exports, and service integrations
**Harness source:** `nebula-agents` event-driven and data-contract guidance

## Purpose

Nebula uses REST APIs for public/user-facing operations and may use events for cross-domain side effects, projections, integrations, and future bounded services. Event contracts must be versioned, replay-safe, and auditable.

## Contract Envelope

Future domain or integration events should include:

| Field | Purpose |
|-------|---------|
| `eventId` | Deduplication and replay identity |
| `eventType` | Stable event name |
| `eventVersion` | Schema version |
| `aggregateId` | Owning aggregate identifier when applicable |
| `aggregateType` | Owning aggregate type when applicable |
| `occurredAt` | UTC timestamp from the producer |
| `correlationId` | Cross-service/request trace |
| `causationId` | Prior event or command that caused this event |
| `tenantId` | Tenant/scope boundary where applicable |
| `payload` | Versioned event-specific body |

## Delivery Rules

- Mutating producers must use transactional outbox or an equivalent reliable publication pattern.
- Consumers must deduplicate by `eventId`.
- Consumers must tolerate unknown fields.
- Breaking payload changes require a new event version.
- Replay must not create duplicate timeline or audit entries.
- Poison messages require dead-letter or quarantine handling.

## Contract Formats

| Boundary | Preferred Contract |
|----------|--------------------|
| Public APIs | OpenAPI |
| Internal async domain events | Versioned event schemas / AsyncAPI when event families grow |
| Dataset-shaped import/export | ODCS-style dataset contract when warranted |
| Internal synchronous query | REST or gRPC only when freshness is required |

## Audit and Security

- Events are not a substitute for CRM audit records unless an ADR explicitly assigns audit ownership.
- Sensitive payload fields must be minimized and redacted in logs.
- Event contracts must state whether payloads contain PII, PHI-like data, financial data, or confidential broker/carrier data.
- Service identity and authorization context must be explicit for command-like callbacks.

## Relationship to ADR-015

ADR-015 remains the product foundation for canonical contracts, adapters, replay, and monitoring. This document provides reusable contract rules for future features that need event-driven architecture before or alongside the full integration hub.

