---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0039-S0005 — Catalog, Prompt Registry, and Composed Resolution Contract

## Story Header

**Story ID:** F0039-S0005
**Feature:** F0039 — Neuron Durable Conversations & Local Phi Intent Resolution
**Title:** Schema-validated intent catalog, versioned prompt registry, and composed scope-and-intent contract
**Priority:** High
**Phase:** MVP

## User Story

**As a** Neuron AI Engineer / Architect defining the routing surface
**I want** a code-reviewed intent catalog, versioned prompt fragments with hashes, and JSON schemas for the
composed scope-and-intent output plus deterministic invariants
**So that** only registered active domains/actions can route and contradictory or invented model outputs are
rejected before anything dispatches.

## Context & Background

The verified smoke test proved a prompt-only model invents unregistered actions (e.g.
`show_renewals_needing_attention`) and can emit shape-valid but contradictory decisions (a `redirect` that
still carries `domain=renewals` + `actions=[renewals.list_attention]`). This story creates the trusted,
code-reviewed configuration that constrains the model: `neuron/config/intent-catalog.yaml` (active
domains/actions → registered heads), versioned prompt assets under `neuron/prompts/**` with metadata + hashes,
and the JSON schemas (`neuron-scope-decision`, `neuron-intent-decision`, composed `neuron-intent-resolution`)
plus the **deterministic cross-field invariants**. It carries no live model call itself — it is the contract
S0006 enforces.

## Acceptance Criteria

**Happy Path:**
- **Given** `intent-catalog.yaml`
- **When** it loads at startup
- **Then** it is **schema-validated** and maps active domains/actions to registered head card ids, with the
  runtime cross-checking: each domain's `target_head_card_id` exists; head active-state agrees with routing;
  action ids unique; each action's domain prefix matches its parent; referenced entity types registered;
  executable actions map to a registered capability; an inactive action can never be returned as executable.

- **Given** the versioned prompt fragments (`crm-scope-guard`, `crm-intent-classifier`) and the composed
  `crm-intent-resolver` prompt + metadata
- **When** they load
- **Then** they load **with content hashes** recorded for provenance.

- **Given** the composed resolution schema
- **When** a model output is validated
- **Then** JSON Schema **rejects unknown shape and additional properties**, and deterministic invariants
  **reject contradictory scope/intent combinations** (e.g. `allow` without `scope=crm`; `redirect` carrying a
  routed domain/actions), mapping violations to a safe `redirect` / `invalid_model_output`.

**Regression fixtures:**
- **Given** the observed `redirect` + `renewals.list_attention` combination and the invented
  `show_renewals_needing_attention` action
- **When** validated
- **Then** both are **regression fixtures** that must be rejected.

**Behavior / Edge Cases:**
- Invalid catalog, missing/absent prompt asset, or invalid schema **prevents readiness** (startup refuses to serve,
  consistent with existing Agent Card / plan / tool validation).
- Dynamic domains/actions loaded from YAML are validated **deterministically after** JSON Schema validation
  (the schema cannot enumerate them).

## Interaction Contract

N/A — trusted configuration + contract assets; no CRM data mutation. (Enforcement/routing is S0006.)

## Data Requirements

**Required:**
- `neuron/config/intent-catalog.yaml` (catalog_version, domains→actions→required_entities,
  `target_head_card_id`, `active`, `requires_explicit_confirmation`).
- `neuron/app/contracts/*.schema.json` (scope, intent, composed resolution, catalog, context, prompt
  metadata).
- `neuron/prompts/**` versioned system fragments + `metadata.yaml` with hashes.

**Validation Rules:**
- Catalog cross-checks (above) all pass; schemas reject additionalProperties and unknown shapes; invariants
  encoded in code, not the schema alone.

## Role-Based Visibility

**Roles that can interact:**
- Architect / AI Engineer author and code-review these assets. End users never see them; they only shape
  routing behavior.

**Data Visibility:**
- InternalOnly: catalog, prompts, schemas, and their hashes.
- ExternalVisible: none.

## Non-Functional Expectations

- **Security:** the catalog/schemas are the trust boundary — the model may only route to what they register;
  invariants encode the routing contract that JSON Schema cannot.
- **Reliability:** fail-fast on invalid catalog/prompt/schema — no partial-ready routing.
- **Maintainability:** prompts and schemas are versioned with hashes for replayable provenance.

## Dependencies

**Depends On:**
- F0038 — Agent Card / plan / tool validation and fail-fast startup this extends.
- F0039-S0004 — the provider that will consume these schemas/prompts.

**Related Stories:**
- F0039-S0006 — enforces the invariants + catalog validation at resolve time.
- F0039-S0007 — resolves the head from this trusted catalog (never model-produced ids).

## Business Rules

1. **Registry is authority (spec §39.3):** structured output guarantees shape, not registration/authorization
   — those are deterministic against this catalog.
2. **No invented capabilities:** unknown/inactive domains and actions are rejected.
3. **Contradictions fail closed:** invariant violations map to a safe redirect.
4. **Versioned + hashed:** prompts and schemas carry provenance hashes.

## Out of Scope

- The live resolver call and preflight — F0039-S0006.
- Dispatcher/persistence integration — F0039-S0007.
- Evaluation datasets — F0039-S0008.

## UI/UX Notes

- No UI. Defines the routing surface behind the conversation experience.

## Questions & Assumptions

**Open Questions:**
- [ ] (Architect/AI Engineer, feature) Final initial active-action set (spec lists renewals active; tasks/
  pipeline/broker_activity inactive at first) and confirmation-required actions.

**Assumptions (to be validated):**
- Only `renewals.*` is active at first delivery (matching the single live F0038 zone); other domains are
  registered-but-inactive.

## Definition of Done

- [ ] Acceptance criteria met (validated catalog, versioned hashed prompts, schemas + invariants)
- [ ] Edge cases handled (dynamic membership validated post-schema; fail-fast on invalid assets)
- [ ] Permissions enforced — catalog is the routing trust boundary; no invented/inactive routes (documented)
- [ ] Audit/provenance — prompt/schema/catalog hashes recorded
- [ ] Tests prove catalog cross-checks, schema rejection of bad shape, invariant rejection, regression fixtures
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
