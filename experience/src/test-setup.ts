/**
 * Global test setup for Vitest + React Testing Library.
 * Imported via vite.config.ts test.setupFiles.
 */
import * as matchers from '@testing-library/jest-dom/matchers'
import { cleanup } from '@testing-library/react'
import { toHaveNoViolations } from 'jest-axe'
import { afterAll, afterEach, beforeAll, expect } from 'vitest'
import { server } from './mocks/server'
import { resetRenewalMockState } from './mocks/renewals'
import { resetSubmissionMockState } from './mocks/submissions'

expect.extend(matchers)
expect.extend(toHaveNoViolations)

const defaultOidcTestEnv = {
  VITE_AUTH_MODE: 'oidc',
  VITE_OIDC_AUTHORITY: 'https://auth.nebula.test/application/o/nebula/',
  VITE_OIDC_CLIENT_ID: 'nebula-web',
  VITE_OIDC_REDIRECT_URI: 'http://localhost/auth/callback',
} satisfies Record<string, string>

const viteEnv = import.meta.env as Record<string, string | boolean | undefined>

for (const [key, value] of Object.entries(defaultOidcTestEnv)) {
  if (!process.env[key]) {
    process.env[key] = value
  }
  if (!viteEnv[key]) {
    viteEnv[key] = process.env[key] ?? value
  }
}

if (typeof window !== 'undefined' && !('ResizeObserver' in window)) {
  class ResizeObserver {
    observe() {}
    unobserve() {}
    disconnect() {}
  }

  Object.defineProperty(window, 'ResizeObserver', {
    configurable: true,
    writable: true,
    value: ResizeObserver,
  })
}

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' })
})

afterEach(() => {
  cleanup()
  server.resetHandlers()
  resetRenewalMockState()
  resetSubmissionMockState()
  if (typeof window !== 'undefined') {
    window.localStorage.clear()
    window.sessionStorage.clear()
    window.history.replaceState({}, '', '/')
  }
  if (typeof document !== 'undefined') {
    document.documentElement.removeAttribute('data-theme')
  }
})

afterAll(() => {
  server.close()
})
