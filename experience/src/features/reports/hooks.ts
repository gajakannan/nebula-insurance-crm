import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type {
  DistributionRollupParams,
  DistributionRollupReport,
  OperationalReportParams,
  OperationalWorkloadReport,
  WorkflowAgingReport,
} from './types';

function buildQuery(params: OperationalReportParams): string {
  const sp = new URLSearchParams();
  if (params.region) sp.set('region', params.region);
  if (params.lineOfBusiness) sp.set('lineOfBusiness', params.lineOfBusiness);
  if (params.ownerUserId) sp.set('ownerUserId', params.ownerUserId);
  if (params.rootNodeId) sp.set('rootNodeId', params.rootNodeId);
  if (params.territoryId) sp.set('territoryId', params.territoryId);
  if (params.producerUserId) sp.set('producerUserId', params.producerUserId);
  if (params.workflowType) sp.set('workflowType', params.workflowType);
  if (params.asOf) sp.set('asOf', params.asOf);
  sp.set('drilldownLimit', String(params.drilldownLimit ?? 50));
  return sp.toString();
}

export function useWorkloadReport(params: OperationalReportParams = {}) {
  return useQuery({
    queryKey: ['operational-reports', 'workload', params],
    queryFn: () => api.get<OperationalWorkloadReport>(`/operational-reports/workload?${buildQuery(params)}`),
  });
}

export function useWorkflowAgingReport(params: OperationalReportParams = {}) {
  return useQuery({
    queryKey: ['operational-reports', 'workflow-aging', params],
    queryFn: () => api.get<WorkflowAgingReport>(`/operational-reports/workflow-aging?${buildQuery(params)}`),
  });
}

export function useDistributionRollupReport(params: DistributionRollupParams = {}) {
  return useQuery({
    queryKey: ['operational-reports', 'distribution-rollups', params],
    queryFn: () => {
      const sp = new URLSearchParams(buildQuery(params));
      sp.set('groupBy', params.groupBy ?? 'Hierarchy');
      sp.set('metricFamily', params.metricFamily ?? 'Production');
      return api.get<DistributionRollupReport>(`/operational-reports/distribution-rollups?${sp.toString()}`);
    },
  });
}
