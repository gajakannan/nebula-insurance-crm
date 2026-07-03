# G2 — Self-Review + QE + Deployability Evidence

**Run:** `2026-07-01-90a75ace` · **Feature:** F0038 (Neuron Day-at-a-Glance Shell)
**Recorded:** 2026-07-02 · **Mode:** clean · **LLM:** mocked (deterministic)

> **SUPERSEDED (2026-07-02):** This was the working consolidation note. G2 is now closed via
> the canonical contract artifacts — `g2-self-review.md`, `test-plan.md`,
> `test-execution-report.md`, `coverage-report.md`, `deployability-check.md` — and
> `validate-feature-evidence.py --stage G2` passes (`validated=1`). Those files are
> authoritative; the content below is retained only as a scratch summary.

## Test Evidence (in-container / runtime-backed)

| Runtime | Command | Result |
|---|---|---|
| Engine (.NET) | `dotnet test tests/Nebula.Tests` (Testcontainers Postgres) | **491 passed / 0 failed / 1 skipped** (3m44s) — prior 476 + F0038-S0008's 10 unit + 5 integration |
| Neuron (Python) | `python3 -m unittest discover -s tests -t .` | **116 passed** — S0001–S0008 incl. scope-guard (21) + telemetry (10) |
| Frontend (React) | `npx vitest run src/features/neuron` | **17 passed** · `tsc -b` exit 0 · eslint 0 errors |

### Coverage (all three runtimes)

| Runtime | Statements/Lines | Branch | Artifact |
|---|---|---|---|
| Engine (.NET) | — (Cobertura from the 491-test run) | — | `artifacts/coverage/engine-coverage.cobertura.xml` |
| Neuron (Python) | **88%** (1221 stmts, 134 miss) | 224 branches / 28 partial | `artifacts/coverage/neuron-coverage.xml` · `neuron-coverage-report.txt` |
| Frontend (React) | **86.2%** stmts/lines | 76.7% | `artifacts/coverage/frontend-coverage.cobertura.xml` · `frontend-coverage-report.txt` |

Neuron coverage run under Python 3.12 (host is 3.14; ran in a `python:3.12-slim` container with the canonical `planning-mds/schemas` mounted so the vendored-contract drift test resolves) — full **116-test** suite green. Notable neuron gaps: `app/main.py` at 0% (FastAPI route wiring — exercised by the in-container smoke, not the unit suite). Frontend coverage scoped to `src/features/neuron`; active code is 88–100% (DayAtAGlance 88.9%, Composer 93%, useSendMessage 100%, AttentionList/ZoneSlot ~96–98%). The 0% frontend entries are type-only (`types.ts`), a barrel (`index.ts`), and two legacy files superseded by the S0007 composer flow (`NeuronPanel.tsx`, `useNeuronChat.ts`).

## Deployability — Neuron service (F0038-S0008 DevOps slice)

New: `neuron/Dockerfile` (python:3.12-slim, non-root uid 10001, editable install, `HEALTHCHECK` on `/health`, uvicorn on :8200) + `neuron/.dockerignore` + a `neuron` service in `docker-compose.yml` (`NEURON_ENGINE_BASE_URL=http://api:8080`, `NEURON_MODEL_PROVIDER=mock`, `NEURON_PERSISTENCE=memory`, port 8200).

Smoke (container running, `docker compose up -d --no-deps neuron` — neuron is stateless and calls the engine only per-request, so it starts independently of the api/authentik chain):

| Check | Result |
|---|---|
| Container health | healthy ~2s after start |
| `GET /health` | 200 — 4 heads + 5 tools registered (validated runtime built, fail-fast startup OK) |
| `GET /ready` | 200 — plan `day-at-a-glance`, 8 agents, `model_provider: mock`, `persistence: memory` |
| `GET /v1/glance` (no auth) | **401** (auth guard) |
| `POST /v1/messages` off-topic + dummy bearer | **200 polite CRM redirect** — S0007 scope guard verified end-to-end in-container (no engine call) |

## Security Scans

Tooling installed + run on this ARM64 host on 2026-07-02: **semgrep** (Docker `semgrep/semgrep:latest`), **gitleaks 8.30.1** (release binary), **pip-audit 2.10.1** (venv). Artifacts in `artifacts/security/`.

| Class | Tool | Result |
|---|---|---|
| SAST | `semgrep scan` — `p/python p/typescript p/csharp p/security-audit` over F0038 source (neuron/app, neuron/crm_agents, experience/src/features/neuron, engine/src/Nebula.Api {Endpoints,Services,Models,Helpers,Program.cs}) | **Clean — 0 findings.** 315 rules on 96 files; all new S0007/S0008 files confirmed in the scanned set (scope_guard.py, messages.py, telemetry.py, NeuronCompanionTelemetry{Service,Endpoints,Models}.cs, Composer.tsx, useSendMessage.ts). `semgrep-sast.txt` / `semgrep-sast.json` |
| Secret scanning | `gitleaks git --log-opts=main..HEAD` (committed diffs) + `gitleaks dir` (working tree) | **Clean — 0 leaks.** 19 scannable commits + working-tree scan of every F0038 source path and the run/evidence folder. Confirms the forwarded user token is never committed and no secret leaked into evidence artifacts. `secret-scan-gitleaks-history.{txt,json}` / `secret-scan-gitleaks-worktree.txt` |
| Dependency/SCA — engine | `dotnet list package --vulnerable` | **Clean** — no vulnerable packages (all 5 projects). `engine-sca-dotnet.txt` |
| Dependency/SCA — neuron | `pip-audit -r <deployed-deps>` (OSV + PyPI advisory DB) | **Clean — no known vulnerabilities.** Audited the 27 exact versions installed in the running `nebula-neuron` container (`pip freeze`), incl. fastapi 0.139.0 / starlette 1.3.1 / uvicorn 0.49.0 / httpx 0.28.1 / pydantic 2.13.4. `neuron-sca-pip-audit.txt` / `neuron-audited-requirements.txt` |
| Dependency/SCA — frontend | `pnpm audit --prod` | **9 vulns (1 low / 2 moderate / 6 high)** — **pre-existing, repo-wide; NOT introduced by F0038** (S0007/S0008 added no new FE deps). e.g. react-router CSRF advisories. Recommend recording as a repo-level waiver, not remediating in-feature. `frontend-sca-pnpm.txt` |

## Outstanding for G2 completion
- Self-review notes per tier + QE acceptance-criteria mapping (8 stories).
- `evidence-manifest.json` → G2 `gate_results` + `validate-feature-evidence.py --stage G2` exit 0.
