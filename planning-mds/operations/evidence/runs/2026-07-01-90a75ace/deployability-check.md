# Deployability Check — F0038 Neuron Day-at-a-Glance Shell

**Run:** `2026-07-01-90a75ace` · **Role:** DevOps · **Stage:** G2 · **Recorded:** 2026-07-02

**Result:** PASS

The new Neuron service builds, containerizes, starts healthy, and honors its health/readiness
contract. Deployment-config changes are scoped and additive; the service is stateless and
starts independently of the identity/API chain.

## Deployment-config changes in this feature

| Change | File | Notes |
|---|---|---|
| New service image | `neuron/Dockerfile` | `python:3.12-slim`, non-root `uid 10001`, editable install, `HEALTHCHECK` on `/health`, uvicorn on `:8200` |
| Build ignore | `neuron/.dockerignore` | excludes `tests/`, caches, `*.egg-info`, legacy hyphenated `crm-agents/` |
| Compose service | `docker-compose.yml` → `neuron` | build `./neuron`, container `nebula-neuron`, `NEURON_ENGINE_BASE_URL=http://api:8080`, `NEURON_MODEL_PROVIDER=mock`, `NEURON_PERSISTENCE=memory`, `NEURON_REQUEST_TIMEOUT=10`, port `8200:8200`, `depends_on: api` |
| Model config | `neuron/config/models.yaml` | mock provider wired behind the router seam |

## Build & Run Evidence

- Image built: `docker compose build neuron` → `nebula-insurance-crm-neuron:latest`.
- Container started: `docker compose up -d --no-deps neuron` — Neuron is stateless and calls
  the engine only per-request, so it starts standalone (does not require the authentik/api
  chain to be healthy first). Health reached **healthy ~2s** after start.

## Health / Readiness Smoke

| Check | Result |
|---|---|
| Container health | healthy (`HEALTHCHECK` on `/health`) |
| `GET /health` | 200 — 4 heads + 5 tools registered (validated runtime built; fail-fast startup OK) |
| `GET /ready` | 200 — plan `day-at-a-glance`, 8 agents, `model_provider: mock`, `persistence: memory` |
| `GET /v1/glance` (no auth) | **401** (auth guard) |
| `POST /v1/messages` off-topic + dummy bearer | **200 polite CRM redirect** — S0007 scope guard verified in-container (no engine call) |

## Operational Properties

- **Statelessness:** no business state held between requests (ADR-027); the operation store
  is the durable boundary. Restart/scale-out safe.
- **Least privilege:** container runs as non-root `uid 10001`.
- **Failure posture:** engine-unreachable surfaces a typed upstream-unavailable error, not a
  500 stack leak (`test_engine_client`). Telemetry emission is fire-and-forget and never
  breaks a user flow.
- **Secrets:** no secrets baked into the image; the user authentik token is forwarded
  per-request and never logged; the LLM run is mocked (no provider key). Confirmed by the
  gitleaks scans (`artifacts/security/secret-scan-gitleaks-*`).

## Rollback

Additive only — remove/scale-to-zero the `neuron` compose service; no schema migration is
applied by this deployability slice (the `neuron.*` store uses the in-memory backend for this
run; the durable 6-table migration ships behind the persistence interface but is not the
default in this mode).

**Result:** PASS
