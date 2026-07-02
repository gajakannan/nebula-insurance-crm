---
template: feature
version: 1.1
applies_to: product-manager
---

# F0021: Communication Hub & Activity Capture

**Feature ID:** F0021
**Feature Name:** Communication Hub & Activity Capture
**Priority:** High
**Phase:** CRM Release MVP

## Feature Statement

**As a** distribution user, coordinator, underwriter, or relationship manager
**I want** communication history captured in Nebula
**So that** broker and customer interactions are visible, auditable, and actionable without relying on memory or email search

## Business Objective

- **Goal:** Move important communication history into the CRM operating record.
- **Metric:** Captured activity volume, follow-up completion, and reduced time spent reconstructing conversations.
- **Baseline:** Important communication lives in Outlook threads, private notes, and fragmented team memory.
- **Target:** Users can capture, review, and act on meaningful communication history directly from broker, account, submission, and policy context.

## Problem Statement

- **Current State:** Communication trails are fragmented and difficult to audit. Follow-ups can be missed when context lives only in an inbox or personal notes.
- **Desired State:** Calls, meetings, notes, and email-linked activity references are visible in context and can create linked follow-up work.
- **Impact:** Faster follow-up, better broker service, stronger institutional memory, and more defensible account handling.

## Scope & Boundaries

**In Scope (MVP):**
- Manual capture of structured communication events for notes, calls, meetings, and email-linked references.
- Communication timeline visibility on broker, account, submission, and policy records.
- Linking one communication event to a primary record plus optional related records.
- Participant capture for internal and external participants.
- Follow-up task creation from a communication event using the existing task capability.
- Controlled correction/redaction flow that preserves the original audit trail.
- Audit/timeline evidence for create, correction, redaction, and follow-up creation.

**Out of Scope:**
- Full email-sending client.
- Real outbound send from Nebula or Neuron.
- Marketing automation.
- External messaging, calendar, telephony, or email connector ingestion.
- Automatic deduplication of externally ingested messages.
- AI-generated communication summaries.
- Communication analytics and relationship scoring.

## Success Criteria

- Users can capture a communication event from an approved contextual record page and see it after reload.
- Users can review relevant communication history in context without switching to inbox search.
- Users can create a linked follow-up task from a communication event and trace that task back to the event.
- Sensitive communication content can be redacted without deleting audit history.
- The MVP provides a stable structure later integrations can map into without becoming a general-purpose messaging platform.

## Requirements Clarifications (Resolved for Phase A)

1. **Email-linked activity means reference-only in MVP.** Users can record an external email subject, occurred-at timestamp, participants, and optional external reference text. Nebula does not send, sync, fetch, or store raw email bodies from an external inbox in F0021.
2. **Primary entity is mandatory.** Each communication event has exactly one primary linked record for ownership and authorization evaluation. Optional related records may be linked for cross-context visibility.
3. **Follow-up ownership uses existing task behavior.** A communication follow-up creates a normal task linked back to the communication event and its primary record; F0021 does not introduce a second workflow engine.
4. **Correction and redaction are audit-preserving.** Users do not hard-delete communication history in MVP. Correction creates a newer corrected view; redaction hides sensitive free-text content while preserving metadata and an audit/timeline entry.
5. **Outbound send remains deferred.** Neuron real outbound send may later use the Communication Hub as its draft or activity home, but F0021 itself does not send external communications.

## Risks & Assumptions

- **Risk:** Communication scope expands into a full messaging platform too early.
- **Risk:** Free-text notes can contain sensitive content, making security review mandatory.
- **Risk:** Timeline volume can grow quickly on high-activity records.
- **Assumption:** Structured manual capture is more valuable than external connector automation in the first CRM Release MVP slice.
- **Mitigation:** Focus on capture, visibility, task linkage, redaction, and pagination before deeper integrations.

## Dependencies

- F0002 Broker & MGA Relationship Management
- F0004 Task Center UI + Manager Assignment
- F0006 Submission Intake Workflow
- F0016 Account 360 & Insured Management
- F0018 Policy Lifecycle & Policy 360

**Downstream Consumers (not prerequisites for F0021 build):**
- F0038/Future Neuron outbound workflows can later use communication records as draft/activity provenance.
- F0030 Integration Hub can later ingest external email/calendar/telephony events into the same model.

## Architecture & Solution Design

### Solution Components

- Introduce a communication/activity capture component that owns user-authored notes, calls, meetings, and email-linked references as auditable CRM records.
- Reuse existing task capability for follow-up work rather than creating a parallel reminder/workflow engine.
- Compose communication history into existing broker, account, submission, and policy detail contexts.
- Reuse the platform timeline pattern for audit narration while keeping the communication event as its own business record.

### Data & Workflow Design

- Communication events carry event type, subject, summary/body text, occurred-at timestamp, primary entity, optional related entities, participants, direction, and follow-up linkage.
- Primary entity determines default ownership and authorization evaluation.
- Optional related entity links allow the same event to appear in additional relevant contexts without duplicating the event.
- Correction/redaction writes an audit-preserving mutation rather than deleting the original event.

### API & Integration Design

- Expose APIs for creating communication events, listing contextual communication history, linking related records, creating follow-up tasks, and correction/redaction.
- Keep API contracts focused on structured capture and retrieval.
- Reserve integration-friendly fields for future external message identifiers, but do not implement connector ingestion in F0021.
- Reuse existing ProblemDetails, pagination, row-version/concurrency, and authorization patterns.

### Security & Operational Considerations

- Apply authorization from the primary linked business record and do not leak communication records through unrelated secondary links.
- Treat free-text note/body content as sensitive CRM content.
- Require audit logging for creation, correction, redaction, and follow-up creation.
- Index contextual list queries by primary entity, related entity, event type, participant, and occurred-at date.

## Architecture Traceability

**Taxonomy Reference:** [Feature Architecture Traceability Taxonomy](../../architecture/feature-architecture-traceability-taxonomy.md)

| Classification | Artifact / Decision | ADR |
|----------------|---------------------|-----|
| Introduces: Feature-Local Component | Communication capture service, communication event model, related-record links, participant capture, correction/redaction flow | Phase B to decide |
| Extends: Cross-Cutting Component | Communication events become future integration-friendly records for external exchange and replay | [ADR-015](../../architecture/decisions/ADR-015-integration-hub-canonical-contracts-and-outbox.md) (Proposed) |
| Reuses: Established Component/Pattern | Activity timeline, task follow-up, ProblemDetails, pagination, Casbin authorization | Existing platform patterns |

## Screen Layouts (ASCII)

F0021 is UI-bearing. It adds communication capture and history surfaces to contextual CRM detail pages.

### Communication Panel — Desktop

```text
+----------------------------------------------------------------------------+
| Account: Globex Manufacturing                         [Add Communication]   |
+----------------------------------------------------------------------------+
| Tabs: Overview | Contacts | Policies | Communications | Timeline             |
+----------------------------------------------------------------------------+
| Communications                                      Filters: [Type] [Date]   |
|----------------------------------------------------------------------------|
| Jul 01 10:30  Call        Renewal pricing discussion        J. Lee          |
| Linked: Account primary, Policy #P-1042                                      |
| Follow-up: Open task "Send revised loss runs"                                |
|----------------------------------------------------------------------------|
| Jun 28 14:05  Email ref   Broker sent updated exposure schedule              |
| Participants: broker@agency.example, Underwriter A                           |
| [Open detail] [Create follow-up]                                             |
+----------------------------------------------------------------------------+
```

### Add Communication Drawer — Desktop

```text
+-------------------------------- Add Communication --------------------------+
| Type [Note v]  Direction [Internal v]  Occurred at [2026-07-01 10:30]       |
| Subject [ Renewal pricing discussion                                      ] |
| Summary [                                                             ]     |
| Primary record: Account / Globex Manufacturing (locked from entry point)    |
| Related records [ + Policy #P-1042 ] [ + Submission #S-3321 ]               |
| Participants [ + Internal user ] [ + External participant ]                 |
| Follow-up [x] Create task  Assignee [J. Lee]  Due [2026-07-03]             |
| [Cancel]                                                   [Save]           |
+----------------------------------------------------------------------------+
```

### Communication Panel — Narrow

```text
+------------------------------+
| Globex Manufacturing         |
| [Add] [Filter]               |
+------------------------------+
| Communications               |
| Call                         |
| Renewal pricing discussion   |
| Jul 01 10:30 · J. Lee        |
| Follow-up: Open              |
| [Open]                       |
+------------------------------+
| Email ref                    |
| Broker sent exposure update  |
| Jun 28 14:05                |
| [Open]                       |
+------------------------------+
```

## Related User Stories

| Story | Title | Cluster |
|-------|-------|---------|
| [F0021-S0001](./F0021-S0001-capture-communication-event.md) | Capture a structured communication event | Capture |
| [F0021-S0002](./F0021-S0002-view-contextual-communication-history.md) | View contextual communication history | Timeline |
| [F0021-S0003](./F0021-S0003-link-communications-to-related-records-and-participants.md) | Link communications to related records and participants | Linkage |
| [F0021-S0004](./F0021-S0004-create-follow-up-task-from-communication.md) | Create a follow-up task from a communication | Follow-up |
| [F0021-S0005](./F0021-S0005-correct-or-redact-communication-with-audit.md) | Correct or redact communication content with audit | Governance |
