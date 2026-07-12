/**
 * @vitest-environment jsdom
 */

import { fireEvent, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type {
  AdminConfigurationAuditResponse,
  AdminConfigurationDomain,
  AdminConfigurationDomainDetail,
  AdminConfigurationValidationResult,
} from '../types'

const mockUseAdminConfigurationDomains = vi.fn()
const mockUseAdminConfigurationDomain = vi.fn()
const mockUseAdminConfigurationAudit = vi.fn()
const mockUseAdminConfigurationMutations = vi.fn()

vi.mock('../hooks', () => ({
  useAdminConfigurationDomains: (...args: unknown[]) => mockUseAdminConfigurationDomains(...args),
  useAdminConfigurationDomain: (...args: unknown[]) => mockUseAdminConfigurationDomain(...args),
  useAdminConfigurationAudit: (...args: unknown[]) => mockUseAdminConfigurationAudit(...args),
  useAdminConfigurationMutations: (...args: unknown[]) => mockUseAdminConfigurationMutations(...args),
}))

import { AdminConfigurationWorkspace } from './AdminConfigurationWorkspace'

describe('AdminConfigurationWorkspace', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockUseAdminConfigurationDomains.mockReturnValue({
      isLoading: false,
      isError: false,
      data: domains,
      refetch: vi.fn(),
    })
    mockUseAdminConfigurationDomain.mockReturnValue({
      isLoading: false,
      isError: false,
      data: detail,
      refetch: vi.fn(),
    })
    mockUseAdminConfigurationAudit.mockReturnValue({
      isLoading: false,
      isError: false,
      data: audit,
      refetch: vi.fn(),
    })
    mockUseAdminConfigurationMutations.mockReturnValue({
      createDraft: mutation(),
      updateDraft: mutation(),
      compareDraft: mutation(validation),
      validateDraft: mutation(),
      publishDraft: mutation(),
      rollback: mutation(),
    })
  })

  it('renders governed domains, validation controls, rollback history, and audit details', () => {
    render(<AdminConfigurationWorkspace />)

    expect(screen.getByRole('heading', { name: 'Admin Configuration' })).toBeInTheDocument()
    expect(screen.getAllByText('Queue and Routing').length).toBeGreaterThanOrEqual(2)
    expect(screen.getAllByText('Workflow SLA Thresholds').length).toBeGreaterThanOrEqual(1)
    expect(screen.getByText('Validation and compare')).toBeInTheDocument()
    expect(screen.getByText('Changed fields')).toBeInTheDocument()
    expect(screen.getAllByRole('button').find((button) => button.textContent?.trim() === 'Publish')).toBeEnabled()
    expect(screen.getAllByRole('button').find((button) => button.textContent?.trim() === 'Rollback')).toBeEnabled()

    const auditRow = screen.getAllByRole('button').find((button) =>
      button.textContent?.includes('Published') && button.textContent.includes('queue-routing'),
    )
    expect(auditRow).toBeDefined()
    fireEvent.click(auditRow!)

    expect(screen.getByRole('dialog', { name: 'Audit details' })).toBeInTheDocument()
    expect(screen.getByText((content) => content.includes('"publishedVersion": 2'))).toBeInTheDocument()
  })

  it('shows retry and empty states when domain loading fails or returns no data', () => {
    mockUseAdminConfigurationDomains.mockReturnValue({
      isLoading: false,
      isError: true,
      error: new Error('catalog unavailable'),
      data: undefined,
      refetch: vi.fn(),
    })
    mockUseAdminConfigurationDomain.mockReturnValue({
      isLoading: false,
      isError: false,
      data: undefined,
      refetch: vi.fn(),
    })
    mockUseAdminConfigurationAudit.mockReturnValue({
      isLoading: false,
      isError: false,
      data: { ...audit, items: [] },
      refetch: vi.fn(),
    })

    render(<AdminConfigurationWorkspace />)

    expect(screen.getByText('catalog unavailable')).toBeInTheDocument()
    expect(screen.getByText('No audit events match the current filters.')).toBeInTheDocument()
  })
})

function mutation<T>(result?: T) {
  return {
    isPending: false,
    mutateAsync: vi.fn().mockResolvedValue(result ?? {}),
  }
}

const domains: AdminConfigurationDomain[] = [
  {
    domainKey: 'queue-routing',
    displayName: 'Queue and Routing',
    owningModule: 'routing',
    status: 'Supported',
    editableSchemaRef: 'planning-mds/schemas/admin-configuration-domain.schema.json',
    supportsRollback: true,
    currentPublishedVersion: 2,
    draftStatus: 'Draft',
    lastValidationStatus: 'Passed',
    lastPublishedBy: 'Admin',
    lastPublishedAt: '2026-07-06T09:00:00Z',
  },
  {
    domainKey: 'workflow-sla-thresholds',
    displayName: 'Workflow SLA Thresholds',
    owningModule: 'workflow-sla',
    status: 'Supported',
    editableSchemaRef: 'planning-mds/schemas/admin-configuration-draft.schema.json',
    supportsRollback: true,
    currentPublishedVersion: null,
    draftStatus: null,
    lastValidationStatus: null,
    lastPublishedBy: null,
    lastPublishedAt: null,
  },
]

const validation: AdminConfigurationValidationResult = {
  id: '30000000-0000-0000-0000-000000000001',
  draftId: '20000000-0000-0000-0000-000000000001',
  status: 'Passed',
  draftPayloadHash: 'sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
  blockingErrors: [],
  warnings: [],
  compareSummary: [{ path: '$.rules', changeType: 'Updated', before: '1', after: '2' }],
}

const detail: AdminConfigurationDomainDetail = {
  domain: domains[0],
  activeDraft: {
    id: '20000000-0000-0000-0000-000000000001',
    domainKey: 'queue-routing',
    basePublishedVersion: 2,
    draftVersion: 1,
    status: 'Validated',
    payload: { rules: 2 },
    payloadHash: validation.draftPayloadHash,
    rowVersion: 'AAAAAAAAB9E=',
    latestValidation: validation,
  },
  currentPublishedSet: {
    id: '40000000-0000-0000-0000-000000000002',
    domainKey: 'queue-routing',
    publishedVersion: 2,
    payloadSnapshot: { rules: 1 },
    payloadHash: 'sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb',
    publishedByUserId: '10000000-0000-0000-0000-000000000001',
    publishedAt: '2026-07-06T09:00:00Z',
    publishReason: 'Previous publish',
    refreshStatuses: [],
  },
  refreshStatuses: [{ id: '50000000-0000-0000-0000-000000000001', consumerKey: 'routing', status: 'Refreshed', refreshedAt: '2026-07-06T09:01:00Z', errorSummary: null }],
  publishedSets: [
    {
      id: '40000000-0000-0000-0000-000000000002',
      domainKey: 'queue-routing',
      publishedVersion: 2,
      payloadSnapshot: { rules: 1 },
      payloadHash: 'sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb',
      publishedByUserId: '10000000-0000-0000-0000-000000000001',
      publishedAt: '2026-07-06T09:00:00Z',
      publishReason: 'Current',
      refreshStatuses: [],
    },
    {
      id: '40000000-0000-0000-0000-000000000001',
      domainKey: 'queue-routing',
      publishedVersion: 1,
      payloadSnapshot: { rules: 0 },
      payloadHash: 'sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc',
      publishedByUserId: '10000000-0000-0000-0000-000000000001',
      publishedAt: '2026-07-05T09:00:00Z',
      publishReason: 'Rollback target',
      refreshStatuses: [],
    },
  ],
}

const audit: AdminConfigurationAuditResponse = {
  page: 1,
  pageSize: 50,
  totalCount: 1,
  items: [
    {
      id: '60000000-0000-0000-0000-000000000001',
      domainKey: 'queue-routing',
      draftId: '20000000-0000-0000-0000-000000000001',
      publishedSetId: '40000000-0000-0000-0000-000000000002',
      action: 'Published',
      outcome: 'Succeeded',
      actorUserId: '10000000-0000-0000-0000-000000000001',
      createdAt: '2026-07-06T09:00:00Z',
      summary: { publishedVersion: 2, reason: 'Routine configuration update' },
    },
  ],
}
