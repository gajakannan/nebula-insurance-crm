# F0028 — Carrier & Market Relationship Management — Getting Started

## Prerequisites

- [ ] Read the current release framing in [ROADMAP.md](../ROADMAP.md)
- [ ] Review F0019 submission quote/proposal and bind workflow dependencies
- [ ] Review F0023 search/reporting dependency
- [ ] Confirm Phase A approval is recorded before starting Phase B
- [ ] Confirm Phase B approval is recorded before starting implementation

## Scope Guardrails

- F0028 manages CRM-side market relationship context only.
- Carrier API integration, rating, quote comparison, reinsurance, commission, billing, and external broker collaboration are out of scope.
- Appetite, appointment, and contact intelligence is internal and commercially sensitive.

## How to Verify Phase A

1. Confirm six F0028 story files exist and pass story validation.
2. Confirm mutation stories include interaction contracts with entry point, editable state, save result, persistence evidence, role/status constraints, validation failure behavior, and audit/timeline expectation.
3. Confirm `PRD.md` contains Desktop and narrow screen layouts for UI-bearing workflows.
4. Confirm `STATUS.md`, `README.md`, `STORY-INDEX.md`, roadmap, and tracker state are synchronized.

## How to Verify Phase B

1. Confirm architecture defines the data model, API/schema contract, authorization model, timeline/audit behavior, and KG bindings.
2. Confirm `scripts/kg/validate.py`, `scripts/kg/validate.py --check-drift`, and tracker validation pass.
3. Confirm explicit Phase B approval is recorded before any feature action implementation starts.
