# F0009-S0004: BrokerUser Access Boundaries

**Story ID:** F0009-S0004
**Feature:** F0009 — Authentication + Role-Based Login
**Title:** Define and enforce BrokerUser access boundaries
**Priority:** Critical
**Phase:** Phase 1

## User Story

**As a** broker user
**I want** to sign in and see only broker-authorized data
**So that** I cannot access internal-only or cross-broker information

## Acceptance Criteria

- **Given** `broker001@example.local` signs in with `BrokerUser`
- **When** broker pages load
- **Then** only BrokerVisible routes/actions/data are available

- **Given** BrokerUser requests internal-only route/API
- **When** authorization evaluates
- **Then** access is denied and restricted payload is not returned

- **Given** BrokerUser requests data outside resolved broker scope
- **When** access evaluates
- **Then** response is deny-by-default

- **Given** BrokerUser data response includes mixed visibility fields
- **When** response is serialized
- **Then** all InternalOnly fields are omitted server-side

- **Given** broker linkage cannot be resolved
- **When** request is evaluated
- **Then** request is denied

## Scope Resolution Contract

- Scope anchor: authenticated `broker_tenant_id` claim (stable IdP-issued broker identity).
- Matching rule: exactly one active broker tenant mapping resolves from `broker_tenant_id`.
- Missing claim, unknown mapping, or ambiguous mapping (`0` or `>1`): deny all BrokerUser-protected resources.

## Authorization Contract

- `authorization-matrix.md` section 2.10 defines allowed BrokerUser resources/actions.
- `policy.csv` must include explicit BrokerUser rows matching the matrix.
- Default deny for any action/resource not explicitly allowed.

## Enforcement Order Contract

1. Query/service layer tenant isolation (cross-broker rows are filtered out before payload shaping).
2. Casbin ABAC resource/action allow-deny decision.
3. DTO/response filtering for `InternalOnly` field exclusion.

Note: DTO filtering is mandatory for field visibility but is not a substitute for tenant isolation.

## Field Boundary Contract

- `BROKER-VISIBILITY-MATRIX.md` is mandatory for field classification.
- If endpoint cannot satisfy required field filtering, endpoint remains denied for BrokerUser in Phase 1.

## Non-Functional Expectations

- Broker page load p95 <= 2s
- Zero known InternalOnly field exposure in BrokerUser responses
- Consistent behavior between direct API calls and UI paths

## Dependencies

- F0009-S0003 route guard behavior
- BrokerUser policy delta in matrix + policy.csv
- `BROKER-VISIBILITY-MATRIX.md`

## Definition of Done

- [ ] Scope linkage logic implemented and fail-closed
- [ ] BrokerUser policy rows implemented and parity-checked
- [ ] Query/service layer tenant isolation implemented using `broker_tenant_id`
- [ ] Field-level filtering implemented and tested server-side
- [ ] Cross-broker deny tests passing
