# F0033 — Structured Logging and QE Toolchain Activation — PRD

**Feature ID:** F0033
**Feature Name:** Structured Logging and QE Toolchain Activation
**Priority:** High
**Phase:** Infrastructure
**Status:** Architecture Complete

## Feature Statement

**As a** release approver, DevOps engineer, or quality engineer
**I want** Nebula CRM to activate its approved structured logging and QE toolchain in the solution runtime
**So that** backend failures are diagnosable with correlated logs and release signoff can rely on executable Bruno, Lighthouse CI, Pact, and SonarQube evidence instead of documentation alone

## Business Objective

- **Goal:** Turn Nebula's intended observability and QE stack into executable repo behavior before more CRM surface area lands.
- **Metric:** Number of approved-but-inactive cross-cutting capabilities with repo-native local and CI execution paths.
- **Baseline:** API runtime still uses default ASP.NET logging, and the repo does not yet expose first-class Bruno, Lighthouse CI, Pact, or SonarQube workflows.
- **Target:** Serilog plus the four QE toolchain elements are all activated with committed configuration, reproducible commands, and evidence/report paths.

## Problem Statement

- **Current State:** `planning-mds/architecture/SOLUTION-PATTERNS.md` and `TESTING-STACK-SUMMARY.md` already establish Serilog, Bruno, Lighthouse CI, Pact, and SonarQube Community as the intended stack, but the running Nebula solution does not yet expose those capabilities as first-class local or CI workflows.
- **Desired State:** The Nebula repo has a clear observability baseline for the API and a concrete QE toolchain for API validation, frontend performance, consumer/provider contracts, and code-quality reporting.
- **Impact:** Without this feature, cross-cutting quality and operability expectations look approved on paper but remain optional in practice, which weakens debugging speed, release confidence, and reviewer trust.

## Scope & Boundaries

**In Scope:**
- Serilog activation in the API runtime with request correlation and redaction guardrails
- Repo-native Bruno collections, environments, and execution scripts for representative API validation
- Lighthouse CI configuration and execution path for selected Nebula frontend routes
- Pact consumer/provider contract activation for one representative Nebula slice
- SonarQube Community activation with explicit local/CI execution flow and coverage import wiring
- Solution-side evidence and tracker updates that make these capabilities discoverable and enforceable

**Out of Scope:**
- Production log shipping to ELK, Loki, Splunk, or another external observability platform
- Full contract coverage for every API endpoint and UI flow in the repo
- Temporal adoption, NJsonSchema rollout, or unrelated architecture backlog items
- Enterprise or paid SaaS tooling
- A full rewrite of existing smoke, Vitest, Playwright, or .NET test suites

## Acceptance Criteria Overview

- [ ] The API runtime uses Serilog as the structured logging baseline, preserving `traceId` correlation between logs and ProblemDetails responses
- [ ] Nebula exposes repo-native Bruno, Lighthouse CI, Pact, and SonarQube Community execution paths with committed configuration and clear local/CI entry points
- [ ] At least one representative vertical slice has executable contract testing from frontend consumer to backend provider verification
- [ ] QE artifacts and evidence paths are explicit, stable, and referenced from solution docs and workflows
- [ ] Activation preserves existing security constraints, including production auth-mode guardrails and log redaction expectations

## UX / Screens

F0033 is an infrastructure and release-enablement feature. It affects developer, QE, and CI workflows rather than creating net-new end-user product screens.

| Surface | Purpose | Key Actions |
|--------|---------|-------------|
| Terminal / CI logs | Structured observability and toolchain execution | Inspect trace-correlated API logs, run scripts, review failing checks |
| Login route (`/login`) | Lighthouse baseline for public route performance | Audit first-load performance and accessibility baseline |
| Dashboard route (`/`) | Lighthouse representative authenticated route | Audit main entry route in approved performance runtime profile |
| Broker list route (`/brokers`) | Representative frontend + API contract slice | Run Lighthouse, Pact, and Bruno validations against a high-value read path |
| API endpoints (`/healthz`, `/brokers`, `/my/tasks`) | Representative API validation surface | Execute Bruno collections and provider verification |

**Key Workflows:**
1. **Diagnose runtime failure** — use Serilog logs and `traceId` to connect an API failure to a specific request and user context.
2. **Run API QE validation** — execute Bruno collections against the local or CI stack and inspect machine-readable reports.
3. **Run frontend performance audit** — execute Lighthouse CI in the approved Nebula performance profile and review threshold failures.
4. **Verify contracts and code quality** — publish/verify a Pact contract and run SonarQube Community with backend/frontend coverage artifacts.

## Data Requirements

**Core Artifacts:**
- Structured API log events with stable fields such as `TraceId`, `RequestPath`, `StatusCode`, `ElapsedMs`, and user context when available
- Bruno collection definitions, environment templates, and report outputs
- Lighthouse CI HTML/JSON reports for approved frontend routes
- Pact contract files and provider verification results
- SonarQube analysis metadata and imported coverage reports

**Validation Rules:**
- Structured logs must not record bearer tokens, raw `Authorization` headers, or other sensitive secrets
- QE reports must be generated by executable repo commands, not asserted manually
- Authenticated performance testing must preserve the production auth guard for production builds and use an explicitly approved non-production performance runtime when needed
- Pact and Sonar activation must remain OSS-compatible and self-hostable

**Data Relationships:**
- API `traceId` in ProblemDetails responses → Serilog log events for the same request
- Bruno API collections → CI gate and evidence artifact path
- Pact consumer contract → provider verification → optional Pact Broker publication
- Backend/frontend coverage outputs → SonarQube analysis

## Role-Based Access

| Role | Access Level | Notes |
|------|-------------|-------|
| Backend Developer | Implement / Operate | Owns Serilog wiring and provider-side contract support |
| Frontend Developer | Implement / Operate | Owns Lighthouse CI runtime path and consumer-side Pact coverage |
| Quality Engineer | Execute / Validate | Owns toolchain evidence and release-facing validation |
| DevOps | Operate / Enforce | Owns workflow, container overlay, and runtime activation |
| Security Reviewer | Review | Validates log redaction and service/toolchain exposure boundaries |
| Code Reviewer | Review | Validates maintainability and regression risk |

Application end users receive no new product permissions from this feature.

## Success Criteria

- Investigating an API error no longer depends on ad hoc console output or reproducing the failure locally from scratch.
- QE can run and point to committed Bruno, Lighthouse, Pact, and Sonar entry points instead of documenting aspirational tooling.
- Reviewers can require evidence from the activated toolchain for relevant stories and features.
- The solution activates the intended OSS stack without weakening existing security guardrails.

## Risks & Assumptions

- **Risk:** Structured logging can accidentally leak PII or secrets if payload logging is too broad.
  **Mitigation:** Keep the baseline to request/response metadata plus explicit redaction rules; do not log raw authorization headers or request bodies by default.
- **Risk:** SonarQube and Pact Broker add operational weight to local development.
  **Mitigation:** Use an opt-in `docker-compose.qe.yml` overlay rather than expanding the default day-to-day stack.
- **Risk:** Lighthouse on authenticated routes can tempt a production-auth bypass.
  **Mitigation:** Keep the production auth-mode guard intact and define a dedicated performance runtime profile for non-production audits only.
- **Assumption:** Broker list is a representative first contract slice for Pact because it already exists in both frontend and backend and is stable enough to validate the workflow.
- **Assumption:** Existing smoke, Vitest, Playwright, and .NET test paths remain valuable; this feature activates missing layers rather than replacing them.

## Dependencies

- Existing architectural decisions in `planning-mds/architecture/SOLUTION-PATTERNS.md`
- Existing testing/tooling baseline in `planning-mds/architecture/TESTING-STACK-SUMMARY.md`
- Existing frontend quality foundations from F0015
- Existing API/runtime stack, auth bootstrap, and smoke path from F0014/F0009/F0005
- GitHub Actions as the current CI execution platform

## Related Stories

- F0033-S0001 — Establish Serilog structured logging baseline
- F0033-S0002 — Activate Bruno API validation path
- F0033-S0003 — Activate Lighthouse CI performance gate
- F0033-S0004 — Establish broker list contract testing with Pact
- F0033-S0005 — Activate SonarQube Community quality reporting

## Business Rules

1. **Structured logging baseline:** Serilog is the canonical API logging pipeline for Nebula once this feature lands; default framework logging should not remain the primary runtime contract.
2. **Trace correlation:** `traceId` in ProblemDetails and request logs must refer to the same request identifier.
3. **Redaction before convenience:** Raw bearer tokens, secrets, and unbounded payload dumps are forbidden in baseline logs.
4. **OSS-only activation:** Bruno, Lighthouse CI, Pact, and SonarQube Community must be activated in a way that keeps the solution self-hostable and free of paid-SaaS lock-in.
5. **Representative-first rollout:** One well-chosen vertical slice per missing QE layer is acceptable for this feature; full-surface expansion can follow later features once the toolchain itself is live.

## Rollout & Enablement

- Add a feature-local getting-started guide with prerequisite services, commands, and evidence paths.
- Document the approved local-vs-CI execution model for each tool.
- Keep toolchain activation in the solution workspace (`engine/`, `experience/`, `.github/`, `scripts/`, `planning-mds/`) rather than hiding it in `agents/**`.
