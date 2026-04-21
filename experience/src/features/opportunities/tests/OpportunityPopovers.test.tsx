import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { buildOpportunityItemsFixture } from '@/mocks/data'
import { OpportunityMiniCard } from '../components/OpportunityMiniCard'
import { OpportunityOutcomePopoverContent } from '../components/OpportunityOutcomePopover'
import { OpportunityPopoverContent } from '../components/OpportunityPopover'

const hookMocks = vi.hoisted(() => ({
  useOpportunityItems: vi.fn(),
  useOpportunityOutcomeItems: vi.fn(),
}))

vi.mock('../hooks/useOpportunityItems', () => ({
  useOpportunityItems: hookMocks.useOpportunityItems,
}))

vi.mock('../hooks/useOpportunityOutcomeItems', () => ({
  useOpportunityOutcomeItems: hookMocks.useOpportunityOutcomeItems,
}))

describe('opportunity popover content', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders loading, error, empty, and populated state for status items', () => {
    hookMocks.useOpportunityItems.mockReturnValueOnce({
      data: undefined,
      isLoading: true,
      isError: false,
    })

    const { rerender, container } = render(
      <OpportunityPopoverContent entityType="submission" status="Received" />,
    )

    expect(container.querySelectorAll('.h-12').length).toBeGreaterThan(0)

    hookMocks.useOpportunityItems.mockReturnValueOnce({
      data: undefined,
      isLoading: false,
      isError: true,
    })

    rerender(<OpportunityPopoverContent entityType="submission" status="Received" />)
    expect(screen.getByText('Unable to load items')).toBeInTheDocument()

    hookMocks.useOpportunityItems.mockReturnValueOnce({
      data: { items: [], totalCount: 0 },
      isLoading: false,
      isError: false,
    })

    rerender(<OpportunityPopoverContent entityType="submission" status="Received" />)
    expect(screen.getByText('No items')).toBeInTheDocument()

    hookMocks.useOpportunityItems.mockReturnValue({
      data: buildOpportunityItemsFixture(),
      isLoading: false,
      isError: false,
    })

    rerender(<OpportunityPopoverContent entityType="submission" status="Received" />)
    expect(screen.getByText('Blue Horizon Manufacturing')).toBeInTheDocument()
    expect(screen.getByText('$185,000')).toBeInTheDocument()
    expect(screen.getByText('NB')).toBeInTheDocument()
  })

  it('renders loading, error, empty, and populated state for outcome items', () => {
    hookMocks.useOpportunityOutcomeItems.mockReturnValueOnce({
      data: undefined,
      isLoading: true,
      isError: false,
    })

    const { rerender, container } = render(
      <OpportunityOutcomePopoverContent outcomeKey="bound" periodDays={180} />,
    )

    expect(container.querySelectorAll('.h-12').length).toBeGreaterThan(0)

    hookMocks.useOpportunityOutcomeItems.mockReturnValueOnce({
      data: undefined,
      isLoading: false,
      isError: true,
    })

    rerender(<OpportunityOutcomePopoverContent outcomeKey="bound" periodDays={180} />)
    expect(screen.getByText('Unable to load outcome items')).toBeInTheDocument()

    hookMocks.useOpportunityOutcomeItems.mockReturnValueOnce({
      data: { items: [], totalCount: 0 },
      isLoading: false,
      isError: false,
    })

    rerender(<OpportunityOutcomePopoverContent outcomeKey="bound" periodDays={180} />)
    expect(screen.getByText('No items for this outcome in the selected period')).toBeInTheDocument()

    hookMocks.useOpportunityOutcomeItems.mockReturnValue({
      data: buildOpportunityItemsFixture(),
      isLoading: false,
      isError: false,
    })

    rerender(<OpportunityOutcomePopoverContent outcomeKey="bound" periodDays={180} />)
    expect(screen.getByText('Summit Contractors Group')).toBeInTheDocument()
    expect(screen.getByText('AK')).toBeInTheDocument()
  })

  it('renders mini cards without amount or assignee details when absent', () => {
    render(
      <OpportunityMiniCard
        item={{
          entityId: 'submission-3',
          entityName: 'Atlas Manufacturing',
          amount: null,
          daysInStatus: 6,
          assignedUserInitials: null,
          assignedUserDisplayName: null,
        }}
      />,
    )

    expect(screen.getByText('Atlas Manufacturing')).toBeInTheDocument()
    expect(screen.getByText('6d in status')).toBeInTheDocument()
    expect(screen.queryByText('NB')).not.toBeInTheDocument()
  })
})
