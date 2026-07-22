# Finance Operations Personas — F0026

These role archetypes were confirmed by the operator during F0026 plan run `2026-07-19-79477865`. They intentionally avoid invented demographics or production-volume assumptions.

## Finance Operations Analyst

**Priority:** Primary
**Archetype:** Internal finance operations user

### Responsibilities

- Create policy-linked agency-bill invoices.
- Record manual receipts and review mock-vendor CSV outcomes.
- Explicitly apply exact receipts and investigate exceptions.
- Request balance-affecting corrections when operational billing data is wrong.

### Goals And Jobs To Be Done

1. **When** an agency-bill obligation is ready, **I want to** create an invoice from policy/version context, **so I can** avoid re-keying or losing the source relationship.
2. **When** a payment reference arrives, **I want to** record its source and reconcile an exact item, **so I can** clear operational work with audit evidence.
3. **When** a receipt does not match, **I want to** see a specific exception and next action, **so I can** resolve it without silently changing finance data.

### Authorization And Constraints

- May create invoices, record/import receipts, apply exact matches, and request corrections within source-record scope.
- Cannot approve or reject their own correction request.
- Cannot post ledger entries, write off balances, connect to a real bank, or access unauthorized policy/account finance data.

### Success Signals

- Exact items clear with one explicit action and persist after reload.
- Mismatches remain visible and do not change invoice balance.
- Source policy/version, receipt provenance, and audit history are available from one workspace.

## Finance Manager

**Priority:** Primary approver
**Archetype:** Internal finance control and oversight user

### Responsibilities

- Approve or reject analyst-requested balance corrections.
- Monitor reconciliation backlog, days open, and source/import failures.
- Review audit history and confirm separation of duties.

### Goals And Jobs To Be Done

1. **When** an analyst requests a correction, **I want to** see source invoice, receipt, reason, and before/after values, **so I can** make a controlled decision.
2. **When** unresolved items accumulate, **I want to** filter backlog and drill to evidence, **so I can** prioritize operational follow-up.
3. **When** a decision is questioned, **I want to** inspect immutable actor/timestamp history, **so I can** explain what changed without relying on a ledger replacement.

### Authorization And Constraints

- May approve/reject eligible correction requests within source-record scope.
- Cannot approve a request they created.
- Does not receive capabilities for automated tolerances, write-offs, real settlement, tax, or general-ledger posting in F0026.

### Success Signals

- Every correction decision has a different requester/decider and a required note.
- Backlog counts and drilldowns are calculated after authorization filtering.
- No external user can infer invoice, receipt, exception, or adjustment data.

## Secondary Read-Only User

Authorized Distribution/Relationship users may see bounded billing summary context from a policy/account surface. They cannot create invoices, record receipts, reconcile, or view restricted finance detail unless a later approved policy explicitly grants it.
