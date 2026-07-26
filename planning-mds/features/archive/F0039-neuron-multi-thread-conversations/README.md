# F0039 — Neuron Durable Conversations & Local Phi Intent Resolution

**Status:** Planned — requirements committed (plan run `2026-07-21-6eeb172f`); no implementation yet
**Priority:** Medium
**Phase:** Neuron Companion (epic — Next)

## Overview

Turns the F0038 Day-at-a-Glance shell into a **durable conversation experience** and replaces Neuron's
mock/deterministic intent seam with a **locally hosted Microsoft Phi** structured scope-and-intent resolver
behind a replaceable provider — while keeping the engine the sole authorization boundary and every model
output deterministically validated and fail-closed.

Two inseparable halves:

1. **Durable conversations** — owner-scoped Postgres thread store (ADR-028: Neuron writes `neuron.*`
   directly), thread lifecycle (create/list/switch/rename/delete/resume), server-owned replayable history,
   and a conversation-first panel where Day-at-a-Glance is a persisted "Daily Brief" assistant message.
2. **Local Phi intent resolution** — one composed, JSON-Schema-constrained scope-and-intent call guarded by
   deterministic preflight + invariants + registry validation, an intent catalog + versioned prompt registry,
   shadow mode + an evaluation harness with acceptance gates, and a tested rollback to the deterministic
   resolver. Contextual adjudication (S0009) is a **gated follow-on**.

The folder slug remains `neuron-multi-thread-conversations`; the display name was updated during the plan run
to reflect the re-derived scope.

## Documents

| Document | Purpose |
|----------|---------|
| [PRD.md](./PRD.md) | Committed requirements, personas, scope, screen layouts, success criteria |
| [neuron-phi-intent-security-implementation-spec.md](./neuron-phi-intent-security-implementation-spec.md) | Authored design source (durable conversations + local Phi intent resolution) |
| [acceptance-criteria-checklist.md](./acceptance-criteria-checklist.md) | Per-story acceptance-criteria rollup |
| [feature-assembly-plan.md](./feature-assembly-plan.md) | Phase B architecture / assembly plan (for the feature action's G0) |
| [STATUS.md](./STATUS.md) | Planning tracker, required-signoff matrix, provenance |
| [GETTING-STARTED.md](./GETTING-STARTED.md) | Setup + how to verify |

Epic intake: [`../F0038-neuron-day-at-a-glance-shell/intake-brief.md`](../F0038-neuron-day-at-a-glance-shell/intake-brief.md).

## Stories

| ID | Title | Status |
|----|-------|--------|
| F0039-S0001 | Durable Neuron conversation store | Planned |
| F0039-S0002 | Owner-scoped thread & history API | Planned |
| F0039-S0003 | Conversation-first Neuron panel | Planned |
| F0039-S0004 | Structured provider & verified local Phi profile | Planned |
| F0039-S0005 | Catalog, prompt registry & composed resolution contract | Planned |
| F0039-S0006 | Deterministic preflight & one-call direct resolver | Planned |
| F0039-S0007 | Dispatcher, persistence & provenance integration | Planned |
| F0039-S0008 | Evaluation, shadow mode & rollout | Planned |
| F0039-S0009 | Contextual adjudicator (GATED follow-on) | Planned (gated) |

**Total Stories:** 9
**Completed:** 0 / 9

## Dependencies

- **F0038** (Done/archived) — `neuron.*` schema scaffold, message envelope, scope-guard/model-provider seams,
  mock-send workflow. **Hard dependency.**
- **ADR-027** (A2A orchestration), **ADR-028** (persistence ownership), and the new Phi intent-resolution ADR.
- **AI Engineer role** + a **local Phi (vLLM) inference service** hosting `microsoft/Phi-4-mini-instruct`.
