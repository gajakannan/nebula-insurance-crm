import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { TimelineEventDto } from '@/contracts/timeline';
import type { PaginatedResponse } from '../types';

export function useBrokerTimeline(brokerId: string, page = 1) {
  return useQuery({
    queryKey: ['timeline', 'broker', brokerId, page],
    queryFn: () =>
      api.get<PaginatedResponse<TimelineEventDto>>(
        `/timeline/events?entityType=Broker&entityId=${brokerId}&page=${page}&pageSize=50`,
      ),
    enabled: !!brokerId,
  });
}
