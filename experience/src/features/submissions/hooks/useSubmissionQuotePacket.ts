import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { SubmissionDto, SubmissionQuotePacketDto, SubmissionQuotePacketUpdateDto } from '../types';

function ifMatch(rowVersion: string) {
  return { 'If-Match': `"${rowVersion}"` };
}

export function useSubmissionQuotePacket(submissionId: string) {
  return useQuery({
    queryKey: ['submissions', 'quote-packet', submissionId],
    queryFn: () => api.get<SubmissionQuotePacketDto>(`/submissions/${submissionId}/quote-packet`),
    enabled: Boolean(submissionId),
  });
}

export function useUpdateSubmissionQuotePacket(submissionId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ dto, rowVersion }: { dto: SubmissionQuotePacketUpdateDto; rowVersion: string }) =>
      api.put<SubmissionDto>(`/submissions/${submissionId}/quote-packet`, dto, ifMatch(rowVersion)),
    onSuccess: async (submission) => {
      queryClient.setQueryData(['submissions', 'detail', submissionId], submission);
      queryClient.setQueryData(['submissions', 'quote-packet', submissionId], submission.quotePacket);
      await Promise.all([
        queryClient.refetchQueries({ queryKey: ['submissions', 'timeline', submissionId], type: 'active' }),
        queryClient.invalidateQueries({ queryKey: ['submissions', 'list'] }),
        queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
      ]);
    },
  });
}
