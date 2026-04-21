import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { TaskDto, TaskCreateRequest, TaskUpdateRequest } from '../types';

export function useCreateTask() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (body: TaskCreateRequest) => api.post<TaskDto>('/tasks', body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['my', 'tasks'] });
    },
  });
}

export function useUpdateTask() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      id,
      body,
      rowVersion,
    }: {
      id: string;
      body: TaskUpdateRequest;
      rowVersion: number;
    }) =>
      api.put<TaskDto>(`/tasks/${id}`, body, {
        'If-Match': String(rowVersion),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['my', 'tasks'] });
    },
  });
}

export function useDeleteTask() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => api.delete(`/tasks/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['my', 'tasks'] });
    },
  });
}
