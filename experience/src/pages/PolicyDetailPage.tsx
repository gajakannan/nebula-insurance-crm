import { useMemo, useState, type ReactNode } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Save } from 'lucide-react';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { Modal } from '@/components/ui/Modal';
import { Skeleton } from '@/components/ui/Skeleton';
import { TextInput } from '@/components/ui/TextInput';
import {
  CANCELLATION_REASON_OPTIONS,
  PolicyRails,
  PolicySummaryHeader,
  describePolicyApiError,
  normalizeOptionalText,
  useCancelPolicy,
  useEndorsePolicy,
  useIssuePolicy,
  usePolicy,
  usePolicyCoverages,
  usePolicyEndorsements,
  usePolicySummary,
  usePolicyTimeline,
  usePolicyVersions,
  useReinstatePolicy,
  type PolicyCoverageInputDto,
} from '@/features/policies';
import { ApiError } from '@/services/api';

export default function PolicyDetailPage() {
  const { policyId = '' } = useParams<{ policyId: string }>();
  const policyQuery = usePolicy(policyId);
  const summaryQuery = usePolicySummary(policyId);
  const versionsQuery = usePolicyVersions(policyId);
  const endorsementsQuery = usePolicyEndorsements(policyId);
  const coveragesQuery = usePolicyCoverages(policyId);
  const timelineQuery = usePolicyTimeline(policyId);
  const issuePolicy = useIssuePolicy(policyId);
  const endorsePolicy = useEndorsePolicy(policyId);
  const cancelPolicy = useCancelPolicy(policyId);
  const reinstatePolicy = useReinstatePolicy(policyId);

  const [action, setAction] = useState<'issue' | 'endorse' | 'cancel' | 'reinstate' | null>(null);
  const [actionError, setActionError] = useState('');
  const [cancelReasonCode, setCancelReasonCode] = useState(CANCELLATION_REASON_OPTIONS[0].value);
  const [cancelReasonDetail, setCancelReasonDetail] = useState('');
  const [cancelEffectiveDate, setCancelEffectiveDate] = useState(toDateInput(new Date()));
  const [endorsementReason, setEndorsementReason] = useState('CoverageChange');
  const [endorsementDetail, setEndorsementDetail] = useState('');
  const [endorsementEffectiveDate, setEndorsementEffectiveDate] = useState(toDateInput(new Date()));
  const [endorsementPremium, setEndorsementPremium] = useState('');
  const [reinstateReason, setReinstateReason] = useState('CorrectedCancellation');
  const [reinstateDetail, setReinstateDetail] = useState('');

  const policy = policyQuery.data;
  const currentCoverages = coveragesQuery.data ?? [];
  const endorsementCoverages = useMemo<PolicyCoverageInputDto[]>(() => {
    if (currentCoverages.length === 0) {
      return [{
        coverageCode: policy?.lineOfBusiness ?? 'GeneralLiability',
        coverageName: policy?.lineOfBusiness ?? 'General Liability',
        limit: 0,
        premium: Number(endorsementPremium || policy?.totalPremium || 0),
      }];
    }

    return currentCoverages.map((coverage) => ({
      coverageCode: coverage.coverageCode,
      coverageName: coverage.coverageName,
      limit: coverage.limit,
      deductible: coverage.deductible,
      premium: Number(endorsementPremium || coverage.premium),
      exposureBasis: coverage.exposureBasis,
      exposureQuantity: coverage.exposureQuantity,
    }));
  }, [currentCoverages, endorsementPremium, policy?.lineOfBusiness, policy?.totalPremium]);

  async function refreshPolicy() {
    await Promise.all([
      policyQuery.refetch(),
      summaryQuery.refetch(),
      versionsQuery.refetch(),
      endorsementsQuery.refetch(),
      coveragesQuery.refetch(),
      timelineQuery.refetch(),
    ]);
  }

  async function runIssue() {
    if (!policy) return;
    try {
      await issuePolicy.mutateAsync({ dto: {}, rowVersion: policy.rowVersion });
      setAction(null);
      await refreshPolicy();
    } catch (error) {
      setActionError(describePolicyApiError(error));
    }
  }

  async function runCancel() {
    if (!policy) return;
    try {
      await cancelPolicy.mutateAsync({
        rowVersion: policy.rowVersion,
        dto: {
          cancellationReasonCode: cancelReasonCode,
          cancellationReasonDetail: normalizeOptionalText(cancelReasonDetail),
          cancellationEffectiveDate: cancelEffectiveDate,
        },
      });
      setAction(null);
      await refreshPolicy();
    } catch (error) {
      setActionError(describePolicyApiError(error));
    }
  }

  async function runReinstate() {
    if (!policy) return;
    try {
      await reinstatePolicy.mutateAsync({
        rowVersion: policy.rowVersion,
        dto: {
          reinstatementReason: reinstateReason,
          reinstatementDetail: normalizeOptionalText(reinstateDetail),
        },
      });
      setAction(null);
      await refreshPolicy();
    } catch (error) {
      setActionError(describePolicyApiError(error));
    }
  }

  async function runEndorse() {
    if (!policy) return;
    try {
      await endorsePolicy.mutateAsync({
        rowVersion: policy.rowVersion,
        dto: {
          endorsementReasonCode: endorsementReason,
          endorsementReasonDetail: normalizeOptionalText(endorsementDetail),
          effectiveDate: endorsementEffectiveDate,
          premiumDelta: endorsementPremium ? Number(endorsementPremium) - policy.totalPremium : null,
          coverages: endorsementCoverages,
        },
      });
      setAction(null);
      await refreshPolicy();
    } catch (error) {
      setActionError(describePolicyApiError(error));
    }
  }

  if (policyQuery.isLoading) {
    return (
      <DashboardLayout title="Policy">
        <div className="space-y-4">
          <Skeleton className="h-28 w-full" />
          <Skeleton className="h-80 w-full" />
        </div>
      </DashboardLayout>
    );
  }

  if (policyQuery.error) {
    const apiError = policyQuery.error instanceof ApiError ? policyQuery.error : null;
    if (apiError?.status === 404) {
      return (
        <DashboardLayout title="Policy">
          <div className="flex flex-col items-center justify-center py-16 text-center">
            <p className="text-sm text-text-secondary">Policy not found.</p>
            <Link to="/policies" className="mt-3 text-sm text-nebula-violet hover:underline">
              Back to policies
            </Link>
          </div>
        </DashboardLayout>
      );
    }

    return (
      <DashboardLayout title="Policy">
        <ErrorFallback message="Unable to load policy." onRetry={() => policyQuery.refetch()} />
      </DashboardLayout>
    );
  }

  if (!policy) return null;

  return (
    <DashboardLayout title="Policy">
      <div className="space-y-6">
        <Link to="/policies" className="text-xs text-text-muted hover:text-text-secondary">
          Policies
        </Link>

        <PolicySummaryHeader
          policy={summaryQuery.data ?? policy}
          onIssue={() => {
            setActionError('');
            setAction('issue');
          }}
          onEndorse={() => {
            setActionError('');
            setEndorsementEffectiveDate(toDateInput(new Date(policy.effectiveDate)));
            setEndorsementPremium(String(policy.totalPremium));
            setAction('endorse');
          }}
          onCancel={() => {
            setActionError('');
            setCancelEffectiveDate(toDateInput(new Date()));
            setAction('cancel');
          }}
          onReinstate={() => {
            setActionError('');
            setAction('reinstate');
          }}
        />

        <PolicyRails
          summary={summaryQuery.data}
          versions={versionsQuery.data}
          endorsements={endorsementsQuery.data}
          coverages={coveragesQuery.data}
          timeline={timelineQuery.data}
        />

        <ActionModal open={action === 'issue'} title="Issue policy" onClose={() => setAction(null)} onSave={runIssue} busy={issuePolicy.isPending} error={actionError}>
          <p className="text-sm text-text-secondary">
            Issuing moves this policy from Pending to Issued and records the lifecycle transition.
          </p>
        </ActionModal>

        <ActionModal open={action === 'endorse'} title="Endorse policy" onClose={() => setAction(null)} onSave={runEndorse} busy={endorsePolicy.isPending} error={actionError}>
          <div className="grid gap-3">
            <Field label="Reason code">
              <TextInput label="Reason code" value={endorsementReason} onChange={(event) => setEndorsementReason(event.target.value)} />
            </Field>
            <Field label="Effective date">
              <TextInput label="Effective date" type="date" value={endorsementEffectiveDate} onChange={(event) => setEndorsementEffectiveDate(event.target.value)} />
            </Field>
            <Field label="New total premium">
              <TextInput label="New total premium" type="number" min="0" value={endorsementPremium} onChange={(event) => setEndorsementPremium(event.target.value)} />
            </Field>
            <Field label="Detail">
              <textarea
                value={endorsementDetail}
                onChange={(event) => setEndorsementDetail(event.target.value)}
                className="min-h-[80px] w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              />
            </Field>
          </div>
        </ActionModal>

        <ActionModal open={action === 'cancel'} title="Cancel policy" onClose={() => setAction(null)} onSave={runCancel} busy={cancelPolicy.isPending} error={actionError}>
          <div className="grid gap-3">
            <Field label="Reason">
              <select
                value={cancelReasonCode}
                onChange={(event) => setCancelReasonCode(event.target.value)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              >
                {CANCELLATION_REASON_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>{option.label}</option>
                ))}
              </select>
            </Field>
            <Field label="Effective date">
              <TextInput label="Effective date" type="date" value={cancelEffectiveDate} onChange={(event) => setCancelEffectiveDate(event.target.value)} />
            </Field>
            <Field label="Detail">
              <textarea
                value={cancelReasonDetail}
                onChange={(event) => setCancelReasonDetail(event.target.value)}
                className="min-h-[80px] w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              />
            </Field>
          </div>
        </ActionModal>

        <ActionModal open={action === 'reinstate'} title="Reinstate policy" onClose={() => setAction(null)} onSave={runReinstate} busy={reinstatePolicy.isPending} error={actionError}>
          <div className="grid gap-3">
            <Field label="Reason">
              <TextInput label="Reason" value={reinstateReason} onChange={(event) => setReinstateReason(event.target.value)} />
            </Field>
            <Field label="Detail">
              <textarea
                value={reinstateDetail}
                onChange={(event) => setReinstateDetail(event.target.value)}
                className="min-h-[80px] w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              />
            </Field>
          </div>
        </ActionModal>
      </div>
    </DashboardLayout>
  );
}

interface ActionModalProps {
  open: boolean;
  title: string;
  onClose: () => void;
  onSave: () => void;
  busy: boolean;
  error: string;
  children: ReactNode;
}

function ActionModal({ open, title, onClose, onSave, busy, error, children }: ActionModalProps) {
  return (
    <Modal open={open} onClose={onClose} title={title}>
      <div className="space-y-4">
        {children}
        {error && <p className="text-sm text-status-error">{error}</p>}
        <div className="flex justify-end gap-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-surface-border px-3 py-1.5 text-sm text-text-secondary hover:bg-surface-card"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={onSave}
            disabled={busy}
            className="inline-flex items-center gap-1.5 rounded-lg bg-nebula-violet px-3 py-1.5 text-sm font-medium text-white hover:bg-nebula-violet/90 disabled:opacity-60"
          >
            <Save size={15} />
            Save
          </button>
        </div>
      </div>
    </Modal>
  );
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="space-y-1.5">
      <span className="block text-xs font-medium text-text-secondary">{label}</span>
      {children}
    </div>
  );
}

function toDateInput(date: Date): string {
  return date.toISOString().slice(0, 10);
}
