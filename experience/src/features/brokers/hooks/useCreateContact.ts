import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { ContactCreateDto, ContactDto } from '../types';

export function useCreateContact(brokerId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: ContactCreateDto) =>
      api.post<ContactDto>('/contacts', dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contacts', brokerId] });
    },
  });
}
