# F0009 Security Review Checklist

**Owner:** Security Agent  
**Status:** Draft (Review-Ready)  
**Scope:** F0009 Authentication + Role-Based Login

## 1. Identity and Claims

- [ ] JWT validation enforces expected issuer/audience.
- [ ] `nebula_roles` claim parsing is strict and fail-closed.
- [ ] BrokerUser requires `broker_tenant_id` claim.
- [ ] Missing/invalid/malformed required claims return deny outcome.

## 2. ABAC Enforcement

- [ ] Casbin checks run on every protected API action.
- [ ] Casbin checks run on secondary access channels (MCP/tools) if enabled.
- [ ] No unprotected route or endpoint bypasses policy enforcement.
- [ ] Matrix (`authorization-matrix.md` §2.10) and `policy.csv` BrokerUser rows are in parity.

## 3. Tenant Isolation (Broker-to-Broker)

- [ ] BrokerUser scope resolves by `broker_tenant_id`.
- [ ] Scope resolution is deterministic: exactly one active mapping required.
- [ ] Unknown/ambiguous mapping is denied by default.
- [ ] Query/service layer enforces tenant predicates before response shaping.
- [ ] Integration tests prove cross-broker list/detail denial.

## 4. Field-Level Visibility

- [ ] Response shaping enforces `BROKER-VISIBILITY-MATRIX.md`.
- [ ] `InternalOnly` fields are absent from BrokerUser responses.
- [ ] Filtering is server-side (frontend-only filtering is not accepted).
- [ ] Timeline event type allowlist for BrokerUser is enforced.

## 5. Deterministic Error Handling

- [ ] API `401` behavior clears session and redirects to login in UI.
- [ ] API `403` behavior stays in context with permission-safe message.
- [ ] ProblemDetails payload does not leak sensitive internals.

## 6. Agent/MCP Hardening (if enabled)

- [ ] No raw SQL tool exposed to LLM agents.
- [ ] Tool interfaces are whitelisted resource/action operations.
- [ ] Tool layer applies ABAC + tenant filters + field visibility filters.
- [ ] Tool calls include user/tenant context and are audit-logged.

## 7. Data-Layer Posture

- [ ] Phase 1 decision acknowledged: RLS not required.
- [ ] Compensating controls verified:
  - tenant-scoped query filters
  - ABAC checks
  - server-side field filtering
  - audit logs (F-008: BrokerUser read audit via ILogger structured logging — see IMPLEMENTATION-CONTRACT.md §16)
- [ ] Phase 2 hardening backlog includes optional RLS rollout plan.

## 8. Evidence Required

- [ ] Test report for seeded users (DistributionUser / Underwriter / BrokerUser).
- [ ] Negative tests for missing `broker_tenant_id`.
- [ ] Cross-broker deny test output.
- [ ] Sample sanitized BrokerUser payload evidence.
- [ ] Policy parity check output (`authorization-matrix.md` vs `policy.csv`).

