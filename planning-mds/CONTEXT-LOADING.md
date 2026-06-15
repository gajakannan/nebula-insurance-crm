# Product Prompt Context Loading

This product uses `planning-mds/context-map.yaml` as the local prompt-loading
strategy for agent runs. The goal is to reduce unnecessary LLM token usage
without changing product behavior, runtime code, contracts, authorization,
validation, compliance, evidence retention, or public interfaces.

## Default Context

Start with the smallest useful context:

- `README.md`
- `lifecycle-stage.yaml`
- `.agentignore`
- `planning-mds/context-map.yaml`
- `planning-mds/features/REGISTRY.md`
- `planning-mds/features/ROADMAP.md`
- the target feature folder only
- KG lookup or hint output when available
- exact changed files, not whole source trees

Load `planning-mds/BLUEPRINT.md` only for planning, architecture, scope
reconciliation, or product-wide tradeoff decisions.

## On-Demand Context

These paths are not default prompt context:

- `planning-mds/features/archive/**`
- `planning-mds/operations/evidence/**`
- screenshots, videos, HAR files, images, logs, and visual artifacts
- old evidence runs and raw report artifacts
- full API specs and full LOB schema directories
- full `engine/**`, `experience/**`, and `neuron/**` trees
- full test trees
- examples and historical feature docs

They remain accessible by exact path when needed for audit, validation,
evidence review, closeout, failure triage, security review, changed-path
routing, KG routing, exact contract checks, or explicit user requests.

## Feature Implementation Flow

1. Read `planning-mds/context-map.yaml`.
2. Load product core context and target feature files.
3. Run or inspect KG hint/lookup output when available.
4. Load `SOLUTION-PATTERNS.md` for implementation conventions.
5. Load only the API spec, schema, source file, or test file needed for the
   target story.
6. Avoid broad reads of `engine/src`, `experience/src`, `neuron`, and test roots
   until changed-path or KG routing identifies exact files.
7. Record validation commands and evidence pointers in the target feature
   status files without loading old runs by default.

## Review And Validation Flow

For review:

- Start from changed files and the target feature status.
- Load exact contracts and schemas touched by the change.
- Load KG lookup output for impacted canonical nodes.
- Load evidence manifests before raw evidence files.

For validation:

- Read the target feature `latest-run.json` when present.
- Read the selected `evidence-manifest.json`.
- Load raw logs, screenshots, reports, or artifacts only when the manifest or
  failing validation requires them.

## Archives And Evidence

Archives and evidence are retained as product records. Do not delete them for
token optimization. Treat them as cold storage:

- Use registry and roadmap first.
- Use target feature files second.
- Use evidence README and manifests before raw evidence.
- Use exact-file access for audits and explicit user inspection.

## Estimating Token Savings

Use this rough estimate for text files:

```text
estimated_tokens = bytes / 4
```

Compare:

- before: all files matched by a broad path set
- after: default context files plus target feature files, KG output, and exact
  changed files

Large binary and visual files should count as "not prompt context" unless the
task explicitly requires visual inspection.

To measure the high-token surfaces from the product root:

```bash
python3 scripts/measure-context-surfaces.py --format markdown
```

## Validator

Run from the product root:

```bash
python3 scripts/validate-context-map.py
```

The validator checks that required layers exist, default layers avoid broad
archive/evidence/source globs, on-demand layers require routing, feature context
is target-scoped, and generated/visual/log artifacts are not default prompt
context.
