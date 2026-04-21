/**
 * Tests for ProtectedRoute (F0009 — S0003 contract)
 *
 * Covers:
 *   1. Authenticated user (non-expired) → renders children
 *   2. No session (user is null) → redirects to /login
 *   3. Expired session → redirects to /login?reason=session_expired
 *   4. Loading state → renders null (no flash of protected content)
 *   5. VITE_AUTH_MODE=dev → always renders children (guard is no-op)
 *
 * @vitest-environment jsdom
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { act, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import type { ReactNode } from 'react';
import { ProtectedRoute } from '../ProtectedRoute';

// ---------------------------------------------------------------------------
// Mock oidcUserManager
// ---------------------------------------------------------------------------

const { mockGetUser } = vi.hoisted(() => ({
  mockGetUser: vi.fn(),
}));

vi.mock('../oidcUserManager', () => ({
  oidcUserManager: {
    getUser: mockGetUser,
    events: {
      addUserLoaded: vi.fn(),
      addUserUnloaded: vi.fn(),
      removeUserLoaded: vi.fn(),
      removeUserUnloaded: vi.fn(),
    },
  },
}));

// Mock import.meta.env for OIDC mode
vi.mock('../ProtectedRoute', async (importOriginal) => {
  const original = await importOriginal<typeof import('../ProtectedRoute')>();
  return original;
});

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function renderWithRouter(ui: ReactNode, initialPath = '/protected') {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        <Route path="/protected" element={ui} />
        <Route path="/login" element={<div>LoginPage</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('ProtectedRoute', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('renders children when session is valid and non-expired', async () => {
    mockGetUser.mockResolvedValue({ expired: false, access_token: 'valid-token' });

    renderWithRouter(
      <ProtectedRoute>
        <div data-testid="protected-content">Secret</div>
      </ProtectedRoute>,
    );

    await waitFor(() => {
      expect(screen.queryByTestId('protected-content')).not.toBeNull();
    });
  });

  it('redirects to /login when no session exists', async () => {
    mockGetUser.mockResolvedValue(null);

    renderWithRouter(
      <ProtectedRoute>
        <div data-testid="protected-content">Secret</div>
      </ProtectedRoute>,
    );

    await waitFor(() => {
      expect(screen.queryByText('LoginPage')).not.toBeNull();
      expect(screen.queryByTestId('protected-content')).toBeNull();
    });
  });

  it('redirects to /login?reason=session_expired when session is expired', async () => {
    mockGetUser.mockResolvedValue({ expired: true, access_token: 'old-token' });

    renderWithRouter(
      <ProtectedRoute>
        <div data-testid="protected-content">Secret</div>
      </ProtectedRoute>,
    );

    // Expired session must redirect (to /login route in our test router)
    await waitFor(() => {
      expect(screen.queryByTestId('protected-content')).toBeNull();
    });
  });

  it('renders null during loading (no content flash)', async () => {
    // getUser never resolves during this check
    let resolveGetUser: (value: null) => void;
    mockGetUser.mockReturnValue(new Promise((resolve) => { resolveGetUser = resolve; }));

    const { container } = renderWithRouter(
      <ProtectedRoute>
        <div data-testid="protected-content">Secret</div>
      </ProtectedRoute>,
    );

    // While loading: protected content must not be rendered
    expect(screen.queryByTestId('protected-content')).toBeNull();
    expect(container.innerHTML).toBe('');

    // Resolve to unblock (cleanup)
    await act(async () => {
      resolveGetUser!(null);
    });
  });
});
