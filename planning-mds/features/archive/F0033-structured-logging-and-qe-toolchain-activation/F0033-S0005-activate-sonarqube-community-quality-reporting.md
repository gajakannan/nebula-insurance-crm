# F0033-S0005 — Activate SonarQube Community Quality Reporting

**Story ID:** F0033-S0005
**Feature:** F0033 — Structured Logging and QE Toolchain Activation
**Title:** Activate SonarQube Community quality reporting
**Priority:** High
**Phase:** Infrastructure

## User Story

**As a** release approver or code reviewer
**I want** Nebula to run SonarQube Community with imported backend and frontend coverage
**So that** code-quality review has a repo-native quality-reporting layer instead of relying only on ad hoc local inspection

## Context & Background

The approved testing stack includes SonarQube Community, but Nebula does not yet expose a scanner path, service runtime, or coverage import wiring. Without that, the repo has no first-class code-quality reporting layer spanning both the .NET backend and React frontend.

This story activates that reporting baseline using OSS-compatible infrastructure.

## Acceptance Criteria

**Happy Path:**
- **Given** the approved SonarQube execution path
- **When** the Sonar analysis entry point runs
- **Then** backend and frontend coverage artifacts are imported into a single solution analysis
- **And** the server/runtime path is self-hosted or otherwise OSS-compatible
- **And** the quality gate or report criteria are explicit and documented
- **And** CI can surface analysis results and fail when the configured quality gate fails

**Alternative Flows / Edge Cases:**
- SonarQube is not running locally → local scripts fail clearly and instruct the user how to start the QE overlay
- Legacy code causes noisy initial analysis → the gate may start with a pragmatic baseline, but the rule set must remain explicit and versioned
- Coverage files move or formats change → report paths are centralized in one scanner entry point

**Checklist:**
- [ ] SonarQube Community runtime path documented and automated
- [ ] Repo-standard scanner script committed
- [ ] Backend coverage import wired
- [ ] Frontend coverage import wired
- [ ] CI analysis/reporting path committed

## Data Requirements

**Required Fields:**
- SonarQube server URL
- Project key / project name
- Backend coverage report path
- Frontend coverage report path

**Optional Fields:**
- Quality gate token
- Branch / pull-request metadata

**Validation Rules:**
- Coverage inputs must come from executable repo commands
- Quality criteria must be explicit, not implied by tribal knowledge
- Solution activation must remain compatible with SonarQube Community, not enterprise-only features

## Role-Based Visibility

**Roles that can approve or operate this story:**
- DevOps
- Quality Engineer
- Code Reviewer
- Backend Developer
- Frontend Developer

**Data Visibility:**
- InternalOnly content: analysis server and quality report details
- ExternalVisible content: none

## Non-Functional Expectations

- Operability: the scan path should be runnable locally and in CI with the same repo configuration
- Maintainability: scanner/report paths should live in one script or config surface, not be duplicated across workflows
- Pragmatism: initial gate severity may be scoped to avoid freezing legacy work, but it must still produce usable quality signals

## Dependencies

**Depends On:**
- Existing backend and frontend test coverage outputs

**Related Stories:**
- F0033-S0001 — backend observability baseline
- F0033-S0003 — frontend runtime quality activation
- F0033-S0004 — contract validation becomes another quality signal alongside Sonar analysis

## Out of Scope

- SonarQube Enterprise features
- Rewriting legacy code solely to satisfy the initial analysis baseline
- Replacing existing linters, tests, or smoke workflows

## Questions & Assumptions

**Open Questions:**
- [ ] Should the initial quality gate block on new-code conditions only, or on whole-repo conditions from day one?

**Assumptions (to be validated):**
- Existing backend and frontend coverage outputs can be imported without introducing a separate coverage feature first

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (N/A — internal tooling)
- [ ] Audit/timeline logged (N/A)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F{NNNN}-S{NNNN}-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
