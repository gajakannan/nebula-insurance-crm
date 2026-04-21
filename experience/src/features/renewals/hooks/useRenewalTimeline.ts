import { useInfiniteQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { TimelineEventDto } from '@/contracts/timeline';
import type { PaginatedResponse } from '../types';

export function useRenewalTimeline(renewalId: string, pageSize = 20) {
  return useInfiniteQuery({
    queryKey: ['renewals', 'timeline', renewalId, pageSize],
    queryFn: ({ pageParam }) =>
      api.get<PaginatedResponse<TimelineEventDto>>(
        `/renewals/${renewalId}/timeline?page=${pageParam}&pageSize=${pageSize}`,
      ),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => (
      lastPage.page < lastPage.totalPages ? lastPage.page + 1 : undefined
    ),
    enabled: !!renewalId,
  });
}
