# F0039 — Neuron Multi-Thread Conversations — Getting Started

> Provisional skeleton. Setup detail is authored during F0039's `plan`/`feature`
> runs, after F0038 establishes the persistence interface and message envelope.

## Prerequisites

- [ ] **F0038 delivered** — persistence-home ADR, message envelope, and thread_id seam in place.
- [ ] Read the epic intake: [`../F0038-neuron-day-at-a-glance-shell/intake-brief.md`](../F0038-neuron-day-at-a-glance-shell/intake-brief.md).
- [ ] Re-derive scope into stories via the `plan` action (do not treat this skeleton as a committed spec).

## How to Verify (target — defined fully at plan)

1. Create multiple companion threads (record-anchored and free-form); they persist across sessions.
2. List, switch, rename, and delete threads; resume a prior thread with history intact.
3. Threads are private to the creating user.

## Notes

- Builds directly on F0038's reserved seams; the anchor of a thread is fixed at creation (no re-anchoring).
