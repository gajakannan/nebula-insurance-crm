import { useEffect, useRef } from 'react'
import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Route } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import CreateSubmissionPage from '@/pages/CreateSubmissionPage'
import SubmissionDetailPage from '@/pages/SubmissionDetailPage'
import { renderRouteWithProviders } from '@/test-utils/render-app'

const authMocks = vi.hoisted(() => ({
  getUser: vi.fn(),
}))

const VALID_CYBER_VALUES = {
  revenueBand: '0-10M', recordsHeld: '1000', mfaEnabled: false,
  mfaMaturity: '', edrEnabled: true, backupEnabled: true,
  trainingFrequency: 'Annual', requestedLimit: '1000000', requestedRetention: '10000',
}

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

// This page test isolates submission creation from the schema-bundle transport;
// DynamicAttributePanel has its own schema/engine contract tests. Keep the
// Cyber values explicit so the integration fixture exercises the current
// required-attribute contract instead of submitting an empty legacy fixture.
vi.mock('@/features/lob-attributes', async () => {
  const actual = await vi.importActual<typeof import('@/features/lob-attributes')>('@/features/lob-attributes')
  return {
    ...actual,
    DynamicAttributePanel: ({ lineOfBusiness, onChange }: {
      lineOfBusiness: string | null | undefined
      onChange?: (value: {
        revenueBand: string
        recordsHeld: string
        mfaEnabled: boolean
        mfaMaturity: string
        edrEnabled: boolean
        backupEnabled: boolean
        trainingFrequency: string
        requestedLimit: string
        requestedRetention: string
      }) => void
    }) => {
      const onChangeRef = useRef(onChange)
      useEffect(() => { if (lineOfBusiness === 'Cyber') onChangeRef.current?.(VALID_CYBER_VALUES) }, [lineOfBusiness])
      if (lineOfBusiness !== 'Cyber') return null
      return <div><h3>Cyber attributes</h3><span>Revenue band</span><span>Records held</span><span>Training frequency</span><span>Requested limit</span><span>Requested retention</span></div>
    },
  }
})

describe('CreateSubmissionPage integration', () => {
  it('validates required fields and creates a submission that navigates to detail', async () => {
    authMocks.getUser.mockResolvedValue({
      expired: false,
      access_token: 'test-token',
      profile: {
        sub: 'user-dist-manager',
        name: 'Sarah Chen',
        email: 'sarah.chen@nebula.local',
        nebula_roles: ['DistributionManager'],
      },
    })

    const user = userEvent.setup()

    renderRouteWithProviders(<CreateSubmissionPage />, {
      route: '/submissions/new',
      path: '/submissions/new',
      additionalRoutes: [
        <Route
          key="submission-detail"
          path="/submissions/:submissionId"
          element={<SubmissionDetailPage />}
        />,
      ],
    })

    await user.click(await screen.findByRole('button', { name: 'Create Submission' }))

    expect(await screen.findByText('Account is required.')).toBeInTheDocument()
    expect(screen.getByText('Broker is required.')).toBeInTheDocument()
    expect(screen.getByText('Effective date is required.')).toBeInTheDocument()

    await user.selectOptions(screen.getByLabelText(/^Account/), 'account-1')
    await user.selectOptions(screen.getByLabelText(/^Broker/), 'broker-1')
    await user.type(screen.getByLabelText(/^Effective Date/), '2026-04-15')
    await user.selectOptions(screen.getByLabelText(/^Line of Business/), 'Cyber')
    await screen.findByText('Cyber attributes')
    await user.type(screen.getByLabelText('Premium Estimate'), '250000')
    await user.type(
      screen.getByLabelText('Description'),
      'Broker provided a complete cyber intake packet for a new regional expansion.',
    )

    await user.click(screen.getByRole('button', { name: 'Create Submission' }))

    expect(await screen.findByRole('heading', { name: 'Blue Horizon Manufacturing' })).toBeInTheDocument()
    expect(await screen.findByText('Received')).toBeInTheDocument()
    expect(await screen.findByRole('button', { name: 'Move to Triaging' })).toBeInTheDocument()
    expect(screen.getByText('Cyber Liability')).toBeInTheDocument()
  })
})
