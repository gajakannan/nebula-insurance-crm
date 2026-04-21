import { useState } from 'react';
import { ConfirmDialog } from './ConfirmDialog';
import { useReactivateBroker } from '../hooks/useReactivateBroker';
import { ApiError } from '@/services/api';

interface ReactivateBrokerActionProps {
  brokerId: string;
  brokerName: string;
  open: boolean;
  onClose: () => void;
}

export function ReactivateBrokerAction({
  brokerId,
  brokerName,
  open,
  onClose,
}: ReactivateBrokerActionProps) {
  const reactivate = useReactivateBroker();
  const [error, setError] = useState('');

  async function handleConfirm() {
    setError('');
    try {
      await reactivate.mutateAsync(brokerId);
      onClose();
    } catch (err) {
      if (err instanceof ApiError && err.code === 'already_active') {
        setError('This broker is already active. Refresh the page to see the current state.');
      } else if (err instanceof ApiError && err.status === 403) {
        setError('You do not have permission to reactivate this broker.');
      } else {
        setError('Unable to reactivate broker. Please try again.');
      }
    }
  }

  return (
    <ConfirmDialog
      open={open}
      onClose={onClose}
      title="Reactivate Broker"
      message={`Reactivate "${brokerName}"? The broker will be restored to Active status and will reappear in search results.`}
      confirmLabel="Reactivate"
      onConfirm={handleConfirm}
      isPending={reactivate.isPending}
      error={error}
    />
  );
}
