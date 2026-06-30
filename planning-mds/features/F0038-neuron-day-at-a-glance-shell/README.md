# F0038 — Neuron Day-at-a-Glance Shell

**Status:** Draft (skeleton — awaiting `plan` run)
**Priority:** High
**Phase:** Neuron Companion (epic first slice — Now)

## Overview

First slice of the Neuron Companion epic: a conversational companion embedded in the CRM that renders a multi-zone **Day-at-a-Glance** shell. The **Renewals zone is live** (surfaces renewals needing attention and offers a one-click, ready-to-edit broker outreach draft + mock-send); the other zones ship as inert "not yet active" stubs. Assembly, not composition — the cross-zone "brain" is deferred. Proves the full companion chain end-to-end and lays the multi-head pathway.

## Planning Guardrails

- F0038 is the first runnable Neuron implementation and must bootstrap the
  stateless Neuron runtime, A2A-aligned internal delegation, simple versioned
  YAML orchestration, specialist-head registry, tool registry, prompt
  provenance, and message/component contract.
- Neuron calls the engine as the user with the forwarded authentik token; the
  engine remains the authorization and source-of-truth boundary.
- Durable Neuron operation state is **Neuron-owned**: the Python service owns its
  own `neuron` Postgres schema + migrations and writes them directly (no engine
  pass-through). Neuron must not become a store for CRM/product data — CRM business
  writes go through the engine as the user.
- The frontend renders registered components from the message envelope. Neuron
  returns component identifiers, props, and actions; it never returns executable
  markup.
- F0038 uses in-CRM component architecture for MCP/tool-style apps. MCP-UI,
  sandboxed resources, and external hosts are deferred.

Architecture source: [ADR-027](../../architecture/decisions/ADR-027-neuron-companion-a2a-orchestration.md)
and [Neuron Companion C4 ASCII sketches](../../architecture/c4-neuron-companion.md).

## Documents

| Document | Purpose |
|----------|---------|
| [PRD.md](./PRD.md) | Product scope and business outcomes (skeleton) |
| [intake-brief.md](./intake-brief.md) | Signed-off Pre-Phase-A stakeholder intake brief (epic source) |
| [STATUS.md](./STATUS.md) | Planning and implementation tracker |
| [GETTING-STARTED.md](./GETTING-STARTED.md) | Setup and refinement notes |

## Stories

| ID | Title | Status |
|----|-------|--------|

**Total Stories:** 0 (defined during the `plan` run)
**Completed:** 0 / 0

## Epic Context

- **F0038 (Now)** — this feature: Day-at-a-Glance shell, Renewals live, draft outreach + mock-send.
- **F0039 (Next)** — Multi-thread conversations (persistence impl + thread UX).
- **F0040 (Next)** — Second specialist head (flip a stub zone to live; head contract hardens).
- **Later** — Day-at-a-Glance brain (cross-zone composition), real outbound send + Comms Hub (F0021), MCP-UI external hosts, richer writes.
