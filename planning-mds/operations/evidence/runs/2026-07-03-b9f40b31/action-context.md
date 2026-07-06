# Action Context — F0022 feature run 2026-07-03-b9f40b31

## Run Identity

- Action: `feature`
- Mode: `clean`
- Feature ID: `F0022`
- Feature: Work Queues, Assignment Rules & Coverage Management
- Product root: `/Users/wallstreet/Desktop/nebula_workspace/nebula-insurance-crm`
- Framework root: `/Users/wallstreet/Desktop/nebula_workspace/nebula-agents`
- Run ID: `2026-07-03-b9f40b31`
- Run folder: `planning-mds/operations/evidence/runs/2026-07-03-b9f40b31`
- Feature index root: `planning-mds/operations/evidence/features/F0022-work-queues-assignment-rules-and-coverage-management`

## Inputs

- `agents/templates/prompts/evidence-contract/feature-operator-friendly.md`
- `agents/actions/feature.md`
- `agents/ROUTER.md`
- `agents/agent-map.yaml`
- `agents/docs/AGENT-USE.md`
- `agents/architect/SKILL.md`
- `planning-mds/features/F0022-work-queues-assignment-rules-and-coverage-management/`
- `planning-mds/operations/evidence/runs/2026-07-03-8aa72827/`

## Scope Boundaries

- Implement F0022 only.
- Preserve F0032 as downstream governance; do not expand this feature into full admin configuration governance.
- Follow accepted ADR-013, F0022 API contracts, schemas, authorization matrix, and policy rows.
- Create `feature-assembly-plan.md` in G0 before implementation.

## Lifecycle Stage

- Current gate: `COMPLETE`
- Prior plan approval: G5 passed in run `2026-07-03-8aa72827`
- Runtime preflight: passed G1
- Implementation: G2 backend, migration, frontend, runtime smoke, and focused authorization evidence complete
- Review: G3 code and security review complete with non-blocking recommendations
- Operator approval: G4 approved by user message `approve`
- Signoff: G5 signoff ledger written with all required role evidence
- Candidate: G6 feature action execution record written
- KG: G7 reconciliation complete; F0022 policy/matrix drift corrected
- Closeout: G8 PM closeout drafted with accepted deferred recommendations

## Assumptions

- Initial routed work types are tasks, submissions, and renewals.
- No-match work routes to `Unassigned Operations Queue`.
- Rule precedence is manual override, coverage/out-of-office, territory/ownership, workload balancing, fallback queue.
- Security review is required because queue visibility and reassignment affect access and work ownership.
