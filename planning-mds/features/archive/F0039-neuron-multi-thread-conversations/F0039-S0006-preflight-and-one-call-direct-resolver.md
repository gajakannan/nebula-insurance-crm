---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0039-S0006 — Deterministic Preflight and One-Call Direct Resolver

## Story Header

**Story ID:** F0039-S0006
**Feature:** F0039 — Neuron Durable Conversations & Local Phi Intent Resolution
**Title:** Deterministic preflight plus one composed Phi scope-and-intent resolution with fail-closed validation
**Priority:** High
**Phase:** MVP

## User Story

**As a** renewal-owning Underwriter
**I want** the companion to safely turn my natural request into a validated route using one local model call,
refusing anything off-scope, manipulative, or unregistered
**So that** I get accurate routing without the companion ever guessing, inventing capabilities, or being
talked out of CRM scope.

## Context & Background

This is the runtime heart of the intent layer. It runs a small **deterministic preflight** (size/encoding/
normalization + a short high-certainty injection-marker list) and then **one composed Phi call** that returns
separate `scope` and `intent` sections (spending the local latency budget once). Every model result passes
**JSON Schema → deterministic invariants (S0005) → registry/route validation** before anything dispatches;
any failure causes a bounded redirect/clarify and **no engine call**. The model owns none of authentication,
authorization, registration, or validation (spec §7.3). Contextual adjudication is **not** part of this story
(it is gated S0009); on an `adjudicate` result here, the resolver clarifies.

## Acceptance Criteria

**Preflight (deterministic):**
- **Given** an inbound message
- **When** preflight runs
- **Then** it deterministically enforces max UTF-8 bytes / characters / lines, NFKC normalization, null-byte
  rejection, and a **short high-certainty injection-marker** list → obvious overrides get an immediate bounded
  CRM redirect; malformed → 400, too-large → 413, rate-limited → 429; **injection rule details are never
  returned to the user**.

**One-call direct resolution:**
- **Given** an in-scope message that passes preflight
- **When** the resolver runs
- **Then** **one Phi request** returns separate `scope` and `intent` sections, receiving only the normalized
  message + registered active domain/action catalog (never CRM records, tokens, tools, or history).

**Deterministic validation / fail-closed:**
- **Given** any model output
- **When** validated
- **Then** only **registered active** domains/actions can route; the **domain resolves to a trusted head from
  the catalog** (never a model-produced head id); a **missing required entity clarifies**; **inactive actions
  do not execute**; and **timeout, malformed output, or invariant failure cause no engine call** (bounded
  redirect/clarify instead).

**Provenance:**
- **Given** the composed call
- **When** telemetry is recorded
- **Then** the **logical scope and intent stages reference the same physical model-call provenance**
  (one physical call, two logically-validated sections).

**Behavior / Edge Cases:**
- On an `adjudicate` result, this story **clarifies** (adjudication is disabled until S0009).
- Compound requests: multiple in-domain actions preserve order; cross-domain combinations do not silently
  collapse (→ clarify/adjudicate).
- No model-generated confidence is emitted or used.

## Interaction Contract

- **Entry point:** the message pipeline, after the inbound message is persisted (S0007), before head dispatch.
- **Action → state:** produces a validated routing decision (route/clarify/redirect); it performs **no** CRM
  write itself. A routed `renewals.mock_send` still requires explicit confirmation downstream.
- **Persistence evidence:** the bounded decision + provenance are recorded (no raw prompt text); the assistant
  envelope is persisted by S0007.
- **Roles/status rules:** `allow` means only "eligible to continue"; it grants no CRM authorization.
- **Validation failure:** any schema/invariant/registry/timeout failure → bounded redirect or clarify, no
  downstream engine call.
- **Audit/timeline:** stage-level decisions recorded via the operation store / telemetry (§32) without raw
  content.

## Data Requirements

**Required:**
- Preflight config (§9.2 limits), S0005 catalog + schemas + prompts, S0004 structured provider.
- Bounded decision records with reason codes (no raw text).

**Validation Rules:**
- Schema validation → deterministic invariants → dynamic domain/action membership → required-entity presence
  → confirmation policy → trusted-head resolution. Any failure fails closed.

## Role-Based Visibility

**Roles that can interact:**
- Underwriter and Distribution (equal treatment by the guard/resolver). No decision widens access; CRM data
  reads still go through the engine with the user token downstream.

**Data Visibility:**
- InternalOnly: resolver decisions/traces and stage telemetry.
- ExternalVisible: only bounded, application-owned response copy (redirect/clarify text).

## Non-Functional Expectations

- **Security:** injection/scope-escape handled by deterministic preflight + fail-closed validation; the model
  is a routing signal, never an authorization authority; no injection details leaked to users.
- **Performance:** one composed call within the combined scope+intent budget (§11.8); prefer fail-closed over
  retries; at most one pre-response transport retry.
- **Reliability:** 100% fail-closed on provider/schema/registry failure — no unbounded routing.

## Dependencies

**Depends On:**
- F0039-S0004 — structured provider.
- F0039-S0005 — catalog, prompts, schemas, invariants.
- F0039-S0001/S0002 — durable store (inbound message persisted before resolution via S0007).

**Related Stories:**
- F0039-S0007 — integrates this into the dispatcher and persists envelopes.
- F0039-S0009 — the gated adjudication path this story defers to (clarify until enabled).

## Business Rules

1. **Deterministic authority (spec §7.3):** the model never owns auth/registration/validation.
2. **Fail closed:** any model/schema/registry/timeout failure → bounded redirect/clarify, no engine call.
3. **Trusted head resolution:** heads come from the catalog, never from model output.
4. **No confirmation bypass:** confirmation-required actions still require explicit confirmation.

## Out of Scope

- Contextual adjudication — F0039-S0009 (this story clarifies on `adjudicate`).
- Persisting envelopes / dispatcher wiring — F0039-S0007.
- Evaluation datasets / shadow mode — F0039-S0008.

## UI/UX Notes

- Redirect/clarify render as bounded, application-owned CRM bubbles in the panel (S0003). No model prose.

## Questions & Assumptions

**Open Questions:**
- [ ] (AI Engineer/Security, feature) Final high-certainty marker list and preflight limits after testing
  against real CRM usage.

**Assumptions (to be validated):**
- One composed scope+intent call meets the latency budget on target hardware; splitting into two calls is a
  later optimization only if evaluation justifies it.

## Definition of Done

- [ ] Acceptance criteria met (deterministic preflight; one composed call; fail-closed validation)
- [ ] Edge cases handled (adjudicate→clarify; compound/cross-domain; no confidence; no leaked injection detail)
- [ ] Permissions enforced — `allow` grants no access; engine remains authorization boundary (documented)
- [ ] Audit/telemetry — stage decisions + shared physical-call provenance recorded without raw text
- [ ] Tests prove: registered-only routing, missing-entity clarify, inactive no-execute, timeout/malformed/
      invariant → no engine call, trusted-head resolution
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
