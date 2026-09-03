---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0040-S0003 — Two-Live-Head Platform Hardening

## Story Header

**Story ID:** F0040-S0003
**Feature:** F0040 — Neuron Second Specialist Head (Broker Activity)
**Title:** Harden shared head dispatch, failure isolation, and telemetry with two live consumers
**Priority:** High
**Phase:** Infrastructure

## User Story

**As a** product stakeholder for the Neuron companion
**I want** Renewals and Broker activity to run through one validated, observable specialist-head platform
**So that** a second live domain adds dependable value without regressing Renewals or prematurely introducing cross-zone intelligence

## Context & Background

F0038 intentionally kept its head contract thin because inactive stubs could not validate reuse. F0040 is the planned extraction point: the first real second consumer must prove shared dispatch, independent typed results, safe registered rendering, and operational visibility. This story defines outcomes, not a required internal class or framework design.

## Acceptance Criteria

**Two-Live-Head Contract:**
- **Given** the shipped F0040 configuration starts successfully
- **When** the head registry and Day-at-a-Glance plan are validated
- **Then** Renewals and Broker activity are active specialist heads, Tasks and Pipeline remain inactive, and every referenced head/tool/component resolves before the service reports ready

- **Given** the user opens the glance
- **When** both live heads succeed
- **Then** each returns its own schema-valid typed zone payload and the shell assembles both without reading, ranking, merging, or rewriting the other head's data

**Failure Isolation:**
- Broker activity fails or times out → its zone becomes `error`; Renewals reaches its own `content` / `empty` result.
- Renewals fails or times out → its zone becomes `error`; Broker activity reaches its own `content` / `empty` result.
- Tasks/Pipeline inactive results → neither performs an engine read nor delays the two active heads.
- An unknown component, invalid props, unresolved head/tool, or invalid plan → fail closed at the existing validation boundary; do not render untrusted content or serve a partially valid startup configuration.

**Compatibility:**
- Existing Renewals glance, drill, draft, mock-send, CRM-scope guard, and persisted-thread behaviors pass without product-visible regression.
- Existing F0039 thread lifecycle, message ordering/idempotency, and intent fail-closed gates pass without weakening thresholds or bypassing the trusted catalog.
- The public message/zone shape remains replay-compatible with already-stored F0038/F0039 envelopes; any required version change has an explicit compatibility path defined in Phase B.

**Telemetry:**
- Every Broker activity zone attempt records entry point (`glance` or `conversation`), zone/domain id, terminal result (`content`, `empty`, `error`, or rejected), latency, and correlation identifiers needed to distinguish a thread/request without raw content.
- Telemetry emission failure is logged and cannot change the user-visible zone/message result.
- Telemetry excludes raw user text, event descriptions, component props, bearer tokens, credentials, and model prompt content.

## Interaction Contract

This infrastructure story persists run/provenance and telemetry side effects even though it adds no CRM business mutation.

| Surface / Entry Point | Trigger | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|---------|----------------|-------------------------|-------------------------------|----------------------------|
| Day-at-a-Glance load or accepted direct Broker activity message | Dispatch a registered live head | No user-editable platform state; configuration assets are code-reviewed source | Existing bounded head/run/tool provenance and one outcome/latency telemetry signal are recorded; no Broker/Contact/Timeline business row changes | Operational evidence can correlate the request/thread with the recorded head outcome, and historical thread envelopes replay unchanged | Engine data is scoped to the authenticated internal user; telemetry is internal operational data only |

- Invalid configuration is rejected before readiness and records no partial active configuration.
- Telemetry write failure is logged, creates no fabricated success event, and does not change the user response.
- Existing Renewals write interactions retain their original contracts; F0040 adds no new write control.

## Audit & Timeline Requirements

- Record the existing bounded head/run/tool provenance for dispatched live-head work and the outcome/latency telemetry required below.
- A validation or startup rejection records the failing asset identifier/reason code without raw component props, user text, tokens, or credentials.
- No `ActivityTimelineEvent` is created because this story changes platform execution/observability, not CRM business state.

## Data Requirements

**Required Fields:**
- Registry state: stable head identity, active/inactive state, accepted output modes/capabilities, and required tool/component references.
- Zone result: zone id, typed terminal status, registered component id/validated props where status is `content`, and user-safe detail for `empty`/`error`/`inactive`.
- Telemetry: timestamp, correlation/thread reference, domain/zone, entry point, terminal outcome, and latency.

**Optional Fields:**
- Version/provenance hashes already required by the existing Neuron orchestration contract.

**Validation Rules:**
- Exactly Renewals and Broker activity are live in F0040; Tasks and Pipeline remain inactive.
- One head cannot consume or reorder another head's result.
- Startup validation rejects unresolved or incompatible registered assets.
- Telemetry contains no raw business/user/prompt payload.

## Role-Based Visibility

**Roles affected:**
- End-user visibility remains the union of each live head's existing engine-authorized result; the shared platform grants no new data access.
- Product/operations roles may consume aggregate telemetry through existing internal operational access only.
- External users have no platform or telemetry surface.

**Data Visibility:**
- InternalOnly content: all head results, registry state, provenance, and telemetry.
- ExternalVisible content: none.

## Non-Functional Expectations

- Performance: live heads execute independently; a head that exceeds its configured timeout cannot delay another head from reaching its terminal zone state.
- Security: all executable heads/tools/components are allow-listed and validated; engine-touching reads use the forwarded user token.
- Reliability: startup is fail-fast for invalid assets, runtime failures are per-head, and telemetry failures are non-blocking.
- Observability: operators can distinguish broker-head success, empty, error, rejection, and latency by entry point without inspecting sensitive content.

## Dependencies

**Depends On:**
- F0038 — orchestration, registry, head cards, zone payload, registered components, Renewals behavior, and companion telemetry foundation.
- F0039 — durable envelopes, trusted intent catalog, fail-closed dispatcher, and provenance.
- F0040-S0001 and F0040-S0002 — the two Broker activity entry points being hardened.

**Related Stories:**
- F0041-S0001 — remains a gated follow-on; this story cannot lower or bypass its gates.

## Business Rules

1. **Generalize on the second real consumer:** shared behavior must be justified by both Renewals and Broker activity, not by inactive stubs or speculative future heads.
2. **Assembly, not composition:** no cross-zone scoring, ranking, synthesis, or next-best-action is introduced.
3. **Independent authority:** the engine owns CRM data/authorization, the trusted intent catalog owns routable domains/actions, and the component registry owns renderable UI.
4. **Rollback remains safe:** Broker activity can return to typed `inactive` without disabling Renewals or corrupting stored conversations.

## Out of Scope

- Cross-zone brain, third head, external federation/public Agent Cards, or a general workflow-composition platform.
- Changes to Renewals business rules or any new write.
- Telemetry dashboards, adoption target setting, or raw content capture.

## UI/UX Notes

- Screens involved: no additional screen beyond S0001/S0002; this story governs shared states and resilience.
- Key user-visible behavior: independent progressive zone results, safe fallback, and no whole-shell failure.

## Questions & Assumptions

**Open Questions:** None at product level. Phase B selects the smallest architecture that satisfies the two-consumer contract and documents any compatibility version change.

**Assumptions (validated from current product artifacts):**
- Renewals and Broker activity can share the existing zone/message/component concepts while retaining domain-owned data rules.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Bidirectional live-head failure, inactive-stub, invalid-asset, telemetry-failure, and replay-compatibility cases handled
- [ ] Existing engine/catalog/component authority boundaries enforced
- [ ] Operational telemetry/provenance recorded; no CRM `ActivityTimelineEvent` created
- [ ] Renewals and F0039 regression suites pass alongside new two-head contract, routing, security, performance, and telemetry tests
- [ ] Architecture decision/assembly plan documents the extracted contract and compatibility strategy
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
