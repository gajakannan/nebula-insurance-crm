import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';

export function useDeleteContact(brokerId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (contactId: string) => api.delete(`/contacts/${contactId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contacts', brokerId] });
    },
  });
}
