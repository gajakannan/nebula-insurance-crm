/**
 * @vitest-environment jsdom
 */

import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type {
  BrokerInsightBenchmark,
  BrokerInsightScorecardResponse,
  BrokerInsightSnapshot,
  BrokerInsightTrend,
} from '../types';

const mockUseBrokerInsightScorecards = vi.fn();
const mockUseBrokerInsightTrend = vi.fn();
const mockUseBrokerInsightBenchmark = vi.fn();
const mockUseBrokerInsightSnapshot = vi.fn();

vi.mock('../hooks', () => ({
  useBrokerInsightScorecards: (...args: unknown[]) => mockUseBrokerInsightScorecards(...args),
  useBrokerInsightTrend: (...args: unknown[]) => mockUseBrokerInsightTrend(...args),
  useBrokerInsightBenchmark: (...args: unknown[]) => mockUseBrokerInsightBenchmark(...args),
  useBrokerInsightSnapshot: (...args: unknown[]) => mockUseBrokerInsightSnapshot(...args),
}));

import { BrokerInsightsWorkspace } from '../components/BrokerInsightsWorkspace';

describe('BrokerInsightsWorkspace', () => {
  it('renders scorecard, trend, benchmark, and snapshot panels for an authorized broker', () => {
    mockUseBrokerInsightScorecards.mockReturnValue({ isLoading: false, data: scorecards });
    mockUseBrokerInsightTrend.mockReturnValue({ data: trend });
    mockUseBrokerInsightBenchmark.mockReturnValue({ data: benchmark });
    mockUseBrokerInsightSnapshot.mockReturnValue({ data: snapshot });

    render(<BrokerInsightsWorkspace />);

    expect(screen.getByRole('heading', { name: 'Broker insights' })).toBeInTheDocument();
    expect(screen.getByText('Acme Brokerage')).toBeInTheDocument();
    expect(screen.getAllByText('Quote count').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('Trend drilldown')).toBeInTheDocument();
    expect(screen.getByText('Visible peers: 6')).toBeInTheDocument();
    expect(screen.getByText('Pipeline looks strong')).toBeInTheDocument();
  });

  it('passes broker filter changes into the scorecard query', () => {
    mockUseBrokerInsightScorecards.mockReturnValue({ isLoading: false, data: { ...scorecards, items: [] } });
    mockUseBrokerInsightTrend.mockReturnValue({ data: undefined });
    mockUseBrokerInsightBenchmark.mockReturnValue({ data: undefined });
    mockUseBrokerInsightSnapshot.mockReturnValue({ data: undefined });

    render(<BrokerInsightsWorkspace />);
    fireEvent.change(screen.getByLabelText('Broker ID'), {
      target: { value: '10000000-0000-0000-0000-000000000001' },
    });

    expect(mockUseBrokerInsightScorecards).toHaveBeenLastCalledWith(
      expect.objectContaining({ brokerId: '10000000-0000-0000-0000-000000000001' }),
    );
    expect(screen.getByText('No authorized broker insight data')).toBeInTheDocument();
  });
});

const scorecards: BrokerInsightScorecardResponse = {
  items: [
    {
      brokerId: '10000000-0000-0000-0000-000000000001',
      brokerName: 'Acme Brokerage',
      periodStart: '2026-01-01',
      periodEnd: '2026-03-31',
      generatedAt: '2026-04-01T00:00:00Z',
      partialData: false,
      metrics: [
        {
          metricKey: 'quoteCount',
          label: 'Quote count',
          value: 12,
          comparisonValue: 9,
          unit: 'count',
          denominator: 20,
          sourceRecordCount: 12,
          status: 'Available',
          drilldownAvailable: true,
          lastRefreshedAt: '2026-04-01T00:00:00Z',
        },
      ],
    },
  ],
  page: 1,
  pageSize: 25,
  totalCount: 1,
  totalPages: 1,
};

const trend: BrokerInsightTrend = {
  brokerId: '10000000-0000-0000-0000-000000000001',
  metricKey: 'quoteCount',
  bucket: 'month',
  periodStart: '2026-01-01',
  periodEnd: '2026-03-31',
  partialData: false,
  generatedAt: '2026-04-01T00:00:00Z',
  sourceRows: [],
  points: [
    {
      bucketStart: '2026-01-01',
      bucketEnd: '2026-01-31',
      value: 12,
      denominator: 20,
      sourceRecordCount: 12,
      status: 'Available',
    },
  ],
};

const benchmark: BrokerInsightBenchmark = {
  brokerId: '10000000-0000-0000-0000-000000000001',
  periodStart: '2026-01-01',
  periodEnd: '2026-03-31',
  generatedAt: '2026-04-01T00:00:00Z',
  peerSet: {
    type: 'visibleBrokerGroup',
    visiblePeerCount: 6,
    minimumPeerCount: 5,
    status: 'Available',
  },
  metrics: [
    {
      metricKey: 'quoteCount',
      brokerValue: 12,
      denominator: 20,
      peerMedian: 10,
      rank: 2,
      percentile: 80,
      variance: 2,
      status: 'Available',
    },
  ],
};

const snapshot: BrokerInsightSnapshot = {
  brokerId: '10000000-0000-0000-0000-000000000001',
  brokerName: 'Acme Brokerage',
  periodStart: '2026-01-01',
  periodEnd: '2026-03-31',
  highlights: [{ label: 'Quote count', value: '12', sourceRecordCount: 12 }],
  risks: [],
  activitySummary: 'Activity is current',
  opportunitySummary: 'Pipeline looks strong',
  sourceLinks: [],
  partialData: false,
  generatedAt: '2026-04-01T00:00:00Z',
};
