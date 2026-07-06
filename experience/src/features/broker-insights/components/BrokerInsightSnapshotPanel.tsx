import type { BrokerInsightSnapshot } from '../types';

export function BrokerInsightSnapshotPanel({ snapshot }: { snapshot: BrokerInsightSnapshot | undefined }) {
  if (!snapshot) {
    return <p className="text-sm text-text-muted">Review snapshot appears after a broker is selected.</p>;
  }

  return (
    <div className="grid gap-4 lg:grid-cols-2">
      <SnapshotList title="Highlights" items={snapshot.highlights} />
      <SnapshotList title="Risks" items={snapshot.risks} />
      <div className="lg:col-span-2 grid gap-3 sm:grid-cols-2">
        <p className="rounded-md border border-surface-border bg-surface-card p-3 text-sm text-text-secondary">
          {snapshot.activitySummary ?? 'No activity summary available.'}
        </p>
        <p className="rounded-md border border-surface-border bg-surface-card p-3 text-sm text-text-secondary">
          {snapshot.opportunitySummary ?? 'No opportunity summary available.'}
        </p>
      </div>
    </div>
  );
}

function SnapshotList({ title, items }: { title: string; items: BrokerInsightSnapshot['highlights'] }) {
  return (
    <div>
      <h3 className="text-sm font-semibold text-text-primary">{title}</h3>
      {items.length === 0 ? (
        <p className="mt-2 text-sm text-text-muted">None</p>
      ) : (
        <ul className="mt-2 space-y-2">
          {items.map((item) => (
            <li key={`${item.label}:${item.value}`} className="rounded-md border border-surface-border bg-surface-card p-3">
              <p className="text-sm font-medium text-text-primary">{item.label}</p>
              <p className="text-sm text-text-secondary">{item.value}</p>
              <p className="text-xs text-text-muted">Sources {item.sourceRecordCount}</p>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
