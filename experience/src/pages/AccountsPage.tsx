import { startTransition } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { Skeleton } from '@/components/ui/Skeleton';
import { AccountReference, AccountStatusBadge, useAccountList } from '@/features/accounts';

const STATUS_OPTIONS = [
  { value: '', label: 'All active/inactive' },
  { value: 'Active', label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
  { value: 'Merged', label: 'Merged' },
  { value: 'Deleted', label: 'Deleted' },
];

export default function AccountsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const q = searchParams.get('q') ?? '';
  const status = searchParams.get('status') ?? '';
  const region = searchParams.get('region') ?? '';
  const includeRemoved = searchParams.get('includeRemoved') === 'true';
  const page = Number(searchParams.get('page') ?? '1');

  const accountsQuery = useAccountList({
    query: q || undefined,
    status: status || undefined,
    region: region || undefined,
    includeRemoved,
    includeSummary: true,
    page,
    pageSize: 25,
  });

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
    <DashboardLayout title="Accounts">
      <div className="space-y-6">
        <div className="flex items-center justify-end">
          <Link
            to="/accounts/new"
            className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90"
          >
            New Account
          </Link>
        </div>

        <Card>
          <CardHeader className="flex-col items-start gap-2 md:flex-row md:items-center">
            <div>
              <CardTitle>Account directory</CardTitle>
              <p className="mt-1 text-xs text-text-muted">
                Search insureds, review lifecycle state, and jump into Account 360.
              </p>
            </div>
            {accountsQuery.data && (
              <span className="rounded-full border border-surface-border bg-surface-card px-3 py-1 text-xs text-text-muted">
                {accountsQuery.data.totalCount} account{accountsQuery.data.totalCount === 1 ? '' : 's'}
              </span>
            )}
          </CardHeader>

          <div className="mb-5 grid gap-3 lg:grid-cols-4">
            <label className="space-y-1.5 lg:col-span-2">
              <span className="block text-xs font-medium text-text-secondary">Search</span>
              <input
                aria-label="Search accounts"
                type="text"
                value={q}
                onChange={(event) => updateParam('q', event.target.value || null)}
                placeholder="Name, legal name, or tax ID"
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary placeholder:text-text-muted focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              />
            </label>

            <label className="space-y-1.5">
              <span className="block text-xs font-medium text-text-secondary">Status</span>
              <select
                aria-label="Filter accounts by status"
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
              <span className="block text-xs font-medium text-text-secondary">Region</span>
              <input
                aria-label="Filter accounts by region"
                type="text"
                value={region}
                onChange={(event) => updateParam('region', event.target.value || null)}
                placeholder="Midwest, Northeast..."
                className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary placeholder:text-text-muted focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              />
            </label>
          </div>

          <label className="mb-5 inline-flex items-center gap-2 text-sm text-text-secondary">
            <input
              type="checkbox"
              checked={includeRemoved}
              onChange={(event) => updateParam('includeRemoved', event.target.checked ? 'true' : null)}
              className="h-4 w-4 rounded border-surface-border bg-surface-card text-nebula-violet"
            />
            Include merged and deleted accounts
          </label>

          {accountsQuery.isLoading && <AccountListSkeleton />}
          {accountsQuery.isError && (
            <ErrorFallback
              message="Unable to load accounts."
              onRetry={() => accountsQuery.refetch()}
            />
          )}

          {!accountsQuery.isLoading && !accountsQuery.isError && accountsQuery.data?.data.length === 0 && (
            <div className="rounded-xl border border-dashed border-surface-border bg-surface-card/40 px-4 py-8 text-center text-sm text-text-muted">
              No accounts match the current filters.
            </div>
          )}

          {!accountsQuery.isLoading && !accountsQuery.isError && accountsQuery.data && accountsQuery.data.data.length > 0 && (
            <>
              <div className="hidden lg:block">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-surface-border text-left text-xs font-medium uppercase tracking-wider text-text-muted">
                      <th className="pb-3 pr-4">Account</th>
                      <th className="pb-3 pr-4">Status</th>
                      <th className="pb-3 pr-4">Broker of Record</th>
                      <th className="pb-3 pr-4">Region</th>
                      <th className="pb-3 pr-4">Policies</th>
                      <th className="pb-3 pr-4">Submissions</th>
                      <th className="pb-3 pr-4">Renewals</th>
                      <th className="pb-3">Last activity</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-surface-border">
                    {accountsQuery.data.data.map((account) => (
                      <tr key={account.id} className="text-text-secondary">
                        <td className="py-3 pr-4">
                          <div className="space-y-1">
                            <AccountReference
                              accountId={account.id}
                              displayName={account.displayName}
                              status={account.status}
                              survivorAccountId={null}
                              className="font-medium text-text-primary hover:text-nebula-violet"
                            />
                            {account.legalName && (
                              <p className="text-xs text-text-muted">{account.legalName}</p>
                            )}
                          </div>
                        </td>
                        <td className="py-3 pr-4">
                          <AccountStatusBadge status={account.status} />
                        </td>
                        <td className="py-3 pr-4">{account.brokerOfRecordName ?? 'Unassigned'}</td>
                        <td className="py-3 pr-4">{account.region ?? '—'}</td>
                        <td className="py-3 pr-4">{account.activePolicyCount ?? '—'}</td>
                        <td className="py-3 pr-4">{account.openSubmissionCount ?? '—'}</td>
                        <td className="py-3 pr-4">{account.renewalDueCount ?? '—'}</td>
                        <td className="py-3">{account.lastActivityAt ? formatDate(account.lastActivityAt) : '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="space-y-3 lg:hidden">
                {accountsQuery.data.data.map((account) => (
                  <Link
                    key={account.id}
                    to={`/accounts/${account.id}`}
                    className="block rounded-lg border border-surface-border p-4 transition-colors hover:bg-surface-highlight"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div className="space-y-1">
                        <p className="font-semibold text-text-primary">{account.displayName}</p>
                        <p className="text-xs text-text-muted">{account.brokerOfRecordName ?? 'Unassigned'}</p>
                      </div>
                      <AccountStatusBadge status={account.status} />
                    </div>
                    <div className="mt-3 grid grid-cols-2 gap-2 text-xs text-text-muted">
                      <span>Region: {account.region ?? '—'}</span>
                      <span>Policies: {account.activePolicyCount ?? '—'}</span>
                      <span>Submissions: {account.openSubmissionCount ?? '—'}</span>
                      <span>Renewals: {account.renewalDueCount ?? '—'}</span>
                    </div>
                  </Link>
                ))}
              </div>

              {accountsQuery.data.totalPages > 1 && (
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
                    Page {accountsQuery.data.page} of {accountsQuery.data.totalPages}
                  </span>
                  <button
                    type="button"
                    onClick={() => updatePage(Math.min(accountsQuery.data.totalPages, page + 1))}
                    disabled={page >= accountsQuery.data.totalPages}
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

function AccountListSkeleton() {
  return (
    <div className="space-y-3">
      {Array.from({ length: 5 }).map((_, index) => (
        <Skeleton key={index} className="h-12 w-full" />
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
