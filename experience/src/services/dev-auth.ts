/**
 * Dev-only: crafts a local JWT with claims matching the seeded dev users.
 * No authentik required — the backend accepts these in dev mode (signature not validated).
 * Remove this when real OIDC login flow is implemented (F0005-S0003).
 *
 * IdpIssuer and IdpSubject values must match DevSeedData.cs:
 *   DevIdpIssuer = "http://localhost:9000/application/o/nebula/"
 *   IdpSubject   = "dev-user-001" (Sarah Chen — DistributionManager)
 */

const DEV_ISS = 'http://localhost:9000/application/o/nebula/';
const DEV_SUB = 'dev-user-001'; // Sarah Chen — DistributionManager

function base64url(obj: object): string {
  return btoa(JSON.stringify(obj))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');
}

let _devToken: string | null = null;

export function getDevToken(): Promise<string> {
  if (!_devToken) {
    const header = base64url({ alg: 'HS256', typ: 'JWT' });
    const payload = base64url({
      iss: DEV_ISS,
      sub: DEV_SUB,
      name: 'Sarah Chen',
      nebula_roles: ['DistributionManager'],
      regions: ['West', 'Central', 'East', 'South'],
      exp: Math.floor(Date.now() / 1000) + 86400 * 365,
    });
    _devToken = `${header}.${payload}.dev`;
  }
  return Promise.resolve(_devToken);
}
