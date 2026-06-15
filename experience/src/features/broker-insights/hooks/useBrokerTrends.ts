import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { BrokerTrends } from '../types';
import { normalizeBrokerTrends } from '../utils';

export function useBrokerTrends(brokerId: string, windowDays = 90) {
  return useQuery({
    queryKey: ['broker-insights', 'trends', brokerId, windowDays],
    queryFn: async () => normalizeBrokerTrends(
      await api.get<BrokerTrends>(
        `/api/v1/broker-insights/${brokerId}/trends?window_days=${windowDays}`,
      ),
    ),
    enabled: !!brokerId,
    staleTime: 5 * 60 * 1000,
  });
}

