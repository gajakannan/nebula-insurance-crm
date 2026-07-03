# Code Review Report — F0038 Neuron Day-at-a-Glance Shell

**Run:** `2026-07-01-90a75ace` · **Role:** Code Reviewer · **Stage:** G3 · **Reviewed:** 2026-07-02

**Result:** APPROVED WITH RECOMMENDATIONS

The F0038 implementation is well-structured, faithful to the assembly plan and ADR-027/028,
and backed by a green multi-runtime test suite (engine 491 / neuron 116 / FE 17) with SAST
0 findings. The code is sound and I recommend approval. Three follow-ups below — one
high-priority (source-control hygiene) and two low-priority cleanups — do not block the
correctness of the change but must be dispositioned (the high one before merge/G6).

## Scope Reviewed

Working-tree diff for F0038 across three runtimes: `neuron/app/**` + `crm_agents/**` +
`orchestration/plans/**`; the new/modified engine files (`NeuronCompanionTelemetry*`,
`RenewalEndpoints`, `RenewalService`, `WorkflowStateMachine`, `RenewalRepository`,
`OutreachContentGuard`, renewal DTOs); and `experience/src/features/neuron/**`. Read with a
focus on the security-relevant seams, mutation atomicity, and the message/component contract.

## Strengths (verified, not assumed)

- **Statelessness + typed upstream errors** (`engine_client.py`): forwarded token only, never
  logged; transport / 401-403 / ≥500 all map to typed errors, no 500/stack leak.
- **Injection-first scope guard** (`scope_guard.py`): injection markers classified before any
  CRM keyword; fail-safe to redirect on classifier error; grants no authorization.
- **Mutation safety** (engine): outreach draft is engine-first with provenance; mock-send is an
  atomic `Identified → Outreach` transition + both timeline events, `renewal:draft_outreach`
  Casbin-gated; no real SMTP.
- **No model markup execution** (FE): only registered component ids with AJV-validated props
  render; unknown id → safe fallback.

## Recommendations

- [high] Commit the uncommitted F0038 implementation before merge — owner: DevOps; follow-up: only the Phase-A/B scaffold and planning docs are committed today (reflog a7cb15c, 321dc4b); the working tree carries the tested implementation (69 files in the neuron tree, 13 new engine files plus unstaged edits, 16 experience files), which must be staged and committed on the feature branch before merge.
- [low] Remove dead hook useNeuronChat.ts and its barrel export — owner: Frontend; follow-up: the hook is unused (only its own feature-barrel re-export references it) and is superseded by useSendMessage; delete it and re-run the FE test suite.
- [low] Remove the legacy hyphenated neuron/crm-agents scaffold directory — owner: AI Engineer; follow-up: three placeholder READMEs superseded by the real crm_agents package (already excluded from the image build); git rm the legacy directory.

## Notes (informational, no action required)

- `NeuronPanel.tsx` shows 0% unit coverage but is NOT dead code — `RightChatPanel.tsx` imports
  it and it renders `<DayAtAGlance/>`. It is the production wrapper, exercised by the
  in-container deployability smoke. A shell smoke test would be nice-to-have, not required.

**Result:** APPROVED WITH RECOMMENDATIONS
