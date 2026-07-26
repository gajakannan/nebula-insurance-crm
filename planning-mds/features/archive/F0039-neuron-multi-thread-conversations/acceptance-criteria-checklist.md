# F0039 — Acceptance-Criteria Checklist

> PM-owned rollup of the acceptance criteria across the 9 stories. Full Given/When/Then criteria live in each
> story file; this is the closeout checklist the feature run validates against (with the §30.4 gates).

## Durable conversations (S0001–S0003)

- [ ] `neuron.*` persisted in Postgres (ADR-028: Neuron writes directly, not through the engine API); the
  `0001` scaffold applied and extended only via forward migration.
- [ ] Threads + messages **survive a Neuron process restart**.
- [ ] Messages ordered by a **server-assigned sequence**; `(thread_id, sequence)` unique; history is
  cursor-paginated and resumes in order.
- [ ] Duplicate client-message / Daily Brief keys are **idempotent** (no duplicate rows).
- [ ] Persistence failure **fails safe before routing**.
- [ ] Create / list / get / rename / soft-delete are **owner-scoped**; cross-user access **fails closed**
  (tested); anchor is **immutable** after creation; delete + retention are explicit.
- [ ] Panel is **conversation-first**: resumes the last visible thread (or picker); transcript is
  **server-rehydrated**; thread switch/rename/delete + loading/empty/switching/send-failure states covered.
- [ ] Day-at-a-Glance is a **persisted assistant "Daily Brief" envelope** with structured app parts.

## Local Phi intent resolution (S0004–S0008)

- [ ] Async **structured** provider interface; mock / scripted / OpenAI-compatible implementations pass a
  shared contract suite; Phi profile targets `microsoft/Phi-4-mini-instruct` (vLLM, 4,096-token budget);
  provenance recorded (model revision, image digest, GPU, latency, concurrency); **no raw-content logging**;
  **Phi never exposed to the browser**.
- [ ] Intent catalog schema-validated and maps active domains/actions → registered heads; versioned prompts +
  schemas load with **hashes**; JSON Schema rejects unknown shape/additional props; deterministic invariants
  reject contradictory scope/intent; regression fixtures (`redirect`+`renewals.list_attention`, invented
  `show_renewals_needing_attention`) rejected; invalid catalog/prompt/schema **prevents readiness**.
- [ ] Deterministic preflight (size/encoding/NFKC/high-certainty markers); **one composed Phi call** returns
  separate scope+intent; only **registered active** domains/actions route; head resolved from the **trusted
  catalog**; missing entity **clarifies**; inactive actions never execute; timeout/malformed/invariant failure
  → **no engine call**; logical scope+intent share one physical-call provenance.
- [ ] Every inbound message persisted before routing; resolver runs **before** head dispatch; clarify/redirect/
  failure/routed envelopes all persisted; full traceability; **engine authorization unchanged**;
  `renewals.mock_send` still requires explicit confirmation.
- [ ] Reviewed direct / adversarial / contradiction datasets; **shadow mode** records but never executes;
  §30.4 gates (0 unregistered/authz-bypass routes, 100% fail-closed, ≥98% schema-valid, ≥95% domain accuracy,
  ≥90% action exact-match, ≥95% redirect precision, ≥95% injection detect/redirect) **pass before** enabling
  direct routing; **rollback to deterministic tested**; load tests at 1/2/4 concurrent.

## Contextual adjudication — GATED (S0009)

- [ ] Implemented/enabled **only after** S0001–S0008 direct-routing + context gates pass.
- [ ] Bounded context: ≤4 recent turns, ≤6,000 chars, whole call ≤4,096 tokens; trusted UI anchors
  revalidated; unauthorized data excluded.
- [ ] A final result **cannot request another adjudication**; **write-like ambiguity clarifies**; all S0006
  deterministic validation still applies.

## Cross-cutting

- [ ] The model owns **none** of authentication, authorization, registration, validation, confirmation, or
  persistence policy (spec §7.3).
- [ ] No chain-of-thought or raw prompts in general telemetry; no model-generated confidence used.
- [ ] Response copy is **application-owned** (no open-ended model prose in this feature).
