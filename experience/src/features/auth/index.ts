/**
 * Public surface for the auth feature slice.
 *
 * F0009 — Authentication and Role-Based Login
 *
 * Exports available to other features and the app shell:
 *   - useSessionTeardown: initiate client-side session teardown (logout / 401)
 *   - useAuthEventHandler: must be mounted once in AppInner (inside BrowserRouter)
 *   - oidcUserManager: singleton UserManager for OIDC operations
 *   - emitAuthEvent / onAuthEvent: low-level auth event bus (prefer hooks above)
 */
export { useSessionTeardown } from './useSessionTeardown';
export type { TeardownReason } from './useSessionTeardown';
export { useAuthEventHandler } from './useAuthEventHandler';
export { oidcUserManager } from './oidcUserManager';
export { emitAuthEvent, onAuthEvent } from './authEvents';
export type { AuthEvent } from './authEvents';
export { useCurrentUser } from './useCurrentUser';
export type { CurrentUser } from './useCurrentUser';
export { ProtectedRoute } from './ProtectedRoute';
