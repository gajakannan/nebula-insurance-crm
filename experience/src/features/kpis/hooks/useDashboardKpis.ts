import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { DashboardKpisDto } from '../types';

export function useDashboardKpis(periodDays = 90) {
  return useQuery({
    queryKey: ['dashboard', 'kpis', periodDays],
    queryFn: () => api.get<DashboardKpisDto>(`/dashboard/kpis?periodDays=${periodDays}`),
  });
}
