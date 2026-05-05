import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { renderWithProviders } from '@/test-utils/render-app'
import { ParentDocumentsPanel } from '../components/ParentDocumentsPanel'

const { mockGetUser } = vi.hoisted(() => ({
  mockGetUser: vi.fn(),
}))

mockGetUser.mockResolvedValue({
  expired: false,
  access_token: 'test-token',
  profile: {
    sub: 'user-documents',
    email: 'documents@nebula.local',
    name: 'Documents User',
    nebula_roles: ['Admin'],
  },
})

vi.mock('@/features/auth/oidcUserManager', () => ({
  oidcUserManager: {
    getUser: mockGetUser,
    events: {
      addUserLoaded: vi.fn(),
      addUserUnloaded: vi.fn(),
      removeUserLoaded: vi.fn(),
      removeUserUnloaded: vi.fn(),
    },
  },
}))

describe('ParentDocumentsPanel', () => {
  it('renders parent-scoped documents and completeness counts', async () => {
    renderWithProviders(
      <ParentDocumentsPanel
        parent={{ type: 'submission', id: 'submission-1' }}
        variant="plain"
      />,
    )

    expect(await screen.findByText('ACORD 125 intake')).toBeInTheDocument()
    expect(screen.getByText(/Available:/)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /ACORD 125 intake/i })).toHaveAttribute('href', '/documents/doc_mock_acord')
  })
})
