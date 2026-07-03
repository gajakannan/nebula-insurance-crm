---
template: feature
version: 1.1
applies_to: product-manager
---

# F0038: Neuron Day-at-a-Glance Shell (Renewals Live + Draft Outreach + Mock-Send)

**Feature ID:** F0038
**Feature Name:** Neuron Day-at-a-Glance Shell
**Epic:** Neuron Companion (AI Conversational Layer) — first slice (Now)
**Priority:** High
**Phase:** Neuron Companion
**Status:** Skeleton (Planned) — full PRD, stories, and architecture authored during the `plan` run.

> **Source intake:** [`intake-brief.md`](./intake-brief.md) (the signed-off
> Neuron Companion Pre-Phase-A intake brief, archived here). It is the
> authoritative stakeholder input for this feature; the PM/architect must not
> invent rules beyond it. This PRD is the **skeleton** distilled from that brief;
> vertically-sliced stories and the architecture (ADRs, endpoints, KG bindings)
> are produced when this feature goes through its own `plan` action.

## Feature Statement

**As an** Underwriter (the renewal owner in commercial P&C)
**I want** a Day-at-a-Glance companion that surfaces the renewals needing my attention and gets me a ready-to-edit broker outreach draft
**So that** I can see "what needs me today" and act on it without leaving my work or hand-writing the email

## Business Objective

- **Goal:** Prove the full companion chain end-to-end (multi-zone shell → live Renewals zone → one assisted write) and lay the multi-head pathway, without changing the .NET engine as source of truth.
- **Metric (primary):** Median time from "renewal needs attention" (surfaced in the glance) → "outreach draft ready." No baseline exists today, so F0038 **instruments both timestamps to establish the baseline**; the improvement target is set after the first weeks of real data.
- **Metric (secondary, minimal v1 set):** companion daily-active usage (renewal-owner persona), % of attention-flagged renewals actioned, count of drafts generated / mock-sent.
- **Baseline:** No single in-companion "what needs me today" view; renewal outreach is manual and inconsistent.

## Problem Statement

- **Current State:** The renewal owner has no fast, single "what needs me today" view in-companion, and renewal outreach is manual (find a due renewal, hand-write a broker email) — costing time and producing inconsistent, sometimes non-compliant messaging.
- **Desired State:** A conversational companion embedded in the CRM surfaces attention-worthy renewals in a Day-at-a-Glance shell and offers a one-click, ready-to-edit outreach draft.
- **Impact:** Faster, more consistent renewal outreach; the foundation (zone-dispatch, message envelope, scope guard) for additional specialist heads.

## Scope & Boundaries

**In Scope (the walking skeleton + first write):**
- **Day-at-a-Glance multi-zone shell** (assembly, not composition) — a frame where each zone is owned by a specialist head.
- **Renewals zone = LIVE** — reads (permission-respecting, via the engine) the user's renewals needing attention and per-renewal drill context.
- **Other zones (Tasks, Pipeline, Broker activity) = inert "not yet active" stubs** that read no data and return a typed `inactive` payload.
- **One write — "draft the renewal outreach email"** — proactively suggested (nudge/CTA), generated only on user click; persisted as a renewal `ActivityTimelineEvent`, surfaced editable in-chat, labelled AI-generated draft, `InternalOnly` until a human edits/approves.
- **Mock-send** — a `[Send]` that **commits the real `Identified → Outreach` workflow transition** (emits `WorkflowTransition` + `ActivityTimelineEvent`) but **fakes the SMTP delivery** (dispatches no real email).
- **CRM-scope guard** — non-CRM intents route to a polite CRM redirect (classifier `out_of_scope` path).
- **Telemetry** — emit the primary + secondary metric events above as a first-class F0038 requirement.
- **Foundational Neuron bootstrap** — first runnable Neuron service slice:
  API/runtime entrypoint, model/prompt configuration, A2A-aligned internal
  delegation, versioned orchestration assets, specialist-head registry, tool
  registry, contract tests, and local run/test instructions.
- **Reserved architecture seams** (see below).

**Out of Scope (deferred — see Epic Roadmap):**
- General-purpose assistant behavior (off-topic queries are redirected, not answered).
- Any write other than the renewal outreach draft.
- **Real email send** — v1 is mock-send only.
- **Cross-zone composition / ranking** ("what matters most today") — the Day-at-a-Glance *brain* (Later).
- Any **live** specialist other than Renewals (other zones ship as inert stubs).
- Multi-thread management UI (F0039) and external hosts / MCP-UI (Later).
- "Refer draft to Distribution for review" flow (future feature).
- MS Agent Framework or another heavy external orchestration runtime. F0038 uses
  simple versioned YAML orchestration assets with schema validation and the
  A2A-aligned internal profile from ADR-027 unless a later ADR changes that.

## The Write — Renewal Outreach Draft (interaction contract summary)

Detailed interaction contract + acceptance criteria are authored as a mutation story during the `plan` run. Locked stakeholder constraints (from the intake brief):

- **Who may draft:** **Underwriter only** (new Casbin `renewal:draft_outreach` on the Underwriter role). Distribution does not get draft rights in v1.
- **Trigger:** proactive nudge/CTA; draft generated **only on user click** — never auto-generated, never auto-sent.
- **Content constraints:** references the upcoming renewal (account, expiry date, broker contact) and requests engagement; **must not** state/imply premium, quote figures, coverage terms, or any binding commitment; labelled **AI-generated draft**; **`InternalOnly`** until a human edits/approves.
- **Landing:** persisted as a renewal `ActivityTimelineEvent` (mandatory audit), editable in-chat; `[Send]` = mock-send (commits `Identified → Outreach`, emits "sent (simulated)" timeline event, no real email).
- **Audit/provenance:** generating a draft and mock-send each emit an `ActivityTimelineEvent` with actor, timestamp, model, prompt id/version, and content hash.

## Read Scope (Renewals zone, permission-respecting via engine)

- A **list** of the user's renewals needing attention (account, expiry date, days-to-expiry, workflow state, last-broker-contact signal).
- Per-renewal **drill context** (account, broker/contact, renewal timeline).
- **"Needs attention" rule:** renewals in `Identified`/`Outreach` **within 90 days of expiry** (configurable default), ordered by urgency, flag "no broker contact in 30+ days." Intra-zone ranking only — not cross-zone.
- **Stub zones read nothing.**

## Screen Layouts (ASCII)

UI-bearing feature: the companion renders a multi-zone Day-at-a-Glance shell with a live Renewals zone, three inert stub zones (operator-confirmed "show not yet active" at G1), and an in-chat draft + mock-send affordance. Account names below are illustrative. Final visual/component treatment is owned by the frontend at the `feature` run; these layouts fix responsibilities and responsive behavior. Components are registry-rendered (intake L1) — no model-generated markup.

### Desktop (≥1024px) — Neuron companion panel, Day-at-a-Glance view

```
┌ Neuron · Day at a Glance ───────────────────────────────────────────────┐
│ ┌ Renewals  ● LIVE ─────────────────────────┐ ┌ Tasks  ○ not yet active ┐│
│ │ 3 need attention                          │ │  coming soon            ││
│ │ ▸ Acme Mfg          exp 12d  Identified   │ └─────────────────────────┘│
│ │     ⚠ no broker contact 41d   [Draft ✏]   │ ┌ Pipeline  ○ not yet ────┐│
│ │ ▸ Globex Logistics  exp 27d  Outreach     │ │  active · coming soon    ││
│ │     [Draft outreach ✏]                    │ └─────────────────────────┘│
│ │ ▸ Initech           exp 63d  Identified   │ ┌ Broker activity  ○ ─────┐│
│ │     [Draft ✏]                             │ │  not yet active          ││
│ └───────────────────────────────────────────┘ └─────────────────────────┘│
│ ┌ Chat (single "Day at a Glance" thread) ───────────────────────────────┐│
│ │ You: draft outreach for Acme Mfg                                       ││
│ │ Neuron:  [ AI-generated draft · InternalOnly ]                         ││
│ │   Re: upcoming renewal — Acme Mfg (expires in 12 days)                 ││
│ │   Hi {broker}, your client's policy renews soon — can we connect to    ││
│ │   start the renewal conversation?  (no premium/quote/terms)            ││
│ │   [ Edit ]   [ Send (mock) ]                                           ││
│ └───────────────────────────────────────────────────────────────────────┘│
└──────────────────────────────────────────────────────────────────────────┘
```

- Renewals zone (live): needs-attention list, urgency-ordered, no-contact-30d⚠ flag, per-row `[Draft]` CTA. Drill opens record context.
- Stub zones (Tasks / Pipeline / Broker activity): visible, inert, read nothing, no CTA.
- Chat: in-chat editable draft labelled AI-generated + InternalOnly; `[Send (mock)]` commits `Identified → Outreach` and fakes SMTP.

### Narrow (<768px, mobile/iPad) — single column, Renewals first

```
┌ Neuron · Day at a Glance ─────────┐
│ ┌ Renewals  ● LIVE ─────────────┐ │
│ │ 3 need attention              │ │
│ │ ▸ Acme Mfg   exp 12d  ⚠41d    │ │
│ │     [Draft ✏]                 │ │
│ │ ▸ Globex     exp 27d  [Draft] │ │
│ │ ▸ Initech    exp 63d  [Draft] │ │
│ └───────────────────────────────┘ │
│ ┌ Tasks  ○ not yet active ──────┐ │
│ └───────────────────────────────┘ │
│ ┌ Pipeline  ○ not yet active ───┐ │
│ └───────────────────────────────┘ │
│ ┌ Broker activity ○ not yet ────┐ │
│ └───────────────────────────────┘ │
│ ┌ Chat ─────────────────────────┐ │
│ │ [AI draft · InternalOnly]     │ │
│ │ Re: Acme Mfg renewal (12d)    │ │
│ │ [ Edit ]  [ Send (mock) ]     │ │
│ └───────────────────────────────┘ │
└───────────────────────────────────┘
```

- Zones stack vertically; the live Renewals zone is first; stubs collapse to compact "not yet active" cards; chat/draft remain full-width.

## Architecture Seams to Reserve (cheap now, painful later — for the architect at Phase B)

1. **Thread id keys all conversation state** (not session/user); single-thread v1 = "one thread, no switcher."
2. **Persistence home decided via ADR** — Neuron remains stateless. Durable
   threads, messages, message parts, agent runs, tool calls, prompt versions,
   and provenance are owned by the **Neuron service directly** — its own `neuron`
   Postgres schema + migrations, written directly (not via an engine pass-through).
   CRM business state remains in the existing application schema (engine-owned). Any temporary adapter must preserve this
   ownership boundary and be explicitly called out in the plan.
3. **Versioned, multi-part message envelope** (`text` | `app` | `status` | `sources` | `actions`).
4. **Zone-dispatch contract (multi-head from day one)** — orchestrator enumerates registered specialist heads and dispatches per zone; Renewals returns content, stubs return a typed `inactive` payload. This is the extension point F0040 flips to live.
5. **Out-of-scope guard in the classifier** (per locked decision L8).
6. **Versioned YAML orchestration** — workflow/head plans are checked-in assets
   with schema validation, registered tool/head names, explicit terminal states,
   trace metadata, and fallback behavior.
7. **A2A-aligned agent delegation** — Neuron's orchestrator, specialist heads,
   and goal-oriented agents use the private/internal A2A-shaped contract from
   ADR-027. Public A2A endpoints, public Agent Cards, and external agent
   federation are deferred.
8. **Prompt/version provenance** — draft generation and mock-send audit metadata
   must include model, prompt id/version, content hash, and trace identifiers.
9. **Component architecture for in-CRM apps** — Neuron returns registered
   component identifiers, validated props, and action payloads. The React app
   renders pre-built components. MCP-UI/external-host resources are deferred.

**Guardrails:** assembly, not composition; the head contract is thin and provisional (do not gold-plate — it hardens at F0040 with the first real second consumer); visible stubs are a conscious product choice.

## Dependencies

- **Engine (source of truth):** Neuron calls the .NET engine **as the user** (forwarded authentik token); engine enforces existing Casbin ABAC. No authz re-implementation in Python.
- **New engine endpoints (architect, Phase B):** (a) list "renewals needing attention" for the current user, (b) per-renewal context reads, (c) persist draft + emit timeline event, (d) mock-send endpoint that commits `Identified → Outreach` **without** dispatching email, and (e) minimal Neuron operation persistence for thread/message/envelope replay plus agent-run/tool-call/prompt provenance. Additive to `nebula-api.yaml`.
- **F0021 Communication Hub — do NOT gate.** Ship the interim timeline-event home + mock-send now; migrate to a real Comms Hub draft + real send Later.
- Builds on completed **F0007 Renewal Pipeline** (renewal records, `Identified`/`Outreach` states) and the existing `experience/src/features/neuron` panel.
- **AI scope:** touches `neuron/` (Python) — the `feature` action will engage the **AI Engineer** role. (Note: AI Engineer role buildout is a prerequisite before this feature's `plan`/`feature` runs.)
- **Architecture decision:** governed by `ADR-027-neuron-companion-a2a-orchestration.md`
  and `planning-mds/architecture/c4-neuron-companion.md`.

## Success Criteria

- Underwriter can open the companion, see renewals needing attention in the live Renewals zone, and produce a ready-to-edit outreach draft in one click.
- Mock-send commits the real `Identified → Outreach` transition with audit, dispatching no real email.
- Stub zones render "not yet active" and read no data.
- Non-CRM queries are politely redirected.
- Neuron runs as a stateless service with versioned YAML orchestration,
  A2A-aligned internal delegation, a registered `crm_agents` package convention,
  and Neuron-owned operation persistence/provenance (its own `neuron` schema,
  written directly — not via engine pass-through).
- The companion response contract supports replayable multi-part messages and
  registered component rendering; no model-generated markup is rendered.
- Both baseline timestamps + the minimal secondary metrics are emitted as telemetry.

## Related User Stories

Authored during the `plan` run (PM Phase A), 8 vertical slices (operator-confirmed fine-grained decomposition, G1 2026-06-30):

| Story | Title | Type | Phase |
|-------|-------|------|-------|
| [F0038-S0001](./F0038-S0001-neuron-service-bootstrap.md) | Neuron service bootstrap (stateless runtime + registries + versioned orchestration) | Infrastructure | Infrastructure |
| [F0038-S0002](./F0038-S0002-day-at-a-glance-shell-and-zone-dispatch.md) | Day-at-a-Glance shell + zone-dispatch + message/component envelope | Read/assembly | MVP |
| [F0038-S0003](./F0038-S0003-live-renewals-zone-read.md) | Live Renewals zone (needs-attention list + drill context) | Read-only | MVP |
| [F0038-S0004](./F0038-S0004-stub-zones-inactive-payload.md) | Stub zones — inert "not yet active" payload | Read-only | MVP |
| [F0038-S0005](./F0038-S0005-renewal-outreach-draft.md) | Renewal outreach draft (generate-on-click, persist, edit in-chat) | Mutation | MVP |
| [F0038-S0006](./F0038-S0006-mock-send-and-workflow-transition.md) | Mock-send (commit `Identified → Outreach`, fake SMTP) | Mutation | MVP |
| [F0038-S0007](./F0038-S0007-crm-scope-guard.md) | CRM-scope guard (out-of-scope → polite redirect) | Behavior | MVP |
| [F0038-S0008](./F0038-S0008-companion-telemetry-instrumentation.md) | Companion telemetry (baseline timestamps + minimal secondary metrics) | Instrumentation | MVP |

Story signoff provenance and the required-role matrix live in [`STATUS.md`](./STATUS.md).
