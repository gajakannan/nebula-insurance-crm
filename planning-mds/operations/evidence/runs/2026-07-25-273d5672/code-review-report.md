# Feature Code Review Report — F0039 run `2026-07-25-273d5672`

**Role:** Code Reviewer · **Gate:** G3 · **Date:** 2026-07-25
**Reviewed:** 64 changed paths (`artifacts/diffs/changed-files.txt`), against the G0-reconciled
assembly plan, `SOLUTION-PATTERNS.md`, and the story acceptance criteria.

## Summary

- **Assessment: APPROVED WITH RECOMMENDATIONS**
- Files reviewed: 64 · Findings: **0 critical, 0 high, 4 medium, 3 low**
- **Re-review 2026-07-25 (post-G4 operator direction): M1 RESOLVED.** 3 medium / 3 low remain open as follow-ups.

## Vertical Slice Completeness

- [x] Backend complete — durable store, thread API, dispatcher integration
- [x] Frontend complete — conversation panel, thread list, server-rehydrated transcript
- [x] AI layer complete — provider, catalog, resolver, evaluation
- [x] Tests complete — 410 neuron (27 real-Postgres, 13 live-Phi) + 32 frontend
- [x] Can be deployed independently — config-only rollback, additive migration

## Findings

### Medium

**M1 — `_finish` swallows a persistence failure after the reply is produced. — ✅ RESOLVED**

**Resolution (verified on re-review):** the envelope is now marked with a `failed` status
part carrying "This reply could not be saved to your conversation." when the assistant
write fails. The reply is still returned — losing the answer as well as the record helps
nobody — but it no longer *looks* saved. Three regression tests cover it: the marker is
present on failure, the content survives, and a successful turn carries no marker. Suite
413 passing, coverage still 90%.

*Original finding, retained for audit:*
`neuron/app/messages.py` catches `PersistenceUnavailableError` when writing the *assistant*
envelope and returns the message anyway. The user sees a reply that is not in their
transcript; on reload it vanishes, and the thread shows a user turn with no answer. The
comment argues the reply should not be lost, which is reasonable, but the asymmetry with
persist-first deserves an explicit product decision rather than an inline judgement.
**Recommendation:** surface a "not saved" marker in the envelope, or fail the turn. Not
blocking — the failure window requires the store to die mid-turn, and the inbound message
is already durable.

**M2 — `evaluation.py` reaches into resolver privates.** `self._resolver._catalog` and
`._prompt` (5 sites). It works because both live in the same package, but it couples the
harness to the resolver's internals and will break silently on refactor.
**Recommendation:** expose read-only `catalog` / `prompt` properties on `IntentResolver`.

**M3 — Four function-local imports in `evaluation.py`** (`_fail_closed_rate`,
`provenance`). Used to dodge a cycle with `models.*`, but they hide the real dependency
and defer import errors to call time. **Recommendation:** hoist to module scope, or make
the dependency explicit by injecting the probe factory.

**M4 — `postgres.py` error-translation branches are the least-tested code** (70%).
Behavioural paths are well covered; the gap is transport-fault mapping. **Recommendation:**
add driver-level fault injection in a follow-up rather than leaving the branches unexercised.

### Low

**L1 — `_confirmation_text` builds user copy by joining action ids** (`renewals.mock_send`),
leaking an internal identifier into a user-facing sentence. Prefer the catalog `description`.

**L2 — `MAX_ACTIONS_PER_MESSAGE` (catalog) and the schema's `maxItems: 4`** encode the same
policy in two places; they can drift. Consider generating one from the other.

**L3 — `subprocess` for `git rev-parse`** in the eval harness uses a partial path
(bandit B607). No untrusted input, dev-path only — accepted, noted for hygiene.

## Pattern Compliance

- [x] Clean architecture respected — persistence behind an interface, no framework leakage
      into domain modules
- [x] SOLID — the provider Protocol and repository ABC are the two extension seams, both
      satisfied by multiple implementations
- [x] `SOLUTION-PATTERNS.md` applied — ProblemDetails errors, audit fields, soft delete
- [x] Test coverage 90% ≥ 80%

## Acceptance Criteria

- [x] All in-scope story ACs met (mapping in `test-plan.md`)
- [x] Edge cases handled — concurrency, retry, malformed model output, cross-user access
- [x] Error scenarios covered — every failure path bounded and tested

## What I specifically checked and liked

The **failure paths are the well-designed part**. `should_route` is false and
`target_head_card_id` is `None` on every rejection, so "no engine call" is structural
rather than a rule someone must remember. Retaining the deterministic guard as a live,
tested mode instead of dead code is the right call for a rollback that must work under
pressure. And the decision *not* to tune the prompt against the holdout set to force a
green gate is the correct engineering judgement, recorded honestly.

## Recommendation

**APPROVE.** No critical or high findings. M1–M4 are follow-ups, not merge blockers;
M1 should get an explicit product answer before direct routing is enabled.
