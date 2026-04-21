/**
 * useCurrentUser
 *
 * Returns identity information derived from the active OIDC user object.
 * In VITE_AUTH_MODE=dev, derives identity from the dev token payload instead.
 *
 * Claims mapping (§5 contract):
 *   - name / preferred_username -> displayName
 *   - nebula_roles             -> roles (string[])
 *   - broker_tenant_id         -> brokerTenantId (BrokerUser only)
 *   - email                    -> email
 *   - sub                      -> sub
 */
import { useEffect, useState } from 'react';
import { oidcUserManager } from './oidcUserManager';

export interface CurrentUser {
  sub: string;
  email: string;
  displayName: string;
  roles: string[];
  brokerTenantId: string | null;
}

const AUTH_MODE = import.meta.env.VITE_AUTH_MODE as string | undefined;

function userFromOidcProfile(profile: Record<string, unknown>): CurrentUser {
  const roles: string[] = Array.isArray(profile['nebula_roles'])
    ? (profile['nebula_roles'] as string[])
    : typeof profile['nebula_roles'] === 'string'
      ? [profile['nebula_roles'] as string]
      : [];

  return {
    sub: (profile['sub'] as string) ?? '',
    email: (profile['email'] as string) ?? '',
    displayName: (profile['name'] as string) ?? (profile['preferred_username'] as string) ?? '',
    roles,
    brokerTenantId: (profile['broker_tenant_id'] as string) ?? null,
  };
}

/**
 * Hook: returns the current authenticated user or null if unauthenticated.
 * Re-renders when the OIDC user is loaded or changes.
 */
export function useCurrentUser(): CurrentUser | null {
  const [user, setUser] = useState<CurrentUser | null>(null);

  useEffect(() => {
    if (AUTH_MODE === 'dev') {
      // In dev mode there is no real OIDC session — return a synthetic user
      // matching the seeded DistributionManager identity for UI rendering.
      setUser({
        sub: 'dev-user-001',
        email: 'sarah.chen@nebula.local',
        displayName: 'Sarah Chen',
        roles: ['DistributionManager'],
        brokerTenantId: null,
      });
      return;
    }

    // OIDC mode: load user from oidc-client-ts sessionStorage.
    oidcUserManager.getUser().then((oidcUser) => {
      if (oidcUser && !oidcUser.expired) {
        setUser(userFromOidcProfile(oidcUser.profile as Record<string, unknown>));
      } else {
        setUser(null);
      }
    });

    const onUserLoaded = (oidcUser: { profile: Record<string, unknown> }) => {
      setUser(userFromOidcProfile(oidcUser.profile));
    };
    const onUserUnloaded = () => setUser(null);

    oidcUserManager.events.addUserLoaded(onUserLoaded as Parameters<typeof oidcUserManager.events.addUserLoaded>[0]);
    oidcUserManager.events.addUserUnloaded(onUserUnloaded);

    return () => {
      oidcUserManager.events.removeUserLoaded(onUserLoaded as Parameters<typeof oidcUserManager.events.removeUserLoaded>[0]);
      oidcUserManager.events.removeUserUnloaded(onUserUnloaded);
    };
  }, []);

  return user;
}
