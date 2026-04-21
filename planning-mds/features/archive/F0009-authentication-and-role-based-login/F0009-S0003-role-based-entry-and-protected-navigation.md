# F0009-S0003: Role-Based Entry and Protected Navigation

**Story ID:** F0009-S0003
**Feature:** F0009 — Authentication + Role-Based Login
**Title:** Route users to role-appropriate entry points and enforce protected navigation
**Priority:** Critical
**Phase:** Phase 1

## User Story

**As a** signed-in user
**I want** navigation and landing routes aligned to my role
**So that** I only access authorized areas

## Acceptance Criteria

- **Given** sign-in as `lisa.wong@nebula.local` (`DistributionUser`)
- **When** callback bootstrap completes
- **Then** user lands on `/` and internal route permissions apply

- **Given** sign-in as `john.miller@nebula.local` (`Underwriter`)
- **When** callback bootstrap completes
- **Then** user lands on `/` and cannot access disallowed actions/resources

- **Given** sign-in as `broker001@example.local` (`BrokerUser`)
- **When** callback bootstrap completes
- **Then** user lands on `/brokers` and broker boundary rules apply

- **Given** authenticated user navigates to disallowed route
- **When** route guard evaluates access
- **Then** user is routed to `/unauthorized`

- **Given** API returns `403`
- **When** UI receives response
- **Then** permission-safe error state is shown in context (with `traceId` when available)

- **Given** API returns `401`
- **When** UI receives response
- **Then** local session is cleared and user is redirected to `/login`

## Route-to-Permission Contract

| Route | Requires Auth | Allowed Roles |
|-------|---------------|---------------|
| `/` | Yes | DistributionUser, Underwriter, DistributionManager, RelationshipManager, ProgramManager, Admin |
| `/brokers` | Yes | DistributionUser, Underwriter, DistributionManager, RelationshipManager, ProgramManager, Admin, BrokerUser |
| `/brokers/new` | Yes | Roles with `broker:create` only |
| `/brokers/:brokerId` | Yes | Roles with `broker:read` only |
| `/login` | No | All |
| `/auth/callback` | No (protocol route) | All |
| `/unauthorized` | Yes | All authenticated users |

## Implementation Contract

- Role claim source: `nebula_roles`
- Deterministic role precedence for multi-role users:
  - `Admin` > `DistributionManager` > `DistributionUser` > `Underwriter` > `BrokerUser`
- Client guards are UX-level; server-side authorization remains authoritative.

## Non-Functional Expectations

- Post-login route resolution <= 300ms client-side
- Guard behavior deterministic for direct URL and in-app nav

## Dependencies

- F0009-S0002
- Authorization matrix/policy parity for role/resource mapping

## Definition of Done

- [ ] Landing route logic implemented for required roles
- [ ] Guard behavior deterministic for unauthorized routes and API errors
- [ ] Route-to-permission mapping documented and tested
