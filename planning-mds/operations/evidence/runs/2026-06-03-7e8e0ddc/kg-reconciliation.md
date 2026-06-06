# KG Reconciliation — F0019-submission-quoting-proposal-and-approval

Run ID: `2026-06-03-7e8e0ddc`  
Gate: `G7 Architect KG Reconciliation`  
Architect role switch: `agents/architect/SKILL.md` read before reconciliation.  
Completed at: `2026-06-03T22:50:09-04:00`

## Binding Delta

G0 baseline expected the Architect to confirm or add code-index coverage for downstream submission workflow surfaces:

- `engine/src/Nebula.Domain/Entities/Submission*.cs`
- `engine/src/Nebula.Application/Services/SubmissionService.cs`
- `engine/src/Nebula.Application/Services/WorkflowStateMachine.cs`
- `engine/src/Nebula.Api/Endpoints/SubmissionEndpoints.cs`
- `engine/src/Nebula.Infrastructure/Repositories/Submission*.cs`
- `engine/src/Nebula.Infrastructure/Persistence/Configurations/Submission*.cs`
- `experience/src/features/submissions/**`

As-built reconciliation updated `planning-mds/knowledge-graph/code-index.yaml` as follows:

- Broadened `entity:submission` from single-file entity/repository/configuration bindings to `Submission*.cs` globs so F0019 downstream records stay covered without file-by-file drift.
- Added explicit code-index bindings for `entity:submission-quote-packet`, `entity:submission-approval-decision`, and `entity:submission-bind-handoff`.
- Added endpoint bindings for:
  - `endpoint:submission-quote-packet-read`
  - `endpoint:submission-quote-packet-update`
  - `endpoint:submission-approval`
  - `endpoint:submission-bind-request`
  - `endpoint:submission-bind-confirmation`
  - `endpoint:submission-archive`
  - `endpoint:submission-reactivate`
- Added event bindings for:
  - `event:submission-packet-updated`
  - `event:submission-approval-granted`
  - `event:submission-approval-declined`
  - `event:submission-archived`
  - `event:submission-reactivated`

The bindings use code paths only and are stable across the G8 archive move.

## Canonical Nodes

`planning-mds/knowledge-graph/canonical-nodes.yaml` was updated with the following shared semantics:

- Added `entity:submission-bind-handoff` for the append-only bind handoff coordination record introduced by F0019-S0004.
- Added canonical endpoint nodes for the seven new F0019 submission API operations listed in the binding delta.
- Added canonical event nodes for the five F0019 activity payload event types listed in the binding delta.

Existing canonical nodes for `entity:submission-quote-packet`, `entity:submission-approval-decision`, `entity:submission`, `workflow:submission`, and the submission approval/archive policy rules were affirmed and reused.

## Validator Results

| Validator | Result | Notes |
|-----------|--------|-------|
| `python3 scripts/kg/validate.py --regenerate-symbols --check-symbols` | PASS | First attempt regenerated symbols but exposed an unconditional stale coverage-report failure. Validator behavior was fixed so explicit symbol/drift checks do not force the G8-only path-sensitive coverage rewrite; rerun exited 0. |
| `python3 scripts/kg/validate.py --check-drift` | PASS | Drift check exited 0 after the same validator fix. |

Known warnings remain pre-existing low-confidence or unknown-symbol warnings in unrelated renewal/product-schema test stubs. They do not block G7.

## Handoff to Closeout

- Do not run `--write-coverage-report` in G7; it is intentionally deferred until after the G8 archive move.
- PM closeout must verify `kg-reconciliation.md` is present, symbol/drift validators are green, and then run `python3 scripts/kg/validate.py --write-coverage-report` after moving the feature folder to archive.
