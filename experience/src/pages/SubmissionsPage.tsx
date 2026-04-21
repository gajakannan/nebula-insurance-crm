import { startTransition, useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { Skeleton } from '@/components/ui/Skeleton';
import { AccountReference, AccountStatusBadge } from '@/features/accounts';
import { useBrokers } from '@/features/brokers';
import { AssigneePicker, type UserSummaryDto } from '@/features/tasks';
import {
  SubmissionStatusBadge,
  getLineOfBusinessLabel,
  SUBMISSION_SORT_OPTIONS,
  SUBMISSION_STATUS_META,
  useAccounts,
  useSubmissions,
} from '@/features/submissions';

export default function SubmissionsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [selectedUser, setSelectedUser] = useState<UserSummaryDto | null>(null);

  const status = searchParams.get('status') ?? '';
  const brokerId = searchParams.get('brokerId') ?? '';
  const accountId = searchParams.get('accountId') ?? '';
  const lineOfBusiness = searchParams.get('lineOfBusiness') ?? '';
  const assignedToUserId = searchParams.get('assignedToUserId') ?? '';
  const staleParam = searchParams.get('stale') ?? '';
  const sort = (searchParams.get('sort') ?? 'createdAt') as 'createdAt' | 'effectiveDate' | 'accountName' | 'currentStatus';
  const sortDir = (searchParams.get('sortDir') ?? 'desc') as 'asc' | 'desc';
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

  const { data, isLoading, isError, refetch } = useSubmissions({
    status: status || undefined,
    brokerId: brokerId || undefined,
    accountId: accountId || undefined,
    lineOfBusiness: lineOfBusiness || undefined,
    assignedToUserId: assignedToUserId || undefined,
    stale: staleParam === 'true' ? true : staleParam === 'false' ? false : undefined,
    sort,
    sortDir,
    page,
    pageSize,
  });
  const { data: accounts = [] } = useAccounts();
  const brokersQuery = useBrokers({ page: 1, pageSize: 100 });
  const brokers = brokersQuery.data?.data ?? [];

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

  return (
    <DashboardLayout title="Submissions">
      <div className="space-y-6">
        <div className="flex items-center justify-end">
          <Link
            to="/submissions/new"
            className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90"
          >
            New Submission
          </Link>
        </div>

        <Card>
          <CardHeader className="flex-col items-start gap-2 md:flex-row md:items-center">
            <div>
              <CardTitle>Submission pipeline</CardTitle>
              <p className="mt-1 text-xs text-text-muted">
                Intake visibility across status, ownership, and stale follow-up.
              </p>
            </div>
            {data && (
              <span className="rounded-full border border-surface-border bg-surface-card px-3 py-1 text-xs text-text-muted">
                {data.totalCount} submission{data.totalCount === 1 ? '' : 's'}
              </span>
            )}
          </CardHeader>

          <div className="mb-5 grid gap-3 lg:grid-cols-4">
            <label className="space-y-1.5">
              <span className="block text-xs font-medium text-text-secondary">Status</span>
              <select
                aria-label="Filter submissions by status"
                value={status}
                onChange={(event) => updateParam('status', event.target.value || null)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              >
                <option value="">All statuses</option>
                {SUBMISSION_STATUS_META.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>

            <label className="space-y-1.5">
              <span className="block text-xs font-medium text-text-secondary">Broker</span>
              <select
                aria-label="Filter submissions by broker"
                value={brokerId}
                onChange={(event) => updateParam('brokerId', event.target.value || null)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              >
                <option value="">All brokers</option>
                {brokers.map((broker) => (
                  <option key={broker.id} value={broker.id}>
                    {broker.legalName}
                  </option>
                ))}
              </select>
            </label>

            <label className="space-y-1.5">
              <span className="block text-xs font-medium text-text-secondary">Account</span>
              <select
                aria-label="Filter submissions by account"
                value={accountId}
                onChange={(event) => updateParam('accountId', event.target.value || null)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              >
                <option value="">All accounts</option>
                {accounts.map((account) => (
                  <option key={account.id} value={account.id}>
                    {account.name}
                  </option>
                ))}
              </select>
            </label>

            <label className="space-y-1.5">
              <span className="block text-xs font-medium text-text-secondary">Line of business</span>
              <select
                aria-label="Filter submissions by line of business"
                value={lineOfBusiness}
                onChange={(event) => updateParam('lineOfBusiness', event.target.value || null)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              >
                <option value="">All lines</option>
                <option value="Property">Property</option>
                <option value="GeneralLiability">General Liability</option>
                <option value="CommercialAuto">Commercial Auto</option>
                <option value="WorkersCompensation">Workers&apos; Compensation</option>
                <option value="ProfessionalLiability">Professional Liability / E&amp;O</option>
                <option value="Marine">Marine / Inland Marine</option>
                <option value="Umbrella">Umbrella / Excess</option>
                <option value="Surety">Surety / Bond</option>
                <option value="Cyber">Cyber Liability</option>
                <option value="DirectorsOfficers">Directors &amp; Officers</option>
              </select>
            </label>

            <div className="lg:col-span-2">
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
              <span className="block text-xs font-medium text-text-secondary">Stale</span>
              <select
                aria-label="Filter submissions by stale flag"
                value={staleParam}
                onChange={(event) => updateParam('stale', event.target.value || null)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              >
                <option value="">All</option>
                <option value="true">Only stale</option>
                <option value="false">Only fresh</option>
              </select>
            </label>

            <label className="space-y-1.5">
              <span className="block text-xs font-medium text-text-secondary">Sort</span>
              <select
                aria-label="Sort submissions"
                value={sort}
                onChange={(event) => updateParam('sort', event.target.value)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              >
                {SUBMISSION_SORT_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>

            <label className="space-y-1.5">
              <span className="block text-xs font-medium text-text-secondary">Direction</span>
              <select
                aria-label="Sort direction"
                value={sortDir}
                onChange={(event) => updateParam('sortDir', event.target.value)}
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              >
                <option value="desc">Descending</option>
                <option value="asc">Ascending</option>
              </select>
            </label>

            <label className="space-y-1.5">
              <span className="block text-xs font-medium text-text-secondary">Page size</span>
              <select
                aria-label="Submission page size"
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

          {isLoading && <SubmissionListSkeleton />}
          {isError && (
            <ErrorFallback
              message="Unable to load submissions."
              onRetry={() => refetch()}
            />
          )}
          {data && data.data.length === 0 && (
            <div className="py-8 text-center text-sm text-text-muted">
              No submissions found matching your filters.
            </div>
          )}
          {data && data.data.length > 0 && (
            <>
              <div className="hidden lg:block">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-surface-border text-left text-xs font-medium uppercase tracking-wider text-text-muted">
                      <th className="pb-3 pr-4">Status</th>
                      <th className="pb-3 pr-4">Account</th>
                      <th className="pb-3 pr-4">Broker</th>
                      <th className="pb-3 pr-4">LOB</th>
                      <th className="pb-3 pr-4">Effective</th>
                      <th className="pb-3 pr-4">Assigned to</th>
                      <th className="pb-3 pr-4">Created</th>
                      <th className="pb-3">Stale</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-surface-border">
                    {data.data.map((submission) => (
                      <tr key={submission.id} className="text-text-secondary">
                        <td className="py-3 pr-4">
                          <SubmissionStatusBadge status={submission.currentStatus} />
                        </td>
                        <td className="py-3 pr-4">
                          <AccountReference
                            accountId={submission.accountId}
                            displayName={submission.accountDisplayName}
                            status={submission.accountStatus}
                            survivorAccountId={submission.accountSurvivorId}
                            className="font-medium text-text-primary hover:text-nebula-violet"
                          />
                        </td>
                        <td className="py-3 pr-4">{submission.brokerName}</td>
                        <td className="py-3 pr-4">{getLineOfBusinessLabel(submission.lineOfBusiness)}</td>
                        <td className="py-3 pr-4">{formatDate(submission.effectiveDate)}</td>
                        <td className="py-3 pr-4">{submission.assignedToDisplayName ?? 'Unassigned'}</td>
                        <td className="py-3 pr-4">{formatDate(submission.createdAt)}</td>
                        <td className="py-3">
                          {submission.isStale ? (
                            <span className="rounded-full border border-status-warning/35 bg-status-warning/20 px-2 py-0.5 text-xs font-medium text-text-primary">
                              Stale
                            </span>
                          ) : (
                            <span className="text-xs text-text-muted">Fresh</span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="space-y-3 lg:hidden">
                {data.data.map((submission) => (
                  <Link
                    key={submission.id}
                    to={`/submissions/${submission.id}`}
                    className="block rounded-lg border border-surface-border p-4 transition-colors hover:bg-surface-highlight"
                  >
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <SubmissionStatusBadge status={submission.currentStatus} />
                      <div className="flex items-center gap-2">
                        <AccountStatusBadge status={submission.accountStatus} />
                        {submission.isStale && (
                          <span className="rounded-full border border-status-warning/35 bg-status-warning/20 px-2 py-0.5 text-xs font-medium text-text-primary">
                            Stale
                          </span>
                        )}
                      </div>
                    </div>
                    <p className="mt-3 text-sm font-semibold text-text-primary">{submission.accountDisplayName}</p>
                    <p className="mt-1 text-xs text-text-secondary">{submission.brokerName}</p>
                    <div className="mt-3 grid grid-cols-2 gap-2 text-xs text-text-muted">
                      <span>{getLineOfBusinessLabel(submission.lineOfBusiness)}</span>
                      <span>{formatDate(submission.effectiveDate)}</span>
                      <span>{submission.assignedToDisplayName ?? 'Unassigned'}</span>
                      <span>{formatDate(submission.createdAt)}</span>
                    </div>
                  </Link>
                ))}
              </div>

              {data.totalPages > 1 && (
                <div className="mt-4 flex items-center justify-between border-t border-surface-border pt-4">
                  <button
                    type="button"
                    onClick={() => updatePage(Math.max(1, page - 1))}
                    disabled={page <= 1}
                    className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-xs font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary disabled:opacity-50"
                  >
                    Previous
                  </button>
                  <span className="text-xs text-text-muted">
                    Page {data.page} of {data.totalPages}
                  </span>
                  <button
                    type="button"
                    onClick={() => updatePage(Math.min(data.totalPages, page + 1))}
                    disabled={page >= data.totalPages}
                    className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-xs font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary disabled:opacity-50"
                  >
                    Next
                  </button>
                </div>
              )}
            </>
          )}
        </Card>
      </div>
    </DashboardLayout>
  );
}

function SubmissionListSkeleton() {
  return (
    <div className="space-y-3">
      {Array.from({ length: 5 }).map((_, index) => (
        <Skeleton key={index} className="h-12 w-full" />
      ))}
    </div>
  );
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(value));
}
