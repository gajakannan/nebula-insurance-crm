# G2 Self-Review — F0038 Neuron Day-at-a-Glance Shell

**Run:** `2026-07-01-90a75ace` · **Stage:** G2 · **Recorded:** 2026-07-02
**Mode:** clean · **LLM:** mocked (deterministic)

**Result:** PASS

Implementer self-review across the eight stories before handing to independent code +
security review (G3). All acceptance criteria are met with real passing tests; the honest
risks and follow-ups below are recorded for the reviewers rather than papered over.

## Scope Review

Delivered exactly the F0038 assembly-plan scope across three runtimes:

- **Neuron** (`neuron/app/**`, `crm_agents/cards/**`, `orchestration/plans/**`): stateless
  FastAPI runtime; Agent-Card + tool registries; fail-fast versioned YAML plan loader;
  `neuron.*` operation store (in-memory behind the repo interface + durable 6-table
  migration); engine client forwarding the user token with typed upstream errors; mocked
  model provider behind the router seam; A2A task manager; glance orchestration; live
  Renewals + stub zone heads; outreach drafter; mock-send action; deterministic scope guard
  + intent classifier on `POST /v1/messages`; fire-and-forget companion telemetry.
- **Engine** (`engine/src/Nebula.Api/**`, `Nebula.Application/**`, `Nebula.Infrastructure/**`):
  renewals needs-attention + companion-context reads; outreach-draft persist (engine-first,
  provenance); atomic `Identified → Outreach` mock-send transition; `POST
  /internal/telemetry/neuron-companion` ingest (batch, Serilog `Nebula.Neuron.Companion`,
  user_id-must-match-subject, closed-shape PII boundary).
- **Frontend** (`experience/src/features/neuron/**`): Day-at-a-Glance shell with per-head zone
  slots, registered-component-only rendering (no model markup), editable in-chat draft,
  `[Send (mock)]`, and the active composer.

**In-scope but intentionally deferred:** live-LLM behavior (mocked this run), real SMTP
(mock-send simulates), thread switching (F0039), and DAST against a deployed environment
(platform/CI). **No scope creep** — nothing outside the plan was added.

Manifest scope booleans reconciled to the as-built change set: `frontend_in_scope` and
`deployment_config_changed` are true (React surface + Dockerfile/compose/config). Token
forwarding + prompt-injection surface make this security-relevant; the Security Reviewer is
required per the plan.

## Acceptance Criteria Review

Every acceptance criterion for **S0001–S0008** is satisfied and backed by an automated test.
The criterion-by-criterion table lives in `test-execution-report.md` (§ Acceptance-Criteria →
Test Mapping). Highlights of the invariants I specifically self-checked:

- **No model-generated markup executes** — the frontend renders only registered component
  identifiers with validated props; an unregistered id yields a safe fallback (`test_envelope`,
  `componentRegistry.test`).
- **Authorization is engine-side** — Neuron forwards the token unverified; the engine denies
  (403) unauthorized renewal reads, draft, and mock-send. Neuron never fabricates rows.
- **Atomicity** — mock-send commits the transition + both timeline events together or not at
  all; no "sent" event without the transition.
- **Guard is injection-first** — injection markers are classified before any keyword routing,
  so user-supplied "act as a general assistant" instructions cannot escape CRM scope; the
  classifier fails safe to redirect.
- **Telemetry is non-blocking** — emission is fire-and-forget; a telemetry failure cannot
  break draft/mock-send (proved by an injected-failure test), and no fabricated "end"
  timestamp is emitted when a draft never happens.

## Implementation Risks

Honest, non-blocking risks handed to the reviewers:

1. **Candidate dead code (FE):** `NeuronPanel.tsx` and `useNeuronChat.ts` are at 0% coverage
   and appear superseded by the S0007 composer flow (`Composer.tsx` + `useSendMessage.ts`).
   If the code reviewer confirms, the fix is removal, not new tests.
2. **`app/main.py` at 0% unit coverage:** FastAPI wiring is proven by the in-container smoke
   (`deployability-check.md`) rather than unit tests — acceptable, but noted.
3. **LLM mocked:** classifier/draft behavior is deterministic this run. Live-model robustness
   (esp. novel injection phrasings) is not exercised here; the deterministic injection-first
   guard is the safety floor.
4. **Frontend dependency advisories (9):** pre-existing and not introduced by F0038; recommend
   a repo-level SCA waiver rather than in-feature remediation.
5. **DAST deferred:** no new unauthenticated dynamic surface (both services are auth-gated);
   DAST belongs at the platform/CI layer against a deployed environment.

## Validation Evidence

- Tests: `test-plan.md`, `test-execution-report.md` — engine **491**, neuron **116**,
  frontend **17** (all green).
- Coverage: `coverage-report.md` — neuron 88%, frontend 86.2%, engine Cobertura.
- Deployability: `deployability-check.md` — build + health/readiness smoke PASS.
- Security scans (QE-run, real tooling): `artifacts/security/` — SAST 0 findings, secrets 0
  leaks, engine + neuron dependency SCA clean.

**Result:** PASS
