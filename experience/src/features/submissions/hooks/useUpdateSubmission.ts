import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { SubmissionDto, SubmissionUpdateDto } from '../types';

function ifMatch(rowVersion: string) {
  return { 'If-Match': `"${rowVersion}"` };
}

export function useUpdateSubmission(submissionId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ dto, rowVersion }: { dto: SubmissionUpdateDto; rowVersion: string }) =>
      api.put<SubmissionDto>(`/submissions/${submissionId}`, dto, ifMatch(rowVersion)),
    onSuccess: async (submission) => {
      queryClient.setQueryData(['submissions', 'detail', submissionId], submission);
      await Promise.all([
        queryClient.refetchQueries({ queryKey: ['submissions', 'timeline', submissionId], type: 'active' }),
        queryClient.invalidateQueries({ queryKey: ['submissions', 'list'] }),
        queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
      ]);
    },
  });
}
