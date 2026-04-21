# F0033-S0001 — Establish Serilog Structured Logging Baseline

**Story ID:** F0033-S0001
**Feature:** F0033 — Structured Logging and QE Toolchain Activation
**Title:** Establish Serilog structured logging baseline
**Priority:** Critical
**Phase:** Infrastructure

## User Story

**As a** DevOps engineer or backend engineer
**I want** the Nebula API to use Serilog for structured request and application logging
**So that** failures can be debugged with queryable, trace-correlated log events instead of ad hoc console output

## Context & Background

Nebula's architecture already calls for structured logging with Serilog, but the running API still relies on default ASP.NET logging configuration. That leaves a gap between the approved architecture and the actual runtime behavior used during local verification and CI investigation.

This story activates the baseline observability contract the solution already expects.

## Acceptance Criteria

**Happy Path:**
- **Given** the Nebula API starts in the approved runtime configuration
- **When** a request is handled successfully or fails
- **Then** Serilog is the primary logging pipeline
- **And** request completion logs include at minimum `TraceId`, `RequestPath`, `StatusCode`, and elapsed timing
- **And** authenticated requests include stable user context properties when available
- **And** the `traceId` returned in ProblemDetails matches the same request's log context
- **And** logging configuration lives in committed appsettings/env-var-driven configuration, not hard-coded logger setup only

**Alternative Flows / Edge Cases:**
- A request is unauthenticated → logging still records request-level correlation properties without assuming user context
- An exception occurs before endpoint execution → the exception path still emits a correlated `traceId`
- Development and CI need different sink verbosity → environment-specific overrides are allowed, but the structured contract stays the same

**Checklist:**
- [ ] Serilog packages are added to the API project
- [ ] `Program.cs` bootstraps Serilog and request logging
- [ ] Request/user context enrichment is implemented
- [ ] `appsettings.json` and environment overrides define sink and level configuration
- [ ] Sensitive headers and secrets are excluded or redacted
- [ ] Automated verification covers the structured logging pipeline

## Data Requirements

**Required Fields:**
- `TraceId`
- `RequestPath`
- `RequestMethod`
- `StatusCode`
- `ElapsedMs`

**Optional Fields:**
- `UserId`
- `UserRoles`
- `RequestId`

**Validation Rules:**
- Raw bearer tokens and `Authorization` headers must not be logged
- Baseline logging must favor structured properties over concatenated strings
- Request correlation must remain compatible with existing ProblemDetails `traceId` behavior

## Role-Based Visibility

**Roles that can approve or operate this story:**
- Backend Developer
- DevOps
- Security Reviewer
- Quality Engineer
- Code Reviewer

**Data Visibility:**
- InternalOnly content: structured runtime logs and redaction configuration
- ExternalVisible content: none

## Non-Functional Expectations

- Reliability: structured logging must not break API startup or request handling
- Performance: request logging overhead must stay low enough for normal local and CI runs
- Security: secrets and sensitive auth material must not enter baseline logs

## Dependencies

**Depends On:**
- Existing API runtime in `engine/src/Nebula.Api`

**Related Stories:**
- F0033-S0005 — SonarQube uses the improved backend baseline as part of code-quality activation

## Business Rules

1. **Serilog becomes canonical:** Once activated, Serilog is the primary runtime logging contract for the API.
2. **Correlation is mandatory:** `traceId` in errors and log events must refer to the same request.
3. **Redaction wins:** If there is any ambiguity about a field containing secrets or sensitive auth material, do not log it in the baseline path.

## Out of Scope

- External log aggregation or retention platform integration
- Logging every request/response body by default
- Building alerting dashboards or incident routing

## Questions & Assumptions

**Open Questions:**
- [ ] Should the initial dev sink remain console-only, or should a rolling file sink also be included for local debugging?

**Assumptions (to be validated):**
- Request/user context enrichment can be added without changing application service signatures

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A — runtime observability feature)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F{NNNN}-S{NNNN}-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
