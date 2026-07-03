import { useState } from 'react';
import type { ReactNode } from 'react';
import { Modal } from '@/components/ui/Modal';
import { TextInput } from '@/components/ui/TextInput';
import { AssigneePicker, type UserSummaryDto } from '@/features/tasks';
import { useAccountList } from '@/features/accounts';
import { ApiError } from '@/services/api';
import { useCreateServiceCase } from '../hooks';
import type { ServiceCaseListQuery, ServiceCasePriority, ServiceCaseType } from '../types';

const TYPES: ServiceCaseType[] = ['Service', 'ClaimSupport', 'Billing', 'PolicyChange', 'General'];
const PRIORITIES: ServiceCasePriority[] = ['Low', 'Medium', 'High', 'Urgent'];

interface Props {
  open: boolean;
  accountId?: string;
  policyId?: string | null;
  onClose: () => void;
  onCreated?: (serviceCaseId: string) => void;
}

export function ServiceCaseCreateModal({ open, accountId, policyId, onClose, onCreated }: Props) {
  const invalidateQuery: ServiceCaseListQuery = policyId ? { policyId, page: 1, pageSize: 20 } : { accountId, page: 1, pageSize: 20 };
  const createServiceCase = useCreateServiceCase(invalidateQuery);
  const accountList = useAccountList({ status: 'Active', page: 1, pageSize: 50 });
  const [selectedAccountId, setSelectedAccountId] = useState(accountId ?? '');
  const [summary, setSummary] = useState('');
  const [description, setDescription] = useState('');
  const [type, setType] = useState<ServiceCaseType>('Service');
  const [priority, setPriority] = useState<ServiceCasePriority>('Medium');
  const [dueDate, setDueDate] = useState('');
  const [owner, setOwner] = useState<UserSummaryDto | null>(null);
  const [carrierClaimNumber, setCarrierClaimNumber] = useState('');
  const [dateOfLoss, setDateOfLoss] = useState('');
  const [claimantDisplayName, setClaimantDisplayName] = useState('');
  const [lossSummary, setLossSummary] = useState('');
  const [error, setError] = useState('');

  async function submit() {
    if (!summary.trim()) {
      setError('Summary is required.');
      return;
    }
    if (!owner) {
      setError('Owner is required.');
      return;
    }
    if (!dueDate) {
      setError('Due date is required.');
      return;
    }
    const effectiveAccountId = accountId ?? selectedAccountId;
    if (!effectiveAccountId) {
      setError('Account is required.');
      return;
    }

    try {
      const serviceCase = await createServiceCase.mutateAsync({
        accountId: effectiveAccountId,
        policyId,
        summary: summary.trim(),
        description: normalize(description),
        type,
        priority,
        ownerUserId: owner.userId,
        dueDate: dueDate || null,
        claimReference: type === 'ClaimSupport'
          ? {
              carrierClaimNumber: normalize(carrierClaimNumber),
              dateOfLoss: dateOfLoss || null,
              claimantDisplayName: normalize(claimantDisplayName),
              lossSummary: normalize(lossSummary),
            }
          : null,
      });
      reset();
      onCreated?.(serviceCase.id);
      onClose();
    } catch (submitError) {
      setError(submitError instanceof ApiError ? submitError.problem?.detail ?? submitError.message : 'Unable to create service case.');
    }
  }

  function reset() {
    setSummary('');
    setDescription('');
    setType('Service');
    setPriority('Medium');
    setDueDate('');
    setSelectedAccountId(accountId ?? '');
    setOwner(null);
    setCarrierClaimNumber('');
    setDateOfLoss('');
    setClaimantDisplayName('');
    setLossSummary('');
    setError('');
  }

  return (
    <Modal open={open} onClose={onClose} title="New service case">
      <div className="space-y-4">
        <div className="grid gap-3 md:grid-cols-2">
          {!accountId && (
            <Field label="Account">
              <select value={selectedAccountId} onChange={(event) => setSelectedAccountId(event.target.value)} className={fieldControlClass}>
                <option value="">Select account</option>
                {(accountList.data?.data ?? []).map((account) => (
                  <option key={account.id} value={account.id}>{account.displayName}</option>
                ))}
              </select>
            </Field>
          )}
          <Field label="Type">
            <select value={type} onChange={(event) => setType(event.target.value as ServiceCaseType)} className={fieldControlClass}>
              {TYPES.map((option) => <option key={option} value={option}>{option}</option>)}
            </select>
          </Field>
          <Field label="Priority">
            <select value={priority} onChange={(event) => setPriority(event.target.value as ServiceCasePriority)} className={fieldControlClass}>
              {PRIORITIES.map((option) => <option key={option} value={option}>{option}</option>)}
            </select>
          </Field>
        </div>
        <TextInput label="Summary" value={summary} onChange={(event) => setSummary(event.target.value)} />
        <Field label="Description">
          <textarea value={description} onChange={(event) => setDescription(event.target.value)} className={`${fieldControlClass} min-h-[80px]`} />
        </Field>
        <div className="grid gap-3 md:grid-cols-2">
          <AssigneePicker label="Owner" selectedUser={owner} onSelect={setOwner} required />
          <TextInput label="Due date" type="date" value={dueDate} onChange={(event) => setDueDate(event.target.value)} />
        </div>
        {type === 'ClaimSupport' && (
          <div className="grid gap-3 rounded-lg border border-surface-border bg-surface-card/35 p-3 md:grid-cols-2">
            <TextInput label="Carrier claim number" value={carrierClaimNumber} onChange={(event) => setCarrierClaimNumber(event.target.value)} />
            <TextInput label="Date of loss" type="date" value={dateOfLoss} onChange={(event) => setDateOfLoss(event.target.value)} />
            <TextInput label="Claimant" value={claimantDisplayName} onChange={(event) => setClaimantDisplayName(event.target.value)} />
            <Field label="Loss summary">
              <textarea value={lossSummary} onChange={(event) => setLossSummary(event.target.value)} className={`${fieldControlClass} min-h-[74px]`} />
            </Field>
          </div>
        )}
        {error && <p className="text-sm text-status-error">{error}</p>}
        <div className="flex justify-end gap-2">
          <button type="button" onClick={onClose} className="rounded-lg border border-surface-border px-3 py-1.5 text-sm text-text-secondary hover:bg-surface-card">Cancel</button>
          <button type="button" onClick={submit} disabled={createServiceCase.isPending} className="rounded-lg bg-nebula-violet px-3 py-1.5 text-sm font-medium text-white hover:bg-nebula-violet/90 disabled:opacity-60">
            Create
          </button>
        </div>
      </div>
    </Modal>
  );
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <label className="block space-y-1.5">
      <span className="block text-xs font-medium text-text-secondary">{label}</span>
      {children}
    </label>
  );
}

function normalize(value: string): string | null {
  return value.trim() ? value.trim() : null;
}

const fieldControlClass = 'w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet';
