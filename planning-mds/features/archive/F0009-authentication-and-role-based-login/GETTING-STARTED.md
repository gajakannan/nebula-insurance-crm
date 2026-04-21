# F0009 — Authentication + Role-Based Login — Getting Started

## Prerequisites

- [ ] Backend API running (`engine/src/Nebula.Api`)
- [ ] Frontend app running (`experience`)
- [ ] authentik services running and reachable
- [ ] Required test identities provisioned with expected `nebula_roles`
- [ ] BrokerUser policy rows present in `planning-mds/security/policies/policy.csv`

## Services to Run

```bash
docker compose up -d db authentik-server authentik-worker
dotnet run --project engine/src/Nebula.Api
cd experience && pnpm dev
```

## Required Test Identities (Non-Production)

| Email | Role | Expected Landing |
|------|------|------------------|
| `lisa.wong@nebula.local` | `DistributionUser` | `/` |
| `john.miller@nebula.local` | `Underwriter` | `/` |
| `broker001@example.local` | `BrokerUser` | `/brokers` |

Notes:
- Internal users above already exist in backend dev seed profiles.
- `broker001@example.local` must exist as authentik user with `BrokerUser` role and valid `broker_tenant_id` claim.
- `broker_tenant_id` must resolve to exactly one active broker tenant mapping in backend.

## Verification Checklist

1. Open protected route unauthenticated -> redirect to `/login`.
2. Sign in each required user -> confirm expected landing route.
3. Trigger API `401` (expired/cleared session) -> redirect `/login`.
4. Trigger API `403` (insufficient permissions) -> permission-safe in-page error with trace id if available.
5. Confirm BrokerUser cannot access cross-broker records.
6. Confirm BrokerUser responses exclude InternalOnly fields per `BROKER-VISIBILITY-MATRIX.md`.

## Troubleshooting

- Login succeeds but access denied:
  - verify `nebula_roles` claim is emitted.
  - verify role is one of supported roles.
- Callback failure:
  - verify redirect URI includes `/auth/callback` and matches OIDC client config.
- BrokerUser denied unexpectedly:
  - verify `broker_tenant_id` claim is present and valid.
  - verify claim resolves to exactly one active broker tenant mapping.
- BrokerUser sees internal data:
  - block release; fix server-side response filtering.
