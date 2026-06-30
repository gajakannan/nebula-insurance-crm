---
template: feature
version: 1.1
applies_to: product-manager
---

# F0040: Neuron Second Specialist Head

**Feature ID:** F0040
**Feature Name:** Neuron Second Specialist Head
**Epic:** Neuron Companion (AI Conversational Layer) — Next
**Priority:** Medium
**Phase:** Neuron Companion
**Status:** Provisional skeleton (Planned) — **scope firms up after F0038 lands.**

> **Provisional.** Reserved placeholder to show the full epic arc. The specialist
> domain to flip live, and the exact head-contract revisions, are decided after
> F0038 proves the zone-dispatch seam with a single live head. The thin,
> provisional head contract is **expected to be revised here** — F0040 is the
> first *real* second consumer, so this is where the head/orchestrator/registry
> platform is extracted and hardened on top of F0038's simple versioned YAML
> orchestration, A2A-aligned internal delegation, and `crm_agents` package
> convention. Do not gold-plate before this point.
> Epic source: [`../F0038-neuron-day-at-a-glance-shell/intake-brief.md`](../F0038-neuron-day-at-a-glance-shell/intake-brief.md).

## Feature Statement

**As an** Underwriter / Distribution user
**I want** a second Day-at-a-Glance zone to become live (e.g., Accounts or Brokers), alongside Renewals
**So that** the companion covers more of my CRM work and the multi-head platform is proven on a real second consumer

## Business Objective

- **Goal:** Flip one stubbed zone from inert to LIVE, and in doing so **extract/harden** the specialist-head contract, orchestrator, and registry on the first real second consumer (generalize on the second live head, not on stubs).
- **Metric:** TBD at plan (e.g., adoption of the second zone, reuse of the head platform).

## Scope & Boundaries (provisional)

**Likely In Scope:**
- One stub zone (candidate: Accounts or Brokers — confirm at plan) becomes a **live specialist head** with its own read scope.
- **Head contract hardening:** revise the thin F0038 head interface into a reusable contract; extract the orchestrator/registry/intent platform.
- Reuse and refine the F0038 versioned YAML plan format, specialist-head
  registry, private Agent Card/capability registry, A2A-shaped task model, tool
  registry, message/component/action envelope, and backend-owned
  prompt/provenance model.

**Out of Scope (provisional):**
- **Cross-zone composition / ranking** — still the Day-at-a-Glance *brain* (Later), even with two live heads.
- New writes beyond what the second domain minimally needs (writes-beyond-drafting are Later).
- Additional heads beyond the second (Later).

## Dependencies

- **F0038** — zone-dispatch contract, simple versioned YAML orchestration,
- A2A-aligned internal delegation, `crm_agents` package convention,
  message/component/action contract, and live Renewals head (hard dependency).
- **ADR-027** — Neuron Companion A2A-aligned orchestration foundation.

## Related User Stories

- To be defined during F0040's `plan` run, after F0038 establishes the single-live-head pathway.
