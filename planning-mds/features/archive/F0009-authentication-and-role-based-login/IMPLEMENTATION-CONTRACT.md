# F0009 — Implementation Contract (Architecture Refinement)

Purpose: define the mandatory "How" decisions for F0009 so implementation agents can build with no requirement ambiguity.

## 1. Authentication Flow Contract

- OIDC flow: Authorization Code + PKCE.
- Frontend OIDC client: `oidc-client-ts`.
- Required frontend routes:
  - `/login`
  - `/auth/callback`
  - `/unauthorized`
- Callback route validation is fail-closed:
  - missing/invalid `state` or callback errors -> clear transient auth state and redirect to `/login?error=callback_failed`.
- `dev-auth.ts` may remain only behind explicit fallback flag `VITE_AUTH_MODE=dev` for local transition.
- Required behavior for `VITE_AUTH_MODE=oidc`:
  - frontend must not call `getDevToken()`.

## 2. Session Lifecycle Contract

- Session source of truth: active OIDC user from `oidc-client-ts` and unexpired access token.
- On app bootstrap:
  - if valid session exists -> continue.
  - if no valid session -> redirect to `/login`.
- Silent renew is out of scope for F0009 Phase 1.
- Expired session behavior:
  - route access -> redirect `/login?reason=session_expired`.
  - clear in-memory session and any persisted OIDC user state.

### 2.1 Session Teardown Contract (F-002 Resolution)

Session teardown must be complete and ordered. Two triggers: explicit logout action, or API `401` response.

**Integration boundary:** Frontend calls `POST /auth/logout`. Backend owns all server-side teardown. Frontend owns all client-side teardown.

**Backend responsibilities (`POST /auth/logout`):**
1. Extract the refresh token from the incoming httpOnly cookie.
2. Call authentik revocation endpoint `POST /application/o/nebula/revoke/` with the refresh token (best-effort — log failure, do not block response).
3. Respond with `Set-Cookie: refresh_token=; Max-Age=0; HttpOnly; Secure; SameSite=Strict; Path=/` to clear the httpOnly cookie.
4. Return `204 No Content`. No body.
5. Endpoint must accept unauthenticated requests (session may already be invalid when this is called on 401 path).

**Frontend responsibilities (both triggers):**
1. Call `POST /auth/logout` (fire-and-forget on `401` path — do not await or block redirect on failure).
2. Call `oidcUserManager.removeUser()` to clear in-memory OIDC user state.
3. Call `oidcUserManager.clearStaleState()` to remove any sessionStorage PKCE/state artifacts.
4. Redirect to `/login` (with `?reason=session_expired` on expiry trigger, no query param on explicit logout).

**Ordering is mandatory:** client-side state must be cleared before redirect, not after.

**Endpoint spec:**
```
POST /auth/logout
Auth:   not required (must accept unauthenticated)
Body:   none
Cookie: refresh_token (httpOnly — read server-side, not accessible to JS)
Response:
  204 No Content
  Set-Cookie: refresh_token=; Max-Age=0; HttpOnly; Secure; SameSite=Strict; Path=/
```

## 3. Route Guard and Navigation Contract

- Protected routes require both:
  - authenticated session
  - role/resource authorization
- Deterministic unauthorized behavior:
  - route-level deny -> navigate to `/unauthorized`.
  - API-level `403` (generic policy deny) -> keep context and show permission-safe state with `traceId` when available.
  - API-level `403` with `code = "broker_scope_unresolvable"` -> navigate to `/unauthorized?reason=broker_inactive` (no session teardown — JWT is valid). See §15 for full contract.
  - API-level `401` -> clear session and redirect to `/login`.
- Role precedence for multi-role users:
  - `Admin` > `DistributionManager` > `DistributionUser` > `Underwriter` > `BrokerUser`.

## 4. Landing Route Contract

- `DistributionUser` -> `/`
- `Underwriter` -> `/`
- `BrokerUser` -> `/brokers`

Notes:
- Underwriter-specific dashboard route is deferred; Underwriter uses dashboard at `/` with API-level authorization.
- BrokerUser landing uses existing broker list surface with broker-scoped data filtering.

## 5. Claims and Identity Contract

- Required claims (all must be present and valid — missing or invalid = 401):
  - `aud` — must equal `nebula` (matches `Authentication__Audience` config). Token with wrong or missing audience is rejected with 401 before any other claim processing. (F-003 Resolution)
  - `iss` — must match configured authority issuer.
  - `sub`
  - `email`
  - `nebula_roles` (one or more)
- Required claim for BrokerUser:
  - `broker_tenant_id` (stable tenant identity from IdP)
- Role resolution input is only `nebula_roles`.
- If `nebula_roles` is missing, malformed, or unsupported:
  - sign-in may complete at IdP
  - Nebula must treat user as unauthorized and route to `/unauthorized`.
- `aud` validation must be enforced in JWT middleware configuration, not in application code. The middleware rejects the token before it reaches any endpoint handler.

## 6. Broker Scope Resolution Contract

- BrokerUser scope anchor: `broker_tenant_id` claim.
- Resolution rule:
  - resolve exactly one active broker tenant mapping by `broker_tenant_id`.
  - zero matches -> deny all broker-protected resources.
  - more than one match -> deny all broker-protected resources.
- Default deny when scope linkage cannot be resolved.

### 6.1 Scope Resolution Failure — HTTP Response Spec (F-010 Resolution)

When scope resolution fails (zero or ambiguous active `broker_tenant_id` mappings), the backend service layer must throw a typed exception that maps to the following HTTP response. This applies to all broker-scoped resource requests, not only `/brokers`.

```
HTTP 403
Content-Type: application/problem+json

{
  "type": "https://nebula.local/problems/broker-scope-unresolvable",
  "title": "Broker scope could not be resolved.",
  "status": 403,
  "code": "broker_scope_unresolvable",
  "traceId": "<trace-id>"
}
```

Constraints:
- The `code` field value `broker_scope_unresolvable` is the discriminator used by the frontend interceptor. The value must be exact — no variation.
- Do not include `broker_tenant_id` value or internal mapping details in the error response.
- This 403 is distinct from a generic Casbin policy-denied 403, which carries a different `code` value (or none).
- The backend exception must not trigger session teardown on the server side — the JWT is valid and the OIDC session is intact.

## 7. Authorization Policy Contract

- `planning-mds/security/authorization-matrix.md` section 2.10 is authoritative for BrokerUser scope.
- `planning-mds/security/policies/policy.csv` must be updated to include explicit BrokerUser policy lines for all allow/deny decisions in 2.10.
- Release is blocked if matrix and policy.csv are out of sync for BrokerUser resources/actions.

## 8. Broker Field Visibility Contract

- Field boundary list in `BROKER-VISIBILITY-MATRIX.md` is mandatory.
- Enforcement order is mandatory:
  1. query/service tenant isolation
  2. Casbin ABAC decision
  3. server-side DTO/response shaping
- Server-side response shaping is required for BrokerUser; UI hiding is defense-in-depth only.
- If an endpoint cannot guarantee field-level filtering for BrokerUser, that endpoint remains denied for BrokerUser in Phase 1.

### 8.1 ActivityTimelineEvent — BrokerDescription Split-Field Contract (F-004 Resolution)

- `ActivityTimelineEvent` carries two description fields:
  - `EventDescription` (InternalOnly) — full internal description, never returned to BrokerUser.
  - `BrokerDescription` (BrokerVisible, nullable) — broker-safe public description.
- `BrokerDescription` is populated **at event creation time** by the domain service for approved BrokerUser event types only. It is never derived or filtered from `EventDescription` at read time.
- Templates are defined in `BROKER-VISIBILITY-MATRIX.md §BrokerDescription Template Ownership`. Domain services must use these templates verbatim. Templates must not include internal user names, user IDs, system references, or InternalOnly field values.
- For all non-approved event types, `BrokerDescription` is NULL and the event must be **excluded entirely** from BrokerUser query results — not returned with a null field.
- BrokerUser timeline event query contract:
  1. Filter by `EntityType = 'Broker' AND EntityId IN (broker_ids within resolved broker scope)`.
  2. Filter by `EventType IN ('BrokerCreated', 'BrokerUpdated', 'BrokerStatusChanged', 'ContactAdded', 'ContactUpdated')`.
  3. Return DTO with: `id`, `entityType`, `entityId`, `eventType`, `brokerDescription`, `occurredAt`, `actorDisplayName`. Omit `eventDescription` and `actorUserId`.

## 9. Seed Identity Contract (Non-Production)

- Required test identities:
  - `lisa.wong@nebula.local` -> `DistributionUser`
  - `john.miller@nebula.local` -> `Underwriter`
  - `broker001@example.local` -> `BrokerUser`
- authentik blueprint/seeding must include:
  - `BrokerUser` group
  - user assignment for all three test identities
  - `nebula_roles` claim emission
- Seeding must be idempotent.

## 10. Test and Acceptance Contract

Minimum release validation must include:

1. Login redirect/callback happy path for each required test identity.
2. Session-expired redirect behavior and stale-state cleanup.
3. Route guard behavior (`401` and `403`) with deterministic UI outcomes.
4. Broker cross-scope denial tests (list + detail).
5. Broker field filtering tests (no `InternalOnly` fields in BrokerUser responses).
6. Matrix vs policy consistency check for BrokerUser actions.
7. Missing/invalid `broker_tenant_id` claim deny tests.

## 13. Dev-Auth Deployment Gate Contract (F-001 Resolution)

`VITE_AUTH_MODE` is the single control variable governing auth mode. Allowed values: `oidc` | `dev`.

**Default must be `oidc`.** Any environment without an explicit `VITE_AUTH_MODE=dev` override runs in OIDC mode. The safe default is authenticated — not bypassed.

**Integration boundary:** both layers below enforce the same rule independently. Either layer alone failing is sufficient to block an unsafe build from reaching deployment. Together they are belt-and-suspenders.

### Frontend responsibility (build-time guard)

A Vite plugin added to `vite.config.ts` must throw a hard error during `vite build` if:
- `process.env.VITE_AUTH_MODE === 'dev'` **AND** `process.env.NODE_ENV === 'production'`

Requirements:
- Plugin must run in the `buildStart` hook (executes before any bundling).
- Error message must be explicit: `"FATAL: VITE_AUTH_MODE=dev is not permitted in production builds. Set VITE_AUTH_MODE=oidc."`
- Plugin must be a no-op during `vite dev` (development server) — do not block local dev workflow.
- Plugin must be a no-op when `VITE_AUTH_MODE` is unset or `oidc`.
- Plugin is registered last in the plugins array (after react, tailwindcss).

### DevOps responsibility (CI assertion + env templates)

1. **CI assertion step** — add to `.github/workflows/frontend-ui.yml` before the `Build frontend` step:
   ```yaml
   - name: Assert VITE_AUTH_MODE is not dev (security gate)
     run: |
       if [ "${VITE_AUTH_MODE:-oidc}" = "dev" ]; then
         echo "FATAL: VITE_AUTH_MODE=dev must not be set in CI builds." >&2
         exit 1
       fi
   ```
   This step must have no `env:` block that sets `VITE_AUTH_MODE=dev`. It fails fast before `pnpm build` runs.

2. **Env templates** — create or update the following files so `VITE_AUTH_MODE=oidc` is the committed default for non-local environments:
   - `experience/.env.example` — add `VITE_AUTH_MODE=oidc` with comment explaining allowed values
   - `experience/.env.staging` (if it exists, or create it) — `VITE_AUTH_MODE=oidc`
   - `experience/.env.production` (if it exists, or create it) — `VITE_AUTH_MODE=oidc`
   - `experience/.env.development.local.example` — `VITE_AUTH_MODE=dev` (explicitly marks dev-only use)

3. **`.env.development` or `.env.local`** — must NOT be committed. Confirm these are in `.gitignore`.

### What each layer does NOT own

- Frontend plugin does not gate `vite dev` — local dev workflow must remain unblocked.
- DevOps CI step does not compile or inspect source code — it only checks the env var.
- Neither layer modifies `dev-auth.ts` — that file stays behind the `VITE_AUTH_MODE=dev` runtime check already in place.

## 12. BrokerUser Task Scope Query Contract (F-005 Resolution)

- BrokerUser task access is scoped via the existing `TaskItem` polymorphic link fields — no schema change required.
- **Scope rule:** BrokerUser may read tasks where:
  - `LinkedEntityType = 'Broker'`
  - `AND LinkedEntityId = <broker_id resolved from broker_tenant_id>`
  - `AND IsDeleted = false`
- Tasks where `LinkedEntityType` is not `'Broker'`, or where `LinkedEntityId` does not match the authenticated broker's resolved ID, must not be returned.
- Tasks linked to sibling broker entities (cross-broker) must not be returned even if the authenticated user has a valid session.
- `AssignedToUserId` is an internal `UserProfile` reference. BrokerUser does not hold an internal `UserProfile` entry and no task assignment-based scope applies in Phase 1. Scope is solely by broker entity link.
- If broker scope resolution fails (missing/unknown/ambiguous `broker_tenant_id` mapping), return HTTP 403 with `code: "broker_scope_unresolvable"` per §6.1/§15.
- If broker scope is valid and resolved, but no tasks match the query filters, return an empty result set.
- Required index: `IX_Tasks_LinkedEntity` on `(LinkedEntityType, LinkedEntityId)` is already defined in `data-model.md §1`. No additional index required.
- BrokerUser task DTO returns: `id`, `title`, `status`, `priority`, `dueDate`, `linkedEntityType`, `linkedEntityId`. Omit: `assignedToUserId`, `createdByUserId`, `updatedByUserId`, `rowVersion`, and all audit timestamps except `createdAt`.

## 14. Dashboard BrokerUser Nudge Contract (F-006 Resolution)

- `dashboard_kpi` and `dashboard_pipeline` are DENY for BrokerUser in Phase 1. These resources aggregate submission and renewal data which BrokerUser cannot access. The existing endpoint shapes cannot be safely filtered without a new contract — returning a partial response (e.g., only `activeBrokers`) would require a new endpoint shape. No policy lines exist for these resources under BrokerUser; the default Casbin deny applies.
- `dashboard_nudge` is ALLOW (read) for BrokerUser with mandatory server-side scope filter.
- BrokerUser nudge filter rule (all conditions must be satisfied at the query layer):
  - Include only NudgeCards where `nudgeType = 'OverdueTask'`
  - AND `linkedEntityType = 'Broker'`
  - AND `linkedEntityId IN (broker IDs resolved from broker_tenant_id scope)`
  - Exclude all `StaleSubmission` and `UpcomingRenewal` nudge types.
- If broker scope resolution fails (missing/unknown/ambiguous `broker_tenant_id` mapping), return HTTP 403 with `code: "broker_scope_unresolvable"` per §6.1/§15.
- If broker scope is valid and resolved, but no nudge cards match the query filters, return an empty array.
- NudgeCard fields returned to BrokerUser: `nudgeType`, `title`, `description`, `linkedEntityType`, `linkedEntityId`, `linkedEntityName`, `urgencyValue`, `ctaLabel`. All are BrokerVisible.
- No NudgeCard field is InternalOnly. The InternalOnly protection is at the nudge type filter level (excluding `StaleSubmission` and `UpcomingRenewal`), not by field suppression within the card schema.
- Required index: existing task link index (`IX_Tasks_LinkedEntity` on `(LinkedEntityType, LinkedEntityId)`, defined in `data-model.md §1`) covers the scope filter query. No additional index required.
- Field classification authority: `BROKER-VISIBILITY-MATRIX.md §Dashboard Resources §dashboard_nudge`.
- Policy authority: `policy.csv §2.10` — `BrokerUser, dashboard_nudge, read, true` is the only dashboard allow row for BrokerUser.

## 15. Deactivated Broker UX Contract (F-010 Resolution)

A BrokerUser may authenticate successfully at the IdP while their broker tenant mapping is deactivated or absent in Nebula. The IdP has no knowledge of broker lifecycle state — the JWT is valid but the scope cannot be resolved.

**Backend behavior (§6.1 specifies the HTTP response):**
- Zero or ambiguous active `broker_tenant_id` mappings → HTTP 403 with `code: "broker_scope_unresolvable"`.
- Applies to all broker-scoped resource requests, not only `/brokers`.
- Do not include `broker_tenant_id` value or internal mapping details in the error response.
- This is not a session teardown trigger on the server side.

**Frontend behavior:**
- `api.ts` interceptor detects `status === 403 AND code === "broker_scope_unresolvable"` and emits `'broker_scope_unresolvable'` on the `authEvents` event bus.
- `useAuthEventHandler` (mounted in `AppInner`) handles the event: calls `navigate('/unauthorized?reason=broker_inactive', { replace: true })`.
- No session teardown is performed — `removeUser()` and `clearStaleState()` are not called. The OIDC session remains intact.
- The failed request is abandoned (the interceptor returns a never-resolving promise so TanStack Query does not process a stale result during redirect).
- `/unauthorized` page reads the `reason` query param:
  - `reason=broker_inactive` → "Your broker account is currently inactive. Please contact your administrator."
  - no `reason` or unrecognised value → default "You do not have permission to access this page."
- Do not retry the failed request after redirect.

**Distinction from other error paths:**
| Trigger | HTTP | Frontend action |
|---------|------|-----------------|
| Expired/missing JWT | 401 | Session teardown → `/login?reason=session_expired` |
| Role policy deny | 403 (no code or other code) | Stay in context, show permission-safe state |
| Broker scope unresolvable | 403 `broker_scope_unresolvable` | Navigate to `/unauthorized?reason=broker_inactive` (no teardown) |
| Route-level role deny | — | Navigate to `/unauthorized` (no reason param) |

**Implementation files:**
- `experience/src/features/auth/authEvents.ts` — `AuthEvent` type includes `'broker_scope_unresolvable'`
- `experience/src/features/auth/useAuthEventHandler.ts` — handles the event, calls `navigate`
- `experience/src/services/api.ts` — `handleErrorIntercept` emits the event on 403 + code match
- `experience/src/pages/UnauthorizedPage.tsx` — reads `reason` param, renders role-appropriate message
- `experience/src/App.tsx` — `/unauthorized` route registered, `UnauthorizedPage` imported

## 16. BrokerUser Audit Event Contract (F-008 Resolution)

All BrokerUser read operations against broker-scoped resources must emit a structured audit log entry. Audit events are written via `ILogger` structured logging (not to `ActivityTimelineEvent` — that table is for domain events). Phase 1: structured application log only. Phase 2 may route to a dedicated audit sink via Serilog/OpenTelemetry.

**Trigger condition:** Audit entry is emitted only when `ICurrentUserService.Roles.Contains("BrokerUser")`. Internal roles are not audited at this granularity in Phase 1.

**Covered resources and resource identifiers:**

| Resource | Identifier field | EntityId value |
|----------|-----------------|----------------|
| `broker.list` | n/a | null |
| `broker.detail` | `brokerId` (path param) | broker UUID |
| `broker.contacts` | `brokerId` (path param) | broker UUID |
| `broker.timeline` | `brokerId` or `entityId` (path param) | broker UUID |
| `broker.tasks` | `brokerId` (path param) | broker UUID |
| `dashboard.nudges` | n/a | null |

**Required structured log properties:**

| Property | Type | Source |
|----------|------|--------|
| `EventType` | `string` | Constant: `"BrokerUserResourceRead"` |
| `Resource` | `string` | Resource identifier from table above |
| `BrokerTenantId` | `string?` | `ICurrentUserService.BrokerTenantId` (`broker_tenant_id` claim) |
| `ResolvedBrokerId` | `Guid?` | Resolved broker entity ID (null for list/nudges where not resolved at log point) |
| `EntityId` | `Guid?` | Path param entity ID (null for list endpoints) |
| `OccurredAt` | `DateTime` | `DateTime.UtcNow` at emit time |

**Log level:** `Information`.

**Log message template:**
```
"BrokerUser access: {Resource} by BrokerTenantId={BrokerTenantId} ResolvedBrokerId={ResolvedBrokerId} EntityId={EntityId} OccurredAt={OccurredAt}"
```

**ICurrentUserService contract extension:**
- `string? BrokerTenantId { get; }` — reads `broker_tenant_id` claim. Returns `null` for non-BrokerUser identities.

**Implementation location:** `AuditBrokerUserRead` private helper method in each service class (`BrokerService`, `ContactService`, `TimelineService`, `TaskService`, `DashboardService`). Called after successful data retrieval, before return.

**What is NOT audited in Phase 1:**
- Write operations (create, update, delete) — these are covered by `ActivityTimelineEvent` domain events.
- Internal role read operations.
- Failed requests (scope resolution failure, 403, 401) — these are logged at Warning/Error level by the error handling middleware.
- BrokerUser's own identity resolution at login — that is the IdP's audit responsibility.

## 11. Deferred Items (Explicitly Not in F0009)

- Silent token renewal.
- Cross-application single logout.
- Broker self-service submission/renewal workflows.
- Multi-role landing preference customization.
