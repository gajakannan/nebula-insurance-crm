import type React from 'react'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  dashboardOpportunitiesFixture,
  opportunityAgingFixture,
  opportunityOutcomesFixture,
  submissionFlowFixture,
} from '@/mocks/data'
import { OpportunityChart } from '../components/OpportunityChart'
import { OpportunityHeatmap } from '../components/OpportunityHeatmap'
import { OpportunityOutcomesRail } from '../components/OpportunityOutcomesRail'
import { OpportunityPipelineBoard } from '../components/OpportunityPipelineBoard'
import { OpportunitySunburst } from '../components/OpportunitySunburst'
import { OpportunityTreemap } from '../components/OpportunityTreemap'
import type {
  OpportunityAgingDto,
  OpportunityFlowDto,
  OpportunityHierarchyDto,
} from '../types'

const hookMocks = vi.hoisted(() => ({
  useOpportunityFlow: vi.fn(),
  useOpportunityAging: vi.fn(),
  useOpportunityHierarchy: vi.fn(),
}))

vi.mock('@/components/ui/Popover', () => ({
  Popover: ({
    trigger,
    children,
  }: {
    trigger: React.ReactNode
    children: React.ReactNode
  }) => (
    <div>
      {trigger}
      <div>{children}</div>
    </div>
  ),
}))

vi.mock('../hooks/useOpportunityFlow', () => ({
  useOpportunityFlow: hookMocks.useOpportunityFlow,
}))

vi.mock('../hooks/useOpportunityAging', () => ({
  useOpportunityAging: hookMocks.useOpportunityAging,
}))

vi.mock('../hooks/useOpportunityHierarchy', () => ({
  useOpportunityHierarchy: hookMocks.useOpportunityHierarchy,
}))

vi.mock('../components/OpportunityPopover', () => ({
  OpportunityPopoverContent: ({
    entityType,
    status,
  }: {
    entityType: string
    status: string
  }) => <div>{entityType}:{status}:details</div>,
}))

vi.mock('../components/OpportunityOutcomePopover', () => ({
  OpportunityOutcomePopoverContent: ({
    outcomeKey,
  }: {
    outcomeKey: string
  }) => <div>{outcomeKey}:outcome-details</div>,
}))

const hierarchyFixture: OpportunityHierarchyDto = {
  periodDays: 180,
  root: {
    id: 'root',
    label: 'All opportunities',
    count: 20,
    children: [
      {
        id: 'submission:stage:Received',
        label: 'Received',
        count: 8,
        colorGroup: 'intake',
      },
      {
        id: 'submission:stage:Triaging',
        label: 'Triaging',
        count: 7,
        colorGroup: 'triage',
      },
      {
        id: 'renewal:stage:Quoted',
        label: 'Quoted',
        count: 5,
        colorGroup: 'decision',
      },
    ],
  },
}

describe('opportunity visualizations', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    Object.defineProperty(HTMLElement.prototype, 'offsetWidth', {
      configurable: true,
      get: () => 960,
    })
  })

  it('renders chart empty, loading, error, and data states', async () => {
    let flowState: {
      data: OpportunityFlowDto | undefined
      isLoading: boolean
      isError: boolean
    } = {
      data: undefined,
      isLoading: false,
      isError: false,
    }

    hookMocks.useOpportunityFlow.mockImplementation(() => flowState)

    const { rerender } = render(
      <OpportunityChart
        label="Submissions"
        entityType="submission"
        statuses={[]}
        periodDays={180}
      />,
    )

    expect(
      screen.getByText('No opportunities or transitions in the selected window.'),
    ).toBeInTheDocument()

    flowState = {
      data: undefined,
      isLoading: true,
      isError: false,
    }

    rerender(
      <OpportunityChart
        label="Submissions"
        entityType="submission"
        statuses={dashboardOpportunitiesFixture.submissions}
        periodDays={180}
      />,
    )
    expect(screen.getByText('Loading opportunity flow...')).toBeInTheDocument()

    flowState = {
      data: undefined,
      isLoading: false,
      isError: true,
    }

    rerender(
      <OpportunityChart
        label="Submissions"
        entityType="submission"
        statuses={dashboardOpportunitiesFixture.submissions}
        periodDays={180}
      />,
    )
    expect(screen.getByText('Unable to load flow data.')).toBeInTheDocument()

    flowState = {
      data: submissionFlowFixture as OpportunityFlowDto,
      isLoading: false,
      isError: false,
    }

    rerender(
      <OpportunityChart
        label="Submissions"
        entityType="submission"
        statuses={dashboardOpportunitiesFixture.submissions}
        periodDays={180}
      />,
    )

    expect(screen.getByText('28 open')).toBeInTheDocument()
    await waitFor(() => {
      expect(screen.getByTitle('Received (10 current)')).toBeInTheDocument()
    })
    expect(screen.getByText('submission:Received:details')).toBeInTheDocument()
    expect(screen.getByText('Quoted')).toBeInTheDocument()
  })

  it('renders the pipeline board empty and populated states', () => {
    const { rerender } = render(
      <OpportunityPipelineBoard
        label="Submissions"
        entityType="submission"
        statuses={[
          { status: 'Received', count: 0, colorGroup: 'intake' },
          { status: 'Triaging', count: 0, colorGroup: 'triage' },
        ]}
      />,
    )

    expect(screen.getByText('No open submissions in this period')).toBeInTheDocument()

    rerender(
      <OpportunityPipelineBoard
        label="Submissions"
        entityType="submission"
        statuses={dashboardOpportunitiesFixture.submissions}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Submissions' })).toBeInTheDocument()
    expect(screen.getByText('Top Bottlenecks')).toBeInTheDocument()
    expect(screen.getAllByRole('button', { name: 'Received: 10 opportunities' })).toHaveLength(2)
    expect(screen.getAllByText('submission:Received:details')[0]).toBeInTheDocument()
  })

  it('renders heatmap loading, error, empty, and populated states', () => {
    hookMocks.useOpportunityAging.mockReturnValueOnce({
      data: undefined,
      isLoading: true,
      isError: false,
      refetch: vi.fn(),
    })

    const { container, rerender } = render(
      <OpportunityHeatmap entityType="submission" periodDays={180} label="Aging" />,
    )

    expect(container.querySelector('.h-40')).not.toBeNull()

    const refetch = vi.fn()
    hookMocks.useOpportunityAging.mockReturnValueOnce({
      data: undefined,
      isLoading: false,
      isError: true,
      refetch,
    })

    rerender(<OpportunityHeatmap entityType="submission" periodDays={180} label="Aging" />)
    expect(screen.getByText('Unable to load aging aging data')).toBeInTheDocument()

    hookMocks.useOpportunityAging.mockReturnValueOnce({
      data: {
        entityType: 'submission',
        periodDays: 180,
        statuses: [
          {
            ...opportunityAgingFixture.statuses[0],
            total: 0,
          },
        ],
      } as OpportunityAgingDto,
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    })

    rerender(<OpportunityHeatmap entityType="submission" periodDays={180} label="Aging" />)
    expect(screen.getByText('No aging data for aging')).toBeInTheDocument()

    hookMocks.useOpportunityAging.mockReturnValue({
      data: {
        ...opportunityAgingFixture,
        statuses: [
          {
            ...opportunityAgingFixture.statuses[0],
            buckets: [
              { key: '0-2', label: '0-2d', count: 3 },
              { key: '3-5', label: '3-5d', count: 2 },
            ],
          },
        ],
      },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    })

    rerender(<OpportunityHeatmap entityType="submission" periodDays={180} label="Aging" />)

    expect(screen.getByRole('grid')).toBeInTheDocument()
    expect(screen.getByText('Triaging')).toBeInTheDocument()
    expect(screen.getByTitle('Triaging / 0-2d: 3')).toBeInTheDocument()
  })

  it('renders the outcomes rail with all branch styles and supporting popovers', () => {
    render(
      <OpportunityOutcomesRail
        outcomes={[
          ...opportunityOutcomesFixture.outcomes,
          {
            key: 'expired',
            label: 'Expired',
            branchStyle: 'gray_dotted',
            count: 2,
            percentOfTotal: 10,
            averageDaysToExit: null,
          },
        ]}
        periodDays={180}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Terminal Outcomes' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Bound: 12 exits, 60\.0% of total/ })).toHaveClass(
      'border-status-success',
    )
    expect(screen.getByRole('button', { name: /Declined: 8 exits, 40\.0% of total/ })).toHaveClass(
      'border-status-error',
    )
    expect(screen.getByRole('button', { name: /Expired: 2 exits, 10\.0% of total/ })).toHaveClass(
      'border-text-muted',
    )
    expect(screen.getByText('bound:outcome-details')).toBeInTheDocument()
  })

  it('renders the sunburst loading, error, empty, and interactive states', () => {
    hookMocks.useOpportunityHierarchy.mockReturnValueOnce({
      data: undefined,
      isLoading: true,
      isError: false,
      refetch: vi.fn(),
    })

    const { container, rerender } = render(<OpportunitySunburst periodDays={180} />)
    expect(container.querySelector('.rounded-full')).not.toBeNull()

    hookMocks.useOpportunityHierarchy.mockReturnValueOnce({
      data: undefined,
      isLoading: false,
      isError: true,
      refetch: vi.fn(),
    })

    rerender(<OpportunitySunburst periodDays={180} />)
    expect(screen.getByText('Unable to load hierarchy data')).toBeInTheDocument()

    hookMocks.useOpportunityHierarchy.mockReturnValueOnce({
      data: {
        ...hierarchyFixture,
        root: { ...hierarchyFixture.root, count: 0, children: [] },
      },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    })

    rerender(<OpportunitySunburst periodDays={180} />)
    expect(screen.getByText('No open opportunities in this period')).toBeInTheDocument()

    hookMocks.useOpportunityHierarchy.mockReturnValue({
      data: hierarchyFixture,
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    })

    rerender(<OpportunitySunburst periodDays={180} />)

    expect(screen.getByRole('img', { name: /Sunburst chart showing 20 total opportunities/ })).toBeInTheDocument()
    const triagingArc = screen.getByRole('button', { name: 'Triaging: 7 opportunities' })
    fireEvent.mouseEnter(triagingArc)
    expect(screen.getByText('7')).toBeInTheDocument()
    expect(screen.getByText('Triaging')).toBeInTheDocument()
  })

  it('renders the treemap loading, error, empty, and drilldown states', () => {
    hookMocks.useOpportunityHierarchy.mockReturnValueOnce({
      data: undefined,
      isLoading: true,
      isError: false,
      refetch: vi.fn(),
    })

    const { container, rerender } = render(<OpportunityTreemap periodDays={180} />)
    expect(container.firstChild).not.toBeNull()

    hookMocks.useOpportunityHierarchy.mockReturnValueOnce({
      data: undefined,
      isLoading: false,
      isError: true,
      refetch: vi.fn(),
    })

    rerender(<OpportunityTreemap periodDays={180} />)
    expect(screen.getByText('Unable to load hierarchy data')).toBeInTheDocument()

    hookMocks.useOpportunityHierarchy.mockReturnValueOnce({
      data: {
        ...hierarchyFixture,
        root: { ...hierarchyFixture.root, count: 0, children: [] },
      },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    })

    rerender(<OpportunityTreemap periodDays={180} />)
    expect(screen.getByText('No open opportunities in this period')).toBeInTheDocument()

    hookMocks.useOpportunityHierarchy.mockReturnValue({
      data: hierarchyFixture,
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    })

    rerender(<OpportunityTreemap periodDays={180} />)

    fireEvent.click(screen.getByRole('button', { name: /Received: 8 opportunities/ }))
    expect(screen.getByText('Received — 8 items')).toBeInTheDocument()
    expect(screen.getByText('submission:Received:details')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Close drilldown' }))
    expect(screen.queryByText('Received — 8 items')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Triaging (7)' }))
    expect(screen.getByText('Triaging — 7 items')).toBeInTheDocument()
  })
})
