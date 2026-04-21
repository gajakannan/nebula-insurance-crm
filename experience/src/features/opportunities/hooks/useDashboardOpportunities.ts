import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { DashboardOpportunitiesDto } from '../types';

export function useDashboardOpportunities(periodDays = 180) {
  return useQuery({
    queryKey: ['dashboard', 'opportunities', periodDays],
    queryFn: () =>
      api.get<DashboardOpportunitiesDto>(
        `/dashboard/opportunities?periodDays=${periodDays}`,
      ),
  });
}
