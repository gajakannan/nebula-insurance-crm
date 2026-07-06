export type AuthMode = 'dev' | 'oidc';

interface AuthModeEnv {
  DEV: boolean;
  VITE_AUTH_MODE?: string;
}

export function resolveAuthMode(
  env: AuthModeEnv = import.meta.env,
): AuthMode {
  const configured = env.VITE_AUTH_MODE?.trim().toLowerCase();

  if (configured === 'dev' || configured === 'oidc') {
    return configured;
  }

  return env.DEV ? 'dev' : 'oidc';
}

export const AUTH_MODE = resolveAuthMode();
export const IS_DEV_AUTH_MODE = AUTH_MODE === 'dev';
