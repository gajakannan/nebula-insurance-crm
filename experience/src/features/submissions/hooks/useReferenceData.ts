import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { AccountReferenceDto, ProgramReferenceDto } from '../types';

export function useAccounts() {
  return useQuery({
    queryKey: ['referenceData', 'accounts'],
    queryFn: async () => {
      const result = await api.get<
        AccountReferenceDto[]
        | {
            data: Array<{
              id: string;
              displayName: string;
              status: string;
              primaryLineOfBusiness: string | null;
            }>;
          }
      >('/accounts?sort=displayName&sortDir=asc&page=1&pageSize=100');

      if (Array.isArray(result)) {
        return result;
      }

      return result.data.map((account) => ({
        id: account.id,
        name: account.displayName,
        status: account.status,
        industry: account.primaryLineOfBusiness,
      }));
    },
  });
}

export function usePrograms() {
  return useQuery({
    queryKey: ['referenceData', 'programs'],
    queryFn: () => api.get<ProgramReferenceDto[]>('/programs'),
  });
}
