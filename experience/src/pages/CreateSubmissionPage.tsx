import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Card } from '@/components/ui/Card';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { Select } from '@/components/ui/Select';
import { Skeleton } from '@/components/ui/Skeleton';
import { TextInput } from '@/components/ui/TextInput';
import { useBrokers } from '@/features/brokers';
import {
  LINE_OF_BUSINESS_OPTIONS,
  describeSubmissionApiError,
  extractProblemFieldErrors,
  normalizeOptionalNumber,
  normalizeOptionalText,
  useAccounts,
  useCreateSubmission,
  usePrograms,
  validateSubmissionCreate,
} from '@/features/submissions';
import { ApiError } from '@/services/api';

export default function CreateSubmissionPage() {
  const navigate = useNavigate();
  const createSubmission = useCreateSubmission();
  const accountsQuery = useAccounts();
  const brokersQuery = useBrokers({ status: 'Active', page: 1, pageSize: 100 });
  const programsQuery = usePrograms();

  const [form, setForm] = useState({
    accountId: '',
    brokerId: '',
    effectiveDate: '',
    programId: '',
    lineOfBusiness: '',
    premiumEstimate: '',
    expirationDate: '',
    description: '',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [serverError, setServerError] = useState('');

  const isLoading = accountsQuery.isLoading || brokersQuery.isLoading || programsQuery.isLoading;
  const isError = accountsQuery.isError || brokersQuery.isError || programsQuery.isError;

  function updateField(field: keyof typeof form, value: string) {
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

    const validationErrors = validateSubmissionCreate(form);
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    try {
      const created = await createSubmission.mutateAsync({
        accountId: form.accountId,
        brokerId: form.brokerId,
        effectiveDate: form.effectiveDate,
        programId: form.programId || null,
        lineOfBusiness: form.lineOfBusiness || null,
        premiumEstimate: normalizeOptionalNumber(form.premiumEstimate),
        expirationDate: form.expirationDate || null,
        description: normalizeOptionalText(form.description),
      });

      navigate(`/submissions/${created.id}`);
    } catch (error) {
      const fieldErrors = extractProblemFieldErrors(error);
      if (Object.keys(fieldErrors).length > 0) {
        setErrors(fieldErrors);
      }

      setServerError(describeSubmissionApiError(error));

      if (error instanceof ApiError && error.code === 'region_mismatch') {
        setErrors((current) => ({
          ...current,
          brokerId: 'Broker region does not align with the selected account.',
        }));
      }
    }
  }

  return (
    <DashboardLayout title="New Submission">
      <div className="space-y-6">
        <Link
          to="/submissions"
          className="inline-flex items-center gap-1 text-xs text-text-muted hover:text-text-secondary"
        >
          <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" />
          </svg>
          Submissions
        </Link>

        {isLoading && (
          <Card className="max-w-3xl space-y-3">
            {Array.from({ length: 6 }).map((_, index) => (
              <Skeleton key={index} className="h-11 w-full" />
            ))}
          </Card>
        )}

        {isError && (
          <Card className="max-w-3xl">
            <ErrorFallback
              message="Unable to load submission reference data."
              onRetry={() => {
                accountsQuery.refetch();
                brokersQuery.refetch();
                programsQuery.refetch();
              }}
            />
          </Card>
        )}

        {!isLoading && !isError && (
          <Card className="max-w-3xl">
            <form noValidate onSubmit={handleSubmit} className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <Select
                  label="Account"
                  required
                  value={form.accountId}
                  onChange={(event) => updateField('accountId', event.target.value)}
                  error={errors.accountId}
                  placeholder="Select account"
                  options={(accountsQuery.data ?? [])
                    .filter((account) => account.status === 'Active')
                    .map((account) => ({ value: account.id, label: account.name }))}
                />

                <Select
                  label="Broker"
                  required
                  value={form.brokerId}
                  onChange={(event) => updateField('brokerId', event.target.value)}
                  error={errors.brokerId}
                  placeholder="Select broker"
                  options={(brokersQuery.data?.data ?? [])
                    .map((broker) => ({ value: broker.id, label: broker.legalName }))}
                />
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <TextInput
                  label="Effective Date"
                  required
                  type="date"
                  value={form.effectiveDate}
                  onChange={(event) => updateField('effectiveDate', event.target.value)}
                  error={errors.effectiveDate}
                />

                <TextInput
                  label="Expiration Date"
                  type="date"
                  value={form.expirationDate}
                  onChange={(event) => updateField('expirationDate', event.target.value)}
                  error={errors.expirationDate}
                />
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <Select
                  label="Program"
                  value={form.programId}
                  onChange={(event) => updateField('programId', event.target.value)}
                  error={errors.programId}
                  placeholder="No program"
                  options={(programsQuery.data ?? []).map((program) => ({
                    value: program.id,
                    label: program.name,
                  }))}
                />

                <Select
                  label="Line of Business"
                  value={form.lineOfBusiness}
                  onChange={(event) => updateField('lineOfBusiness', event.target.value)}
                  error={errors.lineOfBusiness}
                  placeholder="Select line of business"
                  options={LINE_OF_BUSINESS_OPTIONS}
                />
              </div>

              <TextInput
                label="Premium Estimate"
                type="number"
                min="0"
                step="0.01"
                value={form.premiumEstimate}
                onChange={(event) => updateField('premiumEstimate', event.target.value)}
                error={errors.premiumEstimate}
                placeholder="250000"
              />

              <div className="space-y-1.5">
                <label
                  htmlFor="submission-description"
                  className="block text-xs font-medium text-text-secondary"
                >
                  Description
                </label>
                <textarea
                  id="submission-description"
                  value={form.description}
                  onChange={(event) => updateField('description', event.target.value)}
                  rows={5}
                  className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary placeholder:text-text-muted transition-colors focus:outline-none focus:ring-1 focus:ring-nebula-violet"
                  placeholder="Capture intake notes, broker context, or underwriting framing."
                />
                <p className="text-xs text-text-muted">
                  Leave expiration blank to default it from the effective date.
                </p>
              </div>

              {serverError && (
                <p className="text-sm text-status-error">{serverError}</p>
              )}

              <div className="flex gap-3 pt-2">
                <button
                  type="submit"
                  disabled={createSubmission.isPending}
                  className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-50"
                >
                  {createSubmission.isPending ? 'Creating…' : 'Create Submission'}
                </button>
                <Link
                  to="/submissions"
                  className="rounded-lg border border-surface-border bg-surface-card px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
                >
                  Cancel
                </Link>
              </div>
            </form>
          </Card>
        )}
      </div>
    </DashboardLayout>
  );
}
