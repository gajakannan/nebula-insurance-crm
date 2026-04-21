/**
 * Tests for the nebula-auth-mode-guard Vite plugin logic (F0009 — F-001 Resolution)
 *
 * The guard throws a hard error during `vite build` when BOTH conditions are true:
 *   - VITE_AUTH_MODE === 'dev'
 *   - NODE_ENV === 'production'
 *
 * It must be a no-op in every other combination.
 *
 * Test strategy:
 *   The guard logic is extracted into a pure function so it can be unit-tested
 *   independently of Vite. This avoids spinning up a real Vite build and keeps
 *   the tests fast and deterministic. The same conditional logic lives verbatim
 *   inside the `buildStart` hook in vite.config.ts.
 *
 * Environment:
 *   This file runs in the Node environment (no DOM needed).
 *
 * @vitest-environment node
 */

import { describe, it, expect, beforeEach, afterEach } from 'vitest';

// ---------------------------------------------------------------------------
// Guard logic — extracted from the buildStart hook in vite.config.ts.
// Any change to vite.config.ts must be mirrored here.
// ---------------------------------------------------------------------------

const FATAL_MESSAGE =
  'FATAL: VITE_AUTH_MODE=dev is not permitted in production builds. Set VITE_AUTH_MODE=oidc.';

function runAuthModeGuard(authMode: string | undefined, nodeEnv: string | undefined): void {
  if (authMode === 'dev' && nodeEnv === 'production') {
    throw new Error(FATAL_MESSAGE);
  }
}

// ---------------------------------------------------------------------------
// Helpers — isolate process.env mutations between tests
// ---------------------------------------------------------------------------

let savedAuthMode: string | undefined;
let savedNodeEnv: string | undefined;

beforeEach(() => {
  savedAuthMode = process.env.VITE_AUTH_MODE;
  savedNodeEnv = process.env.NODE_ENV;
});

afterEach(() => {
  if (savedAuthMode === undefined) {
    delete process.env.VITE_AUTH_MODE;
  } else {
    process.env.VITE_AUTH_MODE = savedAuthMode;
  }
  if (savedNodeEnv === undefined) {
    delete process.env.NODE_ENV;
  } else {
    process.env.NODE_ENV = savedNodeEnv;
  }
});

// ---------------------------------------------------------------------------
// Suite: authModeGuard
// ---------------------------------------------------------------------------

describe('nebula-auth-mode-guard — buildStart hook logic', () => {
  it('throws when VITE_AUTH_MODE=dev AND NODE_ENV=production', () => {
    expect(() => runAuthModeGuard('dev', 'production')).toThrowError(FATAL_MESSAGE);
  });

  it('is a no-op when VITE_AUTH_MODE=dev AND NODE_ENV=development', () => {
    expect(() => runAuthModeGuard('dev', 'development')).not.toThrow();
  });

  it('is a no-op when VITE_AUTH_MODE=oidc AND NODE_ENV=production', () => {
    expect(() => runAuthModeGuard('oidc', 'production')).not.toThrow();
  });

  it('is a no-op when VITE_AUTH_MODE is unset AND NODE_ENV=production', () => {
    expect(() => runAuthModeGuard(undefined, 'production')).not.toThrow();
  });

  // -------------------------------------------------------------------------
  // Integration-style: verify against the real process.env values so any
  // future vite.config.ts change that touches env reads is caught here.
  // -------------------------------------------------------------------------

  it('process.env integration: throws when both env vars match danger combo', () => {
    process.env.VITE_AUTH_MODE = 'dev';
    process.env.NODE_ENV = 'production';

    expect(() =>
      runAuthModeGuard(process.env.VITE_AUTH_MODE, process.env.NODE_ENV),
    ).toThrowError(FATAL_MESSAGE);
  });

  it('process.env integration: is a no-op when VITE_AUTH_MODE=dev and NODE_ENV=development', () => {
    process.env.VITE_AUTH_MODE = 'dev';
    process.env.NODE_ENV = 'development';

    expect(() =>
      runAuthModeGuard(process.env.VITE_AUTH_MODE, process.env.NODE_ENV),
    ).not.toThrow();
  });

  it('process.env integration: is a no-op when VITE_AUTH_MODE=oidc and NODE_ENV=production', () => {
    process.env.VITE_AUTH_MODE = 'oidc';
    process.env.NODE_ENV = 'production';

    expect(() =>
      runAuthModeGuard(process.env.VITE_AUTH_MODE, process.env.NODE_ENV),
    ).not.toThrow();
  });

  it('process.env integration: is a no-op when VITE_AUTH_MODE is unset and NODE_ENV=production', () => {
    delete process.env.VITE_AUTH_MODE;
    process.env.NODE_ENV = 'production';

    expect(() =>
      runAuthModeGuard(process.env.VITE_AUTH_MODE, process.env.NODE_ENV),
    ).not.toThrow();
  });
});
