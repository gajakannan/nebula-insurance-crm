# F0040 — Neuron Second Specialist Head — Getting Started

> Provisional skeleton. Setup detail is authored during F0040's `plan`/`feature`
> runs, after F0038 proves the zone-dispatch seam with a single live head.

## Prerequisites

- [ ] **F0038 delivered** — zone-dispatch contract + live Renewals head in place.
- [ ] Read the epic intake: [`../F0038-neuron-day-at-a-glance-shell/intake-brief.md`](../F0038-neuron-day-at-a-glance-shell/intake-brief.md).
- [ ] Decide the second domain to flip live (candidate: Accounts or Brokers) at the `plan` run.

## How to Verify (target — defined fully at plan)

1. A second Day-at-a-Glance zone renders live data (no longer "not yet active").
2. Both live zones dispatch through the shared, hardened head contract.
3. No cross-zone ranking is introduced (that remains the deferred brain).

## Notes

- This is where the **thin, provisional head contract from F0038 is revised and hardened** — F0040 is the first real second consumer. Expect to extract the orchestrator/registry/intent platform here.
