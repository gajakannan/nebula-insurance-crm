import { useInfiniteQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { TimelineEventDto } from '@/contracts/timeline';
import type { PaginatedResponse } from '../types';

export function useSubmissionTimeline(submissionId: string, pageSize = 20) {
  return useInfiniteQuery({
    queryKey: ['submissions', 'timeline', submissionId, pageSize],
    queryFn: ({ pageParam }) =>
      api.get<PaginatedResponse<TimelineEventDto>>(
        `/submissions/${submissionId}/timeline?page=${pageParam}&pageSize=${pageSize}`,
      ),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => (
      lastPage.page < lastPage.totalPages ? lastPage.page + 1 : undefined
    ),
    enabled: !!submissionId,
  });
}
