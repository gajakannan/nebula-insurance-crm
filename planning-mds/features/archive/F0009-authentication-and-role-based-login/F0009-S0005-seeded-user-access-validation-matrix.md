# F0009-S0005: Seeded User Access Validation Matrix

**Story ID:** F0009-S0005
**Feature:** F0009 — Authentication + Role-Based Login
**Title:** Provide seeded user identities and validate role-specific login outcomes
**Priority:** High
**Phase:** Phase 1

## User Story

**As a** QA or reviewer
**I want** deterministic seeded identities with expected role outcomes
**So that** login and authorization can be validated repeatedly

## Acceptance Criteria

- **Given** environment bootstrap completes
- **When** identity provisioning runs
- **Then** these users can authenticate with expected roles:
  - `lisa.wong@nebula.local` -> `DistributionUser`
  - `john.miller@nebula.local` -> `Underwriter`
  - `broker001@example.local` -> `BrokerUser`

- **Given** each seeded user signs in
- **When** route resolution completes
- **Then** landing routes and nav behavior match F0009 contracts

- **Given** each seeded user calls out-of-scope APIs
- **When** authorization evaluates
- **Then** deterministic `403` ProblemDetails is returned

- **Given** BrokerUser requests cross-broker or unmapped data
- **When** access evaluates
- **Then** request is denied by default

- **Given** seeding runs repeatedly
- **When** re-applied
- **Then** provisioning is idempotent (no duplicates/drift)

## Provisioning Contract

- authentik blueprint/seeding must include:
  - `BrokerUser` group
  - membership mapping for all three seeded users
  - `nebula_roles` claim mapping
  - `broker_tenant_id` claim mapping for BrokerUser
- Seed identity data must not include plaintext production secrets.

## Validation Matrix Contract

Validation artifact must include per-user checks for:

1. Login success and expected landing route.
2. Allowed route/API access.
3. Denied route/API access.
4. BrokerUser cross-scope deny checks.
5. BrokerUser InternalOnly field exclusion checks.
6. BrokerUser missing/invalid `broker_tenant_id` deny checks.

## Non-Functional Expectations

- Full seeded validation run <= 10 minutes in local dev
- Deterministic role mappings across reruns

## Dependencies

- F0009-S0001 through F0009-S0004
- authentik blueprint/seeding workflow

## Definition of Done

- [ ] Seed identities provisioned idempotently
- [ ] Validation matrix documented and executable
- [ ] Role/route/API allow-deny outcomes verified
- [ ] BrokerUser boundary checks pass
