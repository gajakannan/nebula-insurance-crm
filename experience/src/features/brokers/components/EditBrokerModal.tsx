import { useState, useEffect, useRef } from 'react';
import { Modal } from '@/components/ui/Modal';
import { TextInput } from '@/components/ui/TextInput';
import { Select } from '@/components/ui/Select';
import { useControlledDirtyTracker, useRegisteredForm } from '@/features/forms';
import { useCurrentUser } from '@/features/auth';
import { useUpdateBroker } from '../hooks/useUpdateBroker';
import { validateBrokerUpdate } from '../lib/validation';
import { US_STATES } from '@/lib/us-states';
import { ApiError } from '@/services/api';
import type { BrokerDto, BrokerStatus, BrokerUpdateDto } from '../types';

const STATUS_OPTIONS = [
  { value: 'Active', label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
  { value: 'Pending', label: 'Pending' },
];

interface EditBrokerModalProps {
  broker: BrokerDto;
  open: boolean;
  onClose: () => void;
}

export function EditBrokerModal({ broker, open, onClose }: EditBrokerModalProps) {
  const updateBroker = useUpdateBroker();

  const [form, setForm] = useState<BrokerUpdateDto>({
    legalName: broker.legalName,
    state: broker.state,
    status: broker.status,
    email: broker.email ?? '',
    phone: broker.phone ?? '',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [serverError, setServerError] = useState('');

  // F0036-S0007: controlled-form dirty-tracker baseline.
  const user = useCurrentUser();
  const initialValuesRef = useRef(form);

  useEffect(() => {
    if (open) {
      const reset = {
        legalName: broker.legalName,
        state: broker.state,
        status: broker.status,
        email: broker.email ?? '',
        phone: broker.phone ?? '',
      };
      setForm(reset);
      initialValuesRef.current = reset;
      setErrors({});
      setServerError('');
    }
  }, [open, broker]);

  // F0036-S0008: register AFTER the open-reset effect so a restored snapshot
  // wins over the on-open reset (restore-on-mount).
  const tracker = useControlledDirtyTracker(form, initialValuesRef.current);
  useRegisteredForm({
    registration: {
      formKey: `broker:${broker.id}`,
      route: typeof window !== 'undefined' ? window.location.pathname : '/',
      ...tracker,
    },
    userId: user?.sub ?? null,
    enabled: open,
    onRestore: (record) => {
      setForm(record.form_values);
      initialValuesRef.current = record.form_values;
    },
  });

  function updateField(field: string, value: string) {
    setForm((prev) => ({ ...prev, [field]: value }));
    setErrors((prev) => {
      const next = { ...prev };
      delete next[field];
      return next;
    });
    setServerError('');
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();

    const validationErrors = validateBrokerUpdate(form);
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    try {
      await updateBroker.mutateAsync({
        brokerId: broker.id,
        dto: {
          legalName: form.legalName.trim(),
          state: form.state,
          status: form.status as BrokerStatus,
          email: form.email?.trim() || undefined,
          phone: form.phone?.trim() || undefined,
        },
        rowVersion: broker.rowVersion,
      });
      onClose();
    } catch (err) {
      if (err instanceof ApiError && (err.status === 409 || err.status === 412)) {
        setServerError('This broker was modified by another user. Please refresh and try again.');
      } else {
        setServerError('Unable to update broker. Please try again.');
      }
    }
  }

  return (
    <Modal open={open} onClose={onClose} title="Edit Broker">
      <form noValidate onSubmit={handleSubmit} className="space-y-4">
        <TextInput
          label="Legal Name"
          required
          value={form.legalName}
          onChange={(e) => updateField('legalName', e.target.value)}
          error={errors.legalName}
        />
        <div className="space-y-1.5">
          <label className="block text-xs font-medium text-text-secondary">License Number</label>
          <p className="rounded-lg border border-surface-border bg-surface-highlight px-3 py-2 font-mono text-sm text-text-muted">
            {broker.licenseNumber}
          </p>
        </div>
        <Select
          label="State"
          required
          value={form.state}
          onChange={(e) => updateField('state', e.target.value)}
          error={errors.state}
          options={US_STATES}
        />
        <Select
          label="Status"
          required
          value={form.status}
          onChange={(e) => updateField('status', e.target.value)}
          error={errors.status}
          options={STATUS_OPTIONS}
        />
        <TextInput
          label="Email"
          type="email"
          value={form.email ?? ''}
          onChange={(e) => updateField('email', e.target.value)}
          error={errors.email}
        />
        <TextInput
          label="Phone"
          type="tel"
          value={form.phone ?? ''}
          onChange={(e) => updateField('phone', e.target.value)}
          error={errors.phone}
          placeholder="+12025551234"
        />

        {serverError && <p className="text-sm text-status-error">{serverError}</p>}

        <div className="flex justify-end gap-3 pt-2">
          <button
            type="button"
            onClick={onClose}
            disabled={updateBroker.isPending}
            className="rounded-lg border border-surface-border bg-surface-card px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={updateBroker.isPending}
            className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-50"
          >
            {updateBroker.isPending ? 'Saving...' : 'Save Changes'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
