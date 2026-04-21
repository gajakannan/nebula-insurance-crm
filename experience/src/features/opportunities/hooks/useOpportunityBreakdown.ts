import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type {
  OpportunityBreakdownDto,
  OpportunityBreakdownGroupBy,
  OpportunityEntityType,
} from '../types';

export function useOpportunityBreakdown(
  entityType: OpportunityEntityType,
  status: string,
  groupBy: OpportunityBreakdownGroupBy,
  periodDays = 180,
  options?: { enabled?: boolean },
) {
  return useQuery({
    queryKey: ['dashboard', 'opportunities', entityType, status, 'breakdown', groupBy, periodDays],
    queryFn: () =>
      api.get<OpportunityBreakdownDto>(
        `/dashboard/opportunities/${entityType}/${encodeURIComponent(status)}/breakdown?groupBy=${groupBy}&periodDays=${periodDays}`,
      ),
    enabled: options?.enabled ?? true,
  });
}
