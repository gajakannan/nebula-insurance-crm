# F0033-S0002 — Activate Bruno API Validation Path

**Story ID:** F0033-S0002
**Feature:** F0033 — Structured Logging and QE Toolchain Activation
**Title:** Activate Bruno API validation path
**Priority:** High
**Phase:** Infrastructure

## User Story

**As a** quality engineer
**I want** Nebula to expose committed Bruno collections and a standard CLI execution path
**So that** representative API validation is repeatable in local development and CI without relying on hand-written curl commands

## Context & Background

Nebula already has shell-based smoke testing for a specific path, but the QE documentation expects a repo-native API collection workflow. Bruno is the approved tool, yet the solution currently has no committed collection structure, environment templates, or standardized report output.

This story activates the API-collection layer of the QE stack.

## Acceptance Criteria

**Happy Path:**
- **Given** a running Nebula stack
- **When** the Bruno entry point is executed
- **Then** a committed Bruno collection runs representative checks for health, auth, broker list, and task read flows
- **And** the collection uses environment variables or committed env templates instead of hard-coded secrets
- **And** the run produces machine-readable output suitable for CI artifact upload or local evidence review
- **And** failing requests produce actionable request/response details without requiring ad hoc manual reproduction

**Alternative Flows / Edge Cases:**
- CI environment needs different base URLs or credentials → environment selection is parameterized, not duplicated across collections
- A representative endpoint is temporarily unavailable → the run fails clearly with request context and exit code, not with a silent partial pass
- Later features add more API surfaces → this story only needs the first representative collection structure, not full endpoint parity

**Checklist:**
- [ ] Bruno collection folder committed to the repo
- [ ] Local and CI environment templates committed
- [ ] A repo-standard execution script or command exists
- [ ] Representative API requests cover health, auth, broker list, and task read validation
- [ ] Report output path is explicit and documented

## Data Requirements

**Required Fields:**
- API base URL
- Auth username
- Auth token or token-key input
- Report output directory

**Optional Fields:**
- CI-specific environment selector
- Collection subset selector

**Validation Rules:**
- Committed environment templates must not contain secrets
- Representative requests must assert status code and key response-shape expectations
- CI must be able to fail the build on a Bruno collection failure

## Role-Based Visibility

**Roles that can approve or operate this story:**
- Quality Engineer
- DevOps
- Backend Developer
- Code Reviewer

**Data Visibility:**
- InternalOnly content: collection execution details and auth variables
- ExternalVisible content: none

## Non-Functional Expectations

- Operability: the Bruno path should be simple enough for developers and QE to run without bespoke local setup
- Determinism: the same collection should be usable in local and CI contexts with explicit env selection
- Maintainability: representative requests should be organized by resource and remain readable in version control

## Dependencies

**Depends On:**
- Existing Nebula API endpoints and dev-auth model

**Related Stories:**
- F0014-S0001 — Existing smoke-test auth/bootstrap assumptions remain a useful reference
- F0033-S0004 — Pact adds a deeper contract layer for one representative slice

## Out of Scope

- Full API parity across every Nebula endpoint
- Replacing the existing smoke-test shell suite
- Third-party SaaS API collection hosting

## Questions & Assumptions

**Open Questions:**
- [ ] Should the initial Bruno collection authenticate directly against authentik every run, or should it optionally accept a pre-fetched bearer token for faster local iteration?

**Assumptions (to be validated):**
- Broker list and task read flows are stable enough to serve as representative API validation coverage

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (N/A — internal tooling)
- [ ] Audit/timeline logged (N/A)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F{NNNN}-S{NNNN}-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
