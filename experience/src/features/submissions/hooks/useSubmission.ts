import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { SubmissionDto } from '../types';

export function useSubmission(submissionId: string) {
  return useQuery({
    queryKey: ['submissions', 'detail', submissionId],
    queryFn: () => api.get<SubmissionDto>(`/submissions/${submissionId}`),
    enabled: !!submissionId,
  });
}
