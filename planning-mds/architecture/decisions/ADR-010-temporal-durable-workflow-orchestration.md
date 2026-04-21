# ADR-010: Adopt Temporal for Durable Long-Running CRM Workflows

**Status:** Accepted
**Date:** 2026-03-26 (finalized during F0007 architecture review; originally proposed 2026-03-23)
**Owners:** Architect
**Related Features:** F0007, F0019

## Context

Nebula's planned CRM workflows include long-running, time-based behavior that should survive deploys, retries, process restarts, and infrastructure interruptions. The clearest near-term example is the renewal pipeline, which needs scheduled reminders, escalation timing, and durable execution over long windows.

Ad hoc background jobs or cron-style scheduling would make workflow state, retries, and auditability harder to manage consistently as the platform grows.

## Decision

Adopt Temporal as the durable workflow orchestration engine for long-running CRM workflows that require timers, retries, workflow visibility, and external-event correlation.

Immediate use is expected in renewal reminders and escalations. Later submission or approval flows may reuse the same capability when durable waiting or external workflow signaling becomes necessary.

## Scope

This ADR governs:

- durable orchestration of long-running workflow steps
- timer-based reminders and escalations
- workflow correlation IDs stored on business records
- retry and observability expectations for Temporal-managed workflows

This ADR does not govern:

- immediate user-driven state transitions that remain in application services
- queue routing logic
- generic event delivery infrastructure

## Consequences

### Positive

- Durable timers and retries become first-class platform capabilities.
- Workflow execution state is visible and recoverable.
- Long-running CRM behavior is no longer coupled to process uptime.

### Negative

- Adds a new runtime dependency and operating surface.
- Requires worker deployment, monitoring, and Temporal-aware testing patterns.

## F0007 Renewal Specifics

F0007 MVP does **not** use Temporal for renewal workflows. Overdue and approaching detection is computed at query time from stored dates and `WorkflowSlaThreshold` thresholds. This avoids adding Temporal as a runtime dependency before the platform capability is proven.

The planned Temporal integration (post-MVP) follows this pattern:

- **Workflow:** `RenewalReminderWorkflow` with ID `renewal-reminder-{renewalId}`
- **Task Queue:** `renewal-reminders`
- **Signals:** `RenewalAdvanced(toState)`, `RenewalCancelled` — terminate the workflow when the renewal moves past Identified or is deleted
- **Queries:** `GetNextReminderDate()` — returns the next scheduled action
- **Activities:** `SendApproachingNotification`, `SendOverdueNotification`, `RecordEscalationEvent` — all idempotent (check for existing Task/Event before creating)
- **Correlation:** A nullable `TemporalWorkflowId` column on the Renewal entity stores the Temporal workflow ID for queries and cancellation (added in the Temporal phase, not MVP)

See the [F0007 README Architecture Specification](../../features/F0007-renewal-pipeline/README.md) for the full Temporal workflow design.

## Conventions (to be applied when first Temporal workflow is implemented)

- **Workflow ID format:** `{domain}-{purpose}-{entityId}` (e.g., `renewal-reminder-{uuid}`)
- **Task Queue naming:** `{domain}-{purpose}` (e.g., `renewal-reminders`)
- **Worker hosting:** Temporal workers run as a separate .NET worker service in the same Docker Compose stack
- **Idempotent activities:** All activities must check for pre-existing side effects before executing
- **Workflow correlation:** Store Temporal workflow ID on the business entity for status queries and cancellation
- **Testing:** Temporal workflow tests use the Temporal test server; activities are unit-tested independently

## Follow-up

- ~~Define workflow registration and worker-hosting conventions.~~ Done (see Conventions above).
- Reference this ADR from renewal and other long-running workflow PRDs.
- Align `SOLUTION-PATTERNS.md` and runbooks with Temporal operating guidance when the first Temporal workflow ships.
