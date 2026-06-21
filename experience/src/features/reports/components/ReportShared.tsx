import { Link } from 'react-router-dom';
import type { GlobalSearchResult } from '@/features/search/types';
import type { CountByKey } from '../types';

export function StatTile({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg border border-surface-border bg-surface-card p-4">
      <p className="text-2xl font-semibold text-text-primary">{value}</p>
      <p className="text-xs text-text-muted">{label}</p>
    </div>
  );
}

export function CountList({ title, items }: { title: string; items: CountByKey[] }) {
  return (
    <div>
      <h3 className="text-sm font-semibold text-text-primary">{title}</h3>
      {items.length === 0 ? (
        <p className="mt-1 text-xs text-text-muted">None</p>
      ) : (
        <ul className="mt-1 space-y-1">
          {items.map((i) => (
            <li key={i.key} className="flex justify-between text-sm text-text-secondary">
              <span>{i.label ?? i.key}</span>
              <span className="text-text-muted">{i.count}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export function DrilldownList({ title, rows }: { title: string; rows: GlobalSearchResult[] }) {
  return (
    <div>
      <h3 className="text-sm font-semibold text-text-primary">{title}</h3>
      {rows.length === 0 ? (
        <p className="mt-1 text-xs text-text-muted">No records.</p>
      ) : (
        <ul className="mt-1 divide-y divide-surface-border rounded-md border border-surface-border">
          {rows.map((r) => (
            <li key={`${r.objectType}:${r.objectId}`} className="bg-surface-card p-2 hover:bg-surface-card-hover">
              <Link to={r.targetUrl} className="block text-sm text-text-primary">
                {r.objectType} — {r.subtitle ?? r.title}
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
