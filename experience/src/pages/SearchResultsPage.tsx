import { startTransition } from 'react';
import { useSearchParams } from 'react-router-dom';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { TextInput } from '@/components/ui/TextInput';
import {
  SavedViewsDrawer,
  SearchFilters,
  SearchResultsList,
  useGlobalSearch,
  type SavedView,
  type SearchFilterState,
} from '@/features/search';

export default function SearchResultsPage() {
  const [searchParams, setSearchParams] = useSearchParams();

  const q = searchParams.get('q') ?? '';
  const filters: SearchFilterState = {
    objectType: searchParams.get('objectType') ?? '',
    status: searchParams.get('status') ?? '',
    region: searchParams.get('region') ?? '',
    lineOfBusiness: searchParams.get('lineOfBusiness') ?? '',
    sort: searchParams.get('sort') ?? 'relevance',
  };
  const page = Number(searchParams.get('page') ?? '1');

  const searchQuery = useGlobalSearch({
    q,
    objectTypes: filters.objectType ? [filters.objectType] : undefined,
    status: filters.status || undefined,
    region: filters.region || undefined,
    lineOfBusiness: filters.lineOfBusiness || undefined,
    sort: filters.sort,
    page,
    pageSize: 20,
  });

  function setParam(key: string, value: string | null, resetPage = true) {
    const next = new URLSearchParams(searchParams);
    if (!value) next.delete(key);
    else next.set(key, value);
    if (resetPage && key !== 'page') next.set('page', '1');
    startTransition(() => setSearchParams(next));
  }

  function applySavedView(view: SavedView) {
    const c = (view.criteria ?? {}) as Partial<SearchFilterState> & { q?: string };
    const next = new URLSearchParams();
    if (c.q) next.set('q', c.q);
    if (c.objectType) next.set('objectType', c.objectType);
    if (c.status) next.set('status', c.status);
    if (c.region) next.set('region', c.region);
    if (c.lineOfBusiness) next.set('lineOfBusiness', c.lineOfBusiness);
    next.set('sort', c.sort ?? 'relevance');
    next.set('page', '1');
    startTransition(() => setSearchParams(next));
  }

  return (
    <DashboardLayout title="Search">
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-[1fr_18rem]">
        <div className="space-y-4">
          <Card>
            <CardHeader className="flex-col items-start gap-2">
              <CardTitle>Global search</CardTitle>
              <p className="text-xs text-text-muted">
                Search across accounts, brokers, policies, submissions, renewals, and tasks. Results and counts reflect only records you can access.
              </p>
            </CardHeader>
            <div className="space-y-3 p-4 pt-0">
              <TextInput
                label="Search"
                value={q}
                placeholder="Type at least 2 characters…"
                onChange={(e) => setParam('q', e.target.value)}
              />
              <SearchFilters filters={filters} facets={searchQuery.data?.facets} onChange={(k, v) => setParam(k, v)} />
            </div>
          </Card>

          <SearchResultsList
            query={q}
            data={searchQuery.data}
            isLoading={searchQuery.isLoading && q.trim().length >= 2}
            isError={searchQuery.isError}
            onRetry={() => searchQuery.refetch()}
            onPage={(p) => setParam('page', String(p), false)}
          />
        </div>

        <aside>
          <Card>
            <div className="p-4">
              <SavedViewsDrawer viewType="Search" currentCriteria={{ ...filters, q }} onApply={applySavedView} />
            </div>
          </Card>
        </aside>
      </div>
    </DashboardLayout>
  );
}
