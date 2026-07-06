# Knowledge-Graph Reconciliation — F0027-coi-acord-and-outbound-document-generation run 2026-07-02-b9316621

## Scope

- Feature ID: F0027
- Run ID: 2026-07-02-b9316621
- Date: 2026-07-03
- Reconciled by: Architect (feature-action)

## Binding Delta

Baseline = `feature-assembly-plan.md` knowledge-graph plan and F0027 architecture nodes.

| Capability / node | code-index binding (glob) | G0-declared? | Action |
|-------------------|---------------------------|--------------|--------|
| capability:document-management | `experience/src/features/documents/**`; `engine/src/Nebula.Infrastructure/Documents/**` | yes | confirmed-existing-coverage for shared document slice and repository paths |
| capability:outbound-document-generation | `engine/src/Nebula.Application/DTOs/GeneratedDocumentDtos.cs`; `engine/src/Nebula.Application/Interfaces/IDocumentRenderer.cs`; `engine/src/Nebula.Application/Interfaces/IOutboundMergeDataAssembler.cs`; `engine/src/Nebula.Application/Services/OutboundDocumentGenerationService.cs`; `engine/src/Nebula.Application/Services/OutboundTemplateGovernanceService.cs`; `engine/src/Nebula.Infrastructure/Documents/OutboundMergeDataAssembler.cs`; `engine/src/Nebula.Infrastructure/Documents/SimplePdfDocumentRenderer.cs`; `engine/src/Nebula.Api/Endpoints/OutboundDocumentEndpoints.cs`; `experience/src/features/documents/**`; `engine/tests/Nebula.Tests/Unit/OutboundDocumentGenerationServiceTests.cs` | yes | added |
| endpoint:outbound-documents-preview / issue / regenerate | `engine/src/Nebula.Api/Endpoints/OutboundDocumentEndpoints.cs` | yes | covered through capability:outbound-document-generation binding |

## Canonical Nodes

No new canonical node IDs were introduced at G7. Existing F0027 semantic nodes already cover the shipped behavior:

- `capability:outbound-document-generation`
- `entity:generated-document-artifact`
- `endpoint:outbound-documents-preview`
- `endpoint:outbound-documents-issue`
- `endpoint:outbound-documents-regenerate`
- `policy_rule:outbound-template-manage`
- `policy_rule:outbound-document-preview`
- `policy_rule:outbound-document-issue`
- `policy_rule:outbound-document-regenerate`

## Validator Results

| Check | Command | Result |
|-------|---------|--------|
| symbol regen + check | `python3 scripts/kg/validate.py --regenerate-symbols --check-symbols` | PASS (exit 0) |
| drift | `python3 scripts/kg/validate.py --check-drift` | PASS (exit 0; existing symbol-reference warnings remained nonblocking) |

`coverage-report.yaml` was not regenerated at this gate. Per the harness, path-sensitive coverage regeneration is deferred to G8 after the feature archive move.

## Handoff to Closeout

The semantic graph is green and ready for PM closeout verification. Any binding gap discovered during closeout must route back to Architect for a G7 delta pass rather than being edited during closeout.
