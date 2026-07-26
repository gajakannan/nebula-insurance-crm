# Feature Security Review Report — F0039 run `2026-07-25-273d5672`

**Role:** Security Reviewer · **Gate:** G3 · **Date:** 2026-07-25
**Scope:** the whole slice, with particular attention to the new trust boundary — a local
LLM now participates in routing decisions.

## Summary

- **Assessment: PASS WITH RECOMMENDATIONS**
- Findings: **0 critical, 0 high, 2 medium, 2 low**

## The central question

A model is now in the request path. The question is not "can the model be tricked" — it
can, and it was, repeatedly, in testing. The question is **what a tricked model can
actually cause**. The answer here is: propose a route, and nothing more.

That holds because of three structural properties I verified rather than took on trust:

1. **The model is given nothing to leak.** The resolver sends only the normalized message
   and the active catalog — no records, no bearer token, no tool handles, no history.
   Asserted by test, including a negative check for `Bearer`, `token`, `owner_user_id`,
   and `thread_id` in the system prompt.
2. **The model's output cannot name a head.** Domains, actions, and the target head come
   from the reviewed `intent-catalog.yaml`. A schema-shaped attempt to supply a head id is
   ignored (tested). Both story regression fixtures — a `redirect` still carrying a routed
   action, and an invented action — fail closed.
3. **Authorization is untouched.** No classifier decision grants access; the engine still
   authorizes every read and write as the user. `allow` means only "eligible to continue".

## Scan verdict (four classes — I am accountable for this)

| Class | Ran | Result | Verdict |
|-------|-----|--------|---------|
| dependency | Yes (`pip-audit`) | No known vulnerabilities | **PASS** |
| sast | Yes (`bandit`) | 0 high, 9 medium, 3 low | **PASS** — see S1 |
| secrets | Yes (pattern fallback) | 1 candidate, non-secret | **PASS WITH RECOMMENDATION** — see S2 |
| dast | No | Waived | **ACCEPTED** — see below |

**S1 — the 9 bandit B608 SQL-injection findings are false positives.** I re-verified
independently rather than accepting QE's assessment: every f-string interpolation in
`postgres.py` resolves to a module constant (`_THREAD_COLUMNS`, `_MESSAGE_COLUMNS`) or a
code literal (`keyset`, `seq_filter`); `{thread_id}`/`{run_id}` appear only in exception
messages. All caller-supplied values are bound via `%s`. No injection vector.

**DAST waiver accepted.** The slice adds six HTTP endpoints, all bearer-authenticated and
owner-scoped, with cross-user access covered by explicit negative tests at both the service
and HTTP layers (404, never 403 — see below). A ZAP baseline against an unauthenticated
surface would have added little here. Recommend running DAST before the first deployed
environment regardless.

## Control Checks

- [x] **Authorization coverage complete** — owner-scoping enforced in the WHERE clause of
      every statement, never as a post-fetch check in Python
- [x] **Input validation enforced** — preflight bounds bytes/chars/lines/repeat-runs,
      rejects null bytes, NFKC-normalizes before limits are applied
- [x] **No secrets in code** — the Phi key is read from a gitignored file, never
      command-line, never logged; absent from every evidence artifact
- [x] **Auditability** — A2A runs + digest-only tool calls; provenance is redaction-by-shape

## Findings

### Medium

**S2 — secrets coverage is weaker than policy intends.** `gitleaks` is unavailable on this
host; a pattern scan ran instead. It is genuinely weaker (no entropy analysis, no history
scan). The one candidate — the local compose credential `postgres:postgres` used as a test
default, already on `main` — is not a production secret. **Recommendation:** run gitleaks
in CI before this reaches a deployed environment. Not blocking for a shadow-mode rollout.

**S3 — `model_revision` and `image_digest` are unpinned** (`null` in `models.yaml`). A
decision recorded today cannot be reproduced against a known model build, which weakens
the audit trail the provenance design exists to provide. **Recommendation:** pin both at
deploy time; treat as required before direct routing.

### Low

**S4 — non-disclosure verified.** An injection redirect and an off-topic redirect return
byte-identical copy (tested), and no rule or marker name reaches the user. Preflight marker
hits return **200**, not an error, so the status code leaks nothing either. This is the
right design; recorded so a future change does not casually differentiate them.

**S5 — thread-existence non-disclosure verified.** Cross-user access returns **404, not
403**, identical to a genuinely unknown id — tested at both layers. A 403 would confirm the
thread exists.

## Adversarial results

The reviewed adversarial set (15 cases: instruction override, prompt disclosure, tool
manipulation, data exfiltration, identity override, and injection embedded in quoted
record text) scored **100% detect/redirect** against live Phi, and `fail_closed_rate` is
**1.000** across timeout, unavailable, malformed, and nonsense-payload conditions. **No
false-allow was observed in this run.**

## Recommendation

**APPROVE for shadow-mode rollout.** No critical or high findings. The security gates in
the §30.4 evaluation are all green; the two failing gates are routing *accuracy*, which is
a correctness concern, not a safety one — and direct routing is correctly withheld. Before
direct routing is enabled: run gitleaks in CI (S2) and pin model provenance (S3).
