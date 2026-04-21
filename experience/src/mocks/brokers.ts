import type { BrokerDto, PaginatedResponse } from '@/features/brokers'

export const brokerListFixture: BrokerDto[] = [
  {
    id: 'broker-1',
    legalName: 'Blue Horizon Risk Partners',
    licenseNumber: 'CA-445512',
    state: 'CA',
    status: 'Active',
    email: 'team@bluehorizon.test',
    phone: '+12025550101',
    createdAt: '2026-03-01T00:00:00Z',
    updatedAt: '2026-03-19T00:00:00Z',
    rowVersion: 3,
    isDeactivated: false,
  },
  {
    id: 'broker-2',
    legalName: 'Summit Specialty Group',
    licenseNumber: 'TX-998871',
    state: 'TX',
    status: 'Pending',
    email: 'hello@summit.test',
    phone: null,
    createdAt: '2026-02-15T00:00:00Z',
    updatedAt: '2026-03-18T00:00:00Z',
    rowVersion: 2,
    isDeactivated: false,
  },
  {
    id: 'broker-3',
    legalName: 'Atlas Wholesale Brokerage',
    licenseNumber: 'NY-223344',
    state: 'NY',
    status: 'Inactive',
    email: null,
    phone: null,
    createdAt: '2026-01-10T00:00:00Z',
    updatedAt: '2026-03-10T00:00:00Z',
    rowVersion: 6,
    isDeactivated: true,
  },
]

export function buildBrokerListResponse(
  params: URLSearchParams,
): PaginatedResponse<BrokerDto> {
  const query = params.get('q')?.trim().toLowerCase() ?? ''
  const status = params.get('status')
  const page = Number(params.get('page') ?? '1')
  const pageSize = Number(params.get('pageSize') ?? '10')

  const filtered = brokerListFixture.filter((broker) => {
    const matchesQuery = query.length === 0
      || broker.legalName.toLowerCase().includes(query)
      || broker.licenseNumber.toLowerCase().includes(query)
    const matchesStatus = !status || status === 'All' || broker.status === status
    return matchesQuery && matchesStatus
  })

  const offset = (Math.max(page, 1) - 1) * Math.max(pageSize, 1)
  const paged = filtered.slice(offset, offset + pageSize)

  return {
    data: paged,
    page,
    pageSize,
    totalCount: filtered.length,
    totalPages: Math.max(1, Math.ceil(filtered.length / Math.max(pageSize, 1))),
  }
}
