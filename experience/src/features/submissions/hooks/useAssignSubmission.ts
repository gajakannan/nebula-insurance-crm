import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { SubmissionAssignmentRequestDto, SubmissionDto } from '../types';

function ifMatch(rowVersion: string) {
  return { 'If-Match': `"${rowVersion}"` };
}

export function useAssignSubmission(submissionId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ dto, rowVersion }: { dto: SubmissionAssignmentRequestDto; rowVersion: string }) =>
      api.put<SubmissionDto>(`/submissions/${submissionId}/assignment`, dto, ifMatch(rowVersion)),
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
