# Nebula Solution Ontology Seed

This folder holds the lightweight ontology and mapping layer for the Nebula
solution graph.

It exists to compress repeated cross-feature context, not to replace the source
documents in `planning-mds/`.

## Current Files

- `solution-ontology.yaml` - node types, edge types, ID rules, precedence, and
  ownership
- `canonical-nodes.yaml` - v0 shared canonical nodes for entities, workflows,
  workflow states, capabilities, roles, policy rules, ADRs, schemas, and API
  contracts
- `feature-mappings.yaml` - v0 feature/story links into the canonical layer
- `code-index.yaml` - implementation bindings from stable node IDs into
  `engine/`, `experience/`, and other code-bearing paths
- `coverage-report.yaml` - generated coverage and freshness report for mapped
  scope, exclusions, and bound implementation surfaces

## Authority Rules

Use these files as retrieval aids only.

If they conflict with raw project artifacts, the raw artifacts win in this
order:

1. Target feature folder and `feature-assembly-plan.md` for feature-local
   implementation intent
2. `planning-mds/architecture/decisions/**` for architectural decisions
3. `planning-mds/api/*.yaml` and `planning-mds/schemas/*.json` for contracts
4. `planning-mds/architecture/data-model.md` and `planning-mds/domain/glossary.md`
   for domain definitions
5. `planning-mds/knowledge-graph/*.yaml` for compressed retrieval and routing

If drift is discovered, repair the authoritative source first when needed, then
repair the ontology mapping in the same change set.

## Ownership

- Architect owns canonical shared nodes: `entity`, `glossary_term`, `workflow`,
  `workflow_state`, `capability`, `endpoint`, `ui_route`, `event`, `config_key`,
  `migration`, `role`, `policy_rule`, `schema`, `api_contract`, and `adr`.
  Architect also maintains `code-index.yaml` (implementation bindings) and the
  generated `coverage-report.yaml` artifact.
- Product Manager owns planning-facing nodes and links: `feature`, `story`,
  `persona`, `evidence`, and feature/story mapping freshness.
- Implementation agents do not silently redefine canonical solution semantics.
  They should flag drift and route it back to the Architect or Product Manager
  unless explicitly working in one of those roles.

## Prompt Usage

When a target feature or story has coverage in `feature-mappings.yaml`:

1. Load `solution-ontology.yaml`.
2. Load `canonical-nodes.yaml`.
3. Load the matching feature/story entry from `feature-mappings.yaml`.
4. Load `code-index.yaml` when implementation file routing or reverse lookup is
   needed.
5. Use that subgraph as the first-pass routing context.
6. Open raw ADR/schema/API/feature files only when they are linked, changed, or
   needed for detail or verification.

When coverage does not exist yet, fall back to the current file-centric prompt
pattern.

## Edge Provenance

Edge references in `feature-mappings.yaml` support an optional provenance
annotation to distinguish ground-truth links from speculative ones:

- **Bare string** (default): `entity:submission` — treated as `extracted`.
- **Object form**: `{id: feature:F0018, provenance: inferred, confidence: 0.9}`

Valid provenance values:

| Value | Meaning | Validator behavior |
|-------|---------|-------------------|
| `extracted` | Ground truth from code/docs/config | No warnings |
| `inferred` | Speculative link with confidence 0.0–1.0 | Warns if confidence < 0.5 |
| `ambiguous` | Uncertain — flagged for architect review | Always warns |

Both forms can be mixed in the same list. `lookup.py` surfaces provenance
annotations in its output when present.

## Tooling

- `python3 scripts/kg/validate.py` validates IDs, references, paths, feature
  coverage, code-index bindings, and report freshness.
- `python3 scripts/kg/validate.py --write-coverage-report` refreshes the
  committed `coverage-report.yaml` artifact.
- `python3 scripts/kg/validate.py --check-drift` runs drift checks: Casbin
  policy cross-check (policy_rule nodes vs policy.csv resource/action/role
  alignment). Add `--memory-dir <path>` to also scan an external agent memory
  directory for stale repo-path references (agent-agnostic — works with any
  tool that stores `.md` memory files).
- `python3 scripts/kg/lookup.py F0007-S0003` returns the first-pass ontology scope
  for a mapped feature or story.
- `python3 scripts/kg/lookup.py --file engine/src/Nebula.Domain/Entities/Submission.cs`
  performs reverse lookup from a code file back to ontology nodes and related
  planning scope.
- `python3 scripts/kg/blast.py entity:submission` computes the blast radius for
  a canonical node — impacted features, stories, code bindings, Casbin rules,
  and resolved files.
- `python3 scripts/kg/blast.py --file engine/src/Nebula.Domain/Entities/Submission.cs`
  computes blast radius starting from a code file (reverse-binds to nodes
  first).
- `python3 scripts/kg/blast.py F0007` computes blast radius for a feature by
  expanding its canonical node references.
- `python3 scripts/kg/blast.py entity:renewal --compact` outputs the summary
  counts only.
- `python3 scripts/kg/hint.py engine/src/Nebula.Domain/Entities/Submission.cs`
  outputs a compact KG routing hint (matched nodes, features, stories, Casbin
  rules) for a given file or directory path. Accepts `--json` for structured
  output. This is the agent-agnostic CLI — any agent or human can call it.

## Agent Integration

`hint.py` is the agent-agnostic entry point. Any coding agent can call it
before searching to get KG routing context:

```
python3 scripts/kg/hint.py <repo-relative-path>       # human-readable
python3 scripts/kg/hint.py --json <repo-relative-path> # structured JSON
```

Agent-specific adapters wrap `hint.py` for their hook systems. Any coding
agent with a pre-search hook mechanism can call `hint.py` directly or write
a thin adapter that reads its hook input format and delegates to `hint.py`.
