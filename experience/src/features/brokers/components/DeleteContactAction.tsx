import { useState } from 'react';
import { ConfirmDialog } from './ConfirmDialog';
import { useDeleteContact } from '../hooks/useDeleteContact';
import { ApiError } from '@/services/api';
import type { ContactDto } from '../types';

interface DeleteContactActionProps {
  contact: ContactDto | null;
  brokerId: string;
  open: boolean;
  onClose: () => void;
}

export function DeleteContactAction({ contact, brokerId, open, onClose }: DeleteContactActionProps) {
  const deleteContact = useDeleteContact(brokerId);
  const [error, setError] = useState('');

  async function handleConfirm() {
    if (!contact) return;
    setError('');
    try {
      await deleteContact.mutateAsync(contact.id);
      onClose();
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setError('Contact not found. It may have already been deleted.');
      } else {
        setError('Unable to delete contact. Please try again.');
      }
    }
  }

  return (
    <ConfirmDialog
      open={open}
      onClose={onClose}
      title="Delete Contact"
      message={`Are you sure you want to delete "${contact?.fullName ?? ''}"?`}
      confirmLabel="Delete"
      onConfirm={handleConfirm}
      isPending={deleteContact.isPending}
      destructive
      error={error}
    />
  );
}
