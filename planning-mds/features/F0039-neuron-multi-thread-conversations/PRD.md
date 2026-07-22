---
template: feature
version: 1.1
applies_to: product-manager
---

# F0039: Neuron Durable Conversations & Local Phi Intent Resolution

**Feature ID:** F0039
**Feature Name:** Neuron Durable Conversations & Local Phi Intent Resolution
**Epic:** Neuron Companion (AI Conversational Layer) — Next
**Priority:** Medium
**Phase:** Neuron Companion
**Status:** Planned (requirements committed 2026-07-21 — re-derived from the F0038 as-built seams)

> **Scope note.** This feature was reserved as "Neuron Multi-Thread Conversations". Its `plan` run
> (2026-07-21) re-derived scope from the F0038 as-built architecture and the authored design spec
> [`neuron-phi-intent-security-implementation-spec.md`](./neuron-phi-intent-security-implementation-spec.md)
> (v1.1.0, local Phi runtime verified 2026-07-21). Scope now covers **two inseparable halves**: (1) a
> durable, owner-scoped conversation store + thread-management UX, and (2) replacing Neuron's mock/deterministic
> intent seam with a **locally hosted Microsoft Phi** structured scope-and-intent resolver behind a
> replaceable provider. Both must land together because contextual phrases ("this one", "the renewal we
> discussed") are not reliable until durable thread history and UI anchors exist. The feature folder slug
> remains `neuron-multi-thread-conversations`.

## Feature Statement

**As an** Underwriter / Distribution user working the Neuron companion
**I want** my companion conversations to persist and stay organized across records and sessions, and the
companion to reliably understand natural CRM requests while refusing off-scope and manipulative ones
**So that** my companion work is durable and trustworthy — it resumes where I left off and routes only to
real, authorized CRM capabilities instead of guessing or being talked out of scope.

## Business Objective

- **Goal:** Turn the F0038 Day-at-a-Glance shell into a durable conversation experience and replace the
  deterministic/mock intent classifier with a local Phi structured resolver — while keeping the engine the
  sole authorization boundary and every model output deterministically validated and fail-closed.
- **Metrics (targets validated at feature closeout — see Success Criteria):**
  - **Thread resume rate:** ≥ 60% of returning-user sessions resume an existing thread (vs. starting fresh),
    measured over a 2-week window after rollout.
  - **Intent routing accuracy:** ≥ 95% domain accuracy and ≥ 90% action exact-match on the reviewed
    clear-in-scope holdout set (§30.4).
  - **Security:** 0 authorization bypasses, 0 unregistered-capability routes, and ≥ 95% detect/redirect on
    the reviewed injection set.

## Problem Statement

F0038 shipped a companion shell whose transcript lives in browser memory and whose intent handling is a
deterministic keyword guard plus a `MockProvider`. Two gaps block the companion from being trustworthy:

1. **Conversations are not durable.** A reload loses the transcript; there is no thread list, resume, rename,
   or delete; Day-at-a-Glance is a detached dashboard rather than a replayable assistant message. Users
   cannot rely on the companion as a place their work accumulates.
2. **Intent handling is brittle and mock-backed.** Keyword matching misses natural phrasing, cannot detect
   paraphrased prompt-injection or scope-escape, and cannot handle compound or context-dependent requests.
   A prompt-only model (as the smoke test proved) will happily invent unregistered actions such as
   `show_renewals_needing_attention` and emit shape-valid but contradictory decisions — so it must never be
   trusted without deterministic post-validation.

## Scope & Boundaries

**In Scope:**

- **Durable conversation store (ADR-028 authoritative):** Neuron owns and writes `neuron.*` directly to
  Postgres (via the engine database, NOT through the engine API). Apply the existing `0001_neuron_schema.sql`
  scaffold; add a forward migration for a server-assigned message sequence + idempotency keys.
- **Owner-scoped thread lifecycle:** create, list, get, rename, soft-delete, resume; cursor-paginated,
  replayable message history ordered by server-owned sequence. Threads are private to the creating user.
- **Thread anchoring:** free-form, domain-anchored, and record-anchored threads with auto-title; the anchor
  is **fixed at creation (no re-anchoring)**.
- **Conversation-first Neuron panel:** thread list with new/switch/rename/delete; server-rehydrated
  transcripts; Day-at-a-Glance rendered as a persisted **assistant "Daily Brief" envelope** (structured app
  parts), not a detached local-only dashboard.
- **Local Phi structured resolver (behind a replaceable provider):** one locally hosted
  `microsoft/Phi-4-mini-instruct` runtime serving logically separate capabilities (`crm.scope_guard`,
  `crm.intent_classifier`, `crm.intent_resolver`, `crm.intent_adjudicator`). Initial production path uses one
  composed scope-and-intent generation with independently validated `scope` and `intent` sections.
- **Deterministic guardrails around the model:** preflight (size/encoding/high-certainty injection markers);
  JSON-Schema-constrained output; deterministic cross-field invariants + registry validation; route only to
  registered active domains/actions/heads; fail closed on any model/schema/registry failure.
- **Intent catalog + versioned prompt registry** with provenance (prompt/model/schema/catalog hashes).
- **Shadow mode + evaluation harness** (labeled direct / adversarial / contradiction datasets) and a tested
  rollback to the deterministic resolver.
- **Contextual adjudication (S0009 — GATED):** a second bounded Phi call using sanitized recent history +
  validated UI anchor, **enabled only after** durable context and direct-routing gates pass.

**Out of Scope (this feature):**

- A second live specialist head / additional live zone → **F0040**.
- Cross-zone composition ("the brain") → Later.
- An open-ended user-facing **response composer** (Phi writing prose) → subsequent feature; Neuron uses
  application-owned response copy in this feature.
- Thread sharing, cross-user visibility, full-text thread search → Later.
- Any change to engine authorization, Casbin permissions, or the engine as source of truth.
- Multiple model families / fine-tuned LoRA adapters / encoder routers → Future Evolution (§40 of the spec).

## Personas

Reuses the established Neuron Companion personas (no new personas):

- **Renewal-owning Underwriter (primary):** works renewals attention/summary/outreach in the companion;
  needs durable, resumable threads and reliable in-scope routing.
- **Distribution user (secondary):** authenticated CRM user; the scope guard and owner-scoping apply equally;
  no extra authorization is granted by any classifier decision.

## The Writes — Interaction Contracts (summary; full contracts in the story files)

Every mutation names entry point → action → editable/lifecycle state → persistence evidence → role/status
rules → validation failure → audit/timeline expectation.

1. **Create thread** — panel "New conversation" (or first message) → server creates an owner-scoped
   `neuron.threads` row with immutable anchor + auto-title → persisted (survives restart); owner-scoped;
   invalid anchor → 400; recorded in the operation store.
2. **Rename thread** — thread list → Rename → inline edit → Save → `neuron.threads.title` updated,
   `updated_at` set transactionally → persisted after reload; owner-only (cross-user → 404/denied);
   empty/oversize title → validation error; recorded.
3. **Soft-delete thread** — thread list → Delete → confirm → thread marked deleted (retention per
   `ActivityTimelineEvent` policy) → hidden from list after reload; owner-only; recorded.
4. **Send message** — composer → Send → user message persisted in the versioned envelope with a server
   sequence + client-idempotency key (duplicate keys idempotent) → assistant reply persisted → both replay
   in order after reload; owner-only; recorded.
5. **`renewals.mock_send`** (reuses F0038's existing workflow write) — routed action **requires explicit
   confirmation** (`requires_explicit_confirmation: true`); an inferred intent alone never commits a
   business change; the engine performs authorization and the Identified→Outreach transition; timeline event
   emitted by the engine (unchanged).

## Read Scope

- Companion CRM reads (renewals attention/view/summarize, etc.) remain **permission-respecting via the
  engine** using the forwarded user token. No classifier decision widens data access; `allow` means only
  "eligible to continue through bounded CRM routing".
- The Phi resolver receives **only** the normalized current message + the registered domain/action catalog —
  never CRM records, tokens, tools, or full conversation history.

## Screen Layouts (ASCII)

The conversation-first Neuron panel replaces F0038's dashboard-first shell. Desktop + a narrow variant below.

### Desktop (≥1024px) — Neuron companion panel: thread list + transcript

```text
┌───────────────────────── Neuron Companion ──────────────────────────────┐
│ ┌── Threads ─────────────┐ ┌── Conversation ─────────────────────────┐  │
│ │ [ + New conversation ] │ │  Renewals · anchored: Acme Renewal      │  │
│ │                        │ │  ────────────────────────────────────   │  │
│ │ ▸ Daily Brief   (auto) │ │  ☼ Daily Brief (assistant)              │  │
│ │ ▸ Acme renewal  ⋯      │ │    3 renewals need attention today ▸    │  │
│ │ ▸ Marsh follow-ups     │ │    [ suggested: show attention list ]   │  │
│ │ ▸ (free-form) Q3 not…  │ │                                          │  │
│ │                        │ │  🧑 "show renewals needing attention"    │  │
│ │   ⋯ = rename / delete  │ │  🤖 Here are 3 renewals… (routed)        │  │
│ │                        │ │                                          │  │
│ │   [ empty state: ]     │ │  🧑 "draft outreach for this one"        │  │
│ │   "No conversations    │ │  🤖 Which renewal? (clarify)             │  │
│ │    yet — start one."   │ │  ────────────────────────────────────   │  │
│ └────────────────────────┘ │  [ type a message…            ] [Send]  │  │
│                            └──────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────┘
States: loading (skeleton list + transcript) · empty (no threads) · switching
(transcript re-hydrates from server) · send-failure (inline retry, message kept)
· redirect (bounded CRM redirect bubble) · clarify (bounded question bubble).
```

### Narrow (<768px, mobile/iPad) — single column, transcript-first with thread drawer

```text
┌───────────── Neuron ─────────────┐
│ ☰ Threads   Acme renewal      +  │   ☰ opens the thread drawer
│ ──────────────────────────────── │   (list/switch/rename/delete)
│  ☼ Daily Brief (assistant)       │
│    3 renewals need attention ▸   │
│                                  │
│  🧑 show renewals needing attn.   │
│  🤖 Here are 3 renewals… (routed) │
│                                  │
│  🧑 draft outreach for this one   │
│  🤖 Which renewal? (clarify)      │
│ ──────────────────────────────── │
│ [ type a message…       ] [Send] │
└──────────────────────────────────┘
Drawer (☰) overlays the thread list; selecting a thread closes the drawer and
re-hydrates the transcript from server-owned history.
```

## Requirements Clarification (resolved at G1)

Vague/ambiguous areas from the provisional skeleton were quantified during this run:

- **"Fast" / latency:** direct-route added latency budget targets p95 within the spec's per-stage budgets
  (scope+intent composed call, timeouts 1500–1800 ms; §10.7/§11.7); measured on target hardware at closeout.
  Fail-closed on timeout — no unbounded wait.
- **"Persist" / durability:** threads and messages **survive Neuron process restart**; history is
  server-owned and replayed in **server-sequence order**, not browser memory or timestamp-only.
- **"Private":** all thread/message reads and writes are **owner-scoped**; cross-user access tests must fail
  closed (404/deny). No classifier decision grants access.
- **"Understand requests":** routing is measured against a reviewed labeled dataset with explicit acceptance
  thresholds (§30.4), not "should work".
- **"Secure":** injection/scope-escape handling is measured on a reviewed adversarial set (≥95% detect/
  redirect) and enforced by deterministic preflight + fail-closed validation, never by prompt text alone.
- **Mutation vs. display:** thread rename/delete and message send are **editable/persisted writes** with
  explicit save + persistence evidence; Daily Brief and routed reads are **display**; `renewals.mock_send`
  is a confirmed write. No "display or capture" ambiguity remains.

## Notes for the Architect (Phase B)

- ADR-028 remains authoritative (Neuron writes `neuron.*` directly). Correct any "through the engine"
  persistence wording. Author a new ADR for the **local Phi structured intent-resolution + fail-closed
  validation** decision (physical-one-model / logically-separate-capabilities; deterministic authority
  boundaries; provider seam; shadow→direct→gated-adjudication rollout).
- Complete `neuron-api.yaml` (thread/history/idempotency contract) and the resolver/scope/intent/context
  JSON schemas + intent catalog contract.
- Complete the F0039 ontology mapping (feature + 9 stories) and add canonical nodes for the reusable
  semantics introduced (durable Neuron conversation store, Phi intent-resolution capability, fail-closed
  intent-validation).

## Dependencies

- **F0038 — Neuron Day-at-a-Glance Shell** (Done/archived 2026-07-02): persistence-home ADR (ADR-028),
  `neuron.*` schema scaffold (`0001_neuron_schema.sql`), versioned message envelope, scope-guard seam,
  `ModelProvider`/`ModelRouter` seam, `thread_id` seam, mock-send workflow write. **Hard dependency.**
- **ADR-027** — Neuron Companion A2A-aligned orchestration foundation.
- **ADR-028** — Neuron persistence & outreach authorization (persistence ownership).
- **AI Engineer role** — owns the Phi provider integration + evaluation harness (prerequisite capability).
- **Local Phi inference service** — vLLM OpenAI-compatible server hosting `microsoft/Phi-4-mini-instruct`
  (verified locally 2026-07-21). GPU + serving provenance recorded per story.

## Success Criteria (acceptance gates — approved by product / architecture / security / AI engineering)

Measured on the reviewed holdout set at feature closeout (spec §30.4):

- 0 unregistered agent routes; 0 unregistered action routes; 0 authorization bypasses; 0 tool calls from
  classifier agents.
- 100% fail-closed behavior on provider-failure tests.
- ≥ 98% schema-valid output with constrained decoding.
- ≥ 95% domain accuracy on clear in-scope messages; ≥ 90% action exact-match on clear single-action messages.
- ≥ 95% redirect precision on obvious non-CRM messages; ≥ 95% detect/redirect on the reviewed injection set.
- Durable-conversation checks: created thread + messages survive process restart; reload/switch replay
  server-owned history in order; rename/delete owner-scoped; Daily Brief persisted as an assistant envelope.
- Rollback to the deterministic resolver documented and tested.

## Related User Stories

| ID | Title | Priority | Notes |
|----|-------|----------|-------|
| [F0039-S0001](./F0039-S0001-durable-neuron-conversation-store.md) | Durable Neuron conversation store | High | Postgres repo, server sequence, idempotency, restart persistence |
| [F0039-S0002](./F0039-S0002-owner-scoped-thread-and-history-api.md) | Owner-scoped thread & history API | High | create/list/get/rename/delete + paginated history; owner-scoped |
| [F0039-S0003](./F0039-S0003-conversation-first-neuron-panel.md) | Conversation-first Neuron panel | High | thread list UX; server-rehydrated transcript; Daily Brief envelope |
| [F0039-S0004](./F0039-S0004-structured-provider-and-local-phi-profile.md) | Structured provider & local Phi profile | High | async structured provider; vLLM/Phi profile; provenance |
| [F0039-S0005](./F0039-S0005-catalog-prompt-registry-resolution-contract.md) | Catalog, prompt registry & composed resolution contract | High | intent catalog, versioned prompts, schemas, invariants |
| [F0039-S0006](./F0039-S0006-preflight-and-one-call-direct-resolver.md) | Deterministic preflight & one-call direct resolver | High | preflight + composed scope/intent; fail-closed validation |
| [F0039-S0007](./F0039-S0007-dispatcher-persistence-provenance-integration.md) | Dispatcher, persistence & provenance integration | High | persist every turn; resolve before dispatch; traceability |
| [F0039-S0008](./F0039-S0008-evaluation-shadow-mode-and-rollout.md) | Evaluation, shadow mode & rollout | High | datasets, shadow mode, gates, tested rollback, load tests |
| [F0039-S0009](./F0039-S0009-contextual-adjudicator-gated.md) | Contextual adjudicator (GATED follow-on) | Medium | bounded second Phi call; enabled only after S0001–S0008 gates pass |

## Non-Goals (explicit)

Neuron does not, in this feature: authorize CRM reads/writes; determine Casbin permissions; bypass the
engine; execute model-generated tools; expose Phi to the browser; become a general assistant; route to
model-invented agent/action IDs; persist chain-of-thought or raw prompts in general telemetry; treat
model output as calibrated confidence; make destructive changes from inferred intent alone; add a second
live specialist head (F0040); or use Phi to write open-ended user-facing prose.
