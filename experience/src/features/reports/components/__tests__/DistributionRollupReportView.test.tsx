import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { DistributionRollupReportView } from '../DistributionRollupReportView';
import type { DistributionRollupReport } from '../../types';

const mockRollupState = vi.hoisted(() => ({
  value: {
    data: undefined as DistributionRollupReport | undefined,
    isLoading: false,
    isError: false,
    refetch: vi.fn(),
  },
}));

vi.mock('../../hooks', () => ({
  useDistributionRollupReport: () => mockRollupState.value,
}));

function report(overrides: Partial<DistributionRollupReport> = {}): DistributionRollupReport {
  return {
    groupBy: 'Hierarchy',
    metricFamily: 'Production',
    asOf: '2026-07-06',
    generatedAt: '2026-07-06T00:00:00Z',
    scope: { rootNodeId: null, territoryId: null, producerUserId: null },
    totals: { recordCount: 3, productionCount: 2, workflowOpen: 3, workflowOverdue: 1, activityCount: 1 },
    rows: [
      {
        groupKey: 'broker-1',
        groupLabel: 'Broker 1',
        groupType: 'Hierarchy',
        metrics: { recordCount: 3, productionCount: 2, workflowOpen: 3, workflowOverdue: 1, activityCount: 1 },
        drilldownUrl: '/operational-reports?report=workload&rootNodeId=broker-1&asOf=2026-07-06',
        unavailableReason: null,
      },
    ],
    ...overrides,
  };
}

function renderView() {
  return render(
    <MemoryRouter>
      <DistributionRollupReportView params={{ groupBy: 'Hierarchy', metricFamily: 'Production' }} />
    </MemoryRouter>,
  );
}

describe('DistributionRollupReportView', () => {
  it('renders authorized rollup metrics and drilldown links', () => {
    mockRollupState.value = { data: report(), isLoading: false, isError: false, refetch: vi.fn() };

    renderView();

    expect(screen.getAllByText('Records')).toHaveLength(2);
    expect(screen.getAllByText('3').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByRole('link', { name: 'Broker 1' })).toHaveAttribute(
      'href',
      '/operational-reports?report=workload&rootNodeId=broker-1&asOf=2026-07-06',
    );
    expect(screen.getByText(/hidden records are excluded/i)).toBeInTheDocument();
  });

  it('shows a no-leak empty state for scoped-away rollups', () => {
    mockRollupState.value = {
      data: report({ totals: { recordCount: 0, productionCount: 0, workflowOpen: 0, workflowOverdue: 0, activityCount: 0 }, rows: [] }),
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    };

    renderView();

    expect(screen.getByText(/No visible rollup rows/i)).toBeInTheDocument();
    expect(screen.getByText(/available to your access scope/i)).toBeInTheDocument();
  });
});
