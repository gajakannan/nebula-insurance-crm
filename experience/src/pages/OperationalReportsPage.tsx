import { startTransition } from 'react';
import { useSearchParams } from 'react-router-dom';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Card } from '@/components/ui/Card';
import { Tabs } from '@/components/ui/Tabs';
import {
  ReportControls,
  WorkflowAgingReportView,
  WorkloadReportView,
  type OperationalReportParams,
} from '@/features/reports';

const TABS = ['Workload', 'Workflow aging'];

export default function OperationalReportsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const activeTab = searchParams.get('report') === 'aging' ? 'Workflow aging' : 'Workload';

  const params: OperationalReportParams = {
    region: searchParams.get('region') || undefined,
    lineOfBusiness: searchParams.get('lineOfBusiness') || undefined,
    workflowType: searchParams.get('workflowType') || undefined,
    asOf: searchParams.get('asOf') || undefined,
  };

  function setParam(key: string, value: string | null) {
    const next = new URLSearchParams(searchParams);
    if (!value) next.delete(key);
    else next.set(key, value);
    startTransition(() => setSearchParams(next));
  }

  return (
    <DashboardLayout title="Operational reports">
      <div className="space-y-4">
        <Card>
          <div className="space-y-1 p-4">
            <h1 className="text-base font-semibold text-text-primary">Operational reporting</h1>
            <p className="text-xs text-text-muted">
              Daily workload and workflow aging across submissions, renewals, and tasks you can access.
            </p>
            <div className="pt-2">
              <ReportControls params={params} onChange={(k, v) => setParam(k, v)} />
            </div>
          </div>
        </Card>

        <Tabs
          tabs={TABS}
          activeTab={activeTab}
          onTabChange={(tab) => setParam('report', tab === 'Workflow aging' ? 'aging' : 'workload')}
        >
          <div className="pt-4">
            {activeTab === 'Workload' ? (
              <WorkloadReportView params={params} />
            ) : (
              <WorkflowAgingReportView params={params} />
            )}
          </div>
        </Tabs>
      </div>
    </DashboardLayout>
  );
}
