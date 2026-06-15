import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { SubmissionBindRequestDto, SubmissionDto } from '../types';

function ifMatch(rowVersion: string, idempotencyKey?: string | null) {
  return {
    'If-Match': `"${rowVersion}"`,
    ...(idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : {}),
  };
}

export function useRequestBindSubmission(submissionId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ dto, rowVersion }: { dto: SubmissionBindRequestDto; rowVersion: string }) =>
      api.post<SubmissionDto>(
        `/submissions/${submissionId}/bind-request`,
        dto,
        ifMatch(rowVersion, dto.idempotencyKey),
      ),
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

export function useConfirmBindSubmission(submissionId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ dto, rowVersion }: { dto: SubmissionBindRequestDto; rowVersion: string }) =>
      api.post<SubmissionDto>(
        `/submissions/${submissionId}/bind-confirmation`,
        dto,
        ifMatch(rowVersion, dto.idempotencyKey),
      ),
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
