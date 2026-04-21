import { useState } from 'react';
import { ConfirmDialog } from './ConfirmDialog';
import { useUpdateBroker } from '../hooks/useUpdateBroker';
import { ApiError } from '@/services/api';
import type { BrokerDto } from '../types';

interface DeactivateActionProps {
  broker: BrokerDto;
  open: boolean;
  onClose: () => void;
}

export function DeactivateAction({ broker, open, onClose }: DeactivateActionProps) {
  const updateBroker = useUpdateBroker();
  const [error, setError] = useState('');

  const isInactive = broker.status === 'Inactive';
  const targetStatus = isInactive ? 'Active' : 'Inactive';

  async function handleConfirm() {
    setError('');
    try {
      await updateBroker.mutateAsync({
        brokerId: broker.id,
        dto: {
          legalName: broker.legalName,
          state: broker.state,
          status: targetStatus,
          email: broker.email ?? undefined,
          phone: broker.phone ?? undefined,
        },
        rowVersion: broker.rowVersion,
      });
      onClose();
    } catch (err) {
      if (err instanceof ApiError && (err.status === 409 || err.status === 412)) {
        setError('This broker was modified by another user. Please refresh and try again.');
      } else {
        setError(`Unable to ${isInactive ? 'activate' : 'deactivate'} broker. Please try again.`);
      }
    }
  }

  return (
    <ConfirmDialog
      open={open}
      onClose={onClose}
      title={isInactive ? 'Activate Broker' : 'Deactivate Broker'}
      message={
        isInactive
          ? `Are you sure you want to activate "${broker.legalName}"?`
          : `Are you sure you want to deactivate "${broker.legalName}"? Contact PII will be masked.`
      }
      confirmLabel={isInactive ? 'Activate' : 'Deactivate'}
      onConfirm={handleConfirm}
      isPending={updateBroker.isPending}
      destructive={!isInactive}
      error={error}
    />
  );
}
