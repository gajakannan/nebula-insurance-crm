import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { MyTasksResponseDto } from '../types';

export function useMyTasks() {
  return useQuery({
    queryKey: ['my', 'tasks'],
    queryFn: () => api.get<MyTasksResponseDto>('/my/tasks?limit=10'),
  });
}
