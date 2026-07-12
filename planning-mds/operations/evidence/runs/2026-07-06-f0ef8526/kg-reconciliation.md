# KG Reconciliation

## Binding Delta

F0032 moved from planning-only KG bindings to as-built runtime bindings. `planning-mds/knowledge-graph/code-index.yaml` now maps `capability:admin-configuration-governance` to:

- Backend API: `engine/src/Nebula.Api/Endpoints/AdminConfigurationEndpoints.cs`, `engine/src/Nebula.Api/Program.cs`
- Backend application: DTOs, interfaces, and `AdminConfigurationService`
- Backend domain: configuration domain/draft/validation/published set/refresh/audit entities
- Backend infrastructure: EF configs, repository, domain adapters, DI, `AppDbContext`, and migration
- Frontend: `experience/src/features/admin-configuration/**`, `AdminConfigurationPage.tsx`, `App.tsx`, and `Sidebar.tsx`

No canonical node renames were introduced.

## Canonical Nodes

Reconciled nodes:

- `feature:F0032`
- `capability:admin-configuration-governance`
- `endpoint:admin-configuration-domains`
- `endpoint:admin-configuration-domain-detail`
- `endpoint:admin-configuration-create-draft`
- `endpoint:admin-configuration-update-draft`
- `endpoint:admin-configuration-validate-draft`
- `endpoint:admin-configuration-compare-draft`
- `endpoint:admin-configuration-publish-draft`
- `endpoint:admin-configuration-rollback-domain`
- `endpoint:admin-configuration-audit-events`
- `entity:configuration-domain`
- `entity:configuration-draft`
- `entity:configuration-validation-result`
- `entity:published-operational-configuration-set`
- `entity:configuration-refresh-status`
- `entity:configuration-audit-event`
- `policy_rule:admin-configuration-read`
- `policy_rule:admin-configuration-draft`
- `policy_rule:admin-configuration-validate`
- `policy_rule:admin-configuration-publish`
- `policy_rule:admin-configuration-rollback`
- `policy_rule:admin-configuration-audit`

## Validator Results

| Command | Result | Notes |
|---------|--------|-------|
| `python3 scripts/kg/validate.py --regenerate-symbols` | PASS | Regenerated symbol index; command emitted a local CSSM warning but exited 0. |
| `python3 scripts/kg/validate.py --check-symbols` | PASS | Symbol index check passed. |
| `python3 scripts/kg/validate.py --regenerate-decisions` | PASS | Decision index regenerated. |
| `python3 scripts/kg/validate.py --check-decisions` | PASS | Decision index check passed. |
| `python3 scripts/kg/validate.py --write-coverage-report` | PASS | Coverage report regenerated. |
| `python3 scripts/kg/validate.py --check-drift` | PASS | KG integrity passed with existing low-confidence F0028 warning. |

## Handoff to Closeout

G7 is reconciled for the implemented F0032 runtime slice. PM closeout should retain the accepted recommendations:

- Reconcile EF model snapshot before production hardening.
- Add focused F0032 service, endpoint, and frontend tests.
- Expand domain-specific semantic validation.
- Add audit redaction for audit-only users without underlying module read access.
