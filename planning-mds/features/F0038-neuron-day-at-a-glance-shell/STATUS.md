# F0038 — Neuron Day-at-a-Glance Shell — Status

**Overall Status:** Draft
**Last Updated:** 2026-06-29

> Skeleton feature. Stories, the required-signoff matrix, and architecture are
> finalized during the `plan` run. The signoff rows below are placeholders.

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|

## Required Signoff Roles (Set in Planning)

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Acceptance-criteria + telemetry/workflow-transition validation. | Architect | TBD |
| Code Reviewer | Yes | Independent review of zone-dispatch, draft write, and mock-send logic. | Architect | TBD |
| Security Reviewer | Yes | Token forwarding (on-behalf-of), new Casbin `renewal:draft_outreach`, audit/provenance, prompt-injection surface, and no model-generated markup execution. | Architect | TBD |
| AI Engineer | Yes | `neuron/` bootstrap, classifier, A2A-aligned internal delegation, versioned YAML orchestration, specialist head registry, message/component contract, prompt provenance, and scope guard. | Architect | TBD |
| DevOps | Yes | First runnable Neuron service likely changes runtime/env/container/health-check contracts; confirm exact scope during plan. | Architect | TBD |
| Architect | Yes | Persistence/envelope/head-contract/A2A-profile/token-forwarding/component-contract/orchestration ADRs require explicit approval. | Architect | TBD |

## Story Signoff Provenance

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0038-S0001 | Quality Engineer | - | N/A | - | - | Populate after story breakdown is created in the plan run. |
| F0038-S0001 | Code Reviewer | - | N/A | - | - | Populate after story breakdown is created in the plan run. |

## Notes

- Source of truth for scope: [`intake-brief.md`](./intake-brief.md) (signed off 2026-06-29).
- This feature is **not blocked on F0021** (interim timeline-event home + mock-send now).
