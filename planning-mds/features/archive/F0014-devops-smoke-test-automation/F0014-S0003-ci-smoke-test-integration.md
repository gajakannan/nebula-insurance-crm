# F0014-S0003 — CI Smoke Test Integration

**Story ID:** F0014-S0003
**Feature:** F0014 — DevOps Smoke Test Automation
**Title:** CI smoke test integration
**Priority:** Medium
**Phase:** Future

## User Story

**As a** development team member
**I want** smoke tests to run automatically in CI on every push to main and on pull requests
**So that** auth-layer regressions, API breakages, and blueprint drift are caught before code reaches the main branch

## Context & Background

F0014-S0001 delivered local smoke test scripts and F0014-S0002 adds multi-role coverage. However, these only run when a developer or DevOps agent explicitly invokes them. There is no automated gate preventing broken deployments from merging.

This story integrates the smoke test suite into the CI/CD pipeline (GitHub Actions) so that:
- Every PR gets a smoke test gate before merge
- Pushes to main trigger a verification run
- Failures block the merge and produce actionable logs

## Acceptance Criteria

**Happy Path — PR Gate:**
- **Given** a pull request is opened or updated against main
- **When** the CI pipeline runs
- **Then** the smoke test workflow starts the Nebula stack via docker compose, runs `./scripts/smoke-test.sh --all-users`, and reports pass/fail as a GitHub check

**Happy Path — Main Branch Verification:**
- **Given** a commit is pushed to main
- **When** the CI pipeline runs
- **Then** the smoke test suite runs against a clean stack and results are reported

**Happy Path — Failure Reporting:**
- **Given** a smoke test fails during CI
- **When** the workflow completes
- **Then** the GitHub check is marked as failed, the failing test name and HTTP response are visible in the CI log, and the PR cannot be merged (branch protection)

**Alternative Flows / Edge Cases:**
- Docker compose service fails to start within timeout → workflow fails with infra error (exit code 2), logs include service status and last 15 log lines
- authentik blueprint application is slow → workflow uses adequate wait time (same as dev-reset.sh)
- Concurrent CI runs → each run uses isolated docker compose project names to avoid port/volume conflicts

**Checklist:**
- [ ] GitHub Actions workflow file created (`.github/workflows/smoke-test.yml`)
- [ ] Workflow triggers: `pull_request` (main), `push` (main)
- [ ] Workflow starts docker compose stack in CI runner
- [ ] Workflow runs `./scripts/smoke-test.sh --all-users`
- [ ] Workflow reports exit code as GitHub check status
- [ ] CI logs include full smoke test output (per-test pass/fail, summary)
- [ ] Concurrent run isolation via unique docker compose project names

## Data Requirements

**Required Fields:**
- CI runner: docker compose, bash, curl, python3 available
- GitHub Actions secrets: none required (dev credentials are committed, not production)

**Validation Rules:**
- CI runner must have sufficient resources to run PostgreSQL, authentik, and the Nebula API simultaneously
- Workflow timeout should be set to prevent hung runs (suggested: 15 minutes)

## Role-Based Visibility

**Roles that see CI results:**
- All team members with repository access can view CI check status
- PR authors see pass/fail gate on their pull requests

## Non-Functional Expectations

- Performance: Full CI smoke run (stack startup + all-users test) completes in < 10 minutes
- Reliability: Flaky failures due to timing should be mitigated by adequate health-check polling (inherited from dev-reset.sh)
- Cost: CI minutes consumed per run should be documented for budget awareness

## Dependencies

**Depends On:**
- F0014-S0001 — Smoke test scripts
- F0014-S0002 — Multi-role smoke test verification (`--all-users` mode)
- GitHub Actions — CI/CD platform

**Related Stories:**
- F0014-S0001 — Scripts executed by CI
- F0014-S0002 — Multi-role mode used by CI

## Out of Scope

- Production deployment smoke tests (different network, credentials, and access model)
- Performance/load testing in CI (k6 — separate concern)
- Frontend E2E tests in CI (Playwright — F0015 scope)
- Notification/alerting on CI failures (GitHub native notifications suffice)

## Questions & Assumptions

**Open Questions:**
- [ ] Which CI runner size is needed to run the full docker compose stack? (PostgreSQL + authentik + API)
- [ ] Should smoke tests run on every commit or only on PR events? (Proposed: both, with main-branch runs being informational)
- [ ] Is docker compose available on the CI runner, or does it need to be installed as a step?

**Assumptions (to be validated):**
- GitHub Actions runners have sufficient memory and disk for the Nebula stack
- Docker compose v2 is available or installable on the CI runner
- Port conflicts between concurrent CI runs can be resolved via `COMPOSE_PROJECT_NAME` isolation

## Business Rules

1. **Exit code propagation:** CI workflow must propagate smoke-test.sh exit codes to GitHub check status. Exit 0 = check pass, exit 1 or 2 = check fail.
2. **Branch protection:** Smoke test check should be added to branch protection rules for main, blocking merge on failure.
3. **No secrets in scripts:** Dev credentials (`nebula-dev-token`) are committed in the blueprint and scripts. This is acceptable for dev/CI but must never be used for production verification.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (N/A — CI infrastructure)
- [ ] Audit/timeline logged (N/A)
- [ ] Tests pass (the CI workflow IS the test gate)
- [ ] Documentation updated (README.md updated with CI badge and workflow reference)
- [ ] Story filename matches `Story ID` prefix (`F0014-S0003-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
