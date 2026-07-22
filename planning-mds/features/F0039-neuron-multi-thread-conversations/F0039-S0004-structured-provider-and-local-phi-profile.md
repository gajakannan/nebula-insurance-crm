---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0039-S0004 — Structured Provider and Verified Local Phi Profile

## Story Header

**Story ID:** F0039-S0004
**Feature:** F0039 — Neuron Durable Conversations & Local Phi Intent Resolution
**Title:** Async structured-completion provider seam with a verified local Phi (vLLM) profile and provenance
**Priority:** High
**Phase:** MVP

## User Story

**As a** Neuron AI Engineer maintaining the companion
**I want** a runtime-neutral, asynchronous structured-completion provider interface with mock, scripted, and
OpenAI-compatible implementations plus a verified local Phi profile
**So that** Neuron can call a local model for structured intent output behind a replaceable seam, with full
provenance and no heavy ML dependencies in the Neuron process.

## Context & Background

F0038 defined `ModelProvider`/`ModelResult`/`ModelRouter` and registers only `MockProvider`. This story
extends the seam to **structured** (JSON-Schema-constrained) async completions and adds an
**OpenAICompatibleProvider** targeting the verified local runtime: `microsoft/Phi-4-mini-instruct` on a vLLM
OpenAI-compatible server (`http://127.0.0.1:8000/v1`, bearer auth, `max_model_len=4096`), verified 2026-07-21.
The model runs as a **separate local inference service** (not inside the Neuron FastAPI process) so model
lifecycle, GPU memory, and health are independent. Provider stays runtime-neutral (a local Ollama provider
may also satisfy it).

## Acceptance Criteria

**Happy Path:**
- **Given** the structured provider interface
- **When** a caller requests a structured completion with a JSON Schema
- **Then** an **asynchronous** structured-completion method returns a typed result plus provenance (model id,
  revision, prompt/completion tokens, latency, cost, content hash) and **normalized errors**, with **no
  raw-content logging**.

- **Given** the local Phi profile
- **When** it is configured
- **Then** it targets `microsoft/Phi-4-mini-instruct` at the authenticated vLLM endpoint, enforces the
  **4,096-token limit by request budgeting**, and records **model revision, vLLM image digest, GPU memory,
  latency, and concurrency** evidence (the server reports model + context length but not a pinned HF commit,
  so revision/image provenance are configured and recorded separately).

**Providers:**
- **Given** the same provider contract
- **When** exercised
- **Then** `MockProvider` (structured deterministic), `ScriptedProvider` (test-case → controlled result),
  and `OpenAICompatibleProvider` all implement it and pass a shared contract suite.

**Behavior / Edge Cases:**
- Timeout, connection reset, empty choices, and non-object JSON are mapped to **normalized provider errors**
  (never leaked as raw model text).
- The provider does not log raw prompts or completions in general telemetry (provenance is hashes + counts).
- One transport retry is allowed only for a connection reset **before any response**; otherwise fail closed.

## Interaction Contract

N/A — infrastructure/provider seam; no CRM data mutation. (Downstream persistence/routing is covered by
S0006/S0007.)

## Data Requirements

**Required:**
- Provider config in `neuron/config/models.yaml` + env (endpoint, API key, model, revision, image digest,
  token budget, timeouts). Provenance fields on `ModelResult`.

**Validation Rules:**
- Structured output requested with a JSON Schema; token budget enforced ≤ 4,096.
- Errors normalized; no raw content in logs/telemetry.

## Role-Based Visibility

**Roles that can interact:**
- AI Engineer / Backend Developer configure and operate the provider. End users never reach the provider
  directly; Phi is **never exposed to the browser**.

**Data Visibility:**
- InternalOnly: provider config, provenance metadata, normalized errors.
- ExternalVisible: none.

## Non-Functional Expectations

- **Security:** Phi is not browser-reachable; bearer credential handling per §25 (token/credential handling,
  network controls, SSRF resistance where applicable); no secrets in logs.
- **Performance:** one resident model; request budgeting keeps prompt+output within 4,096 tokens; latency and
  concurrency recorded on target hardware.
- **Replaceability:** provider is runtime-neutral so a future model/runtime can replace Phi without importing
  ML dependencies into Neuron.

## Dependencies

**Depends On:**
- F0038 — `ModelProvider`/`ModelRouter` seam, `MockProvider`, `models.yaml`/`NEURON_MODEL_PROVIDER`.

**Related Stories:**
- F0039-S0005 — schemas/prompts/catalog the provider serves.
- F0039-S0006 — the direct resolver calls this provider.
- F0039-S0008 — load/latency/concurrency evidence collected via this provider.

## Business Rules

1. **Deterministic authority stays out of the model:** the provider returns data only; authorization,
   registration, and validation are deterministic and elsewhere.
2. **Provenance without exposure:** record model/prompt/schema provenance as hashes + counts, never raw
   sensitive content.
3. **Runtime-neutral seam:** vLLM is the reference; the contract must not hard-couple Neuron to it.
4. **Fail closed:** normalized errors on any provider failure; no unbounded retries.

## Out of Scope

- Prompt content, catalog, and schemas — F0039-S0005.
- Resolver logic and deterministic validation — F0039-S0006.
- Model fine-tuning / alternate model families (Future Evolution).

## UI/UX Notes

- No UI. Enables the structured model calls the resolver (S0006) depends on.

## Questions & Assumptions

**Open Questions:**
- [ ] (AI Engineer, feature) Final vLLM image digest, GPU sizing, and whether Ollama is included as a
  developer-convenience provider at first delivery.

**Assumptions (to be validated):**
- The local vLLM endpoint + bearer auth verified 2026-07-21 is the target serving topology; `max_model_len`
  stays 4,096 for the initial profile.

## Definition of Done

- [ ] Acceptance criteria met (async structured interface; mock/scripted/OpenAI-compatible; Phi profile)
- [ ] Edge cases handled (timeout/reset/empty/non-object errors normalized; single pre-response transport retry)
- [ ] Permissions enforced — Phi not browser-exposed; credentials handled securely (documented)
- [ ] Audit/telemetry — provenance recorded as hashes + counts; no raw-content logging (tested)
- [ ] Tests prove shared provider contract across implementations + token-budget enforcement + error mapping
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
