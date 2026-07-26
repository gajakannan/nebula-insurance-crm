# Deployability Check — F0039 run `2026-07-25-273d5672`

**Role:** DevOps · **Gate:** G2 · **Date:** 2026-07-25 · **Result: PASS**

## Runtime configuration changes (basis for `deployment_config_changed = true`)

The `neuron` service in `docker-compose.yml` previously pinned `NEURON_MODEL_PROVIDER=mock`
and `NEURON_PERSISTENCE=memory`. Both change with this slice:

| Variable | Value | Why |
|----------|-------|-----|
| `NEURON_PERSISTENCE` | `postgres` | S0001 — the durable store replaces the in-memory one |
| `NEURON_POSTGRES_DSN` | `postgresql://…@db:5432/nebula` | Neuron writes `neuron.*` directly (ADR-028 §1) |
| `NEURON_POSTGRES_POOL_MIN/MAX` | `1` / `10` | Bounded pool — an unbounded one turns a spike into database exhaustion for every service sharing the engine DB |
| `NEURON_MODEL_PROVIDER` | `${…:-mock}` | **Default stays `mock`** so the stack still starts on a laptop with no GPU and no key |
| `NEURON_PHI_BASE_URL` / `NEURON_PHI_API_KEY` | env passthrough, empty default | The key is never in the repo or the compose file |
| `NEURON_INTENT_MODE` | `${…:-shadow}` | Spec §33 — **never default to `direct`** without a green §30.4 report |

`depends_on` now also waits for `db: service_healthy`, so the service does not start
against a database that is not yet accepting connections.

**Verified:** `docker compose config --quiet` exits 0 (valid after the change).

## Migration story

Schema changes are applied by `python -m app.persistence.migrate`, which records applied
files in `neuron.schema_migrations` and is **idempotent** — verified by running it twice
against the live database: the second run reported `no pending migrations`. `0002` is a
forward migration that adds columns and indexes only; it does not recreate the `0001`
six-table scaffold and backfills existing rows deterministically before applying
`NOT NULL`.

**Rollback:** no schema reversal is needed. Reverting the feature is
`NEURON_INTENT_MODE=deterministic` (restores F0038 routing) and, if required,
`NEURON_PERSISTENCE=memory`. The added columns are additive and harmless if unused.

## New dependency

`psycopg[binary,pool]>=3.2` added to `neuron/pyproject.toml` runtime dependencies;
`pytest-cov` to the `dev` extra. `pip-audit` reports no known vulnerabilities.

## Runtime smoke

Full stack verified healthy at G1 and re-confirmed here: `nebula-db` (healthy),
`nebula-authentik-server` (healthy), `nebula-api` (`/healthz` 200). An end-to-end turn ran
against **live Phi + real Postgres**: three messages (in-scope, off-topic, injection)
produced exactly one engine call and six persisted rows in sequence order.

## No regression to existing services

The change is additive to the `neuron` service only. `db`, `api`, and `authentik` service
definitions are untouched apart from the new `depends_on` edge.

## Follow-up for the operator (not blocking)

`model_revision` and `image_digest` are `null` in `config/models.yaml`. The server reports
neither, so both must be pinned at deploy time to make a decision reproducible later.
Recorded as a deployment-provenance action, not a code defect.
