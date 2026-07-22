---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0039-S0009 — Contextual Adjudicator (Gated Follow-On)

## Story Header

**Story ID:** F0039-S0009
**Feature:** F0039 — Neuron Durable Conversations & Local Phi Intent Resolution
**Title:** Bounded Phi contextual adjudication of context-dependent requests, gated behind direct-routing gates
**Priority:** Medium
**Phase:** MVP (GATED — enabled only after S0001–S0008 gates pass)

## User Story

**As a** renewal-owning Underwriter
**I want** the companion to correctly resolve context-dependent requests like "draft outreach for this one"
using my recent conversation and the record I'm looking at
**So that** I can speak naturally across turns without re-stating identifiers, while the companion stays
bounded and safe.

## Context & Background

Some requests cannot be safely routed from the current message alone ("this one", "the renewal we discussed").
When the direct resolver (S0006) returns `adjudicate`, this story runs a **second bounded Phi call** using a
**sanitized** recent-history summary plus a **validated UI anchor** to select a route — or clarify/redirect.
It is explicitly **gated**: implemented and enabled **only after** durable context (S0001–S0003) and
direct-routing evaluation gates (S0008) pass (spec §33 Phase 4). Context increases privacy and injection risk,
so inputs are strictly bounded and re-validated, and a final result **cannot request another adjudication**.

## Acceptance Criteria

**Gating:**
- **Given** the feature flag `NEURON_PHI_ADJUDICATION_ENABLED`
- **When** the direct-routing and contextual-evaluation gates have **not** passed
- **Then** adjudication stays **off** and S0006 clarifies on `adjudicate` (no second model call).

**Bounded context:**
- **Given** adjudication is enabled and required
- **When** context is built
- **Then** it includes **at most 4 recent turns and 6,000 context characters**, and the **complete prompt +
  schema + context + output fit within the 4,096-token limit**; **trusted UI anchors are revalidated and
  unauthorized data is excluded**.

**Bounded outcome:**
- **Given** an adjudication result
- **When** it is validated
- **Then** it may **route, clarify, or redirect but may NOT adjudicate again**; **write-like ambiguity
  clarifies** rather than acting; and all S0006 deterministic validation (schema → invariants → registry →
  trusted-head) still applies before any dispatch.

**Behavior / Edge Cases:**
- Context is sanitized (untrusted history/UI treated as data, not instructions); indirect injection in quoted
  content is handled as suspicious.
- If the UI anchor fails revalidation or references unauthorized data, it is dropped; adjudication proceeds
  without it or clarifies.
- Timeout / malformed / invariant failure → no engine call (fail closed), consistent with S0006.

## Interaction Contract

- **Entry point:** the resolver pipeline, only on a direct-pass `adjudicate` result and only when enabled.
- **Action → state:** produces a validated route/clarify/redirect decision; performs no CRM write itself; a
  routed confirmation-required action still requires explicit confirmation downstream.
- **Persistence evidence:** the adjudication decision + provenance recorded (no raw sensitive content); the
  resulting assistant envelope persisted by S0007.
- **Roles/status rules:** owner-scoped context only; adjudication grants no authorization; engine remains the
  boundary.
- **Validation failure:** any failure → bounded clarify/redirect, no engine call.
- **Audit/timeline:** a distinct `crm.intent_adjudicator` stage record (separate physical call, own telemetry
  label) is recorded without raw content.

## Data Requirements

**Required:**
- Sanitized bounded context envelope (≤4 turns, ≤6,000 chars) from the durable owner-scoped thread (S0001/2),
  validated UI anchor, `crm-intent-adjudicator` prompt + schema, S0004 provider.

**Validation Rules:**
- Token budget ≤4,096 for the whole call; unauthorized/unvalidated anchor data excluded; final result cannot
  re-adjudicate.

## Role-Based Visibility

**Roles that can interact:**
- Underwriter and Distribution — adjudication uses only the current user's own bounded history + their
  validated UI anchor; no cross-user or unauthorized data.

**Data Visibility:**
- InternalOnly: adjudicator stage decisions/traces; sanitized context is never logged raw.
- ExternalVisible: only bounded application-owned response copy.

## Non-Functional Expectations

- **Security:** context is the highest-risk input (spec §39.5) — strictly bounded, sanitized, re-validated;
  the model remains a routing signal only; injection in context handled as suspicious.
- **Performance:** at most one additional bounded call, only when required; within the 4,096-token budget.
- **Reliability:** fail closed on any failure; a single adjudication per turn (no recursion).

## Dependencies

**Depends On:**
- F0039-S0001/S0002/S0003 — durable owner-scoped context + UI anchors.
- F0039-S0006 — the direct resolver that emits `adjudicate`.
- F0039-S0008 — the direct-routing + contextual-evaluation gates that unlock this story.

**Related Stories:**
- (Future) response composer — explicitly NOT an F0039 dependency.

## Business Rules

1. **Gated:** enabled only after durable context + direct-routing gates pass.
2. **Bounded context:** ≤4 turns / ≤6,000 chars / ≤4,096 tokens total; unauthorized data excluded.
3. **No recursion:** a final result cannot request another adjudication.
4. **Write-like ambiguity clarifies:** ambiguous mutations are never acted on from inferred context.

## Out of Scope

- Open-ended user-facing prose / response composer (future feature).
- Multi-turn free-form context beyond the bounded window.
- New specialist heads — F0040.

## UI/UX Notes

- Screens: the same conversation panel (S0003); an adjudicated route renders like any routed result, a
  clarify like any bounded clarify bubble. No model prose.

## Questions & Assumptions

**Open Questions:**
- [ ] (Architect/Security, feature) Whether contextual evaluation shows a material accuracy gain justifying
  enabling adjudication in production, and the final context-window bounds after evaluation.

**Assumptions (to be validated):**
- The 4-turn / 6,000-char window fits the 4,096-token budget with the adjudicator prompt + schema; contextual
  evaluation will be run before enabling.

## Definition of Done

- [ ] Acceptance criteria met (gated enablement; bounded context; bounded no-recursion outcome)
- [ ] Edge cases handled (sanitized/indirect-injection context; anchor revalidation; fail-closed on failure)
- [ ] Permissions enforced — owner-scoped context only; no authorization granted; engine boundary intact
- [ ] Audit/telemetry — distinct adjudicator stage recorded without raw content
- [ ] Tests prove gating, context bounds + token budget, no-recursion, write-like→clarify, fail-closed
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
