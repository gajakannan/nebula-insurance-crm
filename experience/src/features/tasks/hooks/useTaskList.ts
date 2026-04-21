import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { TaskListResponseDto, TaskListFilters } from '../types';

function buildQueryString(filters: TaskListFilters): string {
  const params = new URLSearchParams();
  params.set('view', filters.view);
  if (filters.status?.length) params.set('status', filters.status.join(','));
  if (filters.priority?.length) params.set('priority', filters.priority.join(','));
  if (filters.dueDateFrom) params.set('dueDateFrom', filters.dueDateFrom);
  if (filters.dueDateTo) params.set('dueDateTo', filters.dueDateTo);
  if (filters.overdue !== undefined) params.set('overdue', String(filters.overdue));
  if (filters.assigneeId) params.set('assigneeId', filters.assigneeId);
  if (filters.linkedEntityType?.length) params.set('linkedEntityType', filters.linkedEntityType.join(','));
  if (filters.createdById) params.set('createdById', filters.createdById);
  params.set('sort', filters.sort);
  params.set('sortDir', filters.sortDir);
  params.set('page', String(filters.page));
  params.set('pageSize', String(filters.pageSize));
  return params.toString();
}

export function useTaskList(filters: TaskListFilters) {
  return useQuery({
    queryKey: ['tasks', filters],
    queryFn: () => api.get<TaskListResponseDto>(`/tasks?${buildQueryString(filters)}`),
  });
}
