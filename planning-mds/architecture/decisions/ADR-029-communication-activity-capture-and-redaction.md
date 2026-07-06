# ADR-029: Communication Activity Capture And Redaction

Status: Proposed
Date: 2026-07-01
Owner: Architect

## Context

F0021 needs structured capture for notes, calls, meetings, and email-linked activity references across broker, account, submission, policy, renewal, and task contexts. Existing `ActivityTimelineEvent` records are append-only feed/audit projections and should not become mutable source records for user-authored communication content.

## Decision

Introduce `CommunicationEvent` as the source business record and project communication activity into `ActivityTimelineEvent`.

Corrections and redactions are append-only audit actions. Redaction masks sensitive body/summary content from normal communication and timeline reads while preserving metadata, linked entities, actor, timestamps, and audit rationale.

Email-linked activity stores metadata/reference only in the MVP. F0021 does not send email, read mailboxes, ingest connectors, or automate marketing messages.

## Consequences

- Communication history can be queried as source records while existing timeline surfaces continue to render append-only events.
- Timeline payloads must never contain sensitive communication body content.
- Security Reviewer signoff is mandatory for feature action implementation.
- BrokerUser and ExternalUser remain denied for MVP communication capture/history.
