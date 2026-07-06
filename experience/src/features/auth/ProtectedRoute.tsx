/**
 * ProtectedRoute
 *
 * Guards a subtree of routes by verifying an active, unexpired OIDC session.
 * In VITE_AUTH_MODE=dev, the guard is a no-op (always renders children).
 *
 * Behaviour (§2 and §3 contract):
 *   - Loading session state:  render null (avoids flash of unprotected content)
 *   - No valid session:       redirect to /login (replace — no back-nav to protected route)
 *   - Expired session:        attempt silent renewal before forced reauth
 *   - Valid session:          render children
 *
 * Roles are not validated here — resource-level authorization is enforced by
 * the backend. Route-level role checks (if any) belong in a separate layer.
 */
import { ReactNode, useEffect, useState } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { IS_DEV_AUTH_MODE } from './authMode';
import { oidcUserManager } from './oidcUserManager';
import { emitAuthEvent } from './authEvents';
import {
  RenewalError,
  renewSessionForExpiredToken,
} from '@/features/session-continuity/sessionRenewal';
import { persistFailureClassEvent } from '@/features/session-continuity/deferredTelemetryBuffer';
import {
  buildSessionContinuityEvent,
  emitSessionContinuityEvent,
  type SessionContinuityEventName,
  type SessionContinuityIdentity,
} from '@/features/session-continuity/sessionTelemetry';

type SessionState = 'loading' | 'authenticated' | 'unauthenticated';

interface Props {
  children: ReactNode;
}

export function ProtectedRoute({ children }: Props) {
  const location = useLocation();
  const [sessionState, setSessionState] = useState<SessionState>('loading');

  useEffect(() => {
    if (IS_DEV_AUTH_MODE) {
      setSessionState('authenticated');
      return;
    }

    oidcUserManager.getUser().then(async (user) => {
      if (!user) {
        setSessionState('unauthenticated');
      } else if (user.expired) {
        try {
          await renewSessionForExpiredToken();
          setSessionState('authenticated');
        } catch (error) {
          const cause = error instanceof RenewalError
            ? error.cause
            : 'idp_unreachable';
          recordFailureClassEvent(user, 'silent-renewal-fail', { cause });
          recordFailureClassEvent(user, 'forced-redirect', {
            cause,
            route_at_redirect: location.pathname,
          });
          emitAuthEvent('forced_reauth', {
            cause,
            method: 'GET',
            endpointRoute: location.pathname,
            returnTo: `${location.pathname}${location.search}`,
          });
          setSessionState('loading');
        }
      } else {
        setSessionState('authenticated');
      }
    });
  }, [location.pathname, location.search]);

  if (IS_DEV_AUTH_MODE || sessionState === 'authenticated') {
    return <>{children}</>;
  }

  if (sessionState === 'loading') {
    // Render nothing while checking session — avoids protected content flash.
    return null;
  }

  // unauthenticated — redirect to /login, preserving intended destination in state
  return <Navigate to="/login" replace state={{ from: location }} />;
}

function recordFailureClassEvent(
  user: SessionContinuityIdentity | null,
  eventName: SessionContinuityEventName,
  payload: Record<string, unknown>,
): void {
  const event = buildSessionContinuityEvent(user, eventName, payload);
  if (!event) {
    return;
  }

  persistFailureClassEvent(event);
  emitSessionContinuityEvent(event);
}
