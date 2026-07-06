import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type {
  BrokerInsightBenchmark,
  BrokerInsightMetricKey,
  BrokerInsightParams,
  BrokerInsightScorecardResponse,
  BrokerInsightSnapshot,
  BrokerInsightTrend,
} from './types';

function buildQuery<T extends object>(params: T): string {
  const sp = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if ((typeof value === 'string' || typeof value === 'number') && value !== '') sp.set(key, String(value));
  });
  return sp.toString();
}

export function useBrokerInsightScorecards(params: BrokerInsightParams) {
  return useQuery({
    queryKey: ['broker-insights', 'scorecards', params],
    queryFn: () => api.get<BrokerInsightScorecardResponse>(`/broker-insights/scorecards?${buildQuery(params)}`),
  });
}

export function useBrokerInsightTrend(
  brokerId: string | undefined,
  metricKey: BrokerInsightMetricKey,
  params: BrokerInsightParams,
) {
  return useQuery({
    enabled: Boolean(brokerId),
    queryKey: ['broker-insights', brokerId, 'trends', metricKey, params],
    queryFn: () => api.get<BrokerInsightTrend>(
      `/broker-insights/${brokerId}/trends?${buildQuery({
        metricKey,
        periodStart: params.periodStart,
        periodEnd: params.periodEnd,
        bucket: 'month',
        page: 1,
        pageSize: 50,
      })}`,
    ),
  });
}

export function useBrokerInsightBenchmark(brokerId: string | undefined, params: BrokerInsightParams) {
  return useQuery({
    enabled: Boolean(brokerId),
    queryKey: ['broker-insights', brokerId, 'benchmarks', params],
    queryFn: () => api.get<BrokerInsightBenchmark>(
      `/broker-insights/${brokerId}/benchmarks?${buildQuery({
        periodStart: params.periodStart,
        periodEnd: params.periodEnd,
        peerSet: 'visibleBrokerGroup',
      })}`,
    ),
  });
}

export function useBrokerInsightSnapshot(brokerId: string | undefined, params: BrokerInsightParams) {
  return useQuery({
    enabled: Boolean(brokerId),
    queryKey: ['broker-insights', brokerId, 'snapshot', params],
    queryFn: () => api.get<BrokerInsightSnapshot>(
      `/broker-insights/${brokerId}/snapshot?${buildQuery({
        periodStart: params.periodStart,
        periodEnd: params.periodEnd,
      })}`,
    ),
  });
}
