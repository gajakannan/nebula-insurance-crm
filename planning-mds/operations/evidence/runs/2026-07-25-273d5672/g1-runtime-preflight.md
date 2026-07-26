# G1 — Runtime Preflight — F0039 run `2026-07-25-273d5672`

**Gate:** G1 — Runtime preflight
**Role:** DevOps
**Feature:** F0039 — Neuron Durable Conversations & Local Phi Intent Resolution
**`runtime_bearing`:** `true` — the slice changes `neuron/` runtime code, adds a forward migration, and
depends on a local model runtime, so runtime evidence is mandatory before any validation command.
**Date:** 2026-07-25

## Preflight result: **PASS**

All runtime dependencies this slice's validation commands touch are up and healthy. Both the initial probe
failure and the restored state are recorded below, per the action's failure-triage rule.

## Initial probe (failed) — recorded, not hidden

| Check | Command | Result |
|-------|---------|--------|
| Container runtime | `docker ps` | **FAIL** — `The command 'docker' could not be found in this WSL 2 distro` (Docker Desktop WSL integration was off) |
| Local Phi endpoint | `curl -s -m 3 http://localhost:8000/v1/models` | **FAIL** — no listener |

Classified `runtime-blocked`. **No code edits were made while blocked.** The operator enabled Docker Desktop
WSL integration and started the vLLM Phi runtime per
`neuron/neuron-local-phi-vllm-wsl2-runbook.md`; preflight was then re-run unchanged.

## Restored runtime (verified)

| Component | Command | Result |
|-----------|---------|--------|
| Postgres 16 (`nebula-db`) | `docker compose up -d db` → `docker compose ps db` | **Up (healthy)**, `0.0.0.0:5433->5432/tcp` |
| Postgres reachability from the Neuron venv | `psycopg.connect(host=127.0.0.1 port=5433 dbname=nebula …)` | **OK** — `PostgreSQL 16.14 (Debian 16.14-1.pgdg13+…)`; `neuron` schema **not yet present** (expected — `0001` scaffold is applied by S0001) |
| authentik OIDC (`nebula-authentik-server`) | `docker compose ps` | **Up (healthy)** |
| Engine API (`nebula-api`) | `docker compose up -d api` → `curl /healthz` | **Up**, `/healthz` **200**, `/openapi/v1.json` **200** |
| Local Phi via vLLM | `curl -H "Authorization: Bearer ***" http://127.0.0.1:8000/v1/models` | **200** — serves `microsoft/Phi-4-mini-instruct` |
| Phi chat completion | `POST /v1/chat/completions` (`max_tokens=20`, `temperature=0`) | **200** — returned valid JSON; `usage.total_tokens=22` |
| Phi **structured** output | `POST /v1/chat/completions` with `response_format.type=json_schema`, `strict=true` | **200** — returned `{"in_scope": true, "domain": "renewals"}`, schema-conformant |
| Neuron test baseline | `.venv/bin/python -m unittest discover -s tests -t . -q` | **OK — 116 tests, 0 failures** (pre-change baseline) |

**Runtime versions:** vLLM `0.25.1` (`system_fingerprint: vllm-0.25.1-247777c8`), Phi-4-mini-instruct on
RTX 5070, PostgreSQL 16.14, engine API on ASP.NET Core (Development).

## Findings carried into implementation

1. **Structured output is available and strict.** vLLM honors `response_format: {type: json_schema, strict:
   true}` and returned an enum-constrained, schema-valid object. S0004's structured provider binds to this
   contract rather than to prose parsing, and S0006's fail-closed validation still re-validates every
   response against the vendored schema — the model's conformance is treated as convenient, never trusted.
2. **Secrets stay out of the repo and out of evidence.** The Phi key is sourced from the gitignored
   `~/.neuron-secrets` (`chmod 600`) via `NEURON_PHI_API_KEY`. It is never passed on a command line, never
   echoed, and is redacted (`***`) in this report and in `commands.log`.
3. **Compose config for `neuron` needs updating (DevOps, S0001/S0004).** The `neuron` service currently
   pins `NEURON_MODEL_PROVIDER: mock` and `NEURON_PERSISTENCE: memory`. Both change with this slice; the
   env-var contract and compose defaults are updated and re-verified at the G2 deployability check. This is
   the concrete basis for `deployment_config_changed = true`.
4. **New dependencies.** `psycopg[binary,pool] >= 3.2` (async Postgres + pooling) is added as a Neuron
   runtime dependency and `pytest` / `pytest-cov` to the `dev` extra. Recorded in `pyproject.toml` under
   S0001; the async pool is what satisfies S0001's "never block the event loop" NFR (G0 decision D2).

## Failure-triage contract for the rest of this run

If any later validation command fails with runtime symptoms (connection refused, DNS failure, missing
container, model endpoint unavailable): stop code edits, classify `runtime-blocked`, re-run this preflight,
restore runtime, and re-run the **same** command unchanged before treating it as a code defect.

**Validator:** `validate-feature-evidence.py --stage G1` — see `lifecycle-gates.log`.
