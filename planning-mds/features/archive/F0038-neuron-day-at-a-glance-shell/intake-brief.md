# Neuron Companion — Pre-Phase-A Intake Brief (DRAFT)

> **What this is:** stakeholder intake for the Neuron in-CRM chat companion initiative, captured *before* Phase A so the Product Manager role has the inputs it is forbidden to invent. It is **not** a PRD. Once signed off, it converts into `BLUEPRINT.md` §3 updates plus the `F0038` PRD + stories, after which this file can be archived.
>
> **Status:** ✅ SIGNED OFF — all stakeholder items resolved 2026-06-29 (A–D closed). Ready to convert into BLUEPRINT §3 + F0038 PRD/stories.
> **Initiative spans:** `F0038` (Now) → `F0039`, `F0040` (Next) → Later.
> **Next free feature ID at time of writing:** F0038.
>
> **How to read the markers:**
> - ✅ **Resolved** — confirmed by stakeholder; PM/architect take as given.
> - 🟡 **Proposed default** — my strawman, lightly pending confirm.
> - 🔴 **CLARIFY** — open; being talked through before Phase A.

---

## 1. Initiative summary

A conversational companion embedded in the CRM (the existing `experience/src/features/neuron` panel) that lets a user chat **about their CRM work** and get back either text or a **visually rich, interactive component** ("app") rendered inline. The backend (`neuron/`, Python) classifies intent and routes to **specialist mini-orchestrators**; each specialist owns one CRM domain. The .NET engine remains the single source of truth (per locked baseline §2; neuron is a secondary interface).

**First slice (F0038) is a "Day at a Glance" shell** — a multi-zone frame where each zone is owned by a specialist head. **Renewals is the one live zone**; the other zones (Tasks, Pipeline, Broker activity) render an inert **"not yet active" stub**. The live Renewals zone surfaces renewals needing attention and offers the first write — **"draft the renewal outreach email"** — as its per-row action.

This is **assembly, not composition**: zones are independent and only Renewals reads data. The *intelligent* cross-zone Day at a Glance (ranking what matters most across domains) is deferred to Later (see §4).

```
┌ Day at a Glance ─────────────────────────────────────────┐
│ ┌ Renewals  ● LIVE ─────────────┐ ┌ Tasks  ○ soon ─────┐ │
│ │ {n} need attention            │ │  (not yet active)  │ │
│ │ ▸ {account}  exp {d}d [Draft] │ │                    │ │
│ │ ▸ {account}  exp {d}d [Draft] │ └────────────────────┘ │
│ │ …                             │ ┌ Pipeline ○ soon ───┐ │
│ └───────────────────────────────┘ │  (not yet active)  │ │
│ ┌ Broker activity ○ soon ───────┐ └────────────────────┘ │
│ │  (not yet active)             │                        │
│ └───────────────────────────────┘                        │
└──────────────────────────────────────────────────────────┘
```

---

## 2. Locked decisions (carry-forward — do not relitigate)

| # | Decision | Value |
|---|----------|-------|
| L1 | **Rendering model** | ✅ Component registry — neuron returns `{component, props}`; React renders pre-built, registered components. LLM picks the component + data; it never emits markup or numbers. |
| L2 | **Data & auth path** | ✅ Neuron calls the .NET engine **as the user** (forwarded token); engine enforces existing Casbin ABAC. No authz re-implementation in Python. |
| L3 | **v1 scope** | ✅ Read-only **+ one write** ("draft renewal outreach email"). **Mock-send commits the real `Identified → Outreach` transition** (only SMTP delivery is faked). Only one *live* specialist (Renewals); other zones are inert stubs. |
| L4 | **Host** | ✅ The CRM's own React app. MCP used as the **tool protocol**; MCP-UI (sandboxed resources) **deferred** to a later "external hosts" feature. |
| L5 | **Thread model** | ✅ **Record-anchored by default, free-form allowed.** Anchors may also be domain-level (a glance is not a single record — see §3.6). |
| L6 | **Source of truth** | ✅ Engine + Postgres. Neuron is stateless/secondary (consistent with baseline §2 "never source of truth"). |
| L7 | **First slice = shell, not card** | ✅ F0038 is the **Day-at-a-Glance multi-zone shell** (assembly) with Renewals live + the others stubbed — so the multi-head pathway is established on day one. Cross-zone **composition/ranking** (the "brain") is explicitly **out** of F0038. |
| L8 | **Conversational scope = CRM only** | ✅ The companion is a CRM assistant, **not** a general-purpose chatbot. Off-topic / non-CRM queries are politely **redirected** to CRM topics (classifier needs an `out_of_scope` → redirect path). |

---

## 3. Stakeholder inputs

### 3.1 Problem & success metric
- **Problem (✅ accepted framing):** the renewal owner has no single, fast "what needs me today" view in-companion, and renewal outreach is manual — find a due renewal, hand-write a broker email — costing time and producing inconsistent, sometimes non-compliant messaging.
- **Primary success metric (✅ Resolved):** **median time from "renewal needs attention" (surfaced in the glance) → "outreach draft ready."** No baseline exists today, so **F0038 instruments both timestamps to *establish* the baseline**; the improvement target is set after the first weeks of real data. → telemetry is an explicit F0038 requirement, not an afterthought.
- **Secondary signals (✅ Resolved — minimal v1 set):** instrument **companion daily-active usage** (renewal-owner persona), **% of attention-flagged renewals actioned**, and **count of drafts generated / mock-sent**. *Defer* edit-distance and compliance-violation tracking (need more infra). "What needs to be done" = emit these as telemetry events from F0038.

### 3.2 Primary persona & job-to-be-done
- **Persona (✅ Resolved — corrected):** **Primary = Underwriter** — in commercial P&C the **underwriter owns the renewal**. **Distribution / Relationship Manager assists as needed** (secondary, in-scope). Maps to BLUEPRINT persona 2 (primary) + personas 1/3 (secondary).
- **JTBD (✅ confirmed):** *"When I open the companion, show me the renewals that need me, and get me a ready-to-edit outreach email to the broker — without leaving my work."*
- **Users in scope (✅ Resolved):** Underwriter (owner / primary user) and Distribution (assist). Both in scope; underwriter primary.

### 3.3 The write — renewal outreach draft (business rules)
This is the mutation story; the framework requires a full interaction contract. Launched from the live Renewals zone (per-row `[Draft outreach]`).

- **Trigger (✅ Resolved):** the companion **proactively suggests** a draft for renewals needing attention via a **nudge/CTA**; the draft is **generated only on user action** (click) — never auto-generated, never auto-sent.
- **Who may draft (✅ Resolved):** **Underwriter only** (Casbin: dedicated `renewal:draft_outreach` on the Underwriter role). Distribution does **not** get draft rights in v1; a **"refer draft to Distribution for review"** flow is a **future feature**, explicitly out of F0038.
- **Never auto-sends (✅ confirmed):** v1 never sends real email. A human reviews/edits; "send" is a **mock-send** only (see landing).
- **Content constraints (✅ agreed):** draft references the upcoming renewal (account, expiry date, broker contact) and requests engagement; **must not** state/imply premium, quote figures, coverage terms, or any binding commitment; labelled **AI-generated draft**; **`InternalOnly`** until a human edits/approves.
- **Where the draft lands (✅ Resolved):** persist the draft as a renewal `ActivityTimelineEvent` (mandatory audit) and surface it **editable in-chat**; add a **mock-send** affordance — a `[Send]` that **commits the real workflow transition `Identified → Outreach`** and emits a "sent (simulated)" timeline event, but **fakes the SMTP delivery** (dispatches **no real email**). So mock-send mutates real pipeline state; only the email transport is simulated. **Not gated on F0021**; migrate to a real Communication Hub draft + real send later.
- **Audit/provenance (✅ confirmed):** generating a draft (and mock-send) emits an `ActivityTimelineEvent` with actor, timestamp, model, prompt id/version, and content hash.

### 3.4 Conversational scope & non-goals (✅ Resolved — see L8)
The companion is **CRM-scoped**: off-topic / non-CRM queries are politely **redirected** to CRM topics; it does not act as a general assistant. **Out of scope for F0038:**
- Acting as a general-purpose assistant (off-topic queries are redirected, not answered).
- Any write other than the renewal outreach draft.
- **Real email send** — v1 is **mock-send only**.
- Sending any outbound communication autonomously.
- **Cross-zone composition / ranking** ("what matters most today" across domains) — the Day-at-a-Glance *brain*. F0038 is the *frame* only; zones are independent.
- Any **live** specialist other than Renewals (Tasks/Pipeline/Broker-activity zones ship as inert stubs that read no data).
- Multi-thread management UI (deferred to F0039) and external hosts / Teams (deferred).

### 3.5 Read scope (✅ Resolved)
The **live Renewals zone** may read and surface, permission-respecting via the engine:
- a **list of the user's renewals needing attention** (account name, expiry date, days-to-expiry, workflow state, last-broker-contact signal), and
- per-renewal **drill context** (account, broker/contact, renewal timeline).

The **stub zones read nothing.** No other domains are read in v1.
- **"Needs attention" rule (✅ Resolved):** renewals in `Identified`/`Outreach` **within 90 days of expiry** (confirmed default, configurable), ordered by urgency, flag "no broker contact in 30+ days." Intra-zone ranking only — not cross-zone.

### 3.6 Thread model detail (✅ Resolved)
- A **glance is a list, not a single record**, so the glance thread is **domain-level (Renewals) or free-form**; drilling into a specific renewal to draft is the **record-context** moment.
- **Anchor types (✅):** domain-level (Renewals), record-level (Renewal / Account / Broker on drill), or free-form.
- **Auto-title (✅):** record-anchored threads auto-title from the record; the glance thread titles as "Day at a Glance" / "Renewals".
- **No re-anchoring (✅):** a thread's anchor is fixed at creation. Threads can be **deleted** or **renamed** (heading) — informs F0039.
- v1 keeps it simple — the glance and the draft action live in **one thread**; full multi-thread management is F0039.

### 3.7 Conversation persistence & retention (✅ Resolved)
- **Persistence (✅):** threads + messages stored in **Postgres via the engine** — inherit RBAC, audit, backup; neuron stays stateless.
- **Visibility (✅):** threads are **private to the creating user**.
- **Retention (✅):** retain per the `ActivityTimelineEvent` policy; threads are **user-deletable**.

### 3.8 Carried-forward decisions
✅ L1–L8 in §2 confirmed to hold.

---

## 4. Epic decomposition & roadmap stacking

Treated as a **feature set**, sequenced Now/Next/Later. No standalone "platform" feature — the reusable platform is *extracted* when the second live specialist arrives (generalize on the second real consumer, not on stubs).

```
 LATER  Day at a Glance BRAIN (cross-zone composition/ranking) · more specialist
        heads · REAL outreach send (email integration) + Comms Hub draft home (F0021)
        · writes beyond drafting · MCP-UI for external hosts (Teams) ·
        thread search/share/auto-title
                                   ▲
 NEXT   F0039 Multi-thread conversations (persistence impl, list/switch/rename/
              delete, resume, record-anchoring UX)
        F0040 Second specialist head — flip a stub (Accounts/Brokers) to LIVE
              ← head contract hardens here (first real second consumer)
                                   ▲
 NOW    F0038 Day at a Glance SHELL (Renewals live, others stubbed) + draft outreach
              + mock-send · frame + zone-dispatch + Renewals head + 1 write +
              CRM-scope guard + telemetry + reserved seams (§5)
```

| Bucket | Feature | Depends on | Note |
|--------|---------|-----------|------|
| **Now** | **F0038** Day-at-a-Glance shell (Renewals live) + draft outreach + mock-send | — | Proves the full chain end-to-end *and* lays the multi-head pathway. Assembly only. |
| **Next** | **F0039** Multi-thread conversations | F0038 persistence ADR | Implements the real store + thread UX. Valuable immediately (record-anchored). |
| **Next** | **F0040** Second specialist head | F0038 | Flip a stub zone to live; the orchestrator/registry/intent **platform and head contract are extracted/hardened here.** |
| **Later** | Day-at-a-Glance **brain** · more heads · **real send + Comms Hub (F0021)** · MCP-UI · richer writes · thread niceties | F0039/F0040 | Cross-zone intelligence + scale, once proven with ≥2 live heads. |

> **"Day at a Glance" appears twice on purpose:** the **frame** (multi-zone assembly) is F0038; the **brain** (cross-zone reasoning/ranking) is Later. Shipping the frame now sets the pathway; the intelligence waits until there are real zones to reason across.

---

## 5. Architecture seams to reserve in F0038 (cheap now, painful later)

Honor these in the walking skeleton even though the full features ship later:

1. **Thread id keys all conversation state** (not session/user). The message envelope carries `thread_id` from day one; single-thread v1 = "one thread, no switcher."
2. **Persistence home decided via ADR in F0038** (recommend Postgres-via-engine) even though thread management UX ships in F0039. F0038 may run on an in-memory store behind that same interface.
3. **Versioned, multi-part message envelope** (`text` | `app` | `status` | `sources` | `actions`) so persisted threads replay correctly as the `app`-part schema evolves.
4. **Zone-dispatch contract (multi-head from day one).** The orchestrator enumerates registered specialist heads and dispatches per zone; the Day-at-a-Glance app assembles each zone's result into a slot. Renewals returns content; stubbed heads return a typed **`inactive`** zone payload. This is the extension point that makes F0040 "flip a stub to live."
5. **Out-of-scope guard in the classifier (per L8).** From day one, non-CRM intents route to a polite CRM redirect rather than a general-assistant answer. Cheap to include now; defines the companion's character.

**Guardrails (hold firm):**
- **Assembly, not composition.** v1 zones are independent — no cross-zone ranking, merge, or "what matters most" logic. Adding that crosses into the deferred Day-at-a-Glance *brain*.
- **The head contract is thin and provisional.** Stubs prove a slot exists; they do **not** validate the head API (a stub agrees with anything). Keep the specialist interface minimal and expect to revise it when F0040 brings the first *live* second specialist. Do not gold-plate it now.
- **Visible stubs are a conscious product choice.** "Not yet active" zones advertise direction (fine for public preview); confirm vs. hiding-until-live.

---

## 6. Dependencies & open questions

- **F0021 Communication Hub (✅ Resolved — do not gate):** ship the interim timeline-event home + mock-send now; migrate to a real Comms Hub draft + real send Later. F0038 is **not** blocked on F0021.
- **Renewal workflow linkage (✅ Resolved):** **mock-send commits the real `Identified → Outreach` workflow transition** and emits the mandatory `WorkflowTransition` + `ActivityTimelineEvent`. Only the **email delivery (SMTP) is faked** — the pipeline state change is real. (Generating a draft alone does *not* transition; the transition fires on mock-send.)
- **New engine endpoints (architect, Phase B):** neuron needs (a) a **list** read for "renewals needing attention" for the current user, (b) per-renewal context reads (account, broker, timeline), (c) a write to persist the draft + emit the timeline event, and (d) a mock-send endpoint that **commits the `Identified → Outreach` transition** (`WorkflowTransition` + timeline event) **without** dispatching email. Additive to `nebula-api.yaml` (engine can take new endpoints — confirmed).
- **Token forwarding:** confirm the engine accepts the user's authentik token on neuron-originated calls (on-behalf-of), so RBAC is enforced unchanged. (architect to validate)

---

## 7. Proposed personas & non-goals (for BLUEPRINT §3 once confirmed)

- **Persona (primary):** **Underwriter** (renewal owner). Secondary: Distribution / Relationship Manager (assist).
- **Vision delta to BLUEPRINT §3.1:** add an AI-assisted, permission-aware, **CRM-scoped** conversational layer over existing CRM data, presented as a multi-zone Day-at-a-Glance shell with a bounded set of assisted writes — without changing the source-of-truth system.
- **Non-goals:** as enumerated in §3.4.

---

## 8. Next steps

1. ✅ All talk-through items resolved (A–D, 2026-06-29). **Switching into the Product Manager role (Phase A):** update `BLUEPRINT.md` §3, create `F0038` in `REGISTRY.md`/`ROADMAP.md`, and write the `F0038` PRD + vertically-sliced stories (glance shell, live Renewals zone, stub zones, the outreach-draft interaction contract + mock-send, CRM-scope guard, telemetry).
2. Hand off to the **Architect** role (Phase B): ADRs (persistence home, envelope, zone-dispatch/head contract, token-forwarding), C4 L2 + component view, new OpenAPI endpoints, Casbin model, and the `F0038` feature-assembly plan.

> **✅ Resolved talk-through items (2026-06-29):**
> - **A** (§3.3) — "suggest draft" = **proactive nudge/CTA; draft generated only on user click.** Not auto-generated, not auto-sent.
> - **B** (§6) — **mock-send commits the real `Identified → Outreach` transition**; only the SMTP email delivery is faked.
> - **C** (§3.3) — **Underwriter only** may draft; "refer to Distribution for review" is a **future feature**, out of F0038.
> - **D** (§3.5) — **90-day** "needs attention" window confirmed.
>
> Brief is **signed off** and ready to convert into Phase A artifacts.
