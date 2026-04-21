/**
 * ProtectedRoute
 *
 * Guards a subtree of routes by verifying an active, unexpired OIDC session.
 * In VITE_AUTH_MODE=dev, the guard is a no-op (always renders children).
 *
 * Behaviour (§2 and §3 contract):
 *   - Loading session state:  render null (avoids flash of unprotected content)
 *   - No valid session:       redirect to /login (replace — no back-nav to protected route)
 *   - Expired session:        redirect to /login?reason=session_expired
 *   - Valid session:          render children
 *
 * Roles are not validated here — resource-level authorization is enforced by
 * the backend. Route-level role checks (if any) belong in a separate layer.
 */
import { ReactNode, useEffect, useState } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { oidcUserManager } from './oidcUserManager';

const AUTH_MODE = import.meta.env.VITE_AUTH_MODE as string | undefined;

type SessionState = 'loading' | 'authenticated' | 'unauthenticated' | 'expired';

interface Props {
  children: ReactNode;
}

export function ProtectedRoute({ children }: Props) {
  const location = useLocation();
  const [sessionState, setSessionState] = useState<SessionState>('loading');

  useEffect(() => {
    if (AUTH_MODE === 'dev') {
      setSessionState('authenticated');
      return;
    }

    oidcUserManager.getUser().then((user) => {
      if (!user) {
        setSessionState('unauthenticated');
      } else if (user.expired) {
        setSessionState('expired');
      } else {
        setSessionState('authenticated');
      }
    });
  }, [location.pathname]);

  if (AUTH_MODE === 'dev' || sessionState === 'authenticated') {
    return <>{children}</>;
  }

  if (sessionState === 'loading') {
    // Render nothing while checking session — avoids protected content flash.
    return null;
  }

  if (sessionState === 'expired') {
    return <Navigate to="/login?reason=session_expired" replace />;
  }

  // unauthenticated — redirect to /login, preserving intended destination in state
  return <Navigate to="/login" replace state={{ from: location }} />;
}
