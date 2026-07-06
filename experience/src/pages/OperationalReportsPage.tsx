import { startTransition } from 'react';
import { useSearchParams } from 'react-router-dom';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Card } from '@/components/ui/Card';
import { Select } from '@/components/ui/Select';
import { Tabs } from '@/components/ui/Tabs';
import { TextInput } from '@/components/ui/TextInput';
import {
  ReportControls,
  DistributionRollupReportView,
  WorkflowAgingReportView,
  WorkloadReportView,
  type DistributionRollupParams,
  type OperationalReportParams,
} from '@/features/reports';

const TABS = ['Workload', 'Workflow aging', 'Distribution rollups'];

export default function OperationalReportsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const reportParam = searchParams.get('report');
  const activeTab = reportParam === 'aging'
    ? 'Workflow aging'
    : reportParam === 'rollups'
      ? 'Distribution rollups'
      : 'Workload';

  const params: OperationalReportParams = {
    region: searchParams.get('region') || undefined,
    lineOfBusiness: searchParams.get('lineOfBusiness') || undefined,
    workflowType: searchParams.get('workflowType') || undefined,
    rootNodeId: searchParams.get('rootNodeId') || undefined,
    territoryId: searchParams.get('territoryId') || undefined,
    producerUserId: searchParams.get('producerUserId') || undefined,
    asOf: searchParams.get('asOf') || undefined,
  };
  const rollupParams: DistributionRollupParams = {
    ...params,
    groupBy: (searchParams.get('groupBy') as DistributionRollupParams['groupBy']) || 'Hierarchy',
    metricFamily: (searchParams.get('metricFamily') as DistributionRollupParams['metricFamily']) || 'Production',
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
            {activeTab === 'Distribution rollups' ? (
              <DistributionRollupControls params={rollupParams} onChange={setParam} />
            ) : (
              <div className="pt-2">
                <ReportControls params={params} onChange={(k, v) => setParam(k, v)} />
              </div>
            )}
          </div>
        </Card>

        <Tabs
          tabs={TABS}
          activeTab={activeTab}
          onTabChange={(tab) => setParam('report', tab === 'Workflow aging' ? 'aging' : tab === 'Distribution rollups' ? 'rollups' : 'workload')}
        >
          <div className="pt-4">
            {activeTab === 'Workload' ? (
              <WorkloadReportView params={params} />
            ) : activeTab === 'Workflow aging' ? (
              <WorkflowAgingReportView params={params} />
            ) : (
              <DistributionRollupReportView params={rollupParams} />
            )}
          </div>
        </Tabs>
      </div>
    </DashboardLayout>
  );
}

function DistributionRollupControls({
  params,
  onChange,
}: {
  params: DistributionRollupParams;
  onChange: (key: string, value: string | null) => void;
}) {
  return (
    <div className="grid grid-cols-1 gap-3 pt-2 sm:grid-cols-2 lg:grid-cols-3">
      <TextInput
        label="As of"
        type="date"
        value={params.asOf ?? ''}
        onChange={(e) => onChange('asOf', e.target.value)}
      />
      <TextInput
        label="Root node"
        value={params.rootNodeId ?? ''}
        placeholder="Hierarchy node ID"
        onChange={(e) => onChange('rootNodeId', e.target.value)}
      />
      <TextInput
        label="Territory"
        value={params.territoryId ?? ''}
        placeholder="Territory ID"
        onChange={(e) => onChange('territoryId', e.target.value)}
      />
      <TextInput
        label="Producer"
        value={params.producerUserId ?? ''}
        placeholder="Producer user ID"
        onChange={(e) => onChange('producerUserId', e.target.value)}
      />
      <Select
        label="Group by"
        value={params.groupBy}
        options={[
          { value: 'Hierarchy', label: 'Hierarchy' },
          { value: 'Territory', label: 'Territory' },
          { value: 'Producer', label: 'Producer' },
        ]}
        onChange={(e) => onChange('groupBy', e.target.value)}
      />
      <Select
        label="Metric family"
        value={params.metricFamily}
        options={[
          { value: 'Production', label: 'Production' },
          { value: 'Workflow', label: 'Workflow' },
          { value: 'Activity', label: 'Activity' },
        ]}
        onChange={(e) => onChange('metricFamily', e.target.value)}
      />
    </div>
  );
}
