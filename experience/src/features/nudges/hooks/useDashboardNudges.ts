import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { NudgesResponseDto } from '../types';

export function useDashboardNudges() {
  return useQuery({
    queryKey: ['dashboard', 'nudges'],
    queryFn: () => api.get<NudgesResponseDto>('/dashboard/nudges'),
  });
}
