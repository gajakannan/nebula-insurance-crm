import type React from 'react'
import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import SubmissionDetailPage from '@/pages/SubmissionDetailPage'
import { renderRouteWithProviders } from '@/test-utils/render-app'

const authMocks = vi.hoisted(() => ({
  getUser: vi.fn(),
}))

vi.mock('@/features/auth/oidcUserManager', () => ({
  oidcUserManager: {
    getUser: authMocks.getUser,
    events: {
      addUserLoaded: vi.fn(),
      addUserUnloaded: vi.fn(),
      removeUserLoaded: vi.fn(),
      removeUserUnloaded: vi.fn(),
    },
  },
}))

vi.mock('@/components/layout/DashboardLayout', () => ({
  DashboardLayout: ({
    title,
    children,
  }: {
    title?: string
    children: React.ReactNode
  }) => (
    <div>
      {title && <h1>{title}</h1>}
      {children}
    </div>
  ),
}))

describe('SubmissionDetailPage integration', () => {
  beforeEach(() => {
    mockAuthenticatedUser({
      sub: 'user-dist-manager',
      name: 'Sarah Chen',
      email: 'sarah.chen@nebula.local',
      nebula_roles: ['DistributionManager'],
    })
  })

  it('supports reassignment against the shared submission state', async () => {
    const user = userEvent.setup()

    renderRouteWithProviders(<SubmissionDetailPage />, {
      route: '/submissions/submission-1',
      path: '/submissions/:submissionId',
    })

    expect(await screen.findByRole('heading', { name: 'Blue Horizon Manufacturing' })).toBeInTheDocument()
    expect(await screen.findByText('Ready for handoff')).toBeInTheDocument()
    expect(await screen.findByText('Submission details were refreshed.')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Reassign' }))
    expect(await screen.findByRole('dialog', { name: 'Assign submission' })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Remove assignee Nadia Brooks' }))
    await user.type(screen.getByRole('combobox', { name: 'Assignee' }), 'alex')
    await user.click(await screen.findByText('Alex Kim'))
    await user.click(screen.getByRole('button', { name: 'Save assignment' }))

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: 'Assign submission' })).not.toBeInTheDocument()
    })
    expect(await screen.findByText('Alex Kim')).toBeInTheDocument()
  })

  it('allows clearing optional intake fields', async () => {
    const user = userEvent.setup()

    renderRouteWithProviders(<SubmissionDetailPage />, {
      route: '/submissions/submission-1',
      path: '/submissions/:submissionId',
    })

    expect(await screen.findByRole('heading', { name: 'Blue Horizon Manufacturing' })).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Edit Intake Details' }))

    const editDialog = await screen.findByRole('dialog', { name: 'Edit intake details' })
    await user.selectOptions(within(editDialog).getByLabelText('Program'), '')
    await user.selectOptions(within(editDialog).getByLabelText('Line of Business'), '')
    await user.clear(within(editDialog).getByLabelText('Expiration Date'))
    await user.clear(within(editDialog).getByLabelText('Premium Estimate'))
    await user.clear(within(editDialog).getByLabelText('Description'))
    await user.click(within(editDialog).getByRole('button', { name: 'Save changes' }))

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: 'Edit intake details' })).not.toBeInTheDocument()
    })

    expect(await screen.findByText('No linked program')).toBeInTheDocument()
    expect(screen.getByText('Unclassified')).toBeInTheDocument()
    expect(screen.getByText('Default from effective date')).toBeInTheDocument()
    expect(screen.getByText('Not set')).toBeInTheDocument()
    expect(screen.getByText('No intake notes recorded yet.')).toBeInTheDocument()
  })

  it('submits a workflow transition from the action bar', async () => {
    const user = userEvent.setup()

    renderRouteWithProviders(<SubmissionDetailPage />, {
      route: '/submissions/submission-1',
      path: '/submissions/:submissionId',
    })

    expect(await screen.findByRole('heading', { name: 'Blue Horizon Manufacturing' })).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Move to Ready for UW Review' }))
    const transitionDialog = await screen.findByRole('dialog', { name: 'Move to Ready for UW Review' })
    await user.type(
      within(transitionDialog).getByLabelText('Note or reason'),
      'Intake package is complete and ready for handoff.',
    )
    await user.click(within(transitionDialog).getByRole('button', { name: 'Confirm transition' }))

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: 'Move to Ready for UW Review' })).not.toBeInTheDocument()
    }, { timeout: 5000 })
    expect(screen.queryByText('Submission is not ready for that transition.')).not.toBeInTheDocument()
    expect(screen.queryByText(/Transition failed/)).not.toBeInTheDocument()
  }, 15000)

  it('hides edit and reassignment actions for read-only intake roles', async () => {
    mockAuthenticatedUser({
      sub: 'user-underwriter-1',
      name: 'Nadia Brooks',
      email: 'nadia.brooks@nebula.local',
      nebula_roles: ['Underwriter'],
    })

    renderRouteWithProviders(<SubmissionDetailPage />, {
      route: '/submissions/submission-1',
      path: '/submissions/:submissionId',
    })

    expect(await screen.findByRole('heading', { name: 'Blue Horizon Manufacturing' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Edit Intake Details' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Reassign' })).not.toBeInTheDocument()
    expect(screen.getByText('No workflow actions are currently available.')).toBeInTheDocument()
  })

  it('disables Ready for UW Review with guidance when completeness preflight fails', async () => {
    const user = userEvent.setup()

    renderRouteWithProviders(<SubmissionDetailPage />, {
      route: '/submissions/submission-2',
      path: '/submissions/:submissionId',
    })

    expect(await screen.findByRole('heading', { name: 'Compass Markets Retail Group' })).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Move to Triaging' }))
    await user.click(await screen.findByRole('button', { name: 'Confirm transition' }))

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: 'Move to Triaging' })).not.toBeInTheDocument()
    })

    const blockedTransition = await screen.findByRole('button', { name: 'Move to Ready for UW Review' })
    expect(blockedTransition).toBeDisabled()
    expect(screen.getByText('Move to Waiting on Broker')).toBeInTheDocument()
    const guidanceText = screen.getByText(/Ready for UW Review is blocked:/)
    expect(guidanceText).toBeInTheDocument()
    expect(guidanceText.textContent).toContain('Line of business')
    expect(guidanceText.textContent).toContain('Assigned underwriter')
  })
})

function mockAuthenticatedUser(profile: Record<string, unknown>) {
  authMocks.getUser.mockResolvedValue({
    expired: false,
    access_token: 'test-token',
    profile,
  })
}
