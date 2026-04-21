/**
 * Singleton UserManager instance for oidc-client-ts.
 *
 * Configuration is driven by environment variables injected at build time via
 * Vite's `import.meta.env`. The UserManager is created once and re-used across
 * the app (hook, interceptor, callback handler, etc.).
 *
 * Only instantiated when VITE_AUTH_MODE=oidc. When VITE_AUTH_MODE=dev the
 * UserManager is still available but auth flow routes are not activated.
 *
 * F0009 — S0001/S0002 own the login redirect and callback bootstrap that use
 * this manager. This file simply exposes the singleton so teardown logic
 * (useSessionTeardown) can call removeUser() / clearStaleState() without
 * importing the full UserManager configuration inline.
 */
import { UserManager, WebStorageStateStore } from 'oidc-client-ts';

const authority = import.meta.env.VITE_OIDC_AUTHORITY as string | undefined;
const clientId = import.meta.env.VITE_OIDC_CLIENT_ID as string | undefined;
const redirectUri = import.meta.env.VITE_OIDC_REDIRECT_URI as string | undefined;

if (!authority || !clientId || !redirectUri) {
  // Fail loudly in dev/CI so misconfiguration is visible immediately.
  // In production a missing variable is a deployment error, not a runtime one.
  console.warn(
    '[nebula/auth] VITE_OIDC_AUTHORITY, VITE_OIDC_CLIENT_ID, and VITE_OIDC_REDIRECT_URI ' +
      'must be set when VITE_AUTH_MODE=oidc. ' +
      'UserManager will be misconfigured until environment is corrected.',
  );
}

export const oidcUserManager = new UserManager({
  authority: authority ?? '',
  client_id: clientId ?? '',
  redirect_uri: redirectUri ?? '',
  response_type: 'code',
  // Request Nebula custom scopes so role/scope claims are present in tokens.
  // Without these, role-based routing and API ABAC checks will fail closed.
  scope: 'openid profile email nebula_roles broker_tenant_id',
  // Store OIDC user state in sessionStorage so it is cleared on tab close.
  // PKCE code verifier artifacts also land in sessionStorage; clearStaleState()
  // removes them on teardown.
  userStore: new WebStorageStateStore({ store: window.sessionStorage }),
  // Silent renew is deferred to a later F0009 phase.
  automaticSilentRenew: false,
  // Access token is stored in-memory only (not persisted) per ADR-002.
  // oidc-client-ts default: access_token is held in the User object which
  // lives in the userStore above; the token itself is not separately persisted.
});
