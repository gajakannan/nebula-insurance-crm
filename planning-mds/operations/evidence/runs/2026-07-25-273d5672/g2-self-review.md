# G2 Self-Review — F0039 run `2026-07-25-273d5672`

**Gate:** G2 — Self-review, QE, and deployability · **Date:** 2026-07-25 · **Result: PASS**

Each implementing role re-ran its own checks after the runtime preflight was re-confirmed.

## Scope Review

Delivered S0001–S0008; S0009 gated-deferred by operator decision. Discovered scope matches
the 64 entries in `changed_paths[]`: `neuron/**` (durable store, thread API, intent layer,
evaluation), `experience/src/features/neuron/**` (conversation panel), and runtime config
(`docker-compose.yml`, `neuron/config/**`, `pyproject.toml`). All four manifest scope
booleans were reconciled against those paths at this gate and flip **true**. No work was
performed outside F0039; two pre-existing repo failures were deliberately left untouched
rather than widening scope.

## Acceptance Criteria Review

Every in-scope story's acceptance criteria are mapped to named tests in `test-plan.md`.
Highlights: restart durability and idempotency proven against **real Postgres**; cross-user
access proven to fail closed and to be indistinguishable from a 404; both story-named
regression fixtures (`redirect` + `renewals.list_attention`, and the invented
`show_renewals_needing_attention`) rejected; persist-first and resolve-before-dispatch
ordering proven by asserting the model and engine are untouched when persistence fails;
shadow mode proven observationally identical to deterministic mode.

One criterion is **met by the harness but not by the measurement**: S0008 requires direct
routing to be enabled only after the §30.4 gates pass. The gates are RED (2 of 8), so
direct routing is not enabled — the criterion is satisfied by *withholding* rollout, which
is the behaviour it specifies.

## Implementation Risks

1. **Routing accuracy is below target** (`domain_accuracy` 0.933 vs 0.95; one
   vague-reference case routed instead of clarifying). Mitigated by shipping in shadow
   mode, pinned by a test. Security gates are all green, so this is an accuracy risk, not
   a safety one.
2. **Model behaviour can regress silently** on a prompt, catalog, or model change. Mitigated
   by content-hashing prompt + catalog into every provenance row and by the evaluation
   harness being runnable as a gate.
3. **`postgres.py` error-translation branches are the least covered code** (70%). The
   uncovered lines are transport-fault paths needing driver-level fault injection; all
   behavioural paths are covered by the real-Postgres suite.
4. **Secrets coverage is weaker than intended** — pattern scan, not gitleaks.
5. **`model_revision` / `image_digest` are unpinned** in config; must be set at deploy time
   or provenance cannot reproduce a decision.

## Validation Evidence

- `test-plan.md`, `test-execution-report.md`, `coverage-report.md` (QE)
- `deployability-check.md` (DevOps)
- artifacts/coverage/neuron-coverage.json
- artifacts/security/dependency-pip-audit.txt
- artifacts/security/sast-bandit.txt
- artifacts/security/secrets-pattern-scan.txt
- artifacts/diffs/changed-files.txt
- `commands.log` — every command with exit code, secrets redacted

## Backend (S0001, S0002, S0007)

- [x] Postgres store implemented behind the extended interface; both backends satisfy it
- [x] `0002` forward migration applies, is idempotent, and backfills before `NOT NULL`
- [x] Endpoints implement the pre-existing `neuron-api.yaml` contract (not extend it)
- [x] Owner-scoping enforced in the WHERE clause of every statement, never post-fetch
- [x] Persist-first / resolve-before-dispatch ordering holds, with tests that prove the
      model and engine are untouched when persistence fails
- [x] 410 tests green; no event-loop blocking (async pool throughout)

## Frontend (S0003)

- [x] Server-rehydrated transcript; local turns are dropped once the refetch replaces them
- [x] Thread list with inline rename and confirmed delete
- [x] `pnpm lint` — **no finding from any F0039 file**; `lint:theme` PASS; `build` PASS
- [x] 32 component tests; a11y suite green after fixing a nested-landmark defect I introduced

## AI Engineer (S0004–S0006, S0008)

- [x] Structured provider contract satisfied identically by mock, scripted, and live providers
- [x] Every provider failure normalized to a typed error carrying no raw content
- [x] Catalog is the sole authority on what exists; head ids never come from model output
- [x] Schema + deterministic invariants reject contradictory output; both named regression
      fixtures fail closed
- [x] Live endpoint verified; **a real defect (unresolvable `$ref` → unconstrained grammar)
      was found and fixed** because the live test existed
- [x] Evaluation harness proven capable of failing before being trusted to pass

## Quality Engineer

- [x] Test plan covers every in-scope acceptance criterion
- [x] Coverage **90%** vs 80% floor; gap characterized honestly, not hidden
- [x] Four security scan classes executed or explicitly waived; findings assessed
- [x] Negative tests for owner-scoping, injection, non-disclosure, and redaction

## DevOps

- [x] Compose config updated and validated; `depends_on` waits for a healthy database
- [x] Migration idempotency verified against the live database
- [x] Rollback path is config-only and continuously regression-tested
- [x] No regression to `db`/`api`/`authentik`

## Architect confirmation against the G0 plan

Output matches the assembly plan as reconciled at G0. All four G0 findings are closed:
D1 (interface extended), D2 (async-first, refined to the whole interface), D3
(`mock_provider` conforms), D4 (schemas vendored + drift-guarded). One additional
canonical change was made deliberately and recorded (card_id allowlist), and one
architectural decision was forced by live evidence (inlined schema for the wire).

## Honest state carried to G3/G4

1. **The §30.4 rollout gate is RED** (2 of 8). Direct routing is therefore not enabled;
   the shipped default is shadow. This is a product/architecture decision for G4, not a
   code defect — all security gates are green and only routing accuracy is short.
2. **Secrets scanned with a pattern fallback**, not gitleaks. Security to rule at G3.
3. **Two pre-existing repo failures** (one frontend test, one lint error) are untouched
   and outside F0039 scope.
