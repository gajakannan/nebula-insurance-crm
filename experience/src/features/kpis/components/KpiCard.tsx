import { Skeleton } from '@/components/ui/Skeleton';

interface KpiCardProps {
  label: string;
  value: string | null;
  isLoading: boolean;
}

export function KpiCard({ label, value, isLoading }: KpiCardProps) {
  return (
    <article className="min-w-0">
      <p className="kpi-label text-xs font-medium uppercase tracking-wider">{label}</p>
      {isLoading ? (
        <Skeleton className="mt-2 h-10 w-24" />
      ) : (
        <p className="mt-1 text-4xl font-bold tracking-tight text-text-primary">
          {value ?? '—'}
        </p>
      )}
    </article>
  );
}
