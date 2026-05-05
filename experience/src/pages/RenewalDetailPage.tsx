import { useState } from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { Modal } from '@/components/ui/Modal';
import { Select } from '@/components/ui/Select';
import { Skeleton } from '@/components/ui/Skeleton';
import { TextInput } from '@/components/ui/TextInput';
import { AccountReference, AccountStatusBadge, useAccount } from '@/features/accounts';
import { useCurrentUser } from '@/features/auth';
import { ParentDocumentsPanel } from '@/features/documents';
import { getLineOfBusinessLabel } from '@/features/submissions';
import {
  RENEWAL_LOST_REASON_OPTIONS,
  RenewalStatusBadge,
  RenewalTimelineSection,
  RenewalUrgencyBadge,
  describeRenewalApiError,
  extractProblemFieldErrors,
  getAllowedAssignmentRoles,
  getRenewalTransitionLabel,
  normalizeOptionalText,
  useAssignRenewal,
  useRenewal,
  useTransitionRenewal,
  type RenewalLostReasonCode,
  type RenewalStatus,
} from '@/features/renewals';
import { AssigneePicker, type UserSummaryDto } from '@/features/tasks';
import { ApiError } from '@/services/api';

interface TransitionFormState {
  reason: string;
  reasonCode: RenewalLostReasonCode | '';
  reasonDetail: string;
  boundPolicyId: string;
  renewalSubmissionId: string;
}

const EMPTY_TRANSITION_FORM: TransitionFormState = {
  reason: '',
  reasonCode: '',
  reasonDetail: '',
  boundPolicyId: '',
  renewalSubmissionId: '',
};

export default function RenewalDetailPage() {
  const { renewalId = '' } = useParams<{ renewalId: string }>();
  const currentUser = useCurrentUser();
  const location = useLocation();
  const renewalQuery = useRenewal(renewalId);
  const assignRenewal = useAssignRenewal(renewalId);
  const transitionRenewal = useTransitionRenewal(renewalId);
  const survivorQuery = useAccount(
    renewalQuery.data?.accountStatus === 'Merged' && renewalQuery.data.accountSurvivorId
      ? renewalQuery.data.accountSurvivorId
      : '',
  );

  const [assignOpen, setAssignOpen] = useState(false);
  const [selectedAssignee, setSelectedAssignee] = useState<UserSummaryDto | null>(null);
  const [assignmentError, setAssignmentError] = useState('');

  const [transitionTarget, setTransitionTarget] = useState<RenewalStatus | null>(null);
  const [transitionForm, setTransitionForm] = useState<TransitionFormState>(EMPTY_TRANSITION_FORM);
  const [transitionErrors, setTransitionErrors] = useState<Record<string, string>>({});
  const [transitionError, setTransitionError] = useState('');

  const returnTo = (location.state as { returnTo?: string } | null)?.returnTo ?? '/renewals';

  if (renewalQuery.isLoading) {
    return (
      <DashboardLayout title="Renewal">
        <div className="space-y-4">
          <Skeleton className="h-16 w-full" />
          <Skeleton className="h-64 w-full" />
        </div>
      </DashboardLayout>
    );
  }

  if (renewalQuery.error) {
    const apiError = renewalQuery.error instanceof ApiError ? renewalQuery.error : null;

    if (apiError?.status === 404) {
      return (
        <DashboardLayout title="Renewal">
          <div className="flex flex-col items-center justify-center py-16 text-center">
            <p className="text-sm text-text-secondary">Renewal not found.</p>
            <Link to={returnTo} className="mt-3 text-sm text-nebula-violet hover:underline">
              Back to renewal pipeline
            </Link>
          </div>
        </DashboardLayout>
      );
    }

    if (apiError?.status === 403) {
      return (
        <DashboardLayout title="Renewal">
          <div className="flex flex-col items-center justify-center py-16 text-center">
            <p className="text-sm text-text-secondary">
              You don&apos;t have permission to view this renewal.
            </p>
            <Link to={returnTo} className="mt-3 text-sm text-nebula-violet hover:underline">
              Back to renewal pipeline
            </Link>
          </div>
        </DashboardLayout>
      );
    }

    return (
      <DashboardLayout title="Renewal">
        <ErrorFallback
          message="Unable to load renewal."
          onRetry={() => renewalQuery.refetch()}
        />
      </DashboardLayout>
    );
  }

  const renewal = renewalQuery.data;
  if (!renewal) return null;

  const canAssignRenewal = currentUser?.roles.some((role) => ['DistributionManager', 'Admin'].includes(role)) ?? false;
  const assignmentAllowedRoles = getAllowedAssignmentRoles(renewal.currentStatus);

  function openAssignModal() {
    const currentRenewal = renewalQuery.data;
    if (!currentRenewal) return;
    setSelectedAssignee(
      currentRenewal.assignedUserDisplayName
        ? {
            userId: currentRenewal.assignedToUserId,
            displayName: currentRenewal.assignedUserDisplayName,
            email: '',
            roles: [],
            isActive: true,
          }
        : null,
    );
    setAssignmentError('');
    setAssignOpen(true);
  }

  async function saveAssignment() {
    const currentRenewal = renewalQuery.data;
    if (!currentRenewal) return;
    if (!selectedAssignee) {
      setAssignmentError('Select an assignee before saving.');
      return;
    }

    try {
      await assignRenewal.mutateAsync({
        dto: { assignedToUserId: selectedAssignee.userId },
        rowVersion: currentRenewal.rowVersion,
      });
      await renewalQuery.refetch();
      setAssignOpen(false);
    } catch (error) {
      setAssignmentError(describeRenewalApiError(error));
    }
  }

  function openTransitionModal(target: RenewalStatus) {
    setTransitionTarget(target);
    setTransitionForm(EMPTY_TRANSITION_FORM);
    setTransitionErrors({});
    setTransitionError('');
  }

  async function saveTransition() {
    const currentRenewal = renewalQuery.data;
    if (!currentRenewal || !transitionTarget) return;

    const nextErrors: Record<string, string> = {};
    if (transitionTarget === 'Lost' && !transitionForm.reasonCode) {
      nextErrors.reasonCode = 'Reason code is required when marking a renewal as lost.';
    }

    if (transitionTarget === 'Lost' && transitionForm.reasonCode === 'Other' && !transitionForm.reasonDetail.trim()) {
      nextErrors.reasonDetail = 'Reason detail is required when the loss reason is Other.';
    }

    if (
      transitionTarget === 'Completed'
      && !transitionForm.boundPolicyId.trim()
      && !transitionForm.renewalSubmissionId.trim()
    ) {
      nextErrors.boundPolicyId = 'Bound policy ID or renewal submission ID is required to complete a renewal.';
    }

    if (Object.keys(nextErrors).length > 0) {
      setTransitionErrors(nextErrors);
      return;
    }

    try {
      await transitionRenewal.mutateAsync({
        dto: {
          toState: transitionTarget,
          reason: normalizeOptionalText(transitionForm.reason),
          reasonCode: transitionTarget === 'Lost'
            ? (transitionForm.reasonCode || null)
            : null,
          reasonDetail: transitionTarget === 'Lost'
            ? normalizeOptionalText(transitionForm.reasonDetail)
            : null,
          boundPolicyId: transitionTarget === 'Completed'
            ? normalizeOptionalText(transitionForm.boundPolicyId)
            : null,
          renewalSubmissionId: transitionTarget === 'Completed'
            ? normalizeOptionalText(transitionForm.renewalSubmissionId)
            : null,
        },
        rowVersion: currentRenewal.rowVersion,
      });
      await renewalQuery.refetch();
      setTransitionTarget(null);
      setTransitionForm(EMPTY_TRANSITION_FORM);
      setTransitionErrors({});
      setTransitionError('');
    } catch (error) {
      setTransitionErrors(extractProblemFieldErrors(error));
      setTransitionError(describeRenewalApiError(error));
    }
  }

  return (
    <DashboardLayout title="Renewal">
      <div className="space-y-6">
        <Link
          to={returnTo}
          className="inline-flex items-center gap-1 text-xs text-text-muted hover:text-text-secondary"
        >
          <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" />
          </svg>
          Renewal pipeline
        </Link>

        <Card>
          <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
            <div className="space-y-3">
              <div className="flex flex-wrap items-center gap-2">
                <RenewalStatusBadge status={renewal.currentStatus} />
                <RenewalUrgencyBadge urgency={renewal.urgency} />
                <AccountStatusBadge status={renewal.accountStatus} />
                <span className="rounded-full border border-surface-border bg-surface-card px-2 py-0.5 text-[11px] font-medium text-text-muted">
                  Renewal {renewal.id.slice(0, 8)}
                </span>
              </div>

              <div>
                <h2 className="text-2xl font-semibold text-text-primary">
                  {renewal.accountDisplayName ?? renewal.accountName ?? renewal.policyNumber ?? 'Renewal detail'}
                </h2>
                <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-text-secondary">
                  <AccountReference
                    accountId={renewal.accountId}
                    displayName={renewal.accountDisplayName ?? renewal.accountName ?? 'Unknown account'}
                    status={renewal.accountStatus}
                    survivorAccountId={renewal.accountSurvivorId}
                    survivorName={survivorQuery.data?.displayName}
                    className="font-medium text-text-primary hover:text-nebula-violet"
                  />
                  {renewal.policyNumber && <span>{renewal.policyNumber}</span>}
                  {renewal.brokerName && (
                    <>
                      <span aria-hidden="true">·</span>
                      <Link to={`/brokers/${renewal.brokerId}`} className="hover:text-nebula-violet">
                        {renewal.brokerName}
                      </Link>
                    </>
                  )}
                  {renewal.lineOfBusiness && (
                    <>
                      <span aria-hidden="true">·</span>
                      <span>{getLineOfBusinessLabel(renewal.lineOfBusiness)}</span>
                    </>
                  )}
                </div>
              </div>
            </div>

            {canAssignRenewal && (
              <button
                type="button"
                onClick={openAssignModal}
                className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
              >
                {renewal.assignedUserDisplayName ? 'Reassign' : 'Assign'}
              </button>
            )}
          </div>

          <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <DetailStat label="Policy Expiration" value={formatDate(renewal.policyExpirationDate)} />
            <DetailStat label="Target Outreach" value={formatDate(renewal.targetOutreachDate)} />
            <DetailStat label="Assigned To" value={renewal.assignedUserDisplayName ?? 'Unassigned'} />
            <DetailStat label="Created" value={formatDateTime(renewal.createdAt)} />
            <DetailStat label="Carrier" value={renewal.policyCarrier ?? 'Unavailable'} />
            <DetailStat label="Policy Premium" value={formatCurrency(renewal.policyPremium)} />
            <DetailStat label="Account Industry" value={renewal.accountIndustry ?? 'Unavailable'} />
            <DetailStat label="Broker State" value={renewal.brokerState ?? 'Unavailable'} />
          </div>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Action bar</CardTitle>
          </CardHeader>

          {renewal.availableTransitions.length > 0 ? (
            <div className="space-y-3">
              <div className="flex flex-wrap gap-2">
                {renewal.availableTransitions.map((target) => (
                  <button
                    key={target}
                    type="button"
                    onClick={() => openTransitionModal(target)}
                    className="rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
                  >
                    {getRenewalTransitionLabel(target)}
                  </button>
                ))}
              </div>
              <p className="text-xs text-text-muted">
                Visible actions are already filtered by the server for the current role and renewal state.
              </p>
            </div>
          ) : (
            <div className="rounded-lg border border-surface-border bg-surface-card/60 px-3 py-3 text-sm text-text-muted">
              No further status transitions are available for this renewal.
            </div>
          )}
        </Card>

        <div className="grid gap-4 xl:grid-cols-2">
          <Card>
            <CardHeader>
              <CardTitle>Policy context</CardTitle>
            </CardHeader>
            <ContextGrid
              items={[
                ['Policy Number', renewal.policyNumber ?? 'Policy data unavailable'],
                ['Line of Business', getLineOfBusinessLabel(renewal.lineOfBusiness)],
                ['Effective Date', renewal.policyEffectiveDate ? formatDate(renewal.policyEffectiveDate) : 'Unavailable'],
                ['Expiration Date', formatDate(renewal.policyExpirationDate)],
                ['Carrier', renewal.policyCarrier ?? 'Unavailable'],
                ['Premium', formatCurrency(renewal.policyPremium)],
              ]}
            />
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Account context</CardTitle>
            </CardHeader>
            <ContextGrid
              items={[
                ['Account', renewal.accountName ?? 'Unavailable'],
                ['Industry', renewal.accountIndustry ?? 'Unavailable'],
                ['Primary State', renewal.accountPrimaryState ?? 'Unavailable'],
                ['Policy ID', renewal.policyId],
              ]}
            />
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Broker context</CardTitle>
            </CardHeader>
            <ContextGrid
              items={[
                ['Broker', renewal.brokerName ?? 'Unavailable'],
                ['License Number', renewal.brokerLicenseNumber ?? 'Unavailable'],
                ['State', renewal.brokerState ?? 'Unavailable'],
                ['Broker ID', renewal.brokerId],
              ]}
            />
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Outcome</CardTitle>
            </CardHeader>
            {renewal.currentStatus === 'Completed' ? (
              <ContextGrid
                items={[
                  ['Bound Policy ID', renewal.boundPolicyId ?? 'Not linked'],
                  ['Renewal Submission', renewal.renewalSubmissionId ? `Submission ${renewal.renewalSubmissionId}` : 'Not linked'],
                ]}
                linkedSubmissionId={renewal.renewalSubmissionId}
              />
            ) : renewal.currentStatus === 'Lost' ? (
              <ContextGrid
                items={[
                  ['Loss Reason', renewal.lostReasonCode ?? 'Unavailable'],
                  ['Detail', renewal.lostReasonDetail ?? 'No additional detail'],
                ]}
              />
            ) : (
              <div className="rounded-lg border border-surface-border bg-surface-card/60 px-3 py-3 text-sm text-text-muted">
                Outcome details appear after a renewal is completed or marked as lost.
              </div>
            )}
          </Card>
        </div>

        <ParentDocumentsPanel parent={{ type: 'renewal', id: renewal.id }} />

        <Card>
          <CardHeader>
            <CardTitle>Activity timeline</CardTitle>
          </CardHeader>
          <RenewalTimelineSection renewalId={renewal.id} />
        </Card>
      </div>

      <Modal
        open={assignOpen}
        onClose={() => setAssignOpen(false)}
        title={renewal.assignedUserDisplayName ? 'Reassign renewal' : 'Assign renewal'}
        description="Only role-compatible owners are shown for the renewal’s current stage."
      >
        <div className="space-y-4">
          <AssigneePicker
            label="Renewal owner"
            selectedUser={selectedAssignee}
            onSelect={setSelectedAssignee}
            allowedRoles={assignmentAllowedRoles}
            error={assignmentError}
          />

          <div className="rounded-lg border border-surface-border bg-surface-card/60 px-3 py-3 text-sm text-text-secondary">
            {renewal.currentStatus === 'Identified' || renewal.currentStatus === 'Outreach'
              ? 'Early-stage renewals can be owned by distribution roles.'
              : 'In review and quoted renewals must be owned by underwriting or admin users.'}
          </div>

          <div className="flex flex-col gap-2 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={() => setAssignOpen(false)}
              className="rounded-lg border border-surface-border bg-surface-card px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={() => void saveAssignment()}
              disabled={assignRenewal.isPending}
              className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-60"
            >
              {assignRenewal.isPending ? 'Saving…' : 'Save owner'}
            </button>
          </div>
        </div>
      </Modal>

      <Modal
        open={transitionTarget !== null}
        onClose={() => setTransitionTarget(null)}
        title={transitionTarget ? getRenewalTransitionLabel(transitionTarget) : 'Transition renewal'}
        description="Confirm the renewal transition and provide any required outcome fields."
      >
        <div className="space-y-4">
          <TextInput
            label="Reason (optional)"
            value={transitionForm.reason}
            onChange={(event) => setTransitionForm((current) => ({ ...current, reason: event.target.value }))}
            placeholder="Add context for the timeline"
          />

          {transitionTarget === 'Lost' && (
            <>
              <Select
                label="Loss reason"
                value={transitionForm.reasonCode}
                onChange={(event) => setTransitionForm((current) => ({
                  ...current,
                  reasonCode: event.target.value as RenewalLostReasonCode | '',
                }))}
                options={RENEWAL_LOST_REASON_OPTIONS}
                placeholder="Select a loss reason"
                error={transitionErrors.reasonCode}
                required
              />
              <TextInput
                label="Reason detail"
                value={transitionForm.reasonDetail}
                onChange={(event) => setTransitionForm((current) => ({ ...current, reasonDetail: event.target.value }))}
                placeholder="Explain the loss context"
                error={transitionErrors.reasonDetail}
                required={transitionForm.reasonCode === 'Other'}
              />
            </>
          )}

          {transitionTarget === 'Completed' && (
            <>
              <TextInput
                label="Bound Policy ID"
                value={transitionForm.boundPolicyId}
                onChange={(event) => setTransitionForm((current) => ({ ...current, boundPolicyId: event.target.value }))}
                placeholder="Optional if linking a renewal submission"
                error={transitionErrors.boundPolicyId}
              />
              <TextInput
                label="Renewal Submission ID"
                value={transitionForm.renewalSubmissionId}
                onChange={(event) => setTransitionForm((current) => ({ ...current, renewalSubmissionId: event.target.value }))}
                placeholder="Optional if linking a bound policy"
                error={transitionErrors.renewalSubmissionId}
              />
            </>
          )}

          {transitionError && (
            <div className="rounded-lg border border-status-error/35 bg-status-error/10 px-3 py-3 text-sm text-text-secondary">
              {transitionError}
            </div>
          )}

          <div className="flex flex-col gap-2 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={() => setTransitionTarget(null)}
              className="rounded-lg border border-surface-border bg-surface-card px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={() => void saveTransition()}
              disabled={transitionRenewal.isPending}
              className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-60"
            >
              {transitionRenewal.isPending ? 'Saving…' : 'Confirm transition'}
            </button>
          </div>
        </div>
      </Modal>
    </DashboardLayout>
  );
}

function DetailStat({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[11px] font-medium uppercase tracking-[0.16em] text-text-muted">{label}</p>
      <p className="mt-1 text-sm text-text-primary">{value}</p>
    </div>
  );
}

function ContextGrid({
  items,
  linkedSubmissionId,
}: {
  items: Array<[string, string]>;
  linkedSubmissionId?: string | null;
}) {
  return (
    <div className="grid gap-3 sm:grid-cols-2">
      {items.map(([label, value]) => (
        <div key={label}>
          <p className="text-[11px] font-medium uppercase tracking-[0.16em] text-text-muted">{label}</p>
          {label === 'Renewal Submission' && linkedSubmissionId ? (
            <Link to={`/submissions/${linkedSubmissionId}`} className="mt-1 block text-sm text-nebula-violet hover:underline">
              {value}
            </Link>
          ) : (
            <p className="mt-1 break-all text-sm text-text-primary">{value}</p>
          )}
        </div>
      ))}
    </div>
  );
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}

function formatCurrency(value: number | null) {
  if (value == null) return 'Unavailable';

  return new Intl.NumberFormat(undefined, {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 0,
  }).format(value);
}
