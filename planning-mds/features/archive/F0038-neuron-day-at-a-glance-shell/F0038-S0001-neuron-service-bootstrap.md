---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0038-S0001 — Neuron Service Bootstrap (Stateless Runtime + Registries + Versioned Orchestration)

## Story Header

**Story ID:** F0038-S0001
**Feature:** F0038 — Neuron Day-at-a-Glance Shell
**Title:** Foundational Neuron service bootstrap
**Priority:** Critical
**Phase:** Infrastructure

## User Story

**As a** Neuron platform engineer (AI Engineer)
**I want** a first runnable, stateless Neuron service with a specialist-head registry, a tool registry, versioned YAML orchestration assets, and an A2A-aligned internal delegation profile
**So that** the Underwriter's companion can run end-to-end on a replayable, extensible foundation without the Neuron tier becoming a source of truth

## Context & Background

F0038 is the first runnable Neuron implementation. Every other F0038 story (shell, Renewals read, draft, mock-send, scope guard, telemetry) plugs into the runtime, registries, and orchestration assets established here. The intake brief (§5, reserved seams 1/2/4/6/7) requires these seams to exist on day one even though their full features ship later. This is an infrastructure story per the story template's Infrastructure guidance.

## Acceptance Criteria

**Happy Path:**
- **Given** the Neuron service is started locally per `GETTING-STARTED.md`
- **When** a health/readiness check is called
- **Then** the service reports healthy and exposes its registered specialist heads and tools

**Behavior:**
- **Given** a versioned YAML orchestration asset is loaded
- **When** the service validates it at startup
- **Then** every referenced specialist head, tool, and terminal state resolves to a registered handler, and an asset with an unknown reference or no terminal state fails validation with a clear error (the service refuses to serve that asset)

- **Given** the orchestrator dispatches an internal task to a specialist head
- **When** the call is made
- **Then** it uses the private/internal A2A-shaped contract from ADR-027 (no public A2A endpoint, Agent Card, or external federation is exposed)

- **Given** the Neuron service handles a request
- **Then** durable operation state (threads, messages, message parts, agent runs, tool calls, provenance) is written to the Neuron-owned `neuron` store interface, and the running service holds no business state between requests (stateless)

**Alternative Flows / Edge Cases:**
- Missing/unreadable orchestration asset at startup → service fails fast with a configuration error; does not start in a half-configured state.
- Unknown head/tool name referenced at dispatch → typed error, request rejected, error recorded in the operation store; no silent fallthrough.
- Engine unreachable → typed upstream-unavailable error surfaced to the caller; not a 500 stack leak.

## Interaction Contract

N/A — infrastructure/runtime bootstrap. No end-user mutation surface. The CRM business mutations live in F0038-S0005 and F0038-S0006.

## Data Requirements

**Service inputs:**
- Versioned YAML orchestration assets (workflow/head plans) — checked-in, schema-validated.
- Model + prompt configuration (model id, prompt id/version references).
- Specialist-head registry entries (name → handler) and tool-registry entries (name → handler).

**Operation store records (Neuron-owned `neuron` schema, interface defined here; durable home decided by the architect's persistence ADR in Phase B — F0038 may run on an in-memory implementation behind the same interface):**
- thread, message, message-part, agent-run, tool-call, and provenance records (model, prompt id/version, content hash, trace identifiers).

**Validation Rules:**
- Orchestration assets must declare explicit terminal states and only reference registered head/tool/agent names.
- The Neuron operation store must not hold CRM/product business data (that stays engine-owned); it holds only companion operation state.

## Role-Based Visibility

**Roles that can operate the service:**
- AI Engineer / DevOps — run, configure, and observe the Neuron service (operational, not an end-user ABAC surface).

**Data Visibility:**
- All engine-sourced data access is performed later (S0003/S0005/S0006) by forwarding the **end user's** authentik token; the engine enforces existing Casbin ABAC/authorization unchanged. The bootstrap re-implements no authorization in Python.
- InternalOnly: companion operation/provenance records are internal operational data, private to the owning user's threads (visibility enforced by the engine token on reads).

## Non-Functional Expectations

- Security: no engine call without a forwarded user token; no secrets in logs; provenance records carry no raw credentials.
- Reliability: fail-fast on invalid configuration; typed errors (not stack traces) on upstream failures; orchestration traces persisted for replay.
- Statelessness: a restarted service serves correctly using only the operation store + engine; no in-process business state is assumed.

## Dependencies

**Depends On:**
- ADR-027 (Neuron Companion A2A orchestration) — internal delegation profile and orchestration approach.
- AI Engineer role buildout (epic prerequisite, per GETTING-STARTED.md).

**Related Stories:**
- F0038-S0002 — the shell + zone-dispatch run on this runtime and message envelope.
- F0038-S0008 — telemetry events are emitted through this runtime.

## Business Rules

1. **Stateless Neuron:** the engine + Postgres remain the single source of truth (intake L6); Neuron is a secondary interface and never a business-data store.
2. **Neuron-owned operation persistence:** companion operation/provenance state is owned by the Neuron service's own `neuron` schema, written directly — not via an engine pass-through.
3. **Internal A2A only:** public A2A endpoints, public Agent Cards, and external agent federation are deferred (intake reserved seam 7).
4. **No heavy external orchestrator:** F0038 uses simple versioned YAML orchestration assets, not MS Agent Framework or equivalent, unless a later ADR changes that.
5. **Package convention:** specialist heads live under `neuron/crm_agents/` (no hyphenated Python packages).

## Out of Scope

- Public A2A federation, external Agent Cards, MCP-UI / sandboxed external-host resources.
- The durable persistence implementation decision itself (architect ADR, Phase B) — only the interface + provenance contract are in scope here.
- Any specialist head other than Renewals being functional (stubs are F0038-S0004).

## UI/UX Notes

- No direct UI. Surfaced indirectly through the shell (F0038-S0002).

## Questions & Assumptions

**Open Questions:**
- [ ] (Architect, Phase B) Final durable persistence home for the `neuron` schema (in-memory acceptable for F0038 behind the interface).

**Assumptions (to be validated):**
- The engine accepts the forwarded user authentik token on Neuron-originated calls (on-behalf-of) — architect validates in Phase B.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (invalid asset, unknown head/tool, engine unavailable)
- [ ] Permissions enforced (engine-side via forwarded token; no Python authz)
- [ ] Audit/provenance logged (operation store records with model/prompt/version/hash)
- [ ] Contract tests pass for registries + orchestration asset validation
- [ ] Local run/test instructions documented in GETTING-STARTED.md
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
