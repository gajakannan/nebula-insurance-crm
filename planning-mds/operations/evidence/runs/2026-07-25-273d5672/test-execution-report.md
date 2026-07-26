# Test Execution Report — F0039 run `2026-07-25-273d5672`

**Role:** Quality Engineer · **Gate:** G2 · **Date:** 2026-07-25
All commands executed against the runtime verified at G1 (Postgres 16.14, engine API,
local Phi on vLLM 0.25.1). Full command list in `commands.log`.

## Results

| Suite | Command | Result |
|-------|---------|--------|
| Neuron unit + integration | `.venv/bin/python -m pytest tests -q` | **413 passed, 13 skipped** (incl. 3 added for the M1 fix) |
| Neuron coverage | `pytest --cov=app` | **90%** (floor 80) |
| Live Phi profile + resolver | `pytest tests/test_local_phi_live.py` (key sourced) | **13 passed** |
| Frontend | `pnpm vitest run` | **329/330** (1 pre-existing failure) |
| Frontend build | `pnpm build` | **PASS** |
| Frontend theme guard | `pnpm lint:theme` | **PASS** |
| Frontend lint | `pnpm lint` | **exit 1** — pre-existing F0037 error only; no F0039 file emits a finding |
| Intent evaluation (§30.4) | `python -m app.intent.evaluate_cli` | **6/8 gates PASS — OVERALL FAIL** |

Baseline before this run: 116 neuron tests. Now 413 (+297).

## The 13 skips

All are the live-Phi suites, which skip (never fail) when `NEURON_PHI_API_KEY` is absent
so the suite runs on a machine with no GPU. They were **executed and passed** with the key
sourced — recorded separately above.

## §30.4 evaluation — RED, and deliberately not worked around

`artifacts/security/../../../neuron/evals/reports/2026-07-25-local-phi.json` (repo path
`neuron/evals/reports/2026-07-25-local-phi.json`):

- PASS: unregistered_routes 0, fail_closed 1.000, schema_valid 1.000, action_match 0.933,
  redirect_precision 1.000, injection_detect 1.000
- **FAIL: authorization_bypasses 1** (target 0), **domain_accuracy 0.933** (target 0.95)

Failing case ids: `d014`, `c003`, `c004`, `c006`, `c008`.

QE note: the prompt was **not** re-tuned against the holdout set to force a pass. Tuning
until the measurement agrees is how a gate stops measuring anything. The consequence was
applied instead — the shipped default is shadow mode, pinned by a test.

## Runtime failure triage

No `runtime-blocked` classifications occurred during execution. The only runtime failure
in this run was at G1 before any code was written, and is recorded there.

## Security scan execution (QE runs, Security owns the verdict at G3)

| Class | Tool | Result | Artifact |
|-------|------|--------|----------|
| dependency | `pip-audit` | **No known vulnerabilities** | `artifacts/security/dependency-pip-audit.txt` |
| sast | `bandit -r neuron/app` | 0 high, 9 medium, 3 low — **all assessed false-positive/accepted, see below** | `artifacts/security/sast-bandit.txt` |
| secrets | pattern scan (**gitleaks unavailable on this host**) | 1 candidate, assessed non-secret | `artifacts/security/secrets-pattern-scan.txt` |
| dast | — | **not run** (waived: no ZAP and no deployed target) | manifest waiver |

### SAST findings, assessed

**9 × B608 "possible SQL injection"** in `neuron/app/persistence/postgres.py` — **false
positives, verified individually.** Every f-string interpolation in that file is either a
module-level column constant (`_THREAD_COLUMNS`, `_MESSAGE_COLUMNS`) or a code-literal
fragment (`keyset`, `seq_filter`); the remaining `{thread_id}`/`{run_id}` interpolations
are in **exception messages, not SQL**. All caller-supplied values are bound through `%s`
parameters. Verification is reproducible: `grep -oE '\{[a-zA-Z_]+\}' postgres.py`.

**B404/B603/B607 subprocess** in `neuron/app/intent/evaluation.py:115` — a `git rev-parse
HEAD` call used to stamp evaluation provenance. No untrusted input reaches it; it runs in
the dev/eval path, never in a request. Accepted, flagged to Security.

### Secrets finding, assessed

`neuron/tests/test_postgres_store.py:32` contains
`postgresql://postgres:postgres@127.0.0.1:5433/nebula` — the local docker-compose
development credential (already present on `main` in `docker-compose.yml`), used as a test
default and overridable via `NEURON_TEST_POSTGRES_DSN`. Not a production secret. The real
Phi key is read from a gitignored `~/.neuron-secrets` and appears nowhere in the repo,
in `commands.log`, or in any evidence artifact.

**Gap for Security to rule on:** the secrets class was covered by a pattern scan rather
than gitleaks, which is weaker. Recorded as `ran: true` with the substitute tool named
explicitly rather than overclaiming a gitleaks run.
