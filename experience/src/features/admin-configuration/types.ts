export interface AdminConfigurationDomain {
  domainKey: string
  displayName: string
  owningModule: string
  status: string
  editableSchemaRef: string
  supportsRollback: boolean
  currentPublishedVersion: number | null
  draftStatus: string | null
  lastValidationStatus: string | null
  lastPublishedBy: string | null
  lastPublishedAt: string | null
}

export interface AdminConfigurationDraft {
  id: string
  domainKey: string
  basePublishedVersion: number
  draftVersion: number
  status: string
  payload: unknown
  payloadHash: string
  rowVersion: string
  latestValidation: AdminConfigurationValidationResult | null
}

export interface AdminConfigurationValidationIssue {
  code: string
  message: string
  path: string | null
}

export interface AdminConfigurationChangeSummary {
  path: string
  changeType: string
  before: string | null
  after: string | null
}

export interface AdminConfigurationValidationResult {
  id: string
  draftId: string
  status: string
  draftPayloadHash: string
  blockingErrors: AdminConfigurationValidationIssue[]
  warnings: AdminConfigurationValidationIssue[]
  compareSummary: AdminConfigurationChangeSummary[]
}

export interface AdminConfigurationRefreshStatus {
  id: string
  consumerKey: string
  status: string
  refreshedAt: string | null
  errorSummary: string | null
}

export interface AdminConfigurationPublishedSet {
  id: string
  domainKey: string
  publishedVersion: number
  payloadSnapshot: unknown
  payloadHash: string
  publishedByUserId: string
  publishedAt: string
  publishReason: string
  refreshStatuses: AdminConfigurationRefreshStatus[]
}

export interface AdminConfigurationDomainDetail {
  domain: AdminConfigurationDomain
  activeDraft: AdminConfigurationDraft | null
  currentPublishedSet: AdminConfigurationPublishedSet | null
  refreshStatuses: AdminConfigurationRefreshStatus[]
  publishedSets: AdminConfigurationPublishedSet[]
}

export interface AdminConfigurationAuditEvent {
  id: string
  domainKey: string
  draftId: string | null
  publishedSetId: string | null
  action: string
  outcome: string
  actorUserId: string
  createdAt: string
  summary: unknown
}

export interface AdminConfigurationAuditResponse {
  items: AdminConfigurationAuditEvent[]
  totalCount: number
  page: number
  pageSize: number
}
