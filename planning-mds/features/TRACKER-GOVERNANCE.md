# Tracker Governance Contract

This document defines how planning trackers stay current and trustworthy.

## Why This Exists

- `REGISTRY.md`, `ROADMAP.md`, `STORY-INDEX.md`, `BLUEPRINT.md`, and per-feature `STATUS.md` are operational controls, not optional docs.
- Any feature/story state change must update tracker state in the same change set.

## Authoritative Tracker Roles

- `planning-mds/features/REGISTRY.md`: authoritative feature inventory, status, and folder paths.
- `planning-mds/features/ROADMAP.md`: authoritative sequencing view (`Now / Next / Later / Completed`).
- `planning-mds/features/STORY-INDEX.md`: auto-generated story rollup from strict story filenames.
- `planning-mds/features/F{NNNN}-{slug}/STATUS.md`: authoritative feature execution state and deferred follow-ups.
- `planning-mds/BLUEPRINT.md`: baseline strategy snapshot; must not contradict tracker state.

## Ownership

- Product Manager: updates tracker docs during planning changes.
- Architect: validates tracker consistency at planning-to-build handoff.
- Implementers: update feature `STATUS.md` whenever story state changes.
- Code Reviewer: blocks approval when tracker drift is detected.
- Quality Engineer: validates acceptance criteria coverage and records test signoff evidence.
- Security Reviewer: records security signoff when included in required signoff roles.

## Signoff Governance (Mandatory)

- Every feature `STATUS.md` must include:
  - `Required Signoff Roles` (set during Architect planning)
  - `Story Signoff Provenance` (story-level execution evidence captured before closeout)
- Architect owns the required signoff matrix at planning time and sets which roles are mandatory for completion.
- Minimum required roles for any feature marked `Done` or moved to archive:
  - `Quality Engineer`
  - `Code Reviewer`
- Architect adds additional required roles based on risk/scope, commonly:
  - `Security Reviewer` for authn/authz, access control, identity/session, secrets, sensitive data boundaries, or policy changes
  - `DevOps` for runtime/deployability or environment-contract changes
  - `Architect` when architecture-sensitive exceptions or tradeoffs require explicit acceptance
- Every required role must have story-level provenance entries for every story in scope with:
  - pass verdict (`PASS` or `APPROVED`)
  - reviewer identity
  - review date
  - concrete evidence path(s)
- Product Manager must not mark completion or archive a feature unless all required signoffs are present and passing.

## Provenance Boundary Rules (Mandatory)

- Signoff provenance is solution execution evidence and must live in this product repository.
- The external agent framework may define process/templates/checklists, but framework content is never accepted as completion evidence.
- Provenance `Evidence` should point to project outputs such as:
  - `planning-mds/**` (reviews, test plans, security evidence, tracker updates)
  - implementation/test artifacts in `engine/**`, `experience/**`, `neuron/**`, `docs/**`, or CI outputs

## Lifecycle Rules

- Feature lifecycle states: `Draft` -> `In Progress` -> `Done` -> `Archived`.
- `Done` means implementation is complete and signoff evidence has been captured in `STATUS.md`.
- `Done` may include a `Deferred Non-Blocking Follow-ups` section in `STATUS.md`; deferments must not change overall completion state.
- Archived features must:
  - live under `planning-mds/features/archive/`
  - be listed under `Archived Features` in `REGISTRY.md`
  - appear in `ROADMAP.md` `Completed` section, not `Now/Next/Later`.

## Orphaned Story Rule (Mandatory)

Before marking a feature `Done` or moving to `Archived`, the PM must verify that all non-completed stories are either:

1. **Explicitly deferred** in `STATUS.md` `Deferred Non-Blocking Follow-ups` with a tracking link to a new or existing feature, or
2. **Promoted** to a new feature ID in `REGISTRY.md` if the scope warrants standalone tracking.

No story file may be archived in a `Not Started` or `In Progress` state without a rehoming decision recorded in the closeout. This prevents future work from being silently buried in the archive.

## Story File Rules

- Story files must follow `F{NNNN}-S{NNNN}-{slug}.md`.
- Non-story documents in feature folders must NOT start with `F{NNNN}-S{NNNN}`.
- Story IDs in file content must match filename prefix.

## Mandatory Sync Triggers

Update trackers immediately when any of the following occurs:

1. A feature is created, renamed, moved, or archived.
2. A story is added, removed, renamed, or moved.
3. A feature/story status changes (including done/archive transitions).
4. Roadmap prioritization or sequencing changes.
5. Blueprint feature/story status text changes.

## Required Validation Commands

Before declaring planning or feature execution complete, run the tracker, story, and story-index validators provided by the external agent framework's product-manager role against this repo's `planning-mds/features/`. Framework binaries are resolved at session time; no hard path is embedded here.

## Definition of Fresh Trackers

All conditions must pass:

- [ ] Every `REGISTRY.md` folder path exists and points to the correct active/archive location.
- [ ] `ROADMAP.md` links resolve and align with current feature state.
- [ ] `STORY-INDEX.md` story count and links match current strict story files.
- [ ] `BLUEPRINT.md` linked feature/story paths resolve and match archive status.
- [ ] No non-story file is parsed as a story.
- [ ] For every feature in `Done` or `Archived` state, required signoff roles have story-level passing provenance evidence for each story in `STATUS.md`.
