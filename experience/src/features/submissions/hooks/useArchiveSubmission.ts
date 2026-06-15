import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { SubmissionArchiveRequestDto, SubmissionDto } from '../types';

function ifMatch(rowVersion: string) {
  return { 'If-Match': `"${rowVersion}"` };
}

function useArchiveMutation(submissionId: string, path: 'archive' | 'reactivate') {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ dto, rowVersion }: { dto: SubmissionArchiveRequestDto; rowVersion: string }) =>
      api.post<SubmissionDto>(`/submissions/${submissionId}/${path}`, dto, ifMatch(rowVersion)),
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

export function useArchiveSubmission(submissionId: string) {
  return useArchiveMutation(submissionId, 'archive');
}

export function useReactivateSubmission(submissionId: string) {
  return useArchiveMutation(submissionId, 'reactivate');
}
