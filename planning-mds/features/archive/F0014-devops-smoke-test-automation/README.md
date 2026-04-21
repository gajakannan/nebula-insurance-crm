# F0014 — DevOps Smoke Test Automation

**Status:** Done
**Priority:** High
**Phase:** Infrastructure

## Overview

Automated smoke tests and a one-command environment reset workflow for Nebula CRM. Eliminates the manual curl/inspect verification cycle that burned 30-60 minutes per feature verification. Includes authentik blueprint corrections for ROPC dev authentication, a 9-test API smoke suite, multi-role ABAC boundary verification, and a CI merge gate workflow.

## Documents

| Document | Purpose |
|----------|---------|
| [PRD.md](./PRD.md) | Full product requirements (why + what + how) |
| [STATUS.md](./STATUS.md) | Completion checklist and progress tracking |
| [GETTING-STARTED.md](./GETTING-STARTED.md) | Developer/agent setup guide |
| [feature-assembly-plan.md](./feature-assembly-plan.md) | Implementation execution plan (S0002 + S0003) |

## Stories

| ID | Title | Priority | Phase | Status |
|----|-------|----------|-------|--------|
| [F0014-S0001](./F0014-S0001-blueprint-ropc-fixes-and-smoke-scripts.md) | Blueprint ROPC fixes and smoke test scripts | Critical | Infrastructure | Done |
| [F0014-S0002](./F0014-S0002-multi-role-smoke-test-verification.md) | Multi-role smoke test verification | High | Infrastructure | Done |
| [F0014-S0003](./F0014-S0003-ci-smoke-test-integration.md) | CI smoke test integration | Medium | Infrastructure | Done |

**Total Stories:** 3
**Completed:** 3 / 3

## Architecture Review (2026-03-27)

**Phase B status:** Complete — no new entities, APIs, workflows, or Casbin policies. Infrastructure/scripts feature.

**Execution Plan:** [`feature-assembly-plan.md`](./feature-assembly-plan.md)

### Key Findings

1. **S0002 BrokerUser expectation is incorrect.** The acceptance criteria claims broker001 gets 403 on both `POST /tasks` and `GET /my/tasks`. However, `policy.csv` line 382 grants BrokerUser `task:read`. Correct behavior: `GET /my/tasks` returns 200 OK, `POST/PUT/DELETE` returns 403. The implementation uses the corrected assertions per the assembly plan.

2. **No application code changes required.** F0014 is entirely shell scripts and CI workflow configuration. No backend, frontend, or AI scope.

3. **CI runner resource concern (S0003).** GitHub Actions `ubuntu-latest` has 7 GB RAM. The workflow starts only the 4 services smoke tests exercise (`db`, `authentik-server`, `authentik-worker`, `api`), skipping `temporal` and `temporal-ui`.

### Architecture Artifacts

| Artifact | Status |
|----------|--------|
| Data model / ERD | N/A — no entity changes |
| API contract (OpenAPI) | N/A — no new endpoints |
| Workflow state machine | N/A — no new workflows |
| Casbin policy | N/A — no policy changes (scripts test existing boundaries) |
| JSON schemas | N/A — no new request/response models |
| C4 diagrams | N/A — no container changes |
| ADRs | None required — no architectural decisions with alternatives to evaluate |
| Assembly plan | [`feature-assembly-plan.md`](./feature-assembly-plan.md) |

## Usage

### Single-User Mode (S0001)

```bash
./scripts/smoke-test.sh                   # Default user (lisa.wong)
./scripts/smoke-test.sh --user john.miller # Specific user
./scripts/smoke-test.sh --reset           # Tear down + rebuild first
```

### Multi-Role Mode (S0002)

```bash
./scripts/smoke-test.sh --all-users       # All 4 dev users with role assertions
```

Runs role-appropriate test suites for each seeded dev user:

| User | Role | Suite | Tests |
|------|------|-------|-------|
| lisa.wong | DistributionUser | Full CRUD | 9 |
| john.miller | Underwriter | Full CRUD | 9 |
| broker001 | BrokerUser | Read-only | 4 |
| akadmin | Admin | Full CRUD | 9 |

### CI Gate (S0003)

The `.github/workflows/smoke-test.yml` workflow runs `--all-users` on every PR and push to main. Add `smoke-test` as a required status check in branch protection settings.
