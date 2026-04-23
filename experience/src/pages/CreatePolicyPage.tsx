import { useState, type ReactNode } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Save } from 'lucide-react';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { TextInput } from '@/components/ui/TextInput';
import { useBrokers } from '@/features/brokers';
import { LINE_OF_BUSINESS_OPTIONS, useAccounts } from '@/features/submissions';
import {
  describePolicyApiError,
  extractPolicyFieldErrors,
  normalizeOptionalText,
  useCreatePolicy,
  type PolicyCreateRequestDto,
} from '@/features/policies';

const CARRIER_OPTIONS = [
  { value: '17000000-0000-0000-0000-000000000001', label: 'Archway Specialty' },
  { value: '17000000-0000-0000-0000-000000000002', label: 'Blue Atlas Insurance' },
  { value: '17000000-0000-0000-0000-000000000003', label: 'Summit National' },
  { value: '17000000-0000-0000-0000-000000000004', label: 'Frontier Casualty' },
  { value: '17000000-0000-0000-0000-000000000005', label: 'Compass Mutual' },
];

interface PolicyCreateForm {
  accountId: string;
  brokerOfRecordId: string;
  carrierId: string;
  lineOfBusiness: string;
  effectiveDate: string;
  expirationDate: string;
  totalPremium: string;
  coverageCode: string;
  coverageLimit: string;
  externalPolicyReference: string;
}

const today = new Date();
const nextYear = new Date(today);
nextYear.setFullYear(today.getFullYear() + 1);

const DEFAULT_FORM: PolicyCreateForm = {
  accountId: '',
  brokerOfRecordId: '',
  carrierId: CARRIER_OPTIONS[0].value,
  lineOfBusiness: 'GeneralLiability',
  effectiveDate: toDateInput(today),
  expirationDate: toDateInput(nextYear),
  totalPremium: '25000',
  coverageCode: 'GeneralLiability',
  coverageLimit: '1000000',
  externalPolicyReference: '',
};

export default function CreatePolicyPage() {
  const navigate = useNavigate();
  const accountsQuery = useAccounts();
  const brokersQuery = useBrokers({ status: 'Active', page: 1, pageSize: 100 });
  const createPolicy = useCreatePolicy();
  const [form, setForm] = useState<PolicyCreateForm>(DEFAULT_FORM);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [serverError, setServerError] = useState('');

  const accounts = accountsQuery.data ?? [];
  const brokers = brokersQuery.data?.data ?? [];

  async function savePolicy() {
    const nextErrors: Record<string, string> = {};
    if (!form.accountId) nextErrors.accountId = 'Account is required.';
    if (!form.brokerOfRecordId) nextErrors.brokerOfRecordId = 'Broker of record is required.';
    if (!form.carrierId) nextErrors.carrierId = 'Carrier is required.';
    if (!form.effectiveDate) nextErrors.effectiveDate = 'Effective date is required.';
    if (!form.expirationDate) nextErrors.expirationDate = 'Expiration date is required.';
    if (Number(form.totalPremium) < 0) nextErrors.totalPremium = 'Premium cannot be negative.';

    if (Object.keys(nextErrors).length > 0) {
      setErrors(nextErrors);
      return;
    }

    const premium = Number(form.totalPremium || 0);
    const dto: PolicyCreateRequestDto = {
      accountId: form.accountId,
      brokerOfRecordId: form.brokerOfRecordId,
      carrierId: form.carrierId,
      lineOfBusiness: form.lineOfBusiness,
      effectiveDate: form.effectiveDate,
      expirationDate: form.expirationDate,
      totalPremium: premium,
      premiumCurrency: 'USD',
      externalPolicyReference: normalizeOptionalText(form.externalPolicyReference),
      coverages: [
        {
          coverageCode: form.coverageCode || form.lineOfBusiness,
          coverageName: form.coverageCode || form.lineOfBusiness,
          limit: Number(form.coverageLimit || 0),
          premium,
        },
      ],
    };

    try {
      const policy = await createPolicy.mutateAsync(dto);
      navigate(`/policies/${policy.id}`);
    } catch (error) {
      setErrors(extractPolicyFieldErrors(error));
      setServerError(describePolicyApiError(error));
    }
  }

  function setField<K extends keyof PolicyCreateForm>(field: K, value: PolicyCreateForm[K]) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  return (
    <DashboardLayout title="New Policy">
      <div className="space-y-6">
        <Link to="/policies" className="text-xs text-text-muted hover:text-text-secondary">
          Policies
        </Link>

        <Card>
          <CardHeader>
            <CardTitle>Policy profile</CardTitle>
          </CardHeader>
          <div className="grid gap-4 md:grid-cols-2">
            <Field label="Account" error={errors.accountId}>
              <select
                value={form.accountId}
                onChange={(event) => setField('accountId', event.target.value)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              >
                <option value="">Select account</option>
                {accounts.map((account) => (
                  <option key={account.id} value={account.id}>{account.name}</option>
                ))}
              </select>
            </Field>

            <Field label="Broker of record" error={errors.brokerOfRecordId}>
              <select
                value={form.brokerOfRecordId}
                onChange={(event) => setField('brokerOfRecordId', event.target.value)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              >
                <option value="">Select broker</option>
                {brokers.map((broker) => (
                  <option key={broker.id} value={broker.id}>{broker.legalName}</option>
                ))}
              </select>
            </Field>

            <Field label="Carrier" error={errors.carrierId}>
              <select
                value={form.carrierId}
                onChange={(event) => setField('carrierId', event.target.value)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              >
                {CARRIER_OPTIONS.map((carrier) => (
                  <option key={carrier.value} value={carrier.value}>{carrier.label}</option>
                ))}
              </select>
            </Field>

            <Field label="Line of business">
              <select
                value={form.lineOfBusiness}
                onChange={(event) => {
                  setField('lineOfBusiness', event.target.value);
                  setField('coverageCode', event.target.value);
                }}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              >
                {LINE_OF_BUSINESS_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>{option.label}</option>
                ))}
              </select>
            </Field>

            <Field label="Effective date" error={errors.effectiveDate}>
              <TextInput label="Effective date" type="date" value={form.effectiveDate} onChange={(event) => setField('effectiveDate', event.target.value)} />
            </Field>

            <Field label="Expiration date" error={errors.expirationDate}>
              <TextInput label="Expiration date" type="date" value={form.expirationDate} onChange={(event) => setField('expirationDate', event.target.value)} />
            </Field>

            <Field label="Total premium" error={errors.totalPremium}>
              <TextInput label="Total premium" type="number" min="0" value={form.totalPremium} onChange={(event) => setField('totalPremium', event.target.value)} />
            </Field>

            <Field label="External reference">
              <TextInput label="External reference" value={form.externalPolicyReference} onChange={(event) => setField('externalPolicyReference', event.target.value)} />
            </Field>
          </div>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Initial coverage</CardTitle>
          </CardHeader>
          <div className="grid gap-4 md:grid-cols-3">
            <Field label="Coverage code">
              <TextInput label="Coverage code" value={form.coverageCode} onChange={(event) => setField('coverageCode', event.target.value)} />
            </Field>
            <Field label="Limit">
              <TextInput label="Limit" type="number" min="0" value={form.coverageLimit} onChange={(event) => setField('coverageLimit', event.target.value)} />
            </Field>
            <Field label="Premium">
              <TextInput label="Premium" type="number" min="0" value={form.totalPremium} onChange={(event) => setField('totalPremium', event.target.value)} />
            </Field>
          </div>
        </Card>

        {serverError && <p className="text-sm text-status-error">{serverError}</p>}
        <div className="flex justify-end">
          <button
            type="button"
            onClick={savePolicy}
            disabled={createPolicy.isPending}
            className="inline-flex items-center gap-1.5 rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-60"
          >
            <Save size={16} />
            Create Policy
          </button>
        </div>
      </div>
    </DashboardLayout>
  );
}

function Field({ label, error, children }: { label: string; error?: string; children: ReactNode }) {
  return (
    <div className="space-y-1.5">
      <span className="block text-xs font-medium text-text-secondary">{label}</span>
      {children}
      {error && <span className="block text-xs text-status-error">{error}</span>}
    </div>
  );
}

function toDateInput(date: Date): string {
  return date.toISOString().slice(0, 10);
}
