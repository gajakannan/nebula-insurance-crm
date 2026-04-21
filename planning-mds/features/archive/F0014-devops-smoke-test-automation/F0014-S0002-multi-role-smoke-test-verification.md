# F0014-S0002 — Multi-Role Smoke Test Verification

**Story ID:** F0014-S0002
**Feature:** F0014 — DevOps Smoke Test Automation
**Title:** Multi-role smoke test verification
**Priority:** High
**Phase:** Infrastructure

## User Story

**As a** DevOps engineer verifying a Nebula deployment
**I want** smoke tests to run as each seeded dev user and verify role-specific access boundaries
**So that** I can confirm that ABAC enforcement works correctly for all roles without manually testing each user

## Context & Background

F0014-S0001 delivered a smoke test that runs as a single user (default: `lisa.wong` / DistributionUser). While the `--user` flag allows manual per-user execution, there is no automated multi-role verification that confirms:

- Each seeded dev user can acquire a JWT
- Each user's `nebula_roles` claim matches expectations
- Role-specific access boundaries are enforced (e.g., `broker001` / BrokerUser is denied Task CRUD per Casbin `ExternalUser` deny-all policy)

This story adds a `--all-users` mode that iterates over all seeded dev users, runs role-appropriate test assertions, and reports a unified pass/fail summary.

## Acceptance Criteria

**Happy Path — All Users Pass:**
- **Given** a running Nebula stack with all dev users provisioned by the authentik blueprint
- **When** I run `./scripts/smoke-test.sh --all-users`
- **Then** the script acquires JWTs for lisa.wong, john.miller, broker001, and akadmin, runs role-appropriate assertions for each, and exits with code 0

**Happy Path — Per-User Claims Verification:**
- **Given** a running stack
- **When** the script acquires a JWT for each user
- **Then** it verifies: `aud` equals `nebula`, `nebula_roles` contains the expected role(s) for that user, and `sub` is a non-empty string

**Happy Path — DistributionUser and Underwriter Access:**
- **Given** JWTs for lisa.wong (DistributionUser) and john.miller (Underwriter)
- **When** the script runs Task CRUD smoke tests for each
- **Then** both users can create, read, update, transition, and delete their own tasks (existing 9-test suite)

**Happy Path — BrokerUser Deny Boundary:**
- **Given** a JWT for broker001 (BrokerUser / ExternalUser)
- **When** the script attempts `POST /tasks` and `GET /my/tasks`
- **Then** the API returns 403 Forbidden for both requests (ExternalUser deny-all Casbin policy)

**Happy Path — Admin Access:**
- **Given** a JWT for akadmin (Admin)
- **When** the script runs Task CRUD smoke tests
- **Then** all operations succeed (Admin has full access)

**Alternative Flows / Edge Cases:**
- One user's token acquisition fails → script reports which user failed, continues with remaining users, exits with code 1
- Role claim mismatch → script reports expected vs actual roles, marks as failure
- `--all-users` combined with `--user` → `--all-users` takes precedence (ignores `--user`)

**Checklist:**
- [ ] `--all-users` flag added to smoke-test.sh
- [ ] Per-user expected role mapping defined in script (lisa.wong=DistributionUser, john.miller=Underwriter, broker001=BrokerUser, akadmin=Admin)
- [ ] JWT claims verification: `aud`, `nebula_roles`, `sub` for each user
- [ ] Role-appropriate test assertions: full CRUD for internal roles, 403 for BrokerUser
- [ ] Unified summary: per-user pass/fail counts + overall result
- [ ] Exit code: 0 only if all users pass all role-appropriate assertions

## Data Requirements

**Required Fields:**
- Dev user matrix: username → expected Casbin role(s) → expected API behavior

**Validation Rules:**
- `nebula_roles` claim must contain at least the expected primary role for each user
- BrokerUser maps to ExternalUser in Casbin, which has deny-all policy on all resources
- Admin role grants unrestricted access to all Task operations

## Role-Based Visibility

**Roles tested:**
- lisa.wong → DistributionUser — full Task CRUD expected
- john.miller → Underwriter — full Task CRUD expected
- broker001 → BrokerUser (ExternalUser) — 403 on all Task operations expected
- akadmin → Admin — full Task CRUD expected

**Data Visibility:**
- Same as S0001: direct DB access for timeline verification, JWT claim decoding for diagnostics

## Non-Functional Expectations

- Performance: Multi-role suite completes in < 2 minutes against a warm stack (4 users x ~30s each)
- Reliability: Failure in one user's tests does not abort remaining users

## Dependencies

**Depends On:**
- F0014-S0001 — Smoke test script infrastructure and blueprint fixes
- F0005 — authentik user provisioning and role claims
- Casbin policy — `ExternalUser` deny-all rule in `planning-mds/security/policies/policy.csv`

**Related Stories:**
- F0014-S0001 — Base smoke test script that this extends

## Out of Scope

- Testing Casbin policy changes (policy is owned by F0005/platform security)
- Testing role-specific data filtering (e.g., manager sees all tasks vs user sees own tasks — this is F0004 scope)
- Submission/Renewal role boundaries (depends on F0006/F0007)

## Questions & Assumptions

**Assumptions (to be validated):**
- broker001's `nebula_roles` claim maps to `BrokerUser` which resolves to `ExternalUser` in Casbin
- All four seeded dev users have app-password tokens provisioned by the blueprint

## Business Rules

1. **ExternalUser deny-all:** Casbin policy denies all resource access for `ExternalUser` role. BrokerUser maps to ExternalUser. Smoke test must verify this boundary returns 403.
2. **Role claim mapping:** Each user's `nebula_roles` JWT claim must contain their assigned role(s). The smoke test verifies this claim matches the expected role matrix.
3. **Isolation:** Each user's smoke test operations are independent. Tasks created by one user are not visible to or affected by another user's tests.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (verified via 403 assertions for BrokerUser)
- [ ] Audit/timeline logged (N/A — extends existing timeline verification)
- [ ] Tests pass (the story IS the test infrastructure)
- [ ] Documentation updated (README.md updated with `--all-users` usage)
- [ ] Story filename matches `Story ID` prefix (`F0014-S0002-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
