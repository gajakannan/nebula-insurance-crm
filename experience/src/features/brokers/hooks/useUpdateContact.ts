import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { ContactDto, ContactUpdateDto } from '../types';

interface UpdateContactParams {
  contactId: string;
  dto: ContactUpdateDto;
  rowVersion?: number;
}

export function useUpdateContact(brokerId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ contactId, dto, rowVersion }: UpdateContactParams) =>
      api.put<ContactDto>(`/contacts/${contactId}`, dto, rowVersion != null
        ? { 'If-Match': `"${rowVersion}"` }
        : undefined),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contacts', brokerId] });
    },
  });
}
