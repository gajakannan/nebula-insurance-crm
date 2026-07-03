import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ServiceCaseListPanel } from '../ServiceCaseListPanel';

const useServiceCases = vi.fn();

vi.mock('../../hooks', () => ({
  useServiceCases: (query: unknown) => useServiceCases(query),
}));

vi.mock('../ServiceCaseCreateModal', () => ({
  ServiceCaseCreateModal: ({ open }: { open: boolean }) => open ? <div role="dialog">New service case</div> : null,
}));

describe('ServiceCaseListPanel', () => {
  beforeEach(() => {
    useServiceCases.mockReset();
  });

  it('renders the empty state for an account without service cases', () => {
    useServiceCases.mockReturnValue({
      isLoading: false,
      data: { data: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 },
    });

    render(
      <MemoryRouter>
        <ServiceCaseListPanel accountId="account-1" title="Account service cases" />
      </MemoryRouter>,
    );

    expect(screen.getByText('Account service cases')).toBeInTheDocument();
    expect(screen.getByText('No service cases recorded.')).toBeInTheDocument();
    expect(useServiceCases).toHaveBeenCalledWith({ accountId: 'account-1', page: 1, pageSize: 10 });
  });

  it('renders service-case rows with status, priority, and detail links', () => {
    useServiceCases.mockReturnValue({
      isLoading: false,
      data: {
        data: [{
          id: 'case-1',
          caseNumber: 'SC-2026-000123',
          accountId: 'account-1',
          policyId: 'policy-1',
          summary: 'Carrier claim follow-up',
          description: null,
          type: 'ClaimSupport',
          status: 'Waiting',
          priority: 'High',
          ownerUserId: 'user-1',
          dueDate: '2026-07-10',
          followUpSummary: null,
          claimReference: null,
          communicationLinks: [],
          taskLinks: [],
          transitions: [],
          resolvedAt: null,
          closedAt: null,
          resolutionSummary: null,
          createdByUserId: 'user-1',
          createdAt: '2026-07-03T00:00:00Z',
          updatedAt: '2026-07-03T00:00:00Z',
          rowVersion: 1,
        }],
        page: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1,
      },
    });

    render(
      <MemoryRouter>
        <ServiceCaseListPanel accountId="account-1" policyId="policy-1" />
      </MemoryRouter>,
    );

    expect(screen.getByRole('link', { name: /SC-2026-000123/i })).toHaveAttribute('href', '/service-cases/case-1');
    expect(screen.getByText('Carrier claim follow-up')).toBeInTheDocument();
    expect(screen.getByText('Waiting')).toBeInTheDocument();
    expect(screen.getByText('High')).toBeInTheDocument();
    expect(screen.getByText(/ClaimSupport/)).toBeInTheDocument();
    expect(useServiceCases).toHaveBeenCalledWith({ policyId: 'policy-1', page: 1, pageSize: 10 });
  });
});
