# Knowledge-Graph Reconciliation — F0021-communication-hub-and-activity-capture run 2026-07-01-9cee64f0

> Required at gate `G7` per `agents/actions/feature.md`. Authored by the Architect after signoff (`G5`) and candidate validation (`G6`), before PM closeout (`G8`).

## Scope

- Feature ID: F0021
- Run ID: 2026-07-01-9cee64f0
- Date: 2026-07-02
- Reconciled by: Architect (feature-action)

## Binding Delta

Baseline = the `feature-assembly-plan.md` "Knowledge-Graph Binding Plan" declared at G0. The plan expected an `entity:communication-event` code-index binding for the backend communication source model, API, tests, frontend communication slice, and contextual detail pages. The as-built implementation matches that plan and required one code-index addition because Phase B had already created canonical nodes but not the as-built source binding.

| Capability / node | code-index binding (glob) | G0-declared? | Action |
|-------------------|---------------------------|--------------|--------|
| entity:communication-event | `engine/src/Nebula.Domain/Entities/Communication*.cs` | yes | added |
| entity:communication-event | `engine/src/Nebula.Application/DTOs/Communication*.cs` | yes | added |
| entity:communication-event | `engine/src/Nebula.Application/Interfaces/ICommunicationRepository.cs` | yes | added |
| entity:communication-event | `engine/src/Nebula.Application/Services/CommunicationService.cs` | yes | added |
| entity:communication-event | `engine/src/Nebula.Application/Validators/Communication*.cs` | yes | added |
| entity:communication-event | `engine/src/Nebula.Infrastructure/Repositories/CommunicationRepository.cs` | yes | added |
| entity:communication-event | `engine/src/Nebula.Infrastructure/Persistence/Configurations/Communication*.cs` | yes | added |
| entity:communication-event | `engine/src/Nebula.Infrastructure/Persistence/Migrations/*F0021*Communication*.cs` | yes | added |
| entity:communication-event | `engine/src/Nebula.Api/Endpoints/CommunicationEndpoints.cs` | yes | added |
| entity:communication-event | `engine/tests/Nebula.Tests/**/Communication*.cs` | yes | added |
| entity:communication-event | `experience/src/features/communications/**` | yes | added |
| entity:communication-event | `experience/src/pages/AccountDetailPage.tsx`, `experience/src/pages/BrokerDetailPage.tsx`, `experience/src/pages/SubmissionDetailPage.tsx`, `experience/src/pages/PolicyDetailPage.tsx`, `experience/src/pages/RenewalDetailPage.tsx` | yes | added |
| entity:activity-timeline-event | existing `engine/src/Nebula.Application/Interfaces/ITimelineRepository.cs` and timeline binding | yes | confirmed-existing-coverage |
| entity:task-item | existing task-center / task-item bindings | yes | confirmed-existing-coverage |

## Canonical Nodes

No new canonical nodes were introduced during G7. F0021 reuses and affirms Phase B canonical semantics already present in `canonical-nodes.yaml`:

- `entity:communication-event`
- `entity:communication-link`
- `entity:communication-participant`
- `endpoint:communication-create`
- `endpoint:communication-list`
- `endpoint:communication-detail`
- `endpoint:communication-follow-up`
- `endpoint:communication-correction`
- `policy_rule:communication-event-create`
- `policy_rule:communication-event-read`
- `policy_rule:communication-event-link`
- `policy_rule:communication-event-correct`
- `policy_rule:communication-event-redact`
- `policy_rule:communication-event-create-follow-up`
- `schema:communication-event`
- `schema:communication-event-create-request`
- `schema:communication-event-correction-request`
- `schema:communication-event-follow-up-request`
- `adr:028`

`CommunicationCorrection` and `CommunicationFollowUpTaskLink` remain implementation details under `entity:communication-event` rather than new canonical entity nodes.

## Validator Results

| Check | Command | Result |
|-------|---------|--------|
| symbol regen + check | `python3 scripts/kg/validate.py --regenerate-symbols --check-symbols` | PASS (exit 0) |
| drift | `python3 scripts/kg/validate.py --check-drift` | PASS (exit 0) |

Both checks reported the pre-existing warning `Low-confidence inferred edge (0.4) on feature:F0028 in feature:F0018.depends_on`; the checks exited 0 and the warning is not introduced by F0021.

`coverage-report.yaml` was **not** regenerated at this gate. It remains deferred to `G8`, after the archive move binds the relocated feature-doc paths.

## Handoff to Closeout

The semantic graph is green and ready for PM closeout to verify, not re-author. If closeout discovers a missed source capability binding, route back to Architect for a G7 delta pass before PM archive/publish work continues.
