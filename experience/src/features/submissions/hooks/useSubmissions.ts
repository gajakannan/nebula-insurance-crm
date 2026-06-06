import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type { PaginatedResponse, SubmissionListItemDto, SubmissionListQuery } from '../types';

interface UseSubmissionsOptions extends SubmissionListQuery {
  enabled?: boolean;
}

export function useSubmissions({
  status,
  brokerId,
  accountId,
  lineOfBusiness,
  assignedToUserId,
  stale,
  approvalPending,
  stuckOnly,
  includeArchived,
  sort = 'createdAt',
  sortDir = 'desc',
  page = 1,
  pageSize = 25,
  enabled = true,
}: UseSubmissionsOptions = {}) {
  const params = new URLSearchParams();
  if (status) params.set('status', status);
  if (brokerId) params.set('brokerId', brokerId);
  if (accountId) params.set('accountId', accountId);
  if (lineOfBusiness) params.set('lineOfBusiness', lineOfBusiness);
  if (assignedToUserId) params.set('assignedToUserId', assignedToUserId);
  if (typeof stale === 'boolean') params.set('stale', String(stale));
  if (typeof approvalPending === 'boolean') params.set('approvalPending', String(approvalPending));
  if (typeof stuckOnly === 'boolean') params.set('stuckOnly', String(stuckOnly));
  if (typeof includeArchived === 'boolean') params.set('includeArchived', String(includeArchived));
  params.set('sort', sort);
  params.set('sortDir', sortDir);
  params.set('page', String(page));
  params.set('pageSize', String(pageSize));

  return useQuery({
    queryKey: [
      'submissions',
      'list',
      {
        status,
        brokerId,
        accountId,
        lineOfBusiness,
        assignedToUserId,
        stale,
        approvalPending,
        stuckOnly,
        includeArchived,
        sort,
        sortDir,
        page,
        pageSize,
      },
    ],
    queryFn: () => api.get<PaginatedResponse<SubmissionListItemDto>>(`/submissions?${params.toString()}`),
    enabled,
  });
}
