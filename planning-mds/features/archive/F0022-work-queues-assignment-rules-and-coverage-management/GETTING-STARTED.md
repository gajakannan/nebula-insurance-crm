# F0022 — Work Queues, Assignment Rules & Coverage Management — Getting Started

## Prerequisites

- [ ] Read the current release framing in [ROADMAP.md](../ROADMAP.md)
- [ ] Review current task, submission, and renewal ownership models
- [ ] Review F0004, F0006, F0007, and F0017 as completed foundations for manual assignment, source workflows, and hierarchy/territory/ownership inputs
- [ ] Confirm Phase A and Phase B approvals before coding

## How to Verify

1. Confirm the first release covers practical queues, routing, and backup coverage rather than abstract workflow automation.
2. Confirm tasks, submissions, and renewals all participate in queueing for the initial release.
3. Verify no-match work lands in the `Unassigned Operations Queue`.
4. Verify coverage activates only from explicit coverage windows.
5. Validate tracker sync after refinement.

## Phase A Planning Notes

- F0022 is usable before F0032. F0032 later centralizes governance and publish/rollback behavior over the queue/rule configuration surface.
- Implementers should wait for the later `feature` action G0 assembly plan before writing code.

## Phase B Architecture Notes

- Queue/routing architecture is governed by accepted [ADR-013](../../architecture/decisions/ADR-013-operational-routing-and-queue-engine.md).
- Data model details live in [data-model.md §1.8](../../architecture/data-model.md#18-work-queues-assignment-rules-and-coverage-f0022).
- API contract additions use the `WorkQueues` tag in [nebula-api.yaml](../../api/nebula-api.yaml).
- Shared schemas are `work-queue`, `assignment-rule`, `coverage-window`, `queue-work-item`, `queue-reassignment-request`, `queue-rebalance-request`, and `routing-event`.
- Security rules are in [authorization-matrix.md §2.6c](../../security/authorization-matrix.md#26c-work-queues-and-operational-routing-f0022) and `policy.csv` as `queue:read`, `queue:manage`, and `queue:assign`.
