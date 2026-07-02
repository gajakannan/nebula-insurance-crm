import { useEffect, useState, useRef } from 'react';
import { Link, useParams } from 'react-router-dom';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { useControlledDirtyTracker, useRegisteredForm } from '@/features/forms';
import { useCurrentUser, type CurrentUser } from '@/features/auth';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { Modal } from '@/components/ui/Modal';
import { Skeleton } from '@/components/ui/Skeleton';
import { Select } from '@/components/ui/Select';
import { TextInput } from '@/components/ui/TextInput';
import { AccountReference, AccountStatusBadge, useAccount } from '@/features/accounts';
import { CommunicationPanel } from '@/features/communications';
import { ParentDocumentsPanel } from '@/features/documents';
import { listFormSnapshotKeysForUser } from '@/features/session-continuity';
import { DynamicAttributePanel, normalizeCyberEnvelope } from '@/features/lob-attributes';
import { usePrograms } from '@/features/submissions/hooks/useReferenceData';
import { AssigneePicker, type UserSummaryDto } from '@/features/tasks';
import {
  LINE_OF_BUSINESS_OPTIONS,
  SubmissionCompletenessPanel,
  SubmissionStatusBadge,
  SubmissionTimelineSection,
  describeSubmissionApiError,
  getLineOfBusinessLabel,
  getSubmissionStatusLabel,
  normalizeOptionalNumber,
  normalizeOptionalText,
  useAssignSubmission,
  useArchiveSubmission,
  useConfirmBindSubmission,
  useReactivateSubmission,
  useRequestBindSubmission,
  useSubmission,
  useSubmissionApproval,
  useTransitionSubmission,
  useUpdateSubmission,
  useUpdateSubmissionQuotePacket,
} from '@/features/submissions';
import type { SubmissionDto, SubmissionStatus } from '@/features/submissions';
import { ApiError } from '@/services/api';

interface SubmissionEditForm {
  programId: string;
  lineOfBusiness: string;
  effectiveDate: string;
  expirationDate: string;
  premiumEstimate: string;
  description: string;
}

export default function SubmissionDetailPage() {
  const { submissionId = '' } = useParams<{ submissionId: string }>();
  const currentUser = useCurrentUser();
  const submissionQuery = useSubmission(submissionId);
  const programsQuery = usePrograms();
  const updateSubmission = useUpdateSubmission(submissionId);
  const assignSubmission = useAssignSubmission(submissionId);
  const transitionSubmission = useTransitionSubmission(submissionId);
  const survivorQuery = useAccount(
    submissionQuery.data?.accountStatus === 'Merged' && submissionQuery.data.accountSurvivorId
      ? submissionQuery.data.accountSurvivorId
      : '',
  );

  const [editOpen, setEditOpen] = useState(false);
  const [assignOpen, setAssignOpen] = useState(false);
  const [transitionTarget, setTransitionTarget] = useState<SubmissionStatus | null>(null);
  const [editForm, setEditForm] = useState<SubmissionEditForm | null>(null);
  const [editErrors, setEditErrors] = useState<Record<string, string>>({});
  const [editServerError, setEditServerError] = useState('');
  const [assignmentError, setAssignmentError] = useState('');
  const [selectedAssignee, setSelectedAssignee] = useState<UserSummaryDto | null>(null);
  const [transitionReason, setTransitionReason] = useState('');
  const [transitionError, setTransitionError] = useState('');

  // F0036-S0007: register the submission edit form with F0035 via the
  // controlled-form dirty-tracker (baseline captured when the edit modal opens).
  const editInitialRef = useRef<SubmissionEditForm | null>(null);
  const editTracker = useControlledDirtyTracker(editForm, editInitialRef.current);
  useRegisteredForm({
    registration: {
      formKey: `submission:${submissionId}`,
      route: typeof window !== 'undefined' ? window.location.pathname : '/',
      ...editTracker,
    },
    userId: currentUser?.sub ?? null,
    enabled: editOpen,
    onRestore: (record) => {
      editInitialRef.current = record.form_values;
      setEditForm(record.form_values);
      setEditOpen(true);
    },
  });

  useEffect(() => {
    if (!currentUser?.sub || !submissionId || editOpen) return;
    if (listFormSnapshotKeysForUser(currentUser.sub, `submission:${submissionId}`).includes(`submission:${submissionId}`)) {
      setEditErrors({});
      setEditServerError('');
      setEditOpen(true);
    }
  }, [currentUser?.sub, editOpen, submissionId]);

  if (submissionQuery.isLoading) {
    return (
      <DashboardLayout title="Submission">
        <div className="space-y-4">
          <Skeleton className="h-16 w-full" />
          <Skeleton className="h-64 w-full" />
        </div>
      </DashboardLayout>
    );
  }

  if (submissionQuery.error) {
    const apiError = submissionQuery.error instanceof ApiError ? submissionQuery.error : null;

    if (apiError?.status === 404) {
      return (
        <DashboardLayout title="Submission">
          <div className="flex flex-col items-center justify-center py-16 text-center">
            <p className="text-sm text-text-secondary">Submission not found.</p>
            <Link to="/submissions" className="mt-3 text-sm text-nebula-violet hover:underline">
              Back to submission list
            </Link>
          </div>
        </DashboardLayout>
      );
    }

    if (apiError?.status === 403) {
      return (
        <DashboardLayout title="Submission">
          <div className="flex flex-col items-center justify-center py-16 text-center">
            <p className="text-sm text-text-secondary">
              You don&apos;t have permission to view this submission.
            </p>
            <Link to="/submissions" className="mt-3 text-sm text-nebula-violet hover:underline">
              Back to submission list
            </Link>
          </div>
        </DashboardLayout>
      );
    }

    return (
      <DashboardLayout title="Submission">
        <ErrorFallback
          message="Unable to load submission."
          onRetry={() => submissionQuery.refetch()}
        />
      </DashboardLayout>
    );
  }

  const submission = submissionQuery.data;
  if (!submission) return null;
  const currentSubmission = submission;
  const canEditSubmissionDetails = canEditSubmission(currentUser, currentSubmission);
  const canReassignSubmission = canAssignSubmission(currentUser, currentSubmission);
  const visibleTransitions = currentSubmission.availableTransitions.filter((target) =>
    canPerformSubmissionTransition(currentUser, currentSubmission.currentStatus, target));
  const readyForUwReviewGuidance = getReadyForUwReviewGuidance(currentSubmission);
  const hasBlockedReadyForUwReview = visibleTransitions.includes('ReadyForUWReview')
    && readyForUwReviewGuidance !== null;

  function openEditModal() {
    if (!canEditSubmissionDetails) return;

    const initial = {
      programId: currentSubmission.programId ?? '',
      lineOfBusiness: currentSubmission.lineOfBusiness ?? '',
      effectiveDate: toDateInput(currentSubmission.effectiveDate),
      expirationDate: currentSubmission.expirationDate ? toDateInput(currentSubmission.expirationDate) : '',
      premiumEstimate: currentSubmission.premiumEstimate != null ? String(currentSubmission.premiumEstimate) : '',
      description: currentSubmission.description ?? '',
    };
    editInitialRef.current = initial;
    setEditForm(initial);
    setEditErrors({});
    setEditServerError('');
    setEditOpen(true);
  }

  function openAssignModal() {
    if (!canReassignSubmission) return;

    setSelectedAssignee(currentAssignee(currentSubmission));
    setAssignmentError('');
    setAssignOpen(true);
  }

  async function saveEdits() {
    if (!editForm) return;

    if (editForm.premiumEstimate) {
      const parsed = Number(editForm.premiumEstimate);
      if (Number.isNaN(parsed) || parsed < 0) {
        setEditErrors({ premiumEstimate: 'Premium estimate must be zero or greater.' });
        return;
      }
    }

    try {
      await updateSubmission.mutateAsync({
        dto: {
          programId: editForm.programId || null,
          lineOfBusiness: editForm.lineOfBusiness || null,
          effectiveDate: editForm.effectiveDate || toDateInput(currentSubmission.effectiveDate),
          expirationDate: editForm.expirationDate || null,
          premiumEstimate: normalizeOptionalNumber(editForm.premiumEstimate),
          description: normalizeOptionalText(editForm.description),
        },
        rowVersion: currentSubmission.rowVersion,
      });
      await submissionQuery.refetch();
      setEditOpen(false);
    } catch (error) {
      setEditServerError(describeSubmissionApiError(error));
    }
  }

  async function saveAssignment() {
    if (!selectedAssignee) {
      setAssignmentError('Select an assignee before saving.');
      return;
    }

    try {
      await assignSubmission.mutateAsync({
        dto: { assignedToUserId: selectedAssignee.userId },
        rowVersion: currentSubmission.rowVersion,
      });
      await submissionQuery.refetch();
      setAssignOpen(false);
    } catch (error) {
      setAssignmentError(describeSubmissionApiError(error));
    }
  }

  async function saveTransition() {
    if (!transitionTarget) return;

    try {
      await transitionSubmission.mutateAsync({
        dto: {
          toState: transitionTarget,
          reason: normalizeOptionalText(transitionReason),
        },
        rowVersion: currentSubmission.rowVersion,
      });
      await submissionQuery.refetch();
      setTransitionTarget(null);
      setTransitionReason('');
      setTransitionError('');
    } catch (error) {
      setTransitionError(describeSubmissionApiError(error));
    }
  }

  return (
    <DashboardLayout title="Submission">
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

        <Card>
          <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
            <div className="space-y-3">
              <div className="flex flex-wrap items-center gap-2">
                <SubmissionStatusBadge status={submission.currentStatus} />
                <AccountStatusBadge status={submission.accountStatus} />
                {submission.isStale && <Pill>Stale</Pill>}
              </div>
              <div>
                <h2 className="text-2xl font-semibold text-text-primary">{submission.accountDisplayName}</h2>
                <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-text-secondary">
                  <AccountReference
                    accountId={submission.accountId}
                    displayName={submission.accountDisplayName}
                    status={submission.accountStatus}
                    survivorAccountId={submission.accountSurvivorId}
                    survivorName={survivorQuery.data?.displayName}
                    className="font-medium text-text-primary hover:text-nebula-violet"
                  />
                  <Link to={`/brokers/${submission.brokerId}`} className="hover:text-nebula-violet">
                    {submission.brokerName}
                  </Link>
                  {submission.accountRegion && (
                    <>
                      <span aria-hidden="true">·</span>
                      <span>{submission.accountRegion}</span>
                    </>
                  )}
                  {submission.accountIndustry && (
                    <>
                      <span aria-hidden="true">·</span>
                      <span>{submission.accountIndustry}</span>
                    </>
                  )}
                </div>
              </div>
            </div>

            {(canEditSubmissionDetails || canReassignSubmission) && (
              <div className="flex flex-wrap gap-2">
                {canEditSubmissionDetails && (
                  <button
                    type="button"
                    onClick={openEditModal}
                    className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
                  >
                    Edit Intake Details
                  </button>
                )}
                {canReassignSubmission && (
                  <button
                    type="button"
                    onClick={openAssignModal}
                    className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
                  >
                    Reassign
                  </button>
                )}
              </div>
            )}
          </div>

          <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <DetailStat label="Line of Business" value={getLineOfBusinessLabel(submission.lineOfBusiness)} />
            <DetailStat label="Effective Date" value={formatDate(submission.effectiveDate)} />
            <DetailStat label="Expiration Date" value={submission.expirationDate ? formatDate(submission.expirationDate) : 'Default from effective date'} />
            <DetailStat label="Assigned To" value={submission.assignedToDisplayName ?? 'Unassigned'} />
            <DetailStat label="Program" value={submission.programName ?? 'No linked program'} />
            <DetailStat label="Premium Estimate" value={submission.premiumEstimate != null ? formatCurrency(submission.premiumEstimate) : 'Not set'} />
            <DetailStat label="Created" value={formatDateTime(submission.createdAt)} />
            <DetailStat label="Updated" value={formatDateTime(submission.updatedAt)} />
          </div>

          <div className="mt-5">
            <DynamicAttributePanel
              lineOfBusiness={submission.lineOfBusiness}
              value={normalizeCyberEnvelope(submission.lobAttributes)}
              readOnly
            />
          </div>

          <div className="mt-5 rounded-xl border border-surface-border bg-surface-card/50 p-4">
            <h3 className="text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">
              Description
            </h3>
            <p className="mt-2 text-sm leading-6 text-text-secondary">
              {submission.description?.trim() ? submission.description : 'No intake notes recorded yet.'}
            </p>
          </div>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Action bar</CardTitle>
          </CardHeader>
          {visibleTransitions.length > 0 ? (
            <div className="space-y-3">
              <div className="flex flex-wrap gap-2">
                {visibleTransitions.map((target) => {
                  const isReadyForUwReview = target === 'ReadyForUWReview';
                  const isBlocked = isReadyForUwReview && readyForUwReviewGuidance !== null;

                  return (
                    <button
                      key={target}
                      type="button"
                      onClick={() => {
                        if (isBlocked) return;

                        setTransitionTarget(target);
                        setTransitionReason('');
                        setTransitionError('');
                      }}
                      disabled={isBlocked}
                      title={isBlocked ? readyForUwReviewGuidance : undefined}
                      aria-describedby={isBlocked ? 'ready-for-uw-review-guidance' : undefined}
                      className={[
                        'rounded-lg px-3 py-1.5 text-sm font-medium transition-colors',
                        isBlocked
                          ? 'cursor-not-allowed border border-surface-border bg-surface-card text-text-muted opacity-70'
                          : 'bg-nebula-violet/15 text-nebula-violet hover:bg-nebula-violet/25',
                      ].join(' ')}
                    >
                      Move to {getSubmissionStatusLabel(target)}
                    </button>
                  );
                })}
              </div>

              {hasBlockedReadyForUwReview && readyForUwReviewGuidance && (
                <p
                  id="ready-for-uw-review-guidance"
                  className="rounded-lg border border-status-warning/35 bg-status-warning/10 px-3 py-2 text-sm text-text-secondary"
                >
                  {readyForUwReviewGuidance}
                </p>
              )}
            </div>
          ) : (
            <p className="text-sm text-text-muted">No workflow actions are currently available.</p>
          )}
        </Card>

        <SubmissionDownstreamPanel
          submission={submission}
          currentUser={currentUser}
          onReload={() => submissionQuery.refetch()}
        />

        <Card>
          <CardHeader>
            <CardTitle>Completeness</CardTitle>
          </CardHeader>
          <SubmissionCompletenessPanel completeness={submission.completeness} />
        </Card>

        <ParentDocumentsPanel parent={{ type: 'submission', id: submission.id }} />

        <CommunicationPanel
          entityType="Submission"
          entityId={submission.id}
          entityLabel={submission.accountDisplayName}
        />

        <Card>
          <CardHeader>
            <CardTitle>Activity Timeline</CardTitle>
          </CardHeader>
          <SubmissionTimelineSection submissionId={submission.id} />
        </Card>
      </div>

      <Modal open={editOpen} onClose={() => setEditOpen(false)} title="Edit intake details" className="max-w-2xl">
        {editForm && (
          <div className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <Select
                label="Program"
                value={editForm.programId}
                onChange={(event) => setEditForm((current) => current ? { ...current, programId: event.target.value } : current)}
                options={(programsQuery.data ?? []).map((program) => ({ value: program.id, label: program.name }))}
                placeholder="No program"
              />
              <Select
                label="Line of Business"
                value={editForm.lineOfBusiness}
                onChange={(event) => setEditForm((current) => current ? { ...current, lineOfBusiness: event.target.value } : current)}
                options={LINE_OF_BUSINESS_OPTIONS}
                placeholder="No line of business"
              />
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <TextInput
                label="Effective Date"
                type="date"
                value={editForm.effectiveDate}
                onChange={(event) => setEditForm((current) => current ? { ...current, effectiveDate: event.target.value } : current)}
              />
              <TextInput
                label="Expiration Date"
                type="date"
                value={editForm.expirationDate}
                onChange={(event) => setEditForm((current) => current ? { ...current, expirationDate: event.target.value } : current)}
              />
            </div>

            <TextInput
              label="Premium Estimate"
              type="number"
              min="0"
              step="0.01"
              value={editForm.premiumEstimate}
              onChange={(event) => {
                setEditErrors((current) => {
                  const next = { ...current };
                  delete next.premiumEstimate;
                  return next;
                });
                setEditForm((current) => current ? { ...current, premiumEstimate: event.target.value } : current);
              }}
              error={editErrors.premiumEstimate}
            />

            <div className="space-y-1.5">
              <label htmlFor="submission-edit-description" className="block text-xs font-medium text-text-secondary">
                Description
              </label>
              <textarea
                id="submission-edit-description"
                rows={5}
                value={editForm.description}
                onChange={(event) => setEditForm((current) => current ? { ...current, description: event.target.value } : current)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary placeholder:text-text-muted transition-colors focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              />
            </div>

            {editServerError && <p className="text-sm text-status-error">{editServerError}</p>}

            <div className="flex gap-3 pt-2">
              <button
                type="button"
                onClick={() => saveEdits()}
                disabled={updateSubmission.isPending}
                className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-50"
              >
                {updateSubmission.isPending ? 'Saving…' : 'Save changes'}
              </button>
              <button
                type="button"
                onClick={() => setEditOpen(false)}
                className="rounded-lg border border-surface-border bg-surface-card px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
              >
                Cancel
              </button>
            </div>
          </div>
        )}
      </Modal>

      <Modal open={assignOpen} onClose={() => setAssignOpen(false)} title="Assign submission">
        <div className="space-y-4">
          <AssigneePicker
            selectedUser={selectedAssignee}
            onSelect={(user) => {
              setAssignmentError('');
              setSelectedAssignee(user);
            }}
            label="Assignee"
          />

          {assignmentError && <p className="text-sm text-status-error">{assignmentError}</p>}

          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={() => saveAssignment()}
              disabled={assignSubmission.isPending}
              className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-50"
            >
              {assignSubmission.isPending ? 'Saving…' : 'Save assignment'}
            </button>
            <button
              type="button"
              onClick={() => setAssignOpen(false)}
              className="rounded-lg border border-surface-border bg-surface-card px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
            >
              Cancel
            </button>
          </div>
        </div>
      </Modal>

      <Modal
        open={transitionTarget !== null}
        onClose={() => setTransitionTarget(null)}
        title={transitionTarget ? `Move to ${getSubmissionStatusLabel(transitionTarget)}` : 'Transition submission'}
      >
        <div className="space-y-4">
          <div className="space-y-1.5">
            <label htmlFor="submission-transition-reason" className="block text-xs font-medium text-text-secondary">
              Note or reason
            </label>
            <textarea
              id="submission-transition-reason"
              rows={4}
              value={transitionReason}
              onChange={(event) => {
                setTransitionError('');
                setTransitionReason(event.target.value);
              }}
              className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary placeholder:text-text-muted transition-colors focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              placeholder="Optional transition context, follow-up note, or broker reason."
            />
          </div>

          {transitionError && <p className="text-sm text-status-error">{transitionError}</p>}

          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={() => saveTransition()}
              disabled={transitionSubmission.isPending}
              className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-50"
            >
              {transitionSubmission.isPending ? 'Saving…' : 'Confirm transition'}
            </button>
            <button
              type="button"
              onClick={() => setTransitionTarget(null)}
              className="rounded-lg border border-surface-border bg-surface-card px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
            >
              Cancel
            </button>
          </div>
        </div>
      </Modal>
    </DashboardLayout>
  );
}

interface SubmissionDownstreamPanelProps {
  submission: SubmissionDto;
  currentUser: CurrentUser | null;
  onReload: () => Promise<unknown>;
}

interface QuotePacketForm {
  linkedDocumentRefs: string;
  recordedPremiumAmount: string;
  recordedLimits: string;
  recordedDeductibles: string;
  effectiveDate: string;
  carrierMarket: string;
}

function SubmissionDownstreamPanel({ submission, currentUser, onReload }: SubmissionDownstreamPanelProps) {
  const updatePacket = useUpdateSubmissionQuotePacket(submission.id);
  const approval = useSubmissionApproval(submission.id);
  const requestBind = useRequestBindSubmission(submission.id);
  const confirmBind = useConfirmBindSubmission(submission.id);
  const archiveSubmission = useArchiveSubmission(submission.id);
  const reactivateSubmission = useReactivateSubmission(submission.id);

  const [packetForm, setPacketForm] = useState<QuotePacketForm>(() => packetToForm(submission));
  const [approvalReason, setApprovalReason] = useState('');
  const [archiveReason, setArchiveReason] = useState('');
  const [serverError, setServerError] = useState('');

  useEffect(() => {
    setPacketForm(packetToForm(submission));
  }, [submission.id, submission.quotePacket.rowVersion, submission.quotePacket.readinessState]);

  const canManageDownstream = canManageDownstreamSubmission(currentUser, submission);
  const canApprove = canManageDownstream
    && submission.currentStatus === 'Quoted'
    && submission.quotePacket.readinessState === 'ReadyForApproval';
  const canRequestBind = canManageDownstream
    && submission.currentStatus === 'Quoted'
    && submission.approvalStatus === 'Granted';
  const canConfirmBind = canManageDownstream && submission.currentStatus === 'BindRequested';
  const canArchive = canArchiveSubmission(currentUser, submission);
  const isPacketEditable = canManageDownstream
    && !submission.isArchived
    && (submission.currentStatus === 'InReview' || submission.currentStatus === 'Quoted');

  async function savePacket(markReady: boolean) {
    const premium = normalizeOptionalNumber(packetForm.recordedPremiumAmount);
    if (packetForm.recordedPremiumAmount && premium == null) {
      setServerError('Recorded premium must be a valid number.');
      return;
    }

    try {
      await updatePacket.mutateAsync({
        dto: {
          linkedDocumentRefs: parseDocumentRefs(packetForm.linkedDocumentRefs),
          recordedPremiumAmount: premium,
          recordedLimits: normalizeOptionalText(packetForm.recordedLimits),
          recordedDeductibles: normalizeOptionalText(packetForm.recordedDeductibles),
          effectiveDate: packetForm.effectiveDate || null,
          carrierMarket: normalizeOptionalText(packetForm.carrierMarket),
          markReady,
        },
        rowVersion: submission.rowVersion,
      });
      setServerError('');
      await onReload();
    } catch (error) {
      setServerError(describeSubmissionApiError(error));
    }
  }

  async function decideApproval(decision: 'Granted' | 'Declined') {
    if (!approvalReason.trim()) {
      setServerError('Approval decisions require a reason.');
      return;
    }

    try {
      await approval.mutateAsync({
        dto: { decision, reason: approvalReason.trim() },
        rowVersion: submission.rowVersion,
      });
      setApprovalReason('');
      setServerError('');
      await onReload();
    } catch (error) {
      setServerError(describeSubmissionApiError(error));
    }
  }

  async function requestBindHandoff() {
    try {
      await requestBind.mutateAsync({
        dto: { idempotencyKey: `submission-bind:${submission.id}:${submission.updatedAt}` },
        rowVersion: submission.rowVersion,
      });
      setServerError('');
      await onReload();
    } catch (error) {
      setServerError(describeSubmissionApiError(error));
    }
  }

  async function confirmBindHandoff() {
    try {
      await confirmBind.mutateAsync({
        dto: { idempotencyKey: submission.bindHandoff?.idempotencyKey ?? null },
        rowVersion: submission.rowVersion,
      });
      setServerError('');
      await onReload();
    } catch (error) {
      setServerError(describeSubmissionApiError(error));
    }
  }

  async function toggleArchive(archive: boolean) {
    if (!archiveReason.trim()) {
      setServerError('Archive and reactivation actions require a reason.');
      return;
    }

    try {
      const mutation = archive ? archiveSubmission : reactivateSubmission;
      await mutation.mutateAsync({
        dto: { reason: archiveReason.trim() },
        rowVersion: submission.rowVersion,
      });
      setArchiveReason('');
      setServerError('');
      await onReload();
    } catch (error) {
      setServerError(describeSubmissionApiError(error));
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Quote, approval, and bind</CardTitle>
      </CardHeader>

      <div className="space-y-5">
        {submission.isArchived && (
          <div className="rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-secondary">
            Archived{submission.archivedAt ? ` on ${formatDateTime(submission.archivedAt)}` : ''}. Reactivate to resume edits.
          </div>
        )}

        <div className="grid gap-4 lg:grid-cols-2">
          <div className="space-y-3 rounded-lg border border-surface-border bg-surface-card/50 p-4">
            <div className="flex items-center justify-between gap-3">
              <h3 className="text-sm font-semibold text-text-primary">Quote/proposal packet</h3>
              <span className="rounded-full border border-surface-border px-2 py-0.5 text-xs text-text-muted">
                {submission.quotePacket.readinessState}
              </span>
            </div>

            <TextInput
              label="Linked document refs"
              value={packetForm.linkedDocumentRefs}
              disabled={!isPacketEditable}
              onChange={(event) => setPacketForm((current) => ({ ...current, linkedDocumentRefs: event.target.value }))}
              placeholder="Comma-separated document IDs"
            />
            <div className="grid gap-3 md:grid-cols-2">
              <TextInput
                label="Recorded premium"
                type="number"
                min="0"
                step="0.01"
                value={packetForm.recordedPremiumAmount}
                disabled={!isPacketEditable}
                onChange={(event) => setPacketForm((current) => ({ ...current, recordedPremiumAmount: event.target.value }))}
              />
              <TextInput
                label="Effective date"
                type="date"
                value={packetForm.effectiveDate}
                disabled={!isPacketEditable}
                onChange={(event) => setPacketForm((current) => ({ ...current, effectiveDate: event.target.value }))}
              />
              <TextInput
                label="Limits"
                value={packetForm.recordedLimits}
                disabled={!isPacketEditable}
                onChange={(event) => setPacketForm((current) => ({ ...current, recordedLimits: event.target.value }))}
              />
              <TextInput
                label="Deductibles"
                value={packetForm.recordedDeductibles}
                disabled={!isPacketEditable}
                onChange={(event) => setPacketForm((current) => ({ ...current, recordedDeductibles: event.target.value }))}
              />
            </div>
            <TextInput
              label="Carrier market"
              value={packetForm.carrierMarket}
              disabled={!isPacketEditable}
              onChange={(event) => setPacketForm((current) => ({ ...current, carrierMarket: event.target.value }))}
            />

            {isPacketEditable && (
              <div className="flex flex-wrap gap-2">
                <button
                  type="button"
                  onClick={() => savePacket(false)}
                  disabled={updatePacket.isPending}
                  className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary disabled:opacity-50"
                >
                  Save packet
                </button>
                <button
                  type="button"
                  onClick={() => savePacket(true)}
                  disabled={updatePacket.isPending}
                  className="rounded-lg bg-nebula-violet px-3 py-1.5 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-50"
                >
                  Mark ready
                </button>
              </div>
            )}
          </div>

          <div className="space-y-4 rounded-lg border border-surface-border bg-surface-card/50 p-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <DetailStat label="Approval" value={submission.approvalStatus} />
              <DetailStat label="Age in state" value={`${submission.ageDaysInState} day${submission.ageDaysInState === 1 ? '' : 's'}`} />
            </div>

            <div className="space-y-2">
              <label htmlFor="submission-approval-reason" className="block text-xs font-medium text-text-secondary">
                Decision reason
              </label>
              <textarea
                id="submission-approval-reason"
                rows={3}
                value={approvalReason}
                disabled={!canApprove}
                onChange={(event) => setApprovalReason(event.target.value)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary placeholder:text-text-muted transition-colors focus:outline-none focus:ring-1 focus:ring-nebula-violet disabled:opacity-60"
              />
              <div className="flex flex-wrap gap-2">
                <button
                  type="button"
                  onClick={() => decideApproval('Granted')}
                  disabled={!canApprove || approval.isPending}
                  className="rounded-lg bg-nebula-violet px-3 py-1.5 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-50"
                >
                  Grant approval
                </button>
                <button
                  type="button"
                  onClick={() => decideApproval('Declined')}
                  disabled={!canApprove || approval.isPending}
                  className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary disabled:opacity-50"
                >
                  Decline approval
                </button>
              </div>
            </div>

            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                onClick={requestBindHandoff}
                disabled={!canRequestBind || requestBind.isPending}
                className="rounded-lg bg-nebula-violet/15 px-3 py-1.5 text-sm font-medium text-nebula-violet transition-colors hover:bg-nebula-violet/25 disabled:opacity-50"
              >
                Request bind
              </button>
              <button
                type="button"
                onClick={confirmBindHandoff}
                disabled={!canConfirmBind || confirmBind.isPending}
                className="rounded-lg bg-nebula-violet/15 px-3 py-1.5 text-sm font-medium text-nebula-violet transition-colors hover:bg-nebula-violet/25 disabled:opacity-50"
              >
                Confirm bound
              </button>
            </div>

            <div className="space-y-2 border-t border-surface-border pt-4">
              <TextInput
                label={submission.isArchived ? 'Reactivation reason' : 'Archive reason'}
                value={archiveReason}
                onChange={(event) => setArchiveReason(event.target.value)}
                disabled={!canArchive}
              />
              <button
                type="button"
                onClick={() => toggleArchive(!submission.isArchived)}
                disabled={!canArchive || archiveSubmission.isPending || reactivateSubmission.isPending}
                className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary disabled:opacity-50"
              >
                {submission.isArchived ? 'Reactivate' : 'Archive'}
              </button>
            </div>
          </div>
        </div>

        {serverError && <p className="text-sm text-status-error">{serverError}</p>}
      </div>
    </Card>
  );
}

function DetailStat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-surface-border bg-surface-card/50 p-4">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">{label}</p>
      <p className="mt-2 text-sm text-text-primary">{value}</p>
    </div>
  );
}

function Pill({ children }: { children: React.ReactNode }) {
  return (
    <span className="rounded-full border border-status-warning/35 bg-status-warning/20 px-2 py-0.5 text-xs font-medium text-text-primary">
      {children}
    </span>
  );
}

function currentAssignee(submission: SubmissionDto): UserSummaryDto {
  return {
    userId: submission.assignedToUserId,
    displayName: submission.assignedToDisplayName ?? 'Current assignee',
    email: '',
    roles: [],
    isActive: true,
  };
}

function hasRole(currentUser: CurrentUser | null, role: string) {
  return currentUser?.roles.some((currentRole) => currentRole === role) ?? false;
}

function canEditSubmission(currentUser: CurrentUser | null, submission: SubmissionDto) {
  if (submission.isArchived) return false;
  if (!currentUser) return false;

  if (hasRole(currentUser, 'Admin') || hasRole(currentUser, 'DistributionManager')) {
    return true;
  }

  return hasRole(currentUser, 'DistributionUser') && currentUser.sub === submission.assignedToUserId;
}

function canAssignSubmission(currentUser: CurrentUser | null, submission: SubmissionDto) {
  if (submission.isArchived) return false;
  if (!currentUser) return false;

  if (hasRole(currentUser, 'Admin') || hasRole(currentUser, 'DistributionManager')) {
    return true;
  }

  return hasRole(currentUser, 'DistributionUser') && currentUser.sub === submission.assignedToUserId;
}

function canPerformSubmissionTransition(
  currentUser: CurrentUser | null,
  fromState: SubmissionStatus,
  toState: SubmissionStatus,
) {
  if (!currentUser) return false;

  if (hasRole(currentUser, 'Admin')) {
    return true;
  }

  const isIntakeTransition =
    (fromState === 'Received' && toState === 'Triaging')
    || (fromState === 'Triaging' && (toState === 'WaitingOnBroker' || toState === 'ReadyForUWReview'))
    || (fromState === 'WaitingOnBroker' && toState === 'ReadyForUWReview');

  if (isIntakeTransition) {
    return hasRole(currentUser, 'DistributionUser') || hasRole(currentUser, 'DistributionManager');
  }

  return hasRole(currentUser, 'Underwriter');
}

function canManageDownstreamSubmission(currentUser: CurrentUser | null, submission: SubmissionDto) {
  if (!currentUser || submission.isArchived) return false;

  if (hasRole(currentUser, 'Admin')) return true;
  return currentUser.sub === submission.assignedToUserId && hasRole(currentUser, 'Underwriter');
}

function canArchiveSubmission(currentUser: CurrentUser | null, submission: SubmissionDto) {
  if (!currentUser) return false;

  const terminal = submission.currentStatus === 'Bound'
    || submission.currentStatus === 'Declined'
    || submission.currentStatus === 'Withdrawn';
  if (!terminal) return false;
  if (hasRole(currentUser, 'Admin')) return true;
  return currentUser.sub === submission.assignedToUserId && hasRole(currentUser, 'Underwriter');
}

function packetToForm(submission: SubmissionDto): QuotePacketForm {
  return {
    linkedDocumentRefs: submission.quotePacket.linkedDocumentRefs.join(', '),
    recordedPremiumAmount: submission.quotePacket.recordedPremiumAmount != null
      ? String(submission.quotePacket.recordedPremiumAmount)
      : '',
    recordedLimits: submission.quotePacket.recordedLimits ?? '',
    recordedDeductibles: submission.quotePacket.recordedDeductibles ?? '',
    effectiveDate: submission.quotePacket.effectiveDate ? toDateInput(submission.quotePacket.effectiveDate) : '',
    carrierMarket: submission.quotePacket.carrierMarket ?? '',
  };
}

function parseDocumentRefs(value: string) {
  return value
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean);
}

function getReadyForUwReviewGuidance(submission: SubmissionDto) {
  if (submission.currentStatus !== 'Triaging' && submission.currentStatus !== 'WaitingOnBroker') {
    return null;
  }

  if (submission.completeness.isComplete) {
    return null;
  }

  if (submission.completeness.missingItems.length === 0) {
    return 'Ready for UW Review is blocked until completeness checks pass.';
  }

  return `Ready for UW Review is blocked: ${submission.completeness.missingItems.join(', ')}.`;
}

function toDateInput(value: string) {
  return value.split('T')[0] ?? value;
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(value));
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  }).format(new Date(value));
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(value);
}
