# Threat Model

Status: Final
Last Updated: 2026-02-17
Owner: Security + Architect

## Scope And Objectives

Define the pre-implementation threat model for the Nebula reference solution and its planned runtime.
This covers the application API surface, workflow orchestration, data stores, and AI workflow paths.

## System Context And Assets

Primary assets:
- Authentication and authorization decisions
- Customer/account/submission/renewal data
- Workflow transition history and audit timeline
- Secrets and environment configuration
- AI workflow prompts and outputs in neuron/

Planned trust boundaries:
- User/browser to API boundary
- API/service to data store boundary
- API/service to external identity provider boundary
- API/service to AI workflow boundary
- CI/CD to runtime boundary

## Threat Inventory (STRIDE)

| Threat | STRIDE | Example Scenario | Risk | Planned Controls |
|---|---|---|---|---|
| Token theft | Spoofing | Stolen access token used to call APIs | High | Short-lived tokens, server-side validation, revoke on incident |
| Privilege escalation | Elevation | User accesses data outside ABAC scope | High | Casbin ABAC enforcement, deny-by-default policies, policy tests |
| Tampering | Tampering | Workflow transitions modified or replayed | Medium | Immutable transition records, append-only timeline, server-side validation |
| Data exposure | Information Disclosure | PII leaks in logs or error payloads | High | Redaction policy, ProblemDetails constraints, structured logging rules |
| Repudiation | Repudiation | User denies a transition action | Medium | Audit trail with subject, timestamp, and request id |
| Availability | DoS | Expensive dashboard queries degrade service | Medium | Indexed queries, request timeouts, rate limiting |
| AI prompt leakage | Information Disclosure | Sensitive data appears in prompts or LLM output | High | Prompt minimization, masking, allow-list fields, retention limits |
| Supply chain | Tampering | Vulnerable dependencies in runtime images | Medium | Dependency scanning, pinned versions, SBOM in CI |

## Risk Scoring And Acceptance

Scoring scale: High / Medium / Low based on impact to confidentiality, integrity, and availability.
Pre-implementation acceptance: No High risks are accepted without an explicit mitigation plan and owner.

## Mitigations And Evidence Plan

- Authorization: define resource-action matrix and policy tests before implementation.
- Input validation: enforce schema validation and reject invalid transitions with ProblemDetails codes.
- Logging: define redaction rules and forbidden fields list.
- AI safety: restrict prompt payloads to minimal fields; prohibit secrets in prompts.
- Supply chain: enable dependency and container scanning in application runtime CI.

## Residual Risks (Pre-Implementation)

- ABAC policy errors due to missing coverage in early implementations.
- Performance degradation in dashboard queries until caching/pre-compute strategy is introduced.
- AI workflow misuse until full policy tests and guardrails are automated.

## Approval And Sign-Off

Security Reviewer: Security Agent
Architect: Architect Agent
Date: 2026-02-22
