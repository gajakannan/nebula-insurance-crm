# Architect Analysis

## Root Cause

PR #47 was branched before upstream Neuron/F0038 planning and KG work landed. The merge conflict is additive planning/KG drift, not an application-code conflict.

The critical semantic conflict was ADR identity reuse:

- PR #47 used `adr:028` / `ADR-028-communication-activity-capture-and-redaction.md` for F0021 Communication.
- Upstream `main` uses `adr:028` / `ADR-028-neuron-companion-persistence-and-outreach-authorization.md` for F0038 Neuron.

## Fix Strategy

- Preserve upstream F0038/Neuron artifacts and F0021 Communication artifacts.
- Keep upstream Neuron as `ADR-028`.
- Rename F0021 Communication to `ADR-029`.
- Regenerate `STORY-INDEX.md` and `coverage-report.yaml`.
- Merge KG source files by canonical `id`, preserving both F0021 and F0038 mappings.
- Remove the stale `coverage.excluded_features` entry for F0021 because archived F0021 mappings now exist.

## Risk Assessment

- F0021 runtime risk: low. No backend/frontend source file had a merge conflict.
- Planning/KG risk: medium before fix due ADR collision; low after KG validation and drift checks passed.
- Harness risk: low. No `nebula-agents` source changed, and `validate_templates.py` passed.

## Boundary Decision

This remained a `defect-bugfix` run. It did not reopen F0021, did not create feature evidence, and did not write `latest-run.json`.
