import { useState } from 'react';
import { Link } from 'react-router-dom';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { Skeleton } from '@/components/ui/Skeleton';
import { BrokerStatusBadge, useBrokers } from '@/features/brokers';
import { useDebounce } from '@/hooks/useDebounce';
import type { BrokerDto, BrokerStatus } from '@/features/brokers';
import { ApiError } from '@/services/api';

const STATUS_OPTIONS = ['All', 'Active', 'Inactive', 'Pending'] as const;

export default function BrokerListPage() {
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('All');
  const [page, setPage] = useState(1);
  const debouncedSearch = useDebounce(search);

  const { data, isLoading, isError, error, refetch } = useBrokers({
    q: debouncedSearch,
    status: statusFilter,
    page,
  });

  return (
    <DashboardLayout title="Brokers">
      <div className="space-y-6">
        <div className="flex items-center justify-end">
          <Link
            to="/brokers/new"
            className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90"
          >
            New Broker
          </Link>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Broker Directory</CardTitle>
          </CardHeader>

          <div className="mb-4 flex flex-col gap-3 sm:flex-row">
            <label htmlFor="broker-directory-search" className="sr-only">
              Search brokers
            </label>
            <input
              id="broker-directory-search"
              type="text"
              placeholder="Search by name or license..."
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
                setPage(1);
              }}
              className="flex-1 rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary placeholder:text-text-muted focus:outline-none focus:ring-1 focus:ring-nebula-violet"
            />
            <label htmlFor="broker-directory-status" className="sr-only">
              Filter brokers by status
            </label>
            <select
              id="broker-directory-status"
              value={statusFilter}
              onChange={(e) => {
                setStatusFilter(e.target.value);
                setPage(1);
              }}
              className="rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
            >
              {STATUS_OPTIONS.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </div>

          {isLoading && <BrokerListSkeleton />}
          {isError && (
            error instanceof ApiError && error.status === 403
              ? <p className="py-8 text-center text-sm text-text-muted">You don't have permission to view the broker directory.</p>
              : <ErrorFallback message="Unable to load brokers." onRetry={() => refetch()} />
          )}
          {data && data.data.length === 0 && (
            <div className="py-8 text-center text-sm text-text-muted">
              No brokers found.
              {(debouncedSearch || statusFilter !== 'All') && (
                <button
                  onClick={() => {
                    setSearch('');
                    setStatusFilter('All');
                    setPage(1);
                  }}
                  className="ml-2 text-nebula-violet hover:underline"
                >
                  Clear filters
                </button>
              )}
            </div>
          )}
          {data && data.data.length > 0 && (
            <>
              {/* Desktop table */}
              <div className="hidden md:block">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-surface-border text-left text-xs font-medium uppercase tracking-wider text-text-muted">
                      <th className="pb-3 pr-4">Name</th>
                      <th className="pb-3 pr-4">License</th>
                      <th className="pb-3 pr-4">State</th>
                      <th className="pb-3 pr-4">Status</th>
                      <th className="pb-3 pr-4">Email</th>
                      <th className="pb-3">Phone</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-surface-border">
                    {data.data.map((broker) => (
                      <BrokerRow key={broker.id} broker={broker} />
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Mobile cards */}
              <div className="space-y-3 md:hidden">
                {data.data.map((broker) => (
                  <BrokerMobileCard key={broker.id} broker={broker} />
                ))}
              </div>

              {/* Pagination */}
              {data.totalPages > 1 && (
                <div className="mt-4 flex items-center justify-between border-t border-surface-border pt-4">
                  <button
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    disabled={page <= 1}
                    className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-xs font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary disabled:opacity-50"
                  >
                    Previous
                  </button>
                  <span className="text-xs text-text-muted">
                    Page {data.page} of {data.totalPages}
                  </span>
                  <button
                    onClick={() => setPage((p) => Math.min(data.totalPages, p + 1))}
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

function BrokerRow({ broker }: { broker: BrokerDto }) {
  return (
    <tr className="text-text-secondary">
      <td className="py-3 pr-4">
        <Link
          to={`/brokers/${broker.id}`}
          className="font-medium text-text-primary hover:text-nebula-violet"
        >
          {broker.legalName}
        </Link>
      </td>
      <td className="py-3 pr-4 font-mono text-xs">{broker.licenseNumber}</td>
      <td className="py-3 pr-4">{broker.state}</td>
      <td className="py-3 pr-4">
        <BrokerStatusBadge status={broker.status} />
      </td>
      <td className="py-3 pr-4">
        <MaskedField value={broker.email} status={broker.status} />
      </td>
      <td className="py-3">
        <MaskedField value={broker.phone} status={broker.status} />
      </td>
    </tr>
  );
}

function BrokerMobileCard({ broker }: { broker: BrokerDto }) {
  return (
    <Link
      to={`/brokers/${broker.id}`}
      className="block rounded-lg border border-surface-border p-4 transition-colors hover:bg-surface-highlight"
    >
      <div className="flex items-start justify-between">
        <div>
          <p className="font-medium text-text-primary">{broker.legalName}</p>
          <p className="mt-0.5 font-mono text-xs text-text-muted">{broker.licenseNumber}</p>
        </div>
        <BrokerStatusBadge status={broker.status} />
      </div>
      <div className="mt-2 flex gap-4 text-xs text-text-secondary">
        <span>{broker.state}</span>
        <MaskedField value={broker.email} status={broker.status} />
      </div>
    </Link>
  );
}

function MaskedField({ value, status }: { value: string | null; status: BrokerStatus }) {
  if (value) return <span>{value}</span>;
  if (status === 'Inactive') return <span className="text-text-muted italic">Masked</span>;
  return <span className="text-text-muted">—</span>;
}

function BrokerListSkeleton() {
  return (
    <div className="space-y-3">
      {Array.from({ length: 5 }).map((_, i) => (
        <Skeleton key={i} className="h-12 w-full" />
      ))}
    </div>
  );
}
