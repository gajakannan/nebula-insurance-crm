import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { UserSearchResponseDto } from '../types';

export function useUserSearch(query: string, enabled = true) {
  return useQuery({
    queryKey: ['users', query],
    queryFn: () => api.get<UserSearchResponseDto>(`/users?q=${encodeURIComponent(query)}`),
    enabled: enabled && query.length >= 2,
  });
}
