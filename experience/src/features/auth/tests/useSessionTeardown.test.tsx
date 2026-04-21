/**
 * Tests for useSessionTeardown (F0009 — F-002 Resolution)
 *
 * Covers the three required scenarios from the task spec:
 *   1. 401 response triggers teardown and redirects to /login?reason=session_expired
 *   2. Explicit logout triggers teardown and redirects to /login
 *   3. Teardown completes even if POST /auth/logout network call fails
 *
 * Also tests the authEvent bus integration:
 *   4. emitAuthEvent('session_expired') -> useAuthEventHandler -> teardown
 *
 * Test strategy:
 *   - renderHook wraps hook under test inside a MemoryRouter (required for useNavigate)
 *   - react-router-dom is partially mocked to capture navigate calls
 *   - oidcUserManager is mocked at module level — no real OIDC infrastructure
 *   - fetch is mocked per test via vi.stubGlobal / vi.spyOn(window, 'fetch')
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import type { ReactNode } from 'react';
import { useSessionTeardown } from '../useSessionTeardown';
import { useAuthEventHandler } from '../useAuthEventHandler';
import { emitAuthEvent } from '../authEvents';

// ---------------------------------------------------------------------------
// Mock: oidc-client-ts UserManager (via oidcUserManager singleton)
// ---------------------------------------------------------------------------

vi.mock('../oidcUserManager', () => ({
  oidcUserManager: {
    removeUser: vi.fn().mockResolvedValue(undefined),
    clearStaleState: vi.fn().mockResolvedValue(undefined),
  },
}));

import { oidcUserManager } from '../oidcUserManager';

// ---------------------------------------------------------------------------
// Mock: react-router-dom navigate
// We replace useNavigate with a factory that returns our spy so we can assert
// on calls without needing to inspect MemoryRouter history directly.
// ---------------------------------------------------------------------------

const navigateSpy = vi.fn();

vi.mock('react-router-dom', async (importOriginal) => {
  const original = await importOriginal<typeof import('react-router-dom')>();
  return {
    ...original,
    useNavigate: () => navigateSpy,
  };
});

// ---------------------------------------------------------------------------
// Wrapper — still need MemoryRouter so BrowserRouter context is available for
// any sub-components, even though useNavigate is mocked.
// ---------------------------------------------------------------------------

function wrapper({ children }: { children: ReactNode }) {
  return <MemoryRouter initialEntries={['/dashboard']}>{children}</MemoryRouter>;
}

// ---------------------------------------------------------------------------
// Setup / teardown
// ---------------------------------------------------------------------------

beforeEach(() => {
  vi.clearAllMocks();
  navigateSpy.mockReset();
  vi.mocked(oidcUserManager.removeUser).mockResolvedValue(undefined);
  vi.mocked(oidcUserManager.clearStaleState).mockResolvedValue(undefined);
});

afterEach(() => {
  vi.unstubAllGlobals();
});

// ---------------------------------------------------------------------------
// Suite 1: useSessionTeardown — session_expired (401 path)
// ---------------------------------------------------------------------------

describe('useSessionTeardown — reason: session_expired', () => {
  it('redirects to /login?reason=session_expired', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, status: 204 }));

    const { result } = renderHook(() => useSessionTeardown(), { wrapper });

    await act(async () => {
      await result.current('session_expired');
    });

    expect(navigateSpy).toHaveBeenCalledWith(
      '/login?reason=session_expired',
      { replace: true },
    );
  });

  it('clears OIDC state (removeUser + clearStaleState) before redirecting', async () => {
    const callOrder: string[] = [];

    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, status: 204 }));

    vi.mocked(oidcUserManager.removeUser).mockImplementation(async () => {
      callOrder.push('removeUser');
    });
    vi.mocked(oidcUserManager.clearStaleState).mockImplementation(async () => {
      callOrder.push('clearStaleState');
    });
    navigateSpy.mockImplementation(() => {
      callOrder.push('navigate');
    });

    const { result } = renderHook(() => useSessionTeardown(), { wrapper });

    await act(async () => {
      await result.current('session_expired');
    });

    // Order: removeUser -> clearStaleState -> navigate (redirect is last)
    expect(callOrder).toEqual(['removeUser', 'clearStaleState', 'navigate']);
  });

  it('completes teardown even if POST /auth/logout throws a network error', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockRejectedValue(new TypeError('Network request failed')),
    );

    const { result } = renderHook(() => useSessionTeardown(), { wrapper });

    await act(async () => {
      await result.current('session_expired');
    });

    // OIDC state cleared despite fetch failure
    expect(oidcUserManager.removeUser).toHaveBeenCalledOnce();
    expect(oidcUserManager.clearStaleState).toHaveBeenCalledOnce();

    // Redirect still happens
    expect(navigateSpy).toHaveBeenCalledWith(
      '/login?reason=session_expired',
      { replace: true },
    );
  });

  it('completes teardown even if POST /auth/logout returns a non-2xx status', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({ ok: false, status: 500 }),
    );

    const { result } = renderHook(() => useSessionTeardown(), { wrapper });

    await act(async () => {
      await result.current('session_expired');
    });

    expect(oidcUserManager.removeUser).toHaveBeenCalledOnce();
    expect(oidcUserManager.clearStaleState).toHaveBeenCalledOnce();
    expect(navigateSpy).toHaveBeenCalledWith(
      '/login?reason=session_expired',
      { replace: true },
    );
  });
});

// ---------------------------------------------------------------------------
// Suite 2: useSessionTeardown — logout (explicit user action)
// ---------------------------------------------------------------------------

describe('useSessionTeardown — reason: logout', () => {
  it('redirects to /login with no query string', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, status: 204 }));

    const { result } = renderHook(() => useSessionTeardown(), { wrapper });

    await act(async () => {
      await result.current('logout');
    });

    expect(navigateSpy).toHaveBeenCalledWith('/login', { replace: true });
    // Ensure no session_expired param is appended
    const [to] = navigateSpy.mock.calls[0] as [string, unknown];
    expect(to).not.toContain('reason=');
  });

  it('calls oidcUserManager.removeUser() and clearStaleState()', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, status: 204 }));

    const { result } = renderHook(() => useSessionTeardown(), { wrapper });

    await act(async () => {
      await result.current('logout');
    });

    expect(oidcUserManager.removeUser).toHaveBeenCalledOnce();
    expect(oidcUserManager.clearStaleState).toHaveBeenCalledOnce();
  });

  it('calls POST /auth/logout with correct method and credentials', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, status: 204 });
    vi.stubGlobal('fetch', fetchMock);

    const { result } = renderHook(() => useSessionTeardown(), { wrapper });

    await act(async () => {
      await result.current('logout');
    });

    expect(fetchMock).toHaveBeenCalledWith('/auth/logout', {
      method: 'POST',
      credentials: 'include',
    });
  });

  it('still clears OIDC state and redirects even if logout endpoint call fails', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockRejectedValue(new TypeError('Network request failed')),
    );

    const { result } = renderHook(() => useSessionTeardown(), { wrapper });

    await act(async () => {
      await result.current('logout');
    });

    expect(oidcUserManager.removeUser).toHaveBeenCalledOnce();
    expect(oidcUserManager.clearStaleState).toHaveBeenCalledOnce();
    expect(navigateSpy).toHaveBeenCalledWith('/login', { replace: true });
  });

  it('clears OIDC state before redirecting', async () => {
    const callOrder: string[] = [];

    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, status: 204 }));

    vi.mocked(oidcUserManager.removeUser).mockImplementation(async () => {
      callOrder.push('removeUser');
    });
    vi.mocked(oidcUserManager.clearStaleState).mockImplementation(async () => {
      callOrder.push('clearStaleState');
    });
    navigateSpy.mockImplementation(() => {
      callOrder.push('navigate');
    });

    const { result } = renderHook(() => useSessionTeardown(), { wrapper });

    await act(async () => {
      await result.current('logout');
    });

    expect(callOrder).toEqual(['removeUser', 'clearStaleState', 'navigate']);
  });
});

// ---------------------------------------------------------------------------
// Suite 3: authEvent bus integration — 401 interceptor -> teardown
// ---------------------------------------------------------------------------

describe('useAuthEventHandler — 401 interceptor wiring', () => {
  it('triggers teardown with session_expired when auth event is emitted', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, status: 204 }));

    const { unmount } = renderHook(() => useAuthEventHandler(), { wrapper });

    await act(async () => {
      emitAuthEvent('session_expired');
      // Give async teardown a tick to start
      await new Promise<void>((resolve) => setTimeout(resolve, 0));
    });

    await waitFor(() => {
      expect(oidcUserManager.removeUser).toHaveBeenCalledOnce();
      expect(oidcUserManager.clearStaleState).toHaveBeenCalledOnce();
    });

    await waitFor(() => {
      expect(navigateSpy).toHaveBeenCalledWith(
        '/login?reason=session_expired',
        { replace: true },
      );
    });

    unmount();
  });

  it('unsubscribes from auth events on unmount — no teardown after unmount', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, status: 204 }));

    const { unmount } = renderHook(() => useAuthEventHandler(), { wrapper });

    // Unmount before emitting the event
    unmount();

    vi.mocked(oidcUserManager.removeUser).mockClear();
    navigateSpy.mockClear();

    await act(async () => {
      emitAuthEvent('session_expired');
      await new Promise<void>((resolve) => setTimeout(resolve, 20));
    });

    // Listener was cleaned up — no teardown should have run
    expect(oidcUserManager.removeUser).not.toHaveBeenCalled();
    expect(navigateSpy).not.toHaveBeenCalled();
  });
});
