---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0038-S0008 — Companion Telemetry Instrumentation (Baseline Timestamps + Minimal Secondary Metrics)

## Story Header

**Story ID:** F0038-S0008
**Feature:** F0038 — Neuron Day-at-a-Glance Shell
**Title:** Emit baseline + secondary companion telemetry as a first-class requirement
**Priority:** High
**Phase:** MVP

## User Story

**As a** product stakeholder for the Neuron companion
**I want** the companion to emit the two baseline timestamps and a minimal set of usage metrics
**So that** we can establish the "needs-attention → draft-ready" baseline and measure adoption from the first weeks of real use

## Context & Background

There is no baseline today, so F0038 **instruments both timestamps to establish the baseline**; the improvement target is set later from real data. Telemetry is an explicit F0038 requirement, not an afterthought (intake §3.1). This story makes the metric events first-class and verifiable.

## Acceptance Criteria

**Happy Path:**
- **Given** a renewal is surfaced as needing attention in the glance
- **When** it is presented to the Underwriter
- **Then** a "needs-attention surfaced" timestamp event is emitted (primary metric, start)

- **Given** an outreach draft becomes ready (generated/edited-ready) for that renewal
- **When** the draft is ready
- **Then** a "draft-ready" timestamp event is emitted (primary metric, end), enabling the median "needs-attention → draft-ready" duration to be computed

**Secondary (minimal v1 set):**
- **Given** companion usage
- **Then** events are emitted for: companion daily-active usage (renewal-owner persona), % of attention-flagged renewals actioned, and count of drafts generated / mock-sent

**Edge Cases:**
- A draft that is never produced for a surfaced renewal → the start event still exists with no paired end (so "not actioned" is measurable; no fabricated end timestamp).
- Telemetry emission failure → it must not break the user flow (draft/mock-send still work); failures are logged.
- Events carry enough identity (thread/renewal/user) to compute the metrics without exposing PII beyond what the metric requires.

## Interaction Contract

N/A — instrumentation/observability. No user-facing CRM mutation; emits metric events as a side effect of S0003/S0005/S0006 flows.

## Data Requirements

**Required event fields:**
- Primary: `needs_attention_surfaced` and `draft_ready` events with timestamp + renewal/thread/user reference.
- Secondary: daily-active-usage event (renewal-owner persona), attention-flagged-actioned signal, drafts-generated count, mock-sent count.

**Validation Rules:**
- The two primary timestamps must be correlatable per renewal to compute the duration.
- Deferred (explicitly not in v1): edit-distance and compliance-violation tracking.

## Role-Based Visibility

**Roles that can view telemetry:**
- Product/analytics roles (downstream) — not an end-user CRM surface. Event emission requires the same authenticated user context as the action it instruments.

**Data Visibility:**
- InternalOnly: telemetry events are internal operational/analytics data.
- ExternalVisible: none.

## Non-Functional Expectations

- Performance: emission is non-blocking; it adds negligible latency to the instrumented action.
- Security/privacy: events carry only the identifiers needed for the metric; no draft body content or credentials in telemetry payloads.
- Reliability: telemetry failures are isolated from the user flow and are logged.

## Dependencies

**Depends On:**
- F0038-S0003 — surfaces needs-attention (primary start event).
- F0038-S0005 — draft-ready (primary end event) + drafts-generated count.
- F0038-S0006 — mock-sent count.

**Related Stories:**
- F0038-S0002 — companion open/usage signals originate at the shell.

## Business Rules

1. **Baseline-by-instrumentation (intake §3.1):** F0038 establishes the baseline; no improvement target is asserted yet.
2. **Minimal secondary set (intake §3.1):** DAU (renewal-owner), % attention-flagged actioned, drafts generated / mock-sent. Edit-distance and compliance tracking are deferred.
3. **Telemetry is first-class:** emitting these events is an acceptance requirement of F0038, not optional.

## Out of Scope

- Dashboards/visualization of the metrics (downstream/Later).
- Edit-distance and compliance-violation tracking.
- Setting the improvement target (done after first weeks of data).

## UI/UX Notes

- No direct UI. Instruments existing flows (shell, Renewals read, draft, mock-send).

## Questions & Assumptions

**Open Questions:**
- [ ] (Architect/AI Engineer, Phase B) Telemetry event schema and sink/destination.

**Assumptions (to be validated):**
- The minimal secondary metric set is sufficient for v1 adoption measurement.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (unpaired start, emission failure isolation, PII minimization)
- [ ] Permissions enforced — events emitted in the authenticated user context (documented)
- [ ] Audit/timeline logged — N/A (telemetry is itself the observability stream; not a CRM timeline mutation)
- [ ] Tests prove both primary timestamps emit and correlate, and the secondary events emit on their triggers
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
