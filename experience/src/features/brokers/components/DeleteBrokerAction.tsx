import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ConfirmDialog } from './ConfirmDialog';
import { useDeleteBroker } from '../hooks/useDeleteBroker';
import { ApiError } from '@/services/api';

interface DeleteBrokerActionProps {
  brokerId: string;
  brokerName: string;
  open: boolean;
  onClose: () => void;
}

export function DeleteBrokerAction({ brokerId, brokerName, open, onClose }: DeleteBrokerActionProps) {
  const navigate = useNavigate();
  const deleteBroker = useDeleteBroker();
  const [error, setError] = useState('');

  async function handleConfirm() {
    setError('');
    try {
      await deleteBroker.mutateAsync(brokerId);
      navigate('/brokers');
    } catch (err) {
      if (err instanceof ApiError && err.code === 'active_dependencies_exist') {
        setError('Cannot deactivate: broker has active submissions or renewals.');
      } else {
        setError('Unable to delete broker. Please try again.');
      }
    }
  }

  return (
    <ConfirmDialog
      open={open}
      onClose={onClose}
      title="Delete Broker"
      message={`Are you sure you want to delete "${brokerName}"? This action cannot be undone.`}
      confirmLabel="Delete"
      onConfirm={handleConfirm}
      isPending={deleteBroker.isPending}
      destructive
      error={error}
    />
  );
}
