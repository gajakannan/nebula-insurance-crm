import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type {
  SubmissionDto,
  WorkflowTransitionRecordDto,
  WorkflowTransitionRequestDto,
} from '../types';

function ifMatch(rowVersion: string) {
  return { 'If-Match': `"${rowVersion}"` };
}

export function useTransitionSubmission(submissionId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ dto, rowVersion }: { dto: WorkflowTransitionRequestDto; rowVersion: string }) =>
      api.post<WorkflowTransitionRecordDto>(`/submissions/${submissionId}/transitions`, dto, ifMatch(rowVersion)),
    onSuccess: async () => {
      const submission = await api.get<SubmissionDto>(`/submissions/${submissionId}`);
      queryClient.setQueryData(['submissions', 'detail', submissionId], submission);
      await Promise.all([
        queryClient.refetchQueries({ queryKey: ['submissions', 'timeline', submissionId], type: 'active' }),
        queryClient.invalidateQueries({ queryKey: ['submissions', 'list'] }),
        queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
      ]);
    },
  });
}
