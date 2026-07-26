# F0041 — Neuron Contextual Intent Adjudicator (gated follow-on)

**Status:** Planned (gated) · **Depends on:** F0039 · **Governed by:** ADR-035

Promoted from `F0039-S0009` when F0039 closed (feature run `2026-07-25-273d5672`). It was
never built there: its gate opens only after F0039's direct-routing and context gates pass,
and F0039 finished with the §30.4 rollout gate **red** on routing accuracy, so direct
routing is withheld and the companion runs in shadow mode.

Until this feature is built, an `adjudicate` decision from the resolver degrades to a
bounded clarify — implemented and tested in F0039
(`neuron/app/intent/validation.py`, `test_intent_validation.py`).

## Entry conditions

1. F0039's §30.4 gates pass on a reviewed evaluation run (`python -m app.intent.evaluate_cli`).
2. Direct routing is enabled (`NEURON_INTENT_MODE=direct`) and stable.
3. Durable-context and contextual-evaluation gates defined and passing.

## Stories

| Story | Title | Status |
|-------|-------|--------|
| F0041-S0001 | Contextual adjudicator (bounded second Phi call) | Planned (gated) |
