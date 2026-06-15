import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { BrokerScorecard } from '../types';
import { normalizeBrokerScorecard } from '../utils';

export function useBrokerScorecard(brokerId: string, windowDays = 90) {
  return useQuery({
    queryKey: ['broker-insights', 'scorecard', brokerId, windowDays],
    queryFn: async () => normalizeBrokerScorecard(
      await api.get<BrokerScorecard>(
        `/api/v1/broker-insights/${brokerId}/scorecard?window_days=${windowDays}`,
      ),
    ),
    enabled: !!brokerId,
    staleTime: 5 * 60 * 1000,
  });
}

