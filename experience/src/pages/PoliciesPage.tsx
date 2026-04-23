import { startTransition, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { FilePlus2, Upload } from 'lucide-react';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { Skeleton } from '@/components/ui/Skeleton';
import {
  PolicyFilterToolbar,
  PolicyListTable,
  usePolicies,
  type PolicyStatus,
} from '@/features/policies';

export default function PoliciesPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [searchDraft, setSearchDraft] = useState(searchParams.get('q') ?? '');

  const query = searchParams.get('q') ?? '';
  const status = (searchParams.get('status') ?? '') as '' | PolicyStatus;
  const lineOfBusiness = searchParams.get('lineOfBusiness') ?? '';
  const sort = searchParams.get('sort') ?? 'expirationDate:asc';
  const page = Number(searchParams.get('page') ?? '1');
  const pageSize = Number(searchParams.get('pageSize') ?? '25');

  const policiesQuery = usePolicies({
    query,
    status,
    lineOfBusiness,
    sort,
    page,
    pageSize,
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

  function submitSearch() {
    updateParam('q', searchDraft.trim() || null);
  }

  function updatePage(nextPage: number) {
    const next = new URLSearchParams(searchParams);
    next.set('page', String(nextPage));
    startTransition(() => setSearchParams(next));
  }

  return (
    <DashboardLayout title="Policies">
      <div className="space-y-6">
        <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <div>
            <h2 className="text-lg font-semibold text-text-primary">Policy book</h2>
            <p className="mt-1 text-sm text-text-muted">
              Review in-force, pending, cancelled, and expired policies with account and carrier context.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <Link
              to="/policies/import"
              className="inline-flex items-center gap-1.5 rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
            >
              <Upload size={16} />
              Import
            </Link>
            <Link
              to="/policies/new"
              className="inline-flex items-center gap-1.5 rounded-lg bg-nebula-violet px-3 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90"
            >
              <FilePlus2 size={16} />
              New Policy
            </Link>
          </div>
        </div>

        <Card>
          <CardHeader className="flex-col items-start gap-3 md:flex-row md:items-center">
            <CardTitle>Filters</CardTitle>
            {policiesQuery.data && (
              <span className="rounded-full border border-surface-border bg-surface-card px-3 py-1 text-xs text-text-muted">
                {policiesQuery.data.totalCount} policies
              </span>
            )}
          </CardHeader>
          <form
            onSubmit={(event) => {
              event.preventDefault();
              submitSearch();
            }}
          >
            <PolicyFilterToolbar
              query={searchDraft}
              status={status}
              lineOfBusiness={lineOfBusiness}
              sort={sort}
              onQueryChange={setSearchDraft}
              onStatusChange={(value) => updateParam('status', value || null)}
              onLineOfBusinessChange={(value) => updateParam('lineOfBusiness', value || null)}
              onSortChange={(value) => updateParam('sort', value)}
            />
          </form>
        </Card>

        <Card>
          {policiesQuery.isLoading && (
            <div className="space-y-3">
              <Skeleton className="h-14 w-full" />
              <Skeleton className="h-14 w-full" />
              <Skeleton className="h-14 w-full" />
            </div>
          )}
          {policiesQuery.error && (
            <ErrorFallback message="Unable to load policies." onRetry={() => policiesQuery.refetch()} />
          )}
          {policiesQuery.data && (
            <div className="space-y-4">
              <PolicyListTable policies={policiesQuery.data.data} />
              {policiesQuery.data.totalPages > 1 && (
                <div className="flex items-center justify-between text-sm">
                  <button
                    type="button"
                    onClick={() => updatePage(Math.max(1, page - 1))}
                    disabled={page <= 1}
                    className="rounded-lg border border-surface-border px-3 py-1.5 text-text-secondary disabled:opacity-50"
                  >
                    Previous
                  </button>
                  <span className="text-text-muted">
                    Page {page} of {policiesQuery.data.totalPages}
                  </span>
                  <button
                    type="button"
                    onClick={() => updatePage(Math.min(policiesQuery.data.totalPages, page + 1))}
                    disabled={page >= policiesQuery.data.totalPages}
                    className="rounded-lg border border-surface-border px-3 py-1.5 text-text-secondary disabled:opacity-50"
                  >
                    Next
                  </button>
                </div>
              )}
            </div>
          )}
        </Card>
      </div>
    </DashboardLayout>
  );
}
