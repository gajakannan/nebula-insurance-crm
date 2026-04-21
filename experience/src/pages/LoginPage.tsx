/**
 * LoginPage
 *
 * Renders at /login. Initiates the OIDC Authorization Code + PKCE redirect
 * to authentik when the user clicks Sign In.
 *
 * Behaviour (S0001 / §1 contract):
 *   - VITE_AUTH_MODE=oidc: show Sign In button → signinRedirect()
 *   - VITE_AUTH_MODE=dev:  show dev-mode bypass notice and auto-navigate to /
 *   - Missing OIDC config (authority/clientId/redirectUri unset): show config error
 *   - ?reason=session_expired: show session expired notice above the button
 *   - IdP redirect failure: show error inline (never navigate away)
 */
import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { oidcUserManager } from '@/features/auth/oidcUserManager';

const AUTH_MODE = import.meta.env.VITE_AUTH_MODE as string | undefined;
const OIDC_AUTHORITY = import.meta.env.VITE_OIDC_AUTHORITY as string | undefined;
const OIDC_CLIENT_ID = import.meta.env.VITE_OIDC_CLIENT_ID as string | undefined;
const OIDC_REDIRECT_URI = import.meta.env.VITE_OIDC_REDIRECT_URI as string | undefined;

const REASON_MESSAGES: Record<string, string> = {
  session_expired: 'Your session has expired. Please sign in again.',
};

export function LoginPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const reason = searchParams.get('reason') ?? '';
  const error = searchParams.get('error') ?? '';

  const [signinError, setSigninError] = useState<string | null>(null);
  const [isRedirecting, setIsRedirecting] = useState(false);

  // Dev mode: skip login UI entirely — navigate to home immediately.
  useEffect(() => {
    if (AUTH_MODE === 'dev') {
      navigate('/', { replace: true });
    }
  }, [navigate]);

  if (AUTH_MODE === 'dev') {
    return null;
  }

  // Config guard: missing required OIDC environment variables.
  const isMisconfigured = !OIDC_AUTHORITY || !OIDC_CLIENT_ID || !OIDC_REDIRECT_URI;

  async function handleSignIn() {
    setSigninError(null);
    setIsRedirecting(true);
    try {
      await oidcUserManager.signinRedirect();
      // Navigation away from this page occurs via the OIDC redirect.
      // setIsRedirecting stays true until the page unloads.
    } catch {
      setIsRedirecting(false);
      setSigninError(
        'Unable to reach the identity provider. Please try again or contact support.',
      );
    }
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-6 p-8 text-center">
      <div className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold text-text-primary">Welcome to Nebula</h1>
        <p className="text-sm text-text-muted">Commercial P&amp;C Insurance CRM</p>
      </div>

      {reason && REASON_MESSAGES[reason] && (
        <p
          role="status"
          className="max-w-sm rounded-md bg-amber-50 px-4 py-2 text-sm text-amber-800 ring-1 ring-amber-200"
        >
          {REASON_MESSAGES[reason]}
        </p>
      )}

      {error === 'callback_failed' && (
        <p
          role="alert"
          className="max-w-sm rounded-md bg-red-50 px-4 py-2 text-sm text-red-700 ring-1 ring-red-200"
        >
          Sign-in could not be completed. Please try again.
        </p>
      )}

      {isMisconfigured ? (
        <p
          role="alert"
          className="max-w-sm rounded-md bg-red-50 px-4 py-2 text-sm text-red-700 ring-1 ring-red-200"
        >
          Authentication is not configured. Contact your administrator.
        </p>
      ) : (
        <>
          <button
            onClick={handleSignIn}
            disabled={isRedirecting}
            className="rounded-md bg-blue-600 px-6 py-2 text-sm font-medium text-white shadow-sm hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isRedirecting ? 'Redirecting…' : 'Sign In'}
          </button>

          {signinError && (
            <p
              role="alert"
              className="max-w-sm rounded-md bg-red-50 px-4 py-2 text-sm text-red-700 ring-1 ring-red-200"
            >
              {signinError}
            </p>
          )}
        </>
      )}
    </main>
  );
}
