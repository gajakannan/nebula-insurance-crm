# F0039 — Neuron Durable Conversations & Local Phi Intent Resolution — Getting Started

> Requirements are committed (plan run `2026-07-21-6eeb172f`). Implementation happens in the `feature`/`build`
> action. This page captures prerequisites and the target verification path.

## Prerequisites

- [ ] **F0038 delivered** (Done/archived 2026-07-02) — persistence-home ADR (ADR-028), `neuron.*` schema
  scaffold (`0001_neuron_schema.sql`), versioned message envelope, scope-guard seam, `ModelProvider`/
  `ModelRouter` seam, `thread_id` seam, and mock-send workflow are in place.
- [ ] **Postgres** reachable for the `neuron.*` schema (engine database; Neuron writes it directly per ADR-028).
- [ ] **Local Phi inference service** — a vLLM OpenAI-compatible server hosting
  `microsoft/Phi-4-mini-instruct` (bearer auth, `max_model_len=4096`). Verified locally 2026-07-21 at
  `http://127.0.0.1:8000/v1`. Record model revision + vLLM image digest separately (the server does not
  report a pinned HF commit).
- [ ] Read [`PRD.md`](./PRD.md) and the design source
  [`neuron-phi-intent-security-implementation-spec.md`](./neuron-phi-intent-security-implementation-spec.md).

## How to Verify (target — full acceptance at the feature run)

**Durable conversations**
1. Create multiple companion threads (free-form, domain-anchored, record-anchored); they persist across
   sessions and **survive a Neuron process restart**.
2. List, switch, rename, and soft-delete threads; **resume** a prior thread with ordered history intact.
3. Threads are **private to the creating user** (cross-user access fails closed).
4. Day-at-a-Glance renders as a **persisted "Daily Brief" assistant message**, replayable in the transcript.

**Local Phi intent resolution**
5. An in-scope renewal request routes to the renewals head; a non-CRM or injection message causes **no engine
   call** (bounded redirect); an ambiguous request **clarifies**.
6. Invalid / contradictory / unregistered model output (e.g. the invented `show_renewals_needing_attention`,
   or a `redirect` carrying `renewals.list_attention`) is **rejected** deterministically — fail closed.
7. Shadow mode records Phi decisions without executing them; direct routing is enabled only after the §30.4
   gates pass; rollback to the deterministic resolver works with no DB/schema change.

## Notes

- Builds directly on F0038's reserved seams; a thread's **anchor is fixed at creation** (no re-anchoring).
- The model is a **routing signal, never an authorization authority** — the engine remains the sole boundary.
- **S0009 (contextual adjudicator) is gated** — implemented/enabled only after S0001–S0008 gates pass.
