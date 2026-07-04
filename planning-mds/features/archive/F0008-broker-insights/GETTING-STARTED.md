# F0008 — Broker Insights — Getting Started

## Prerequisites

- [ ] Read the current release framing in [ROADMAP.md](../ROADMAP.md)
- [ ] Review the F0008 Phase A story set before architecture or coding
- [ ] Review available broker, submission, renewal, policy, activity, hierarchy, producer, territory, and reporting source data
- [ ] Confirm F0037 boundaries before proposing any hierarchy-aware access enforcement or distribution rollup behavior
- [ ] Read ADR-029 and the `BrokerInsights` OpenAPI section before implementation planning
- [ ] Review `broker_insight:read` in the authorization matrix and policy file

## How to Verify

1. Confirm the feature metrics are grounded in trusted workflow data.
2. Define the minimal scorecards and trend views needed for the first release.
3. Validate tracker sync after refinement.
4. Confirm every count, benchmark, trend point, snapshot value, and source row is permission-filtered.
5. Confirm F0008 stays read-only until a future approved story adds a mutation path.
6. Confirm benchmark rank/percentile suppression when fewer than five visible peers match.
