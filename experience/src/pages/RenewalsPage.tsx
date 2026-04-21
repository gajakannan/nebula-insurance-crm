import { startTransition, useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { Modal } from '@/components/ui/Modal';
import { Skeleton } from '@/components/ui/Skeleton';
import { TextInput } from '@/components/ui/TextInput';
import { AccountReference, AccountStatusBadge } from '@/features/accounts';
import { useCurrentUser } from '@/features/auth';
import {
  LINE_OF_BUSINESS_OPTIONS,
  getLineOfBusinessLabel,
} from '@/features/submissions';
import {
  RenewalStatusBadge,
  RenewalUrgencyBadge,
  RENEWAL_SORT_OPTIONS,
  describeRenewalApiError,
  extractProblemFieldErrors,
  getAllowedAssignmentRoles,
  useCreateRenewal,
  useRenewals,
  type RenewalCreateDto,
} from '@/features/renewals';
import { AssigneePicker, type UserSummaryDto } from '@/features/tasks';

interface RenewalCreateForm {
  policyId: string;
  lineOfBusiness: string;
}

const DUE_WINDOW_OPTIONS = [
  { value: '', label: 'All' },
  { value: '90', label: '90-day' },
  { value: '60', label: '60-day' },
  { value: '45', label: '45-day' },
  { value: 'overdue', label: 'Overdue' },
];

const STATUS_OPTIONS = [
  { value: '', label: 'All statuses' },
  { value: 'Identified', label: 'Identified' },
  { value: 'Outreach', label: 'Outreach' },
  { value: 'InReview', label: 'In Review' },
  { value: 'Quoted', label: 'Quoted' },
  { value: 'Completed', label: 'Completed' },
  { value: 'Lost', label: 'Lost' },
];

const URGENCY_OPTIONS = [
  { value: '', label: 'All urgency' },
  { value: 'overdue', label: 'Overdue only' },
  { value: 'approaching', label: 'Approaching only' },
];

export default function RenewalsPage() {
  const currentUser = useCurrentUser();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const [selectedUser, setSelectedUser] = useState<UserSummaryDto | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [createAssignee, setCreateAssignee] = useState<UserSummaryDto | null>(null);
  const [createForm, setCreateForm] = useState<RenewalCreateForm>({ policyId: '', lineOfBusiness: '' });
  const [createErrors, setCreateErrors] = useState<Record<string, string>>({});
  const [createServerError, setCreateServerError] = useState('');

  const dueWindow = searchParams.get('dueWindow') ?? '';
  const status = searchParams.get('status') ?? '';
  const lineOfBusiness = searchParams.get('lineOfBusiness') ?? '';
  const assignedToUserId = searchParams.get('assignedToUserId') ?? '';
  const urgency = searchParams.get('urgency') ?? '';
  const sort = (searchParams.get('sort') ?? 'policyExpirationDate') as 'policyExpirationDate' | 'accountName' | 'currentStatus' | 'assignedToUserId';
  const sortDir = (searchParams.get('sortDir') ?? 'asc') as 'asc' | 'desc';
  const page = Number(searchParams.get('page') ?? '1');
  const pageSize = Number(searchParams.get('pageSize') ?? '25');

  useEffect(() => {
    if (!assignedToUserId) {
      setSelectedUser(null);
      return;
    }

    setSelectedUser((current) => (
      current?.userId === assignedToUserId
        ? current
        : {
            userId: assignedToUserId,
            displayName: 'Selected assignee',
            email: '',
            roles: [],
            isActive: true,
          }
    ));
  }, [assignedToUserId]);

  const renewalsQuery = useRenewals({
    dueWindow: dueWindow ? (dueWindow as '45' | '60' | '90' | 'overdue') : undefined,
    status: status || undefined,
    assignedToUserId: assignedToUserId || undefined,
    lineOfBusiness: lineOfBusiness || undefined,
    urgency: urgency ? (urgency as 'overdue' | 'approaching') : undefined,
    sort,
    sortDir,
    page,
    pageSize,
  });
  const createRenewal = useCreateRenewal();

  const canCreateRenewals = currentUser?.roles.some((role) => ['DistributionUser', 'DistributionManager', 'Admin'].includes(role)) ?? false;
  const canChooseAssigneeAtCreate = currentUser?.roles.some((role) => ['DistributionManager', 'Admin'].includes(role)) ?? false;
  const hasActiveFilters = Boolean(dueWindow || status || lineOfBusiness || urgency || assignedToUserId);

  function updateParam(key: string, value: string | null) {
    const next = new URLSearchParams(searchParams);
    if (!value) {
      next.delete(key);
    } else {
      next.set(key, value);
    }
    next.set('page', '1');
    startTransition(() => setSearchParams(next));
  }

  function updatePage(nextPage: number) {
    const next = new URLSearchParams(searchParams);
    next.set('page', String(nextPage));
    startTransition(() => setSearchParams(next));
  }

  function openCreateModal() {
    setCreateForm({ policyId: '', lineOfBusiness: '' });
    setCreateAssignee(null);
    setCreateErrors({});
    setCreateServerError('');
    setCreateOpen(true);
  }

  async function saveCreate() {
    const nextErrors: Record<string, string> = {};
    if (!createForm.policyId.trim()) {
      nextErrors.policyId = 'Policy ID is required.';
    }

    if (Object.keys(nextErrors).length > 0) {
      setCreateErrors(nextErrors);
      return;
    }

    try {
      const dto: RenewalCreateDto = {
        policyId: createForm.policyId.trim(),
        lineOfBusiness: createForm.lineOfBusiness || null,
        assignedToUserId: canChooseAssigneeAtCreate ? createAssignee?.userId ?? null : null,
      };

      const renewal = await createRenewal.mutateAsync(dto);
      setCreateOpen(false);
      navigate(`/renewals/${renewal.id}`);
    } catch (error) {
      setCreateErrors(extractProblemFieldErrors(error));
      setCreateServerError(describeRenewalApiError(error));
    }
  }

  const renewals = renewalsQuery.data?.data ?? [];

  return (
    <DashboardLayout title="Renewals">
      <div className="space-y-6">
        <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <div>
            <h2 className="text-lg font-semibold text-text-primary">Renewal pipeline</h2>
            <p className="mt-1 text-sm text-text-muted">
              Filter due windows, surface overdue work, and hand off renewals with full policy context.
            </p>
          </div>
          {canCreateRenewals && (
            <button
              type="button"
              onClick={openCreateModal}
              className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90"
            >
              Create Renewal
            </button>
          )}
        </div>

        <Card>
          <CardHeader className="flex-col items-start gap-3 md:flex-row md:items-center">
            <div className="space-y-2">
              <CardTitle>Pipeline filters</CardTitle>
              <div className="flex flex-wrap gap-2" aria-label="Renewal due windows">
                {DUE_WINDOW_OPTIONS.map((option) => {
                  const active = dueWindow === option.value;
                  return (
                    <button
                      key={option.label}
                      type="button"
                      onClick={() => updateParam('dueWindow', option.value || null)}
                      className={[
                        'rounded-full border px-3 py-1.5 text-xs font-medium transition-colors',
                        active
                          ? 'border-nebula-violet/40 bg-nebula-violet/15 text-nebula-violet'
                          : 'border-surface-border bg-surface-card text-text-secondary hover:bg-surface-card-hover hover:text-text-primary',
                      ].join(' ')}
                    >
                      {option.label}
                    </button>
                  );
                })}
              </div>
            </div>

            {renewalsQuery.data && (
              <span className="rounded-full border border-surface-border bg-surface-card px-3 py-1 text-xs text-text-muted">
                {renewalsQuery.data.totalCount} renewal{renewalsQuery.data.totalCount === 1 ? '' : 's'}
              </span>
            )}
          </CardHeader>

          <div className="grid gap-3 lg:grid-cols-4">
            <label className="space-y-1.5">
              <span className="block text-xs font-medium text-text-secondary">Status</span>
              <select
                aria-label="Filter renewals by status"
                value={status}
                onChange={(event) => updateParam('status', event.target.value || null)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              >
                {STATUS_OPTIONS.map((option) => (
                  <option key={option.label} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>

            <label className="space-y-1.5">
              <span className="block text-xs font-medium text-text-secondary">Line of business</span>
              <select
                aria-label="Filter renewals by line of business"
                value={lineOfBusiness}
                onChange={(event) => updateParam('lineOfBusiness', event.target.value || null)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              >
                <option value="">All lines</option>
                {LINE_OF_BUSINESS_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>

            <label className="space-y-1.5">
              <span className="block text-xs font-medium text-text-secondary">Urgency</span>
              <select
                aria-label="Filter renewals by urgency"
                value={urgency}
                onChange={(event) => updateParam('urgency', event.target.value || null)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              >
                {URGENCY_OPTIONS.map((option) => (
                  <option key={option.label} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>

            <div className="lg:col-span-1">
              <AssigneePicker
                label="Assigned user"
                selectedUser={selectedUser}
                onSelect={(user) => {
                  setSelectedUser(user);
                  updateParam('assignedToUserId', user?.userId ?? null);
                }}
              />
            </div>

            <label className="space-y-1.5">
              <span className="block text-xs font-medium text-text-secondary">Sort</span>
              <select
                aria-label="Sort renewals"
                value={sort}
                onChange={(event) => updateParam('sort', event.target.value)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              >
                {RENEWAL_SORT_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>

            <label className="space-y-1.5">
              <span className="block text-xs font-medium text-text-secondary">Direction</span>
              <select
                aria-label="Renewal sort direction"
                value={sortDir}
                onChange={(event) => updateParam('sortDir', event.target.value)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              >
                <option value="asc">Ascending</option>
                <option value="desc">Descending</option>
              </select>
            </label>

            <label className="space-y-1.5">
              <span className="block text-xs font-medium text-text-secondary">Page size</span>
              <select
                aria-label="Renewal page size"
                value={String(pageSize)}
                onChange={(event) => updateParam('pageSize', event.target.value)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              >
                <option value="25">25</option>
                <option value="50">50</option>
                <option value="100">100</option>
              </select>
            </label>
          </div>
        </Card>

        {renewalsQuery.isLoading && <RenewalListSkeleton />}
        {renewalsQuery.isError && (
          <ErrorFallback
            message="Unable to load renewals."
            onRetry={() => renewalsQuery.refetch()}
          />
        )}

        {!renewalsQuery.isLoading && !renewalsQuery.isError && renewals.length === 0 && (
          <Card>
            <div className="rounded-xl border border-dashed border-surface-border bg-surface-card/50 px-4 py-8 text-center">
              <h3 className="text-sm font-semibold text-text-primary">
                {hasActiveFilters ? 'No renewals in this window' : 'No renewals in this pipeline yet'}
              </h3>
              <p className="mt-2 text-sm text-text-muted">
                {hasActiveFilters
                  ? 'Try widening the due window or clearing one of the active filters.'
                  : 'Create the first renewal from an expiring policy to start the pipeline.'}
              </p>
            </div>
          </Card>
        )}

        {!renewalsQuery.isLoading && !renewalsQuery.isError && renewals.length > 0 && (
          <>
            <div className="grid gap-4">
              {renewals.map((renewal) => (
                <Card key={renewal.id} className="p-0">
                  <div className="flex flex-col gap-4 p-5 lg:flex-row lg:items-start lg:justify-between">
                    <div className="space-y-3">
                      <div className="flex flex-wrap items-center gap-2">
                        <RenewalStatusBadge status={renewal.currentStatus} />
                        <RenewalUrgencyBadge urgency={renewal.urgency} />
                        <AccountStatusBadge status={renewal.accountStatus} />
                        <span className="rounded-full border border-surface-border bg-surface-card px-2 py-0.5 text-[11px] font-medium text-text-muted">
                          {renewal.policyNumber}
                        </span>
                      </div>

                      <div>
                        <Link
                          to={`/renewals/${renewal.id}`}
                          state={{ returnTo: `/renewals${searchParams.toString() ? `?${searchParams.toString()}` : ''}` }}
                          className="text-lg font-semibold text-text-primary transition-colors hover:text-nebula-violet"
                        >
                          {renewal.policyNumber}
                        </Link>
                        <div className="mt-1">
                          <AccountReference
                            accountId={renewal.accountId}
                            displayName={renewal.accountDisplayName ?? renewal.accountName}
                            status={renewal.accountStatus}
                            survivorAccountId={renewal.accountSurvivorId}
                            className="text-sm font-medium text-text-primary hover:text-nebula-violet"
                          />
                        </div>
                        <p className="mt-1 text-sm text-text-secondary">
                          {renewal.brokerName}
                          {renewal.brokerState ? ` · ${renewal.brokerState}` : ''}
                          {renewal.accountPrimaryState ? ` · ${renewal.accountPrimaryState}` : ''}
                        </p>
                      </div>
                    </div>

                    <div className="grid gap-3 text-sm text-text-secondary md:grid-cols-2 lg:min-w-[24rem]">
                      <DetailPair label="Line of business" value={getLineOfBusinessLabel(renewal.lineOfBusiness)} />
                      <DetailPair label="Assigned to" value={renewal.assignedUserDisplayName ?? 'Unassigned'} />
                      <DetailPair label="Expiration" value={formatDate(renewal.policyExpirationDate)} />
                      <DetailPair label="Target outreach" value={formatDate(renewal.targetOutreachDate)} />
                    </div>
                  </div>
                </Card>
              ))}
            </div>

            <div className="flex flex-col gap-3 rounded-xl border border-surface-border bg-surface-card/40 px-4 py-3 md:flex-row md:items-center md:justify-between">
              <p className="text-sm text-text-muted">
                Page {renewalsQuery.data?.page ?? 1} of {renewalsQuery.data?.totalPages ?? 1}
              </p>
              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={() => updatePage(Math.max(1, page - 1))}
                  disabled={page <= 1}
                  className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-sm text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary disabled:opacity-50"
                >
                  Previous
                </button>
                <button
                  type="button"
                  onClick={() => updatePage(Math.min(renewalsQuery.data?.totalPages ?? page, page + 1))}
                  disabled={page >= (renewalsQuery.data?.totalPages ?? page)}
                  className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-sm text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary disabled:opacity-50"
                >
                  Next
                </button>
              </div>
            </div>
          </>
        )}
      </div>

      <Modal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        title="Create Renewal"
        description="Enter the linked policy ID and create a new renewal in Identified. Policy search UI is still pending the Policy 360 surface."
      >
        <div className="space-y-4">
          <TextInput
            label="Policy ID"
            value={createForm.policyId}
            onChange={(event) => setCreateForm((current) => ({ ...current, policyId: event.target.value }))}
            placeholder="00000000-0000-0000-0000-000000000000"
            error={createErrors.policyId}
            required
          />

          <label className="space-y-1.5">
            <span className="block text-xs font-medium text-text-secondary">Line of business override</span>
            <select
              aria-label="Renewal line of business override"
              value={createForm.lineOfBusiness}
              onChange={(event) => setCreateForm((current) => ({ ...current, lineOfBusiness: event.target.value }))}
              className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
            >
              <option value="">Use policy line</option>
              {LINE_OF_BUSINESS_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>

          {canChooseAssigneeAtCreate && (
            <AssigneePicker
              label="Initial assignee"
              selectedUser={createAssignee}
              onSelect={setCreateAssignee}
              allowedRoles={getAllowedAssignmentRoles('Identified')}
            />
          )}

          {createServerError && (
            <div className="rounded-lg border border-status-error/35 bg-status-error/10 px-3 py-3 text-sm text-text-secondary">
              {createServerError}
            </div>
          )}

          <div className="flex flex-col gap-2 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={() => setCreateOpen(false)}
              className="rounded-lg border border-surface-border bg-surface-card px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={() => void saveCreate()}
              disabled={createRenewal.isPending}
              className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-60"
            >
              {createRenewal.isPending ? 'Creating…' : 'Create renewal'}
            </button>
          </div>
        </div>
      </Modal>
    </DashboardLayout>
  );
}

function DetailPair({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[11px] font-medium uppercase tracking-[0.16em] text-text-muted">{label}</p>
      <p className="mt-1 text-sm text-text-primary">{value}</p>
    </div>
  );
}

function RenewalListSkeleton() {
  return (
    <div className="space-y-4">
      {Array.from({ length: 3 }).map((_, index) => (
        <Card key={index} className="space-y-3">
          <Skeleton className="h-5 w-28" />
          <Skeleton className="h-6 w-56" />
          <div className="grid gap-3 md:grid-cols-2">
            <Skeleton className="h-12 w-full" />
            <Skeleton className="h-12 w-full" />
          </div>
        </Card>
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
