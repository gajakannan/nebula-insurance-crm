# F0009 — Broker Visibility Matrix (Phase 1)

Purpose: define required field-level response boundaries for `BrokerUser`.

## Classification Rules

- `BrokerVisible`: may be returned to BrokerUser.
- `InternalOnly`: must not be returned to BrokerUser.
- If classification is unknown for a field, treat as `InternalOnly`.

## Broker Resource

| Field | Classification | Notes |
|-------|----------------|-------|
| `id` | BrokerVisible | Required for routing/detail navigation. |
| `legalName` | BrokerVisible | |
| `licenseNumber` | BrokerVisible | |
| `state` | BrokerVisible | |
| `status` | BrokerVisible | |
| `email` | BrokerVisible | |
| `phone` | BrokerVisible | |
| `rowVersion` | InternalOnly | Concurrency token not exposed to BrokerUser in Phase 1. |
| `isDeactivated` | InternalOnly | Internal lifecycle metadata. |

## Contact Resource

| Field | Classification | Notes |
|-------|----------------|-------|
| `id` | BrokerVisible | |
| `brokerId` | BrokerVisible | |
| `name` | BrokerVisible | |
| `title` | BrokerVisible | |
| `email` | BrokerVisible | |
| `phone` | BrokerVisible | |
| `rowVersion` | InternalOnly | Concurrency token not exposed to BrokerUser in Phase 1. |
| Internal assignment/ownership metadata | InternalOnly | Any non-contract metadata fields are denied. |

## Timeline Event Resource

| Field | Classification | Notes |
|-------|----------------|-------|
| `id` | BrokerVisible | |
| `entityType` | BrokerVisible | |
| `entityId` | BrokerVisible | |
| `eventType` | BrokerVisible | Only approved event types listed below. |
| `brokerDescription` | BrokerVisible | Broker-safe public description. Populated at event creation by the domain layer using predefined templates. Never contains internal staff identifiers, system references, or policy metadata. NULL for InternalOnly event types — these events are excluded from BrokerUser responses entirely. |
| `eventDescription` | InternalOnly | Full internal description. Never returned to BrokerUser. |
| `occurredAt` | BrokerVisible | |
| `actorDisplayName` | BrokerVisible | |
| `actorUserId` | InternalOnly | |

### BrokerDescription Template Ownership

`BrokerDescription` is populated by the domain service at event creation time — not derived or filtered post-hoc from `EventDescription`. Each approved event type has a fixed template:

| EventType | BrokerDescription Template |
|-----------|---------------------------|
| `BrokerCreated` | `"Broker record created."` |
| `BrokerUpdated` | `"Broker profile updated."` |
| `BrokerStatusChanged` | `"Broker status changed to {newStatus}."` |
| `ContactAdded` | `"Contact added to broker."` |
| `ContactUpdated` | `"Contact record updated."` |

Templates must not include: internal user names, user IDs, system references, policy codes, or any field classified InternalOnly. Template additions require Security Agent approval.

### Approved BrokerUser Timeline Event Types (Phase 1)

- `BrokerCreated`
- `BrokerUpdated`
- `BrokerStatusChanged`
- `ContactAdded`
- `ContactUpdated`

All other event types are InternalOnly for BrokerUser. Events with InternalOnly types must be excluded from BrokerUser query results entirely — not returned with a null `brokerDescription`.

## Dashboard Resources

Three dashboard sub-resources exist. Each is classified independently for BrokerUser Phase 1.

### dashboard_kpi — GET /dashboard/kpis

**Decision: DENY for BrokerUser (Phase 1).**

Rationale: The `DashboardKpis` response shape contains four fields. Three of the four (`openSubmissions`, `renewalRate`, `avgTurnaroundDays`) are aggregates derived entirely from submission and renewal data, both of which are DENY resources for BrokerUser. Returning only `activeBrokers` to BrokerUser would require a new endpoint shape or conditional field suppression. Because the existing endpoint shape cannot be safely filtered without a new contract, the endpoint is denied in Phase 1 rather than returning a misleading partial response.

| Field | Classification | Notes |
|-------|----------------|-------|
| `activeBrokers` | InternalOnly (endpoint denied) | Would be BrokerVisible in isolation, but endpoint shape cannot be safely filtered in Phase 1. |
| `openSubmissions` | InternalOnly | Submission aggregate — DENY resource for BrokerUser. |
| `renewalRate` | InternalOnly | Renewal aggregate — DENY resource for BrokerUser. |
| `avgTurnaroundDays` | InternalOnly | Submission/renewal aggregate — DENY resource for BrokerUser. |

### dashboard_pipeline — GET /dashboard/pipeline

**Decision: DENY for BrokerUser (Phase 1).**

Rationale: The `DashboardOpportunities` (pipeline) response is composed entirely of submission and renewal status-count arrays. No field in this response can be returned to BrokerUser without exposing submission or renewal data. No filtering path exists within the current response shape.

| Field | Classification | Notes |
|-------|----------------|-------|
| `submissions[]` | InternalOnly | Submission status counts — DENY resource for BrokerUser. |
| `renewals[]` | InternalOnly | Renewal status counts — DENY resource for BrokerUser. |

### dashboard_nudge — GET /dashboard/nudges

**Decision: ALLOW (read) for BrokerUser with mandatory server-side scope filter.**

Rationale: The `NudgeCard` response shape itself contains no InternalOnly fields. The InternalOnly protection for this resource operates at the nudge type filter level, not at the field level. The backend must apply a server-side filter to restrict the returned nudge cards to `OverdueTask` type only, further constrained to broker-linked entities within the authenticated BrokerUser's resolved broker scope.

**Mandatory server-side filter rule (all conditions must be satisfied):**
1. `nudgeType = 'OverdueTask'`
2. `linkedEntityType = 'Broker'`
3. `linkedEntityId IN (broker IDs resolved from broker_tenant_id scope)`
4. `StaleSubmission` and `UpcomingRenewal` nudge types must be excluded entirely.

If the resolved broker scope is empty, the endpoint must return an empty array — not 403. Casbin has permitted the resource; scope enforcement is at the query layer.

| Field | Classification | Notes |
|-------|----------------|-------|
| `nudgeType` | BrokerVisible | After server-side filter, will only ever be `'OverdueTask'` for BrokerUser responses. |
| `title` | BrokerVisible | Broker-safe nudge title. |
| `description` | BrokerVisible | Broker-safe nudge description. |
| `linkedEntityType` | BrokerVisible | After filter, always `'Broker'` for BrokerUser responses. |
| `linkedEntityId` | BrokerVisible | Broker entity ID within the authenticated user's resolved scope. |
| `linkedEntityName` | BrokerVisible | Broker entity display name. |
| `urgencyValue` | BrokerVisible | Numeric urgency value used for ordering. |
| `ctaLabel` | BrokerVisible | Call-to-action label string. |

No NudgeCard field is InternalOnly. The InternalOnly protection is enforced at the nudge type filter level (excluding `StaleSubmission` and `UpcomingRenewal`), not by field suppression within the card schema.

## Dashboard/Task Responses (General Rule)

- Only summary fields needed for broker workspace rendering are BrokerVisible.
- Any internal assignment metadata, policy diagnostics, or internal staff identifiers are InternalOnly.
- If an existing response shape cannot be safely filtered, endpoint remains denied for BrokerUser.

## Enforcement

- Enforcement order is mandatory:
  1. tenant-isolated query/service filtering by resolved broker tenant scope
  2. Casbin ABAC resource/action allow-deny decision
  3. server-side DTO/response field filtering by visibility class
- Field filtering is server-side mandatory for BrokerUser.
- Frontend-only filtering does not satisfy this requirement.
- Verification must include response payload assertions in integration tests.
