import { useDashboardKpis } from '../hooks/useDashboardKpis';
import { formatPercent } from '@/lib/format';
import { cn } from '@/lib/utils';
import { KpiCard } from './KpiCard';

interface KpiCardsRowProps {
  periodDays: number;
  className?: string;
}

export function KpiCardsRow({ periodDays, className }: KpiCardsRowProps) {
  const { data, isLoading, isError } = useDashboardKpis(periodDays);

  if (isError) {
    // Show dashes for all KPIs on error — silent degradation per plan
  }

  const kpis = [
    {
      label: 'Active Brokers',
      value: data ? String(data.activeBrokers) : null,
    },
    {
      label: 'Open Submissions',
      value: data ? String(data.openSubmissions) : null,
    },
    {
      label: 'Renewal Rate',
      value: data?.renewalRate != null ? formatPercent(data.renewalRate) : null,
    },
    {
      label: 'Avg Turnaround',
      value:
        data?.avgTurnaroundDays != null
          ? `${data.avgTurnaroundDays.toFixed(1)} days`
          : null,
    },
  ];

  return (
    <section className={cn('canvas-section canvas-zone-default', className)} aria-label="KPI band">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
      {kpis.map((kpi) => (
        <KpiCard
          key={kpi.label}
          label={kpi.label}
          value={kpi.value}
          isLoading={isLoading}
        />
      ))}
      </div>
    </section>
  );
}
