import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { TimelineEventDto } from '@/contracts/timeline';

interface PaginatedTimeline {
  data: TimelineEventDto[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export function useTimelineEvents(entityType: string, limit: number) {
  return useQuery({
    queryKey: ['timeline', 'events', entityType, limit],
    queryFn: async () => {
      const result = await api.get<PaginatedTimeline>(
        `/timeline/events?entityType=${entityType}&pageSize=${limit}&page=1`,
      );
      return result.data;
    },
  });
}
