/**
 * useAuthEventHandler
 *
 * Subscribes to auth events published by the API error interceptor
 * (authEvents.ts) and dispatches the appropriate navigation response.
 *
 * Handled events:
 *   - 'session_expired'            → session teardown → /login?reason=session_expired
 *   - 'broker_scope_unresolvable'  → navigate to /unauthorized?reason=broker_inactive
 *                                    (no session teardown — JWT is valid)
 *
 * Must be mounted once near the root of the app (inside BrowserRouter, outside
 * any protected guard) so it is always active and can respond to API errors on
 * any page.
 *
 * Usage in App.tsx:
 *   function AppInner() {
 *     useAuthEventHandler();
 *     return <Routes>...</Routes>;
 *   }
 */
import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { onAuthEvent } from './authEvents';
import { useSessionTeardown } from './useSessionTeardown';

export function useAuthEventHandler(): void {
  const teardown = useSessionTeardown();
  const navigate = useNavigate();

  useEffect(() => {
    const unsubscribe = onAuthEvent((event) => {
      if (event === 'session_expired') {
        // teardown is async but we intentionally do not await here.
        // The function handles its own sequencing internally.
        void teardown('session_expired');
      } else if (event === 'broker_scope_unresolvable') {
        // JWT is valid — do not tear down the session. Navigate directly to
        // the unauthorized page with reason so the user sees a broker-specific
        // message. The failed request is abandoned (never-resolving promise in
        // api.ts) and must not be retried.
        navigate('/unauthorized?reason=broker_inactive', { replace: true });
      }
    });

    return unsubscribe;
  }, [teardown, navigate]);
}
