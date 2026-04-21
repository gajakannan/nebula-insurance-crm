import { axe } from 'jest-axe'
import { describe, expect, it } from 'vitest'
import { LoginPage } from '@/pages/LoginPage'
import { renderRouteWithProviders } from '@/test-utils/render-app'

describe('LoginPage accessibility', () => {
  it('has no detectable accessibility violations in oidc mode', async () => {
    const { container } = renderRouteWithProviders(<LoginPage />, {
      route: '/login',
      path: '/login',
    })

    await expect(axe(container)).resolves.toHaveNoViolations()
  })
})
