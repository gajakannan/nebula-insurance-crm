import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { ContactDto, PaginatedResponse } from '../types';

export function useBrokerContacts(brokerId: string) {
  return useQuery({
    queryKey: ['contacts', brokerId],
    queryFn: () => api.get<PaginatedResponse<ContactDto>>(`/contacts?brokerId=${brokerId}`),
    enabled: !!brokerId,
  });
}
