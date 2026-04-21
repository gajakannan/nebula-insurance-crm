import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { BrokerDto, PaginatedResponse } from '../types';

interface UseBrokersParams {
  q?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

export function useBrokers({ q, status, page = 1, pageSize = 10 }: UseBrokersParams = {}) {
  const params = new URLSearchParams();
  if (q) params.set('q', q);
  if (status && status !== 'All') params.set('status', status);
  params.set('page', String(page));
  params.set('pageSize', String(pageSize));

  return useQuery({
    queryKey: ['brokers', { q, status, page, pageSize }],
    queryFn: () => api.get<PaginatedResponse<BrokerDto>>(`/brokers?${params.toString()}`),
  });
}
