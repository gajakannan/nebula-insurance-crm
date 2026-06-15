import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { LeaderboardResponse } from '../types';
import { normalizeLeaderboardResponse } from '../utils';

export function useBrokerLeaderboard(limit = 10) {
  return useQuery({
    queryKey: ['broker-insights', 'leaderboard', limit],
    queryFn: async () => normalizeLeaderboardResponse(
      await api.get<LeaderboardResponse>('/api/v1/broker-insights/leaderboard?limit=' + limit),
    ),
    staleTime: 5 * 60 * 1000,
  });
}

