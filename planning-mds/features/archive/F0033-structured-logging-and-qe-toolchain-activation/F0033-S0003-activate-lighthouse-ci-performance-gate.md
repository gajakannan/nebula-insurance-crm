# F0033-S0003 — Activate Lighthouse CI Performance Gate

**Story ID:** F0033-S0003
**Feature:** F0033 — Structured Logging and QE Toolchain Activation
**Title:** Activate Lighthouse CI performance gate
**Priority:** High
**Phase:** Infrastructure

## User Story

**As a** release approver or frontend engineer
**I want** Nebula to run Lighthouse CI against selected frontend routes with committed thresholds and reports
**So that** frontend performance and accessibility regressions are visible in the same repo-native validation flow as other quality checks

## Context & Background

Nebula already has frontend linting, tests, and visual smoke coverage, but the documented performance layer is still missing from the solution runtime. Lighthouse CI is the approved tool, yet the repo has no committed configuration, route list, or evidence/report path.

This story activates the frontend performance gate in a way that respects Nebula's auth constraints.

## Acceptance Criteria

**Happy Path:**
- **Given** the approved Lighthouse runtime profile
- **When** the Lighthouse entry point runs
- **Then** Nebula audits a committed route set that includes `/login` plus representative protected routes such as `/` and `/brokers`
- **And** threshold values are versioned in the repo
- **And** HTML/JSON artifacts are produced for review
- **And** CI can fail when committed thresholds regress

**Alternative Flows / Edge Cases:**
- Protected routes require non-production auth assistance → the runtime profile is explicit, isolated to performance validation, and does not weaken the production auth-mode guard
- A route is temporarily too unstable for performance gating → the route set may be narrowed deliberately, but the change must remain explicit in committed config
- The dev server is used instead of a production build for protected-route audits → that distinction is documented and intentional

**Checklist:**
- [ ] Lighthouse CI configuration file committed
- [ ] Route list and thresholds committed
- [ ] Repo-standard execution script or package script exists
- [ ] Artifact output path is explicit
- [ ] CI workflow or gate path exists
- [ ] Production auth guard remains intact

## Data Requirements

**Required Fields:**
- Route URLs to audit
- Threshold values per audit category
- Artifact output directory

**Optional Fields:**
- Dedicated performance-only auth/runtime profile
- Route grouping by public vs protected surface

**Validation Rules:**
- Performance validation must not require disabling production auth safeguards for production builds
- Reports must be reviewable after local or CI execution
- Threshold failures must be machine-detectable by CI

## Role-Based Visibility

**Roles that can approve or operate this story:**
- Frontend Developer
- Quality Engineer
- DevOps
- Code Reviewer
- Security Reviewer

**Data Visibility:**
- InternalOnly content: perf-runtime profile details and report artifacts
- ExternalVisible content: none

## Non-Functional Expectations

- Determinism: the chosen runtime path must be repeatable enough to make threshold changes meaningful
- Security: any auth bypass used for performance-only runs must stay isolated from production builds
- Maintainability: the route set should be representative but intentionally small at first

## Dependencies

**Depends On:**
- Existing frontend app and route structure
- Existing F0015 frontend-quality foundations

**Related Stories:**
- F0033-S0005 — SonarQube will report on the broader code-quality surface while Lighthouse covers runtime perf/accessibility metrics

## Out of Scope

- Real-user monitoring or production analytics instrumentation
- Auditing every route in the app during the initial activation
- Replacing Playwright visual checks

## Questions & Assumptions

**Open Questions:**
- [ ] Should the first Lighthouse gate run on a Vite dev server with approved dev auth, or should it use a seeded authenticated browser session against a fuller stack?

**Assumptions (to be validated):**
- `/login`, `/`, and `/brokers` give enough first-pass coverage for the toolchain activation feature

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (N/A — internal tooling)
- [ ] Audit/timeline logged (N/A)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F{NNNN}-S{NNNN}-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
