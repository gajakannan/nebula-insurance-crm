# F0033-S0004 — Establish Broker List Contract Testing with Pact

**Story ID:** F0033-S0004
**Feature:** F0033 — Structured Logging and QE Toolchain Activation
**Title:** Establish broker list contract testing with Pact
**Priority:** High
**Phase:** Infrastructure

## User Story

**As a** frontend or backend engineer
**I want** Nebula to verify one representative consumer/provider contract with Pact
**So that** contract drift between the SPA and API is caught with executable evidence before it becomes a runtime regression

## Context & Background

Pact is already the approved contract-testing tool, but Nebula does not yet have a consumer test, provider verification path, or optional broker workflow in the repo. Trying to activate contract testing for the entire API at once would be too broad for the first slice.

Broker list is a good first contract because it already exists in both frontend and backend, is user-visible, and has a stable paginated read shape.

## Acceptance Criteria

**Happy Path:**
- **Given** the representative broker list slice
- **When** the Pact consumer test runs from the frontend side
- **Then** it generates a contract file describing the expected `GET /brokers` interaction
- **And** a provider verification path in the backend test suite verifies that contract against the real API behavior
- **And** the repo documents how to publish and verify against a self-hosted Pact Broker when desired
- **And** CI can fail on contract drift

**Alternative Flows / Edge Cases:**
- The broker list query shape evolves with optional filters → the representative contract may cover a constrained subset first, but the scope must be explicit
- Pact Broker is not running locally → local consumer/provider verification should still work without requiring broker publication
- Later features add more Pact coverage → this story only establishes the first vertical slice and broker workflow pattern

**Checklist:**
- [ ] Frontend consumer Pact test committed
- [ ] Backend provider verification test committed
- [ ] Contract artifact path is explicit
- [ ] Optional Pact Broker execution path is documented
- [ ] CI can fail when provider verification or contract publication/verification fails

## Data Requirements

**Required Fields:**
- Consumer name
- Provider name
- Contract file output path
- Representative request and response shape for broker list

**Optional Fields:**
- Pact Broker base URL
- Contract version / git SHA metadata

**Validation Rules:**
- The representative contract must exercise a real frontend-consumed shape, not a synthetic DTO invented for testing only
- Provider verification must run against the actual backend endpoint behavior
- Contract publication, if enabled, must remain self-hostable

## Role-Based Visibility

**Roles that can approve or operate this story:**
- Frontend Developer
- Backend Developer
- Quality Engineer
- DevOps
- Code Reviewer

**Data Visibility:**
- InternalOnly content: contract artifacts and broker publication details
- ExternalVisible content: none

## Non-Functional Expectations

- Maintainability: the first Pact slice should be small enough to stay current as implementation evolves
- Determinism: consumer/provider verification must be runnable without hidden manual steps
- Operability: Pact Broker remains optional for local validation but first-class for shared workflows

## Dependencies

**Depends On:**
- Existing broker list route and API endpoint

**Related Stories:**
- F0033-S0002 — Bruno covers representative request execution; Pact covers consumer/provider contract correctness

## Out of Scope

- Full API contract coverage for every Nebula endpoint
- Replacing OpenAPI or existing integration tests
- Paid contract broker services

## Questions & Assumptions

**Open Questions:**
- [ ] Should the initial Pact slice cover only the happy-path broker list, or should it also include one validation/empty-result scenario?

**Assumptions (to be validated):**
- Broker list is stable enough to be the first contract-testing anchor for the solution

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (N/A — internal tooling)
- [ ] Audit/timeline logged (N/A)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F{NNNN}-S{NNNN}-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
