# F0037 — Hierarchy-Aware Access Scoping & Distribution Rollups — Getting Started

## Prerequisites

- [ ] F0017 (Broker/MGA Hierarchy, Producer Ownership & Territory Management) delivered — provides the structural model, effective-dated reads, and audit events this feature consumes
- [ ] Review F0023 reporting substrate before designing hierarchy-aware rollups
- [ ] Review the authorization/policy foundation (Casbin ABAC) before designing access scoping

## How to Verify

1. Confirm F0017's hierarchy/territory/ownership identifiers and effective-dated reads are stable.
2. Refine F0037 into stories (access enforcement + rollup reporting) via the `plan` action.
3. Validate tracker sync after refinement.
