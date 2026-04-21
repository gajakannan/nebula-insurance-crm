import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { SubmissionCreateDto, SubmissionDto } from '../types';

export function useCreateSubmission() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: SubmissionCreateDto) =>
      api.post<SubmissionDto>('/submissions', dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['submissions'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });
}
