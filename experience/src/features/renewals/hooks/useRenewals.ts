import { useQuery } from '@tanstack/react-query';
import { api } from '@/services/api';
import type {
  PaginatedResponse,
  RenewalListItemDto,
  RenewalListQuery,
} from '../types';

interface UseRenewalsOptions extends RenewalListQuery {
  enabled?: boolean;
}

export function useRenewals({
  dueWindow,
  status,
  assignedToUserId,
  lineOfBusiness,
  accountId,
  brokerId,
  urgency,
  sort = 'policyExpirationDate',
  sortDir = 'asc',
  page = 1,
  pageSize = 25,
  includeTerminal,
  enabled = true,
}: UseRenewalsOptions = {}) {
  const params = new URLSearchParams();
  if (dueWindow) params.set('dueWindow', dueWindow);
  if (status) params.set('status', status);
  if (assignedToUserId) params.set('assignedToUserId', assignedToUserId);
  if (lineOfBusiness) params.set('lineOfBusiness', lineOfBusiness);
  if (accountId) params.set('accountId', accountId);
  if (brokerId) params.set('brokerId', brokerId);
  if (urgency) params.set('urgency', urgency);
  if (typeof includeTerminal === 'boolean') params.set('includeTerminal', String(includeTerminal));
  params.set('sort', sort);
  params.set('sortDir', sortDir);
  params.set('page', String(page));
  params.set('pageSize', String(pageSize));

  return useQuery({
    queryKey: [
      'renewals',
      'list',
      { dueWindow, status, assignedToUserId, lineOfBusiness, accountId, brokerId, urgency, sort, sortDir, page, pageSize, includeTerminal },
    ],
    queryFn: () => api.get<PaginatedResponse<RenewalListItemDto>>(`/renewals?${params.toString()}`),
    enabled,
  });
}
