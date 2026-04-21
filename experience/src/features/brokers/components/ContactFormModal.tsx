import { useState, useEffect } from 'react';
import { Modal } from '@/components/ui/Modal';
import { TextInput } from '@/components/ui/TextInput';
import { useCreateContact } from '../hooks/useCreateContact';
import { useUpdateContact } from '../hooks/useUpdateContact';
import { validateContact } from '../lib/validation';
import { ApiError } from '@/services/api';
import type { ContactDto } from '../types';

interface ContactFormModalProps {
  brokerId: string;
  contact: ContactDto | null;
  open: boolean;
  onClose: () => void;
}

export function ContactFormModal({ brokerId, contact, open, onClose }: ContactFormModalProps) {
  const isEdit = !!contact;
  const createContact = useCreateContact(brokerId);
  const updateContact = useUpdateContact(brokerId);

  const [form, setForm] = useState({
    fullName: '',
    email: '',
    phone: '',
    role: '',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [serverError, setServerError] = useState('');

  useEffect(() => {
    if (open) {
      setForm({
        fullName: contact?.fullName ?? '',
        email: contact?.email ?? '',
        phone: contact?.phone ?? '',
        role: contact?.role ?? '',
      });
      setErrors({});
      setServerError('');
    }
  }, [open, contact]);

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

    const validationErrors = validateContact(form);
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    try {
      if (isEdit) {
        await updateContact.mutateAsync({
          contactId: contact.id,
          dto: {
            fullName: form.fullName.trim(),
            email: form.email.trim(),
            phone: form.phone.trim(),
            role: form.role.trim() || undefined,
          },
          rowVersion: contact.rowVersion,
        });
      } else {
        await createContact.mutateAsync({
          brokerId,
          fullName: form.fullName.trim(),
          email: form.email.trim(),
          phone: form.phone.trim(),
          role: form.role.trim() || undefined,
        });
      }
      onClose();
    } catch (err) {
      if (err instanceof ApiError && (err.status === 409 || err.status === 412)) {
        setServerError('This contact was modified by another user. Please refresh and try again.');
      } else {
        setServerError(`Unable to ${isEdit ? 'update' : 'create'} contact. Please try again.`);
      }
    }
  }

  const isPending = createContact.isPending || updateContact.isPending;

  return (
    <Modal open={open} onClose={onClose} title={isEdit ? 'Edit Contact' : 'Add Contact'}>
      <form noValidate onSubmit={handleSubmit} className="space-y-4">
        <TextInput
          label="Full Name"
          required
          value={form.fullName}
          onChange={(e) => updateField('fullName', e.target.value)}
          error={errors.fullName}
        />
        <TextInput
          label="Email"
          type="email"
          required
          value={form.email}
          onChange={(e) => updateField('email', e.target.value)}
          error={errors.email}
        />
        <TextInput
          label="Phone"
          type="tel"
          required
          value={form.phone}
          onChange={(e) => updateField('phone', e.target.value)}
          error={errors.phone}
          placeholder="+12025551234"
        />
        <TextInput
          label="Role"
          value={form.role}
          onChange={(e) => updateField('role', e.target.value)}
          error={errors.role}
          placeholder="e.g., Account Manager"
        />

        {serverError && <p className="text-sm text-status-error">{serverError}</p>}

        <div className="flex justify-end gap-3 pt-2">
          <button
            type="button"
            onClick={onClose}
            disabled={isPending}
            className="rounded-lg border border-surface-border bg-surface-card px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isPending}
            className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-50"
          >
            {isPending ? 'Saving...' : isEdit ? 'Save Changes' : 'Add Contact'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
