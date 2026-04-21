/**
 * authEvents — lightweight event bus for auth-driven side-effects.
 *
 * The API fetch layer (api.ts) is a plain module with no access to React
 * Router's `navigate`. Rather than coupling api.ts to React, we publish
 * auth events here. A React component/hook (useAuthEventHandler) subscribes
 * to these events and executes the actual teardown — which requires
 * `useNavigate` and therefore must run inside the React tree.
 *
 * This keeps api.ts free of framework dependencies and keeps the teardown
 * logic inside React where it belongs.
 *
 * Subscribers receive the event synchronously via a simple Set of callbacks.
 * Multiple subscribers are supported but in practice only one
 * (useAuthEventHandler) will be active at a time.
 */

export type AuthEvent = 'session_expired' | 'broker_scope_unresolvable';

type AuthEventListener = (event: AuthEvent) => void;

const listeners = new Set<AuthEventListener>();

/**
 * Subscribe to auth events. Returns an unsubscribe function.
 * Call inside a useEffect cleanup.
 */
export function onAuthEvent(listener: AuthEventListener): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

/**
 * Publish an auth event to all registered listeners.
 * Called by the API 401 interceptor.
 */
export function emitAuthEvent(event: AuthEvent): void {
  for (const listener of listeners) {
    listener(event);
  }
}
