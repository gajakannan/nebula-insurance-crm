# Microservices Decision Framework

**Status:** Active guidance
**Owner:** Architect
**Applies to:** Future service extraction and distributed-system planning
**Harness source:** `nebula-agents` Architect microservices guidance

## Default Position

Nebula CRM defaults to a modular monolith. Microservices are justified only when a bounded context has clear forces that outweigh the operational cost of distribution.

## Decision Gate

Before approving a microservice, answer each question:

| Question | Required Evidence |
|----------|-------------------|
| What bounded context is being extracted? | Capability name, owner, upstream/downstream dependencies |
| Why is module-level separation insufficient? | Release, scale, runtime, persistence, or team-autonomy evidence |
| Who owns the data? | Database/schema/store boundary; no shared mutable tables |
| What contracts cross the boundary? | OpenAPI, event schema, dataset contract, or gRPC contract |
| How is consistency managed? | Transactional outbox, idempotency, replay, compensation |
| How is authorization handled? | Enforcer vs consumer boundary; service identity rules |
| How is audit handled? | Immutable audit owner and event/timeline policy |
| How is it operated? | Health, readiness, metrics, traces, logs, rollback |

## Architecture Outputs

When microservices are approved, the Architect must produce:

- accepted ADR for modular monolith vs microservice decision
- context map with service ownership
- communication topology
- event or API contract definitions
- data ownership model
- deployment topology guidance
- observability requirements
- rollback and fallback strategy

## Extraction Modes

| Mode | Use When | Notes |
|------|----------|-------|
| Modular monolith module | Most CRM capabilities | Preferred default |
| Bounded service extraction | Clear independent runtime/data/scale need | Requires ADR and contracts |
| Event-driven projection | Read model or reporting state crosses domains | Use outbox and replay |
| External integration boundary | Connector or batch/feed lifecycle differs from CRM | Align with ADR-015 |

## Approval Bar

Microservice approval requires:

- domain boundary is coarse and stable
- database ownership is exclusive
- contract versioning is explicit
- local development and CI validation are practical
- security, audit, and compliance boundaries are preserved
- failure modes do not block core CRM workflows without approved fallback

