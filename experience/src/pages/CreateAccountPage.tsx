import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Card } from '@/components/ui/Card';
import { Select } from '@/components/ui/Select';
import { TextInput } from '@/components/ui/TextInput';
import { AssigneePicker, type UserSummaryDto } from '@/features/tasks';
import { useBrokers } from '@/features/brokers';
import {
  describeAccountApiError,
  extractProblemFieldErrors,
  useCreateAccount,
  type AccountCreateRequestDto,
} from '@/features/accounts';
import { US_STATES } from '@/lib/us-states';

export default function CreateAccountPage() {
  const navigate = useNavigate();
  const createAccount = useCreateAccount();
  const brokersQuery = useBrokers({ status: 'Active', page: 1, pageSize: 100 });

  const [producer, setProducer] = useState<UserSummaryDto | null>(null);
  const [form, setForm] = useState<AccountCreateRequestDto>({
    displayName: '',
    legalName: '',
    taxId: '',
    industry: '',
    primaryLineOfBusiness: '',
    brokerOfRecordId: '',
    primaryProducerUserId: '',
    territoryCode: '',
    region: '',
    address1: '',
    address2: '',
    city: '',
    state: '',
    postalCode: '',
    country: 'USA',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [serverError, setServerError] = useState('');

  function updateField(field: keyof AccountCreateRequestDto, value: string) {
    setForm((current) => ({ ...current, [field]: value }));
    setErrors((current) => {
      const next = { ...current };
      delete next[field];
      return next;
    });
    setServerError('');
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();

    if (!form.displayName.trim()) {
      setErrors({ displayName: 'Display name is required.' });
      return;
    }

    try {
      const created = await createAccount.mutateAsync({
        ...form,
        displayName: form.displayName.trim(),
        legalName: normalizeOptional(form.legalName),
        taxId: normalizeOptional(form.taxId),
        industry: normalizeOptional(form.industry),
        primaryLineOfBusiness: normalizeOptional(form.primaryLineOfBusiness),
        brokerOfRecordId: normalizeOptional(form.brokerOfRecordId),
        primaryProducerUserId: producer?.userId ?? null,
        territoryCode: normalizeOptional(form.territoryCode),
        region: normalizeOptional(form.region),
        address1: normalizeOptional(form.address1),
        address2: normalizeOptional(form.address2),
        city: normalizeOptional(form.city),
        state: normalizeOptional(form.state),
        postalCode: normalizeOptional(form.postalCode),
        country: normalizeOptional(form.country),
      });
      navigate(`/accounts/${created.id}`);
    } catch (error) {
      setErrors(extractProblemFieldErrors(error));
      setServerError(describeAccountApiError(error));
    }
  }

  const brokers = brokersQuery.data?.data ?? [];

  return (
    <DashboardLayout title="New Account">
      <div className="space-y-6">
        <Link
          to="/accounts"
          className="inline-flex items-center gap-1 text-xs text-text-muted hover:text-text-secondary"
        >
          <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" />
          </svg>
          Accounts
        </Link>

        <Card className="max-w-4xl">
          <form noValidate onSubmit={handleSubmit} className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <TextInput
                label="Display Name"
                required
                value={form.displayName}
                onChange={(event) => updateField('displayName', event.target.value)}
                error={errors.displayName}
              />
              <TextInput
                label="Legal Name"
                value={form.legalName ?? ''}
                onChange={(event) => updateField('legalName', event.target.value)}
                error={errors.legalName}
              />
              <TextInput
                label="Tax ID"
                value={form.taxId ?? ''}
                onChange={(event) => updateField('taxId', event.target.value)}
                error={errors.taxId}
              />
              <TextInput
                label="Industry"
                value={form.industry ?? ''}
                onChange={(event) => updateField('industry', event.target.value)}
                error={errors.industry}
              />
              <TextInput
                label="Primary Line of Business"
                value={form.primaryLineOfBusiness ?? ''}
                onChange={(event) => updateField('primaryLineOfBusiness', event.target.value)}
                error={errors.primaryLineOfBusiness}
              />
              <Select
                label="Broker of Record"
                value={form.brokerOfRecordId ?? ''}
                onChange={(event) => updateField('brokerOfRecordId', event.target.value)}
                options={brokers.map((broker) => ({ value: broker.id, label: broker.legalName }))}
                placeholder="Unassigned"
                error={errors.brokerOfRecordId}
              />
              <TextInput
                label="Territory Code"
                value={form.territoryCode ?? ''}
                onChange={(event) => updateField('territoryCode', event.target.value)}
                error={errors.territoryCode}
              />
              <TextInput
                label="Region"
                value={form.region ?? ''}
                onChange={(event) => updateField('region', event.target.value)}
                error={errors.region}
              />
              <TextInput
                label="Address 1"
                value={form.address1 ?? ''}
                onChange={(event) => updateField('address1', event.target.value)}
                error={errors.address1}
              />
              <TextInput
                label="Address 2"
                value={form.address2 ?? ''}
                onChange={(event) => updateField('address2', event.target.value)}
                error={errors.address2}
              />
              <TextInput
                label="City"
                value={form.city ?? ''}
                onChange={(event) => updateField('city', event.target.value)}
                error={errors.city}
              />
              <Select
                label="State"
                value={form.state ?? ''}
                onChange={(event) => updateField('state', event.target.value)}
                options={US_STATES}
                placeholder="Select state"
                error={errors.state}
              />
              <TextInput
                label="Postal Code"
                value={form.postalCode ?? ''}
                onChange={(event) => updateField('postalCode', event.target.value)}
                error={errors.postalCode}
              />
              <TextInput
                label="Country"
                value={form.country ?? ''}
                onChange={(event) => updateField('country', event.target.value)}
                error={errors.country}
              />
            </div>

            <AssigneePicker
              label="Primary Producer"
              selectedUser={producer}
              onSelect={setProducer}
              error={errors.primaryProducerUserId}
            />

            {serverError && (
              <p className="text-sm text-status-error">{serverError}</p>
            )}

            <div className="flex gap-3 pt-2">
              <button
                type="submit"
                disabled={createAccount.isPending}
                className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-50"
              >
                {createAccount.isPending ? 'Creating…' : 'Create Account'}
              </button>
              <Link
                to="/accounts"
                className="rounded-lg border border-surface-border bg-surface-card px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
              >
                Cancel
              </Link>
            </div>
          </form>
        </Card>
      </div>
    </DashboardLayout>
  );
}

function normalizeOptional(value: string | null | undefined) {
  const trimmed = value?.trim();
  return trimmed ? trimmed : null;
}
