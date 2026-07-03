# F0038 Action Context

## Run Identity

- Feature: F0038
- Feature slug: neuron-day-at-a-glance-shell
- Run ID: 2026-07-01-90a75ace
- Action: feature
- Mode: clean
- Slice order source: assembly-plan
- Prior run: none (first governed feature run for F0038)
- Contract effective date: 2026-07-01

## Inputs

- Product root: `nebula-insurance-crm` working tree
- Feature folder: `planning-mds/features/F0038-neuron-day-at-a-glance-shell/`
- Plan sign-off: Phase A (`2026-06-30-dbc93ab5`) + Phase B architecture (`2026-06-30-d1dd91f7`, G5 operator approval 2026-06-30)
- Primary spec: `feature-assembly-plan.md` — authored at G0 Step 0 of this run (not a plan deliverable)
- Retrieval tier: clean → start_tier 1, max_auto_tier 2

## Assumptions

- LLM provider is **mocked/stubbed** for this run per operator decision (deterministic
  evidence, no live Anthropic calls). Provider seam left injectable for a later live smoke test.
- Baseline runtime (Postgres, Authentik, Temporal, engine `api`) is brought up in Docker for
  runtime-preflight and validation; the new `neuron` service gets its runtime contract in this run (S0001).
- `.env` created from `.env.example` with dev-only Authentik secrets; `ANTHROPIC_API_KEY` left as placeholder (mocked).

## Scope Boundaries

In scope: the F0038 vertical slice across `neuron/`, `experience/`, and `engine/` for stories
S0001–S0008, plus feature-level tests, deployability, and evidence. Reserved seams for F0039
(conversations) / F0040 (second head) are stubbed, not implemented.

Out of scope: the cross-zone Day-at-a-Glance brain, real outbound send + Comms Hub (F0021) draft
home, MCP-UI external hosts, and any feature other than F0038.

## Lifecycle Stage

G0 — evidence package initialized (draft); Architect assembly-plan authoring in progress.
