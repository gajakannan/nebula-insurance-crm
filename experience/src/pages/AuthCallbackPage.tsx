/**
 * AuthCallbackPage
 *
 * Renders at /auth/callback. Processes the OIDC Authorization Code callback,
 * bootstraps the session, and redirects to the appropriate landing route.
 *
 * Behaviour (S0002 / §1–§4 contract):
 *   - Calls signinRedirectCallback() to exchange the code for tokens.
 *   - On success: redirect to role-based landing route (§4).
 *   - On error (missing/invalid state, callback errors):
 *       clear transient auth state → redirect to /login?error=callback_failed.
 *   - Renders a neutral loading state while async processing is in flight
 *     (never flashes protected content).
 *
 * Landing routes (§4):
 *   - BrokerUser  → /brokers
 *   - all others  → /
 */
import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { oidcUserManager } from '@/features/auth/oidcUserManager';

const CALLBACK_LOCK_KEY = 'nebula_oidc_callback_inflight';
const CALLBACK_DONE_KEY = 'nebula_oidc_callback_done';
const CALLBACK_LANDING_KEY = 'nebula_oidc_callback_landing';

function resolveLandingRoute(roles: string[]): string {
  if (roles.includes('BrokerUser')) return '/brokers';
  return '/';
}

export function AuthCallbackPage() {
  const navigate = useNavigate();

  useEffect(() => {
    async function handleCallback() {
      const callbackSignature = `${window.location.pathname}${window.location.search}`;
      const previouslyCompleted = window.sessionStorage.getItem(CALLBACK_DONE_KEY);
      const rememberedLanding = window.sessionStorage.getItem(CALLBACK_LANDING_KEY) ?? '/';

      // React StrictMode replays effects in development. If this exact callback
      // was already completed, treat this invocation as a no-op success.
      if (previouslyCompleted === callbackSignature) {
        navigate(rememberedLanding, { replace: true });
        return;
      }

      const inFlight = window.sessionStorage.getItem(CALLBACK_LOCK_KEY);
      if (inFlight === callbackSignature) {
        return;
      }
      window.sessionStorage.setItem(CALLBACK_LOCK_KEY, callbackSignature);

      try {
        const user = await oidcUserManager.signinRedirectCallback();
        const roles: string[] = Array.isArray(user.profile['nebula_roles'])
          ? (user.profile['nebula_roles'] as string[])
          : typeof user.profile['nebula_roles'] === 'string'
            ? [user.profile['nebula_roles'] as string]
            : [];

        const landingRoute = resolveLandingRoute(roles);
        window.sessionStorage.setItem(CALLBACK_DONE_KEY, callbackSignature);
        window.sessionStorage.setItem(CALLBACK_LANDING_KEY, landingRoute);
        window.sessionStorage.removeItem(CALLBACK_LOCK_KEY);
        navigate(landingRoute, { replace: true });
      } catch {
        window.sessionStorage.removeItem(CALLBACK_LOCK_KEY);

        // If a prior invocation in this browser already completed this callback,
        // prefer the successful landing route instead of surfacing a false error.
        if (window.sessionStorage.getItem(CALLBACK_DONE_KEY) === callbackSignature) {
          navigate(window.sessionStorage.getItem(CALLBACK_LANDING_KEY) ?? '/', { replace: true });
          return;
        }

        // Callback failed: missing/invalid state, replay, or IdP error.
        // Clear any leftover PKCE/state artifacts before redirecting.
        try {
          await oidcUserManager.clearStaleState();
        } catch {
          // Swallow — clearStaleState failure must not block the redirect.
        }
        navigate('/login?error=callback_failed', { replace: true });
      }
    }

    void handleCallback();
  }, [navigate]);

  return (
    <main className="flex min-h-screen items-center justify-center">
      <p className="text-sm text-text-muted">Completing sign-in…</p>
    </main>
  );
}
