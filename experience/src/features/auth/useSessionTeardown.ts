/**
 * useSessionTeardown
 *
 * Implements the client-side session teardown contract defined in
 * IMPLEMENTATION-CONTRACT.md §2.1 and §3 (F-002 Resolution).
 *
 * Two trigger points:
 *   - reason='session_expired' → called from the API 401 interceptor
 *     (fire-and-forget: POST /auth/logout must not block redirect)
 *   - reason='logout' → called from explicit user logout action
 *
 * Mandatory ordering (from contract):
 *   1. Call POST /auth/logout  (fire-and-forget on 401 path)
 *   2. oidcUserManager.removeUser()
 *   3. oidcUserManager.clearStaleState()
 *   4. Redirect to /login (with ?reason=session_expired on expiry trigger)
 *
 * State must be fully cleared before redirect — never redirect first.
 */
import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { oidcUserManager } from './oidcUserManager';

export type TeardownReason = 'session_expired' | 'logout';

const LOGOUT_API_PATH = '/auth/logout';

/**
 * Calls POST /auth/logout as fire-and-forget.
 * Never throws — failure is swallowed so teardown always continues.
 * The endpoint must accept unauthenticated requests (§2.1 contract).
 */
async function callLogoutEndpoint(): Promise<void> {
  try {
    await fetch(LOGOUT_API_PATH, {
      method: 'POST',
      credentials: 'include', // send httpOnly refresh_token cookie server-side
    });
  } catch {
    // Network failure or server error — swallow and continue teardown.
    // The server-side cookie will expire naturally; client-side state
    // is cleared regardless below.
  }
}

/**
 * Clears all client-side OIDC state synchronously (well, awaited but fast).
 * Both removeUser() and clearStaleState() are safe to call even when no
 * session exists (idempotent).
 */
async function clearOidcState(): Promise<void> {
  await oidcUserManager.removeUser();
  await oidcUserManager.clearStaleState();
}

/**
 * Hook that returns a stable `teardown` function bound to React Router's
 * `navigate`. Callers do not need to worry about race conditions — the
 * redirect only fires after all client-side state has been cleared.
 *
 * Usage:
 *   const teardown = useSessionTeardown();
 *   // On explicit logout button:
 *   await teardown('logout');
 *   // In API 401 interceptor (call without await, or await is fine too):
 *   teardown('session_expired');
 */
export function useSessionTeardown() {
  const navigate = useNavigate();

  const teardown = useCallback(
    async (reason: TeardownReason): Promise<void> => {
      if (reason === 'session_expired') {
        // Fire-and-forget: do NOT await; redirect must not block on network.
        void callLogoutEndpoint();
      } else {
        // Explicit logout: await the server call so the cookie is cleared
        // before we redirect (avoids immediate re-auth on next login attempt).
        await callLogoutEndpoint();
      }

      // Clear all client-side OIDC state before navigating.
      await clearOidcState();

      // Redirect — always last, after state is cleared.
      if (reason === 'session_expired') {
        navigate('/login?reason=session_expired', { replace: true });
      } else {
        navigate('/login', { replace: true });
      }
    },
    [navigate],
  );

  return teardown;
}
