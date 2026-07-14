import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import CommissionsPage from '@/pages/CommissionsPage'
import CommissionDetailPage from '@/pages/CommissionDetailPage'

const mutate = vi.fn()

vi.mock('@/components/layout/DashboardLayout', () => ({
  DashboardLayout: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}))

vi.mock('@/features/commissions', async (importActual) => {
  const actual = await importActual<typeof import('@/features/commissions')>()
  return {
    ...actual,
    useExpectedCommissions: () => ({
      data: {
        data: [{
          id: 'ec-1',
          policyId: '11111111-1111-1111-1111-111111111111',
          policyNumber: 'NEB-DEMO-001',
          accountDisplayName: 'Harbor Agency',
          policyVersionId: null,
          carrierMarketId: '22222222-2222-2222-2222-222222222222',
          carrierMarketName: 'Summit Mutual Programs',
          brokerName: 'Compass Markets',
          producerUserId: 'producer-1',
          producerDisplayName: 'Producer 1',
          lineOfBusiness: 'General Liability',
          commissionScheduleId: null,
          producerSplitAssignmentId: null,
          premiumBasisAmount: 12000,
          expectedGrossCommission: 1200,
          approvedAdjustmentTotal: 0,
          adjustedExpectedCommission: 1200,
          status: 'Calculated',
          exceptionState: 'None',
          calculatedAt: '2026-07-07T10:00:00Z',
          rowVersion: 1,
        }],
        page: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
      },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    }),
    useRevenueAttributionRollups: () => ({
      data: {
        query: {
          startDate: '2026-04-07',
          endDate: '2026-07-07',
          groupBy: 'producer',
          producerId: null,
          brokerId: null,
          territoryId: null,
          carrierMarketId: null,
          status: null,
          exceptionState: null,
        },
        rows: [{
          groupKey: 'producer-1',
          groupLabel: 'Producer 1',
          expectedGrossCommissionTotal: 1200,
          approvedAdjustmentTotal: 0,
          adjustedExpectedCommissionTotal: 1200,
          producerAllocationTotal: 1200,
          recordCount: 1,
          exceptionCount: 0,
        }],
      },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    }),
    useExpectedCommission: () => ({
      data: {
        commission: {
          id: 'ec-1',
          policyId: '11111111-1111-1111-1111-111111111111',
          policyNumber: 'NEB-DEMO-001',
          accountDisplayName: 'Harbor Agency',
          policyVersionId: null,
          carrierMarketId: '22222222-2222-2222-2222-222222222222',
          carrierMarketName: 'Summit Mutual Programs',
          brokerName: 'Compass Markets',
          producerUserId: 'producer-1',
          producerDisplayName: 'Producer 1',
          lineOfBusiness: 'General Liability',
          commissionScheduleId: null,
          producerSplitAssignmentId: null,
          premiumBasisAmount: 12000,
          expectedGrossCommission: 1200,
          approvedAdjustmentTotal: 0,
          adjustedExpectedCommission: 1200,
          status: 'Calculated',
          exceptionState: 'None',
          calculatedAt: '2026-07-07T10:00:00Z',
          rowVersion: 1,
        },
        schedules: [],
        splits: [],
        adjustments: [],
      },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    }),
    useCalculateExpectedCommission: () => ({ mutate, isPending: false, isError: false, error: null }),
    useCreateCommissionSchedule: () => ({ mutate: vi.fn(), isPending: false, isError: false }),
    useUpsertProducerSplit: () => ({ mutate: vi.fn(), isPending: false, isError: false }),
    useRequestCommissionAdjustment: () => ({ mutate: vi.fn(), isPending: false, isError: false }),
    useDecideCommissionAdjustment: () => ({ mutate: vi.fn(), isPending: false, isError: false }),
  }
})

describe('F0025 commission UI', () => {
  it('shows searchable expected commission records and rollup totals', () => {
    render(
      <MemoryRouter>
        <CommissionsPage />
      </MemoryRouter>,
    )

    fireEvent.change(screen.getByLabelText('Search commission records'), { target: { value: 'policy 111' } })

    expect(screen.getByText('Expected Commission Records')).toBeInTheDocument()
    expect(screen.getByText('NEB-DEMO-001')).toBeInTheDocument()
    expect(screen.getByText(/Harbor Agency/)).toBeInTheDocument()
    expect(screen.getAllByText('$1,200.00').length).toBeGreaterThan(0)
    expect(screen.getByText('Producer 1')).toBeInTheDocument()
  })

  it('wires recalculation from the detail page', () => {
    render(
      <MemoryRouter initialEntries={['/commissions/ec-1']}>
        <Routes>
          <Route path="/commissions/:expectedCommissionId" element={<CommissionDetailPage />} />
        </Routes>
      </MemoryRouter>,
    )

    fireEvent.click(screen.getByRole('button', { name: /recalculate/i }))

    expect(mutate).toHaveBeenCalled()
  })
})
