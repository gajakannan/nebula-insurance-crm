# nebula-insurance-crm

Nebula Insurance CRM — a commercial Property & Casualty insurance CRM product.

---

## What it is

A production-grade Commercial P&C Insurance CRM covering broker/MGA workflows: producer hierarchies, policy lifecycle, submission/quoting/proposal, document management, communication capture, claims tracking, commissions, billing, and carrier relationships.

The product is in **public preview**. Feature scope and sequencing are owned by `planning-mds/features/REGISTRY.md` and `planning-mds/features/ROADMAP.md`.

## Tech stack

- **Backend** — C# .NET under `engine/` (API, domain, application, infrastructure)
- **Frontend** — React + TypeScript under `experience/`
- **AI layer** — Python under `neuron/`
- **Database** — PostgreSQL (see `docker/postgres/` and `docker-compose.yml`)
- **Identity** — Authentik (see `docker/authentik/`)
- **Knowledge-graph tooling** — Python under `scripts/kg/`
- **API contract tests** — Bruno collections under `bruno/`

See `planning-mds/BLUEPRINT.md` for the full architectural baseline.

## How to run locally

Prerequisites: Docker + Docker Compose, Node 20+ with pnpm, .NET 8 SDK, Python 3.12.

```bash
# Bring up database and identity
docker compose up -d db authentik

# Backend
dotnet run --project engine/<api-project>

# Frontend
pnpm --dir experience install
pnpm --dir experience dev

# AI layer
cd neuron && python -m venv .venv && source .venv/bin/activate && pip install -e .
```

See `docker-compose.yml` and per-layer READMEs for the authoritative local-dev commands.

## Planning structure

- `planning-mds/BLUEPRINT.md` — single source of truth for product vision, features, architecture, API, NFRs
- `planning-mds/domain/glossary.md` — canonical domain vocabulary
- `planning-mds/api/nebula-api.yaml` — OpenAPI contract
- `planning-mds/features/REGISTRY.md` — feature registry (authoritative list)
- `planning-mds/features/ROADMAP.md` — sequencing and status
- `planning-mds/features/F{NNNN}-<slug>/` — per-feature plans, stories, ADRs, evidence
- `planning-mds/knowledge-graph/` — canonical-nodes, code-index, solution-ontology, schemas
- `planning-mds/architecture/`, `planning-mds/security/`, `planning-mds/operations/` — cross-cutting planning surfaces

## Feature roadmap

- Active work and sequencing: `planning-mds/features/ROADMAP.md`
- Full registry: `planning-mds/features/REGISTRY.md`
- Release plan: `planning-mds/features/COMMERCIAL-PC-CRM-RELEASE-PLAN.md`

## Current work state

`planning-mds/features/ROADMAP.md`, `planning-mds/features/REGISTRY.md`, and `lifecycle-stage.yaml` are authoritative for what is active. The repo split itself did not activate or start a feature — consult those files for current status.

## Validation

Product-local gates declared in `lifecycle-stage.yaml` run against this repo only:

```bash
python3 scripts/kg/validate.py                             # knowledge_graph_sync
python3 planning-mds/testing/validate-nebula-api-contract.py planning-mds/api/nebula-api.yaml   # solution_contract
python3 planning-mds/testing/validate-frontend-quality-gate.py                                  # frontend_quality
```

## Migration provenance

Split provenance is recorded in `.split-baseline`. See [docs/migration-from-nebula-crm.md](docs/migration-from-nebula-crm.md) and [CHANGELOG.md](CHANGELOG.md).

## License

See [LICENSE](LICENSE).
