---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0038-S0007 — CRM-Scope Guard (Out-of-Scope → Polite CRM Redirect)

## Story Header

**Story ID:** F0038-S0007
**Feature:** F0038 — Neuron Day-at-a-Glance Shell
**Title:** Classifier out-of-scope guard redirects non-CRM intents
**Priority:** High
**Phase:** MVP

## User Story

**As a** renewal-owning Underwriter
**I want** the companion to stay focused on my CRM work and politely steer me back when I ask something off-topic
**So that** I trust it as a CRM assistant rather than a general chatbot, and it does not produce answers outside its remit

## Context & Background

Per locked decision L8, the companion is a **CRM-scoped assistant, not a general-purpose chatbot**. The classifier needs an `out_of_scope` path from day one (intake reserved seam 5): non-CRM intents route to a polite redirect to CRM topics rather than a general-assistant answer. This defines the companion's character cheaply now.

## Acceptance Criteria

**Happy Path:**
- **Given** the Underwriter sends a CRM-related message (e.g., about renewals)
- **When** the classifier evaluates intent
- **Then** it routes to the matching CRM handler/zone and responds normally

- **Given** the Underwriter sends a non-CRM / off-topic message
- **When** the classifier evaluates intent
- **Then** it classifies `out_of_scope` and returns a **polite redirect** to CRM topics (it does not attempt a general-assistant answer)

**Behavior / Edge Cases:**
- Ambiguous intent → the companion asks a brief CRM-framed clarifying question rather than answering off-topic.
- Attempted prompt-injection that tries to make the companion act as a general assistant or escape CRM scope → handled as out-of-scope/denied; the guard is not bypassed by user-supplied instructions.
- The redirect response uses the same versioned message envelope (text part) as all other responses.

## Interaction Contract

N/A — conversational classification/response behavior; no CRM data mutation.

## Data Requirements

**Required:**
- Classifier intent label including an `out_of_scope` value.
- Redirect response text (CRM-framed), returned via the message envelope.

**Validation Rules:**
- Non-CRM intents must not reach data-reading or mutating handlers.
- The guard decision is recorded in the operation store for observability.

## Role-Based Visibility

**Roles that can interact:**
- Underwriter and Distribution — both authenticated CRM users; the scope guard applies equally regardless of role. No additional authorization is granted by the guard.

**Data Visibility:**
- InternalOnly: classifier decisions/traces are internal operational data.
- ExternalVisible: none.

## Non-Functional Expectations

- Performance: classification adds negligible latency to a turn.
- Security: the guard is resistant to prompt-injection attempts to escape CRM scope or assume general-assistant behavior; it never broadens data access.
- Reliability: a classifier failure defaults to the safe path (treat as out-of-scope / ask to rephrase), never an unbounded general answer.

## Dependencies

**Depends On:**
- F0038-S0001 — classifier/runtime + operation store.
- F0038-S0002 — redirect responses use the message envelope.

**Related Stories:**
- F0038-S0003 / F0038-S0005 — in-scope CRM intents route to these handlers.

## Business Rules

1. **CRM-scoped only (intake L8):** off-topic queries are redirected, not answered.
2. **Guard from day one (intake seam 5):** the `out_of_scope` → redirect path exists in v1.
3. **Fail safe:** uncertain/failed classification defaults to the bounded CRM path, never a general answer.

## Out of Scope

- General-purpose assistant behavior of any kind.
- Acting on or answering non-CRM requests.
- Multi-turn off-topic conversation handling beyond a single polite redirect/clarify.

## UI/UX Notes

- Screens involved: companion chat surface (see PRD `## Screen Layouts (ASCII)`).
- Key interactions: off-topic message → polite CRM redirect bubble.

## Questions & Assumptions

**Open Questions:**
- [ ] (AI Engineer, Phase B/feature) Exact redirect copy and the classifier's CRM-intent taxonomy.

**Assumptions (to be validated):**
- A single redirect/clarify response is sufficient v1 behavior for out-of-scope intents.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (ambiguous intent, injection attempts, classifier failure fail-safe)
- [ ] Permissions enforced — guard grants no access; authorization unchanged (documented)
- [ ] Audit/timeline logged — classifier decisions recorded in the operation store (observability)
- [ ] Tests prove CRM intents route correctly and non-CRM intents return a polite redirect (not a general answer)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
