import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/Badge';
import { Skeleton } from '@/components/ui/Skeleton';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import type { GlobalSearchResponse } from '../types';

interface SearchResultsListProps {
  query: string;
  data?: GlobalSearchResponse;
  isLoading: boolean;
  isError: boolean;
  onRetry: () => void;
  onPage: (page: number) => void;
}

export function SearchResultsList({ query, data, isLoading, isError, onRetry, onPage }: SearchResultsListProps) {
  if (query.trim().length < 2) {
    return <p className="text-sm text-text-muted">Enter at least 2 characters to search.</p>;
  }
  if (isLoading) {
    return (
      <div className="space-y-2" aria-busy="true" aria-label="Loading search results">
        {Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-16 w-full" />)}
      </div>
    );
  }
  if (isError || !data) {
    return <ErrorFallback message="Search failed. Please retry." onRetry={onRetry} />;
  }
  if (data.data.length === 0) {
    return <p className="text-sm text-text-muted">No results match “{query}”. Counts reflect records you can access.</p>;
  }

  return (
    <div className="space-y-4">
      <p className="text-xs text-text-muted">
        {data.totalCount} authorized result{data.totalCount === 1 ? '' : 's'} · page {data.page} of {Math.max(data.totalPages, 1)}
      </p>

      <ul className="divide-y divide-surface-border rounded-lg border border-surface-border">
        {data.data.map((r) => (
          <li key={`${r.objectType}:${r.objectId}`} className="bg-surface-card p-3 hover:bg-surface-card-hover">
            <Link to={r.targetUrl} className="block">
              <div className="flex items-center gap-2">
                <Badge variant="info">{r.objectType}</Badge>
                <span className="font-medium text-text-primary">{r.title}</span>
                {r.status && <span className="text-xs text-text-muted">· {r.status}</span>}
              </div>
              {r.subtitle && <p className="mt-1 text-sm text-text-secondary">{r.subtitle}</p>}
              {r.snippet && r.snippet !== r.subtitle && <p className="mt-1 text-xs text-text-muted">{r.snippet}</p>}
              <div className="mt-1 flex flex-wrap gap-3 text-xs text-text-muted">
                {r.ownerDisplayName && <span>Owner: {r.ownerDisplayName}</span>}
                {r.region && <span>Region: {r.region}</span>}
                {r.lineOfBusiness && <span>LOB: {r.lineOfBusiness}</span>}
              </div>
            </Link>
          </li>
        ))}
      </ul>

      {data.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <button
            type="button"
            disabled={data.page <= 1}
            onClick={() => onPage(data.page - 1)}
            className="rounded-md border border-surface-border px-3 py-1 text-sm text-text-secondary disabled:opacity-40"
          >
            Previous
          </button>
          <button
            type="button"
            disabled={data.page >= data.totalPages}
            onClick={() => onPage(data.page + 1)}
            className="rounded-md border border-surface-border px-3 py-1 text-sm text-text-secondary disabled:opacity-40"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}
