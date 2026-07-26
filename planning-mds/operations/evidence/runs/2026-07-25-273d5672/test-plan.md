# Test Plan — F0039 run `2026-07-25-273d5672`

**Role:** Quality Engineer · **Gate:** G2 · **Date:** 2026-07-25
**Scope:** S0001–S0008 (S0009 gated-deferred).

## Strategy

The feature has two halves with different risk profiles, so they are tested differently.

**The durable half (S0001–S0003)** is about data correctness under concurrency and
restart. Assertions against a dict prove nothing about a Postgres constraint, so its
invariants — server sequence, idempotency, owner-scoping — are tested against **real
Postgres**, with the in-memory suite asserting the same behaviour so both backends stay
substitutable.

**The intent half (S0004–S0008)** is about a component that is allowed to be wrong. Tests
therefore concentrate on what happens when the model misbehaves: contradictory output,
invented actions, malformed JSON, timeouts, injection. The shared provider contract suite
runs identically against mock, scripted, and OpenAI-compatible providers, and a separate
live suite exercises the real Phi endpoint (skipped, not failed, when it is absent).

## Acceptance-criteria coverage

| Story | Key criteria | Where tested |
|-------|--------------|--------------|
| S0001 | Restart durability, server sequence, idempotent append, fail-safe-before-routing | `test_postgres_store.py` (27, real Postgres), `test_persistence.py` |
| S0002 | Owner-scoped CRUD, cursor history, cross-user fails closed, ProblemDetails | `test_threads_api.py` (41, incl. HTTP surface) |
| S0003 | Server-rehydrated transcript, thread UX, empty/error states | `NeuronConversation.test.tsx` (32) |
| S0004 | Async structured completion, provenance, normalized errors, budget, one-retry | `test_structured_provider.py` (64), `test_local_phi_live.py` |
| S0005 | Catalog cross-checks, prompt hashes, schema + invariant rejection, regression fixtures | `test_intent_catalog.py`, `test_intent_validation.py` (76) |
| S0006 | Preflight limits/markers, one-call resolution, fail-closed, provenance | `test_intent_resolver.py` (36), live resolver suite |
| S0007 | Persist-first, resolve-before-dispatch, all outcomes persisted, traceability | `test_dispatcher_integration.py` (19) |
| S0008 | Datasets, §30.4 gates, shadow inertness, rollback, load 1/2/4 | `test_evaluation_and_rollout.py` (21) |

## Security test classes

Owner-scoping (cross-user read/write/rename/delete/history), prompt-injection refusal,
non-disclosure of rule details, redaction-by-shape of provenance, and secret handling are
each covered by explicit negative tests rather than being inferred from happy paths.

## Out of scope

S0009 (gated). Engine-side authorization is unchanged and remains covered by the engine's
own suite — this feature adds no authorization path.
