# KG Reconciliation

## Binding Delta

No semantic graph binding delta is required. The follow-up changes the global sidebar navigation and a focused component test only.

`experience/src/components/layout/Sidebar.tsx` is not currently bound to a F0037 canonical node, and the change does not introduce a new capability, endpoint, schema, policy rule, or domain entity.

## Canonical Nodes

No new canonical nodes introduced. Existing F0037 nodes remain authoritative:

- `capability:distribution-rollup-reporting`
- `capability:operational-reporting`

## Validator Results

- `python3 scripts/kg/lookup.py --file experience/src/components/layout/Sidebar.tsx --tier 3 --run-id 2026-07-06-38152f5c --telemetry-file .kg-state/telemetry.jsonl`: PASS, no matched nodes.
- `python3 scripts/kg/validate.py --regenerate-symbols --check-symbols`: PASS.
- `python3 scripts/kg/validate.py --check-drift`: PASS.

## Handoff to Closeout

PM closeout should verify this G7 artifact and publish the follow-up evidence run. No archive move is required because F0037 is already archived.
