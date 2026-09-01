---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0040-S0002 — Conversational Broker Activity Routing

## Story Header

**Story ID:** F0040-S0002
**Feature:** F0040 — Neuron Second Specialist Head (Broker Activity)
**Title:** Route direct recent-broker-activity requests to the live specialist with durable replay
**Priority:** High
**Phase:** MVP

## User Story

**As a** Relationship Manager or Distribution user
**I want** to ask Neuron for recent broker activity in ordinary CRM language
**So that** I can retrieve the same authorized feed without navigating away from my conversation

## Context & Background

F0039 already registers the `broker_activity` domain, `broker_activity.list` action, and target head, but keeps all three inactive. This story activates the existing read-only route. The trusted catalog, not model output, selects the head/action, and the response reuses S0001's registered list semantics rather than creating a second feed contract.

## Acceptance Criteria

**Happy Path:**
- **Given** an eligible authenticated user is in an owner-scoped Neuron thread
- **When** the user sends an unqualified recent-activity request such as `show recent broker activity`
- **Then** the request resolves to active domain `broker_activity`, active action `broker_activity.list`, and registered head `crm.broker_activity.head`
- **And** the assistant returns the registered Broker activity component with the same authorized, newest-20 behavior defined by F0040-S0001
- **And** both user and assistant messages are stored in the current thread with server-owned ordering

**Durable Replay:**
- **Given** a successful, empty, or error Broker activity response was stored
- **When** the owner reloads or resumes the thread
- **Then** the historical envelope replays in its original message position with the same component data/status and without a duplicate assistant message or a new broker read

**Alternative Flows / Edge Cases:**
- No authorized events exist → return the explicit registered empty state.
- The broker read fails after routing → persist a user-safe typed error response; do not expose internal exception details or affect other stored messages.
- The resolver proposes an unknown head, inactive domain/action, cross-domain action, or executable broker mutation → reject it through the existing fail-closed path; invoke no engine tool.
- The request asks to create, edit, delete, assign, approve, contact, or follow up with a broker → do not execute; explain that F0040 Broker activity is read-only.
- The request asks for filtering by a named broker, event type, or date range → do not pretend the newest-20 list satisfies the filter; respond with a bounded unsupported-capability message.
- A user attempts to access another owner's thread → preserve F0039's not-found/no-disclosure behavior.

## Interaction Contract

The Broker domain operation is read-only, but the conversation send persists thread history and therefore carries this interaction contract.

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Neuron thread composer | Enter an unqualified recent Broker activity request and select Send | User message is editable before Send; the accepted user/assistant messages are immutable history after persistence | One user message and one terminal assistant envelope are appended with server-owned sequence; no Broker/Contact/Timeline business row changes | Reload/resume returns both messages once, in sequence, with the identical stored registered-component payload/status and no new broker read | Thread owner only; supported internal roles receive engine-scoped data; external users have no route |

- Render-only UI cannot satisfy the story; the send path and durable replay must both pass.
- An empty message is rejected by the existing composer validation and creates no stored message.
- A failed read still produces one user-safe terminal assistant envelope; retry is a new explicit user action.
- No CRM timeline event is required because no broker business state changes.

## Audit & Timeline Requirements

- Persist the user message, one terminal assistant envelope, and the existing F0039 resolver/head/tool provenance with server-owned sequence and correlation identifiers.
- A rejected route records bounded rejection/provenance codes without raw user text and invokes no engine tool.
- No `ActivityTimelineEvent` or broker-domain audit record is created because the story performs no broker business mutation.

## Data Requirements

**Required Fields:**
- Trusted resolution: domain `broker_activity`, action `broker_activity.list`, target head `crm.broker_activity.head`.
- Request context: owner-scoped `thread_id`, authenticated user context, user message sequence.
- Response: versioned message envelope with registered Broker activity component or typed empty/error text/status.
- Provenance: existing resolver/catalog/head references and outcome metadata required by F0039.

**Optional Fields:**
- Source references already allowed by the message-envelope contract; no raw authorization decision or secret is exposed.

**Validation Rules:**
- Head and action are resolved from the reviewed trusted catalog, never accepted from model output.
- `broker_activity.list` requires no record entity because it returns the authorized newest-20 feed.
- No Broker activity write action exists or becomes active in F0040.
- Stored replay uses the persisted envelope; it does not silently refresh historical component props.

## Role-Based Visibility

**Roles that can request:**
- `DistributionUser`, `DistributionManager`, `RelationshipManager`, `ProgramManager`, `Underwriter`, and `Admin` — same result scoping as F0040-S0001.
- `BrokerUser` / external user — no access to the Neuron Broker activity route.

**Data Visibility:**
- InternalOnly content: user message, broker event results, resolver provenance, and thread history.
- ExternalVisible content: none.

## Non-Functional Expectations

- Performance: the routed response reaches a terminal stored envelope within the existing companion response budget, and the broker read itself preserves S0001's p95 < 2 second target.
- Security: catalog and registry validation remain deterministic authority boundaries; rejected output invokes no tool.
- Reliability: message persistence is idempotent and owner-scoped; provider or engine failures produce one terminal response.
- Privacy: telemetry and logs exclude raw user text, broker event descriptions, bearer tokens, and model credentials.

## Dependencies

**Depends On:**
- F0039-S0001 through F0039-S0007 — owner-scoped persistence, thread API, trusted intent catalog, resolver validation, dispatcher integration, and provenance.
- F0040-S0001 — live Broker activity result behavior and registered component.

**Related Stories:**
- F0040-S0003 — proves the same specialist works in a two-live-head platform without weakening route validation.
- F0041-S0001 — contextual adjudication remains separately gated and is not required here.

## Business Rules

1. **Read-only route:** `broker_activity.list` is the only Broker activity action activated by F0040.
2. **Trusted registry:** neither the user nor the model may name an executable head/tool that bypasses the catalog.
3. **Same feed, two entry points:** glance and direct conversation use one product result contract.
4. **No false fulfillment:** a newest-20 response cannot claim to satisfy an unsupported broker/date/type filter.

## Out of Scope

- Broker-specific/date/type search or filtering, natural-language aggregation, summaries, recommendations, or follow-up actions.
- Contextual adjudication, model fine-tuning, or changing the F0039 rollout gate.
- Any broker/contact/timeline business mutation.

## UI/UX Notes

- Screens involved: existing Neuron conversation transcript and registered component response.
- Key interactions: type a recent Broker activity request → view result → select broker link; reload thread → view the identical stored response.

## Questions & Assumptions

**Open Questions:** None — the existing catalog reserves the precise read-only action and target head.

**Assumptions (validated from current product artifacts):**
- F0039 owner-scoped persistence and fail-closed intent validation remain the baseline contract.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Empty, engine failure, unsupported filter/write, invalid resolver output, and cross-owner thread cases handled
- [ ] Catalog/registry and engine authorization boundaries enforced
- [ ] Conversation persistence and existing Neuron message/run provenance recorded; no broker `ActivityTimelineEvent` created
- [ ] Tests cover deterministic and configured resolver modes, rejected route proposals, durable replay, idempotency, and no-tool-on-rejection
- [ ] Documentation and intent evaluation fixtures updated
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
