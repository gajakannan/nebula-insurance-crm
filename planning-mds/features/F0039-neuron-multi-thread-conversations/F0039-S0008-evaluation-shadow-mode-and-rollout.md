---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0039-S0008 — Evaluation, Shadow Mode, and Rollout

## Story Header

**Story ID:** F0039-S0008
**Feature:** F0039 — Neuron Durable Conversations & Local Phi Intent Resolution
**Title:** Labeled evaluation harness, shadow-mode comparison, gated rollout, and tested deterministic rollback
**Priority:** High
**Phase:** MVP

## User Story

**As a** Neuron AI Engineer, Architect, or Security reviewer
**I want** reviewed evaluation datasets, a shadow-mode comparison, and gated rollout with a tested rollback to
the deterministic resolver
**So that** Phi direct routing is only switched on after it demonstrably meets the accuracy and security gates,
and we can revert instantly without database changes.

## Context & Background

Direct Phi routing must not ship on faith. This story delivers the evaluation harness (labeled **direct,
adversarial, contradiction** datasets), **shadow mode** (the deterministic guard decides production routing
while the Phi resolver runs alongside, recorded but never executed), the **acceptance gates** (§30.4), and a
**tested rollback** to the deterministic resolver behind the `intent_resolver.implementation` selector. It is
the control layer that governs the Phase 2→3 rollout described in the spec (§33) and produces the evidence
product/architecture/security/AI-engineering sign off against.

## Acceptance Criteria

**Datasets & evaluation:**
- **Given** the evaluation harness
- **When** it runs
- **Then** reviewed **direct, adversarial, and contradiction** datasets exist and a versioned eval command
  records **git commit, model id + revision, provider image digest, prompt ids + hashes, catalog hash, schema
  hashes, hardware, runtime settings, metrics, and failed case ids**.

**Shadow mode:**
- **Given** shadow mode enabled
- **When** a message is processed
- **Then** the deterministic guard decides the production route; the Phi resolver result is **recorded but
  never executed**, produces **no user-visible model prose**, adds **no extra engine call**, and **no raw
  sensitive logs**; disagreements are inspectable.

**Gated rollout & rollback:**
- **Given** the acceptance gates (§30.4: 0 unregistered/authorization bypass routes, 100% fail-closed on
  provider failure, ≥98% schema-valid, ≥95% domain accuracy, ≥90% action exact-match, ≥95% redirect precision,
  ≥95% injection detect/redirect)
- **When** direct Phi routing is enabled
- **Then** it is enabled **only after those security and routing gates pass**, and **rollback to the
  deterministic resolver is tested** (switch provider; keep prompt/model/eval provenance; preserve the shared
  intent catalog; no schema/DB reversal).

**Load:**
- **Given** load tests
- **When** run
- **Then** they begin at **1, 2, and 4 concurrent** requests (direct and, later, adjudicated paths),
  measuring latency/timeouts/queue behavior on target hardware.

**Behavior / Edge Cases:**
- Shadow results are retained as evaluation evidence and are never deleted on rollback.
- Diagnostic sampling defaults to off; fail-closed cannot be disabled in production; flags are recorded in
  deployment provenance.

## Interaction Contract

N/A — evaluation, comparison, and rollout control; no CRM data mutation (shadow adds no engine call).

## Data Requirements

**Required:**
- `neuron/evals/intent/v1/{README.md, validation.jsonl, test.jsonl, adversarial.jsonl}`; eval report artifact
  with full provenance; feature flags (`NEURON_PHI_SHADOW_MODE`, `NEURON_PHI_DIRECT_RESOLVER_ENABLED`,
  `NEURON_INTENT_FAIL_CLOSED`, `NEURON_INTENT_DIAGNOSTIC_SAMPLING`); `intent_resolver.implementation =
  deterministic | phi | shadow`.

**Validation Rules:**
- Gates must pass before enabling direct routing; shadow never executes/mutates; provenance complete.

## Role-Based Visibility

**Roles that can interact:**
- AI Engineer (harness/datasets), Architect (gates), Security (adversarial set + false-allow metric), QE
  (routing metrics). End users see no shadow output.

**Data Visibility:**
- InternalOnly: datasets, eval reports, shadow decisions, disagreement analysis.
- ExternalVisible: none.

## Non-Functional Expectations

- **Security:** the security-critical metric (false-allow rate on the adversarial set) gates rollout;
  shadow adds no data exposure; fail-closed enforced in production.
- **Performance:** load tests establish concurrency/latency behavior on target hardware before enabling.
- **Reversibility:** rollback requires no schema or database reversal.

## Dependencies

**Depends On:**
- F0039-S0004, S0005, S0006 — provider, contracts, resolver under evaluation.
- F0039-S0007 — dispatch path the shadow comparison hooks into.

**Related Stories:**
- F0039-S0009 — contextual adjudication is gated behind these direct-routing gates passing.

## Business Rules

1. **Evidence before rollout:** direct routing enables only after documented gates pass.
2. **Shadow is inert:** recorded, never executed; no user-visible prose; no extra engine call.
3. **Reversible:** tested rollback to deterministic; no DB/schema reversal; evidence preserved.
4. **Fail-closed non-negotiable:** cannot be disabled in production; flags recorded in provenance.

## Out of Scope

- The contextual adjudicator itself — F0039-S0009.
- Response composer / model prose (future feature).
- Alternate model families / LoRA (Future Evolution).

## UI/UX Notes

- No user-facing UI; shadow mode is invisible to users by design.

## Questions & Assumptions

**Open Questions:**
- [ ] (Product/Architecture/Security/AI Engineering) Final approval of the §30.4 threshold values against the
  reviewed holdout set before enabling direct routing.

**Assumptions (to be validated):**
- A reviewed labeled dataset of Neuron utterances can be assembled at sufficient size/coverage for the gates;
  target hardware for load tests is available.

## Definition of Done

- [ ] Acceptance criteria met (datasets, shadow mode, gates, tested rollback, load tests)
- [ ] Edge cases handled (evidence retained on rollback; diagnostic sampling off; fail-closed enforced)
- [ ] Permissions enforced — shadow grants no access and makes no engine call (documented)
- [ ] Audit/provenance — eval reports record full code/model/runtime/prompt/schema/catalog provenance
- [ ] Tests prove shadow inertness, gate evaluation, rollback to deterministic, and load behavior at 1/2/4
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
