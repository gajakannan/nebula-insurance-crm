# Feature Action Execution — F0039 run `2026-07-25-273d5672`

**Gate:** G6 — Candidate evidence validation · **Role:** Quality Engineer · **Date:** 2026-07-25
Pre-closeout candidate: no PM closeout, no tracker sync, no `latest-run.json` yet.

## Gate-by-gate timeline

| Gate | Role | Outcome | What happened |
|------|------|---------|---------------|
| **G0** | Architect | PASS | Reconciled the plan-authored `feature-assembly-plan.md` rather than overwriting it. Four additive plan↔as-built findings (D1–D4). Aligned STATUS role names to the validator's canonical set and added DevOps as scope-forced. |
| **G1** | DevOps | PASS | Initial probe **failed** (`docker` absent, nothing on :8000) → recorded `runtime-blocked`, **no code edited while blocked**. After the operator restored runtime: Postgres, authentik, engine API healthy; Phi on vLLM 0.25.1 with strict `json_schema` verified. |
| **S0001–S0008** | Backend / Frontend / AI | — | Eight stories implemented. Suite 116 → 413. |
| **G2** | QE + DevOps | PASS | Scope booleans reconciled against 64 changed paths (all four true). Coverage 90% vs 80 floor. Four scan classes run or waived. Compose updated; migration idempotency verified live. |
| **G3** | Code Reviewer + Security | PASS WITH RECOMMENDATIONS | 0 critical, 0 high. Security independently re-verified the 9 bandit SQL findings as false positives. Adversarial set 100% detect/redirect; fail-closed 1.000. |
| **G4** | Operator | PASS | `gate_policy.py` → ACCEPTABLE, no justification required. Operator approved **conditional on fixing M1 first**; M1 fixed and re-verified (413 passing, 90%, bandit 0 high), G3 re-run, then approval recorded. |
| **G5** | PM | PASS | Six required roles signed off; 48 per-story rows. Both `WITH RECOMMENDATIONS` verdicts satisfy all five §15 conditions explicitly. |
| **G6** | QE | this gate | Candidate evidence validated below. |

## Three things this run got right by testing rather than assuming

1. **The live endpoint caught a defect the mocked suite could not.** vLLM's guided decoding
   cannot resolve an external `$ref`, so the composed schema compiled an *unconstrained*
   grammar and returned `"scope": "redirect"` as a bare string. Structured output looked
   requested and was not enforced. Fixed by inlining refs for the wire while local
   validation still resolves them properly.
2. **The evaluation gate was allowed to fail.** It went red on routing accuracy, and the
   prompt was **not** re-tuned against the holdout set to force a pass. The consequence was
   applied instead — direct routing withheld, shadow default pinned by a test.
3. **Rollback is exercised, not assumed.** The deterministic guard is a live mode with the
   F0038 dispatcher tests pointed at it, so the rollback path is regression-tested every run.

## Candidate state confirmed

- [x] G0–G5 evidence present, all verdicts passing
- [x] `evidence-manifest.json` `status: in-progress`
- [x] `gate_results` populated through `signoff`
- [x] **No** `pm_closeout`, **no** `tracker_sync`, **no** `latest-run.json` — closeout has not begun
- [x] `changed_paths[]` populated (64 entries)
- [x] Conditional booleans cross-checked against §7 path classes at G2 (all four true)
- [x] Non-required absent artifacts recorded (DAST waiver in `security_scans`)

## Known state carried into closeout

- §30.4 rollout gate **RED** on two accuracy gates → shipping in shadow mode (specified behaviour, not a defect)
- 5 medium / 5 low review recommendations accepted as follow-ups
- Two pre-existing repo failures (one frontend test, one lint error) untouched — outside F0039 scope
- Two `resume-brief.py` framework defects recorded as follow-ups
