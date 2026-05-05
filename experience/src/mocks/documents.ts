import type {
  DocumentClassification,
  DocumentDetailDto,
  DocumentMetadata,
  DocumentParentRefDto,
  DocumentSidecarDto,
  DocumentStatus,
  DocumentTemplateDto,
} from '@/features/documents'

const USER_ID = '11111111-1111-1111-1111-111111111111'
const NOW = '2026-05-04T12:00:00Z'

export const documentMetadataSchemas = {
  schemas: [
    {
      id: 'acord',
      version: 1,
      status: 'active',
      schemaHash: 'sha256:mock-acord',
      schema: {
        title: 'ACORD Metadata',
        type: 'object',
        additionalProperties: false,
        properties: {
          formNumber: { type: 'string', title: 'Form Number', enum: ['25', '125', '126', '130', '140'] },
          namedInsured: { type: 'string', title: 'Named Insured', maxLength: 200 },
          effectiveDate: { type: 'string', title: 'Effective Date', format: 'date' },
          expirationDate: { type: 'string', title: 'Expiration Date', format: 'date' },
          carrier: { type: 'string', title: 'Carrier', maxLength: 120 },
        },
      },
    },
    {
      id: 'loss-run',
      version: 1,
      status: 'active',
      schemaHash: 'sha256:mock-loss-run',
      schema: {
        title: 'Loss Run Metadata',
        type: 'object',
        additionalProperties: false,
        properties: {
          valuationDate: { type: 'string', title: 'Valuation Date', format: 'date' },
          periodStart: { type: 'string', title: 'Period Start', format: 'date' },
          periodEnd: { type: 'string', title: 'Period End', format: 'date' },
          carrier: { type: 'string', title: 'Carrier', maxLength: 120 },
          claimCount: { type: 'integer', title: 'Claim Count', minimum: 0 },
        },
      },
    },
    {
      id: 'financials',
      version: 1,
      status: 'active',
      schemaHash: 'sha256:mock-financials',
      schema: {
        title: 'Financial Statement Metadata',
        type: 'object',
        additionalProperties: false,
        properties: {
          fiscalYear: { type: 'integer', title: 'Fiscal Year', minimum: 1900 },
          statementPeriodEnd: { type: 'string', title: 'Statement Period End', format: 'date' },
          statementType: { type: 'string', title: 'Statement Type', enum: ['audited', 'reviewed', 'compiled', 'internal'] },
          revenue: { type: 'number', title: 'Revenue', minimum: 0 },
          currency: { type: 'string', title: 'Currency', enum: ['USD', 'CAD'] },
        },
      },
    },
    {
      id: 'supplemental',
      version: 1,
      status: 'active',
      schemaHash: 'sha256:mock-supplemental',
      schema: {
        title: 'Supplemental Metadata',
        type: 'object',
        additionalProperties: false,
        properties: {
          description: { type: 'string', title: 'Description', maxLength: 300 },
          receivedDate: { type: 'string', title: 'Received Date', format: 'date' },
          source: { type: 'string', title: 'Source', maxLength: 120 },
        },
      },
    },
    {
      id: 'template',
      version: 1,
      status: 'active',
      schemaHash: 'sha256:mock-template',
      schema: {
        title: 'Template Metadata',
        type: 'object',
        additionalProperties: false,
        properties: {
          templateCategory: { type: 'string', title: 'Template Category', enum: ['acord', 'supplemental', 'proposal', 'notice'] },
          jurisdiction: { type: 'string', title: 'Jurisdiction', maxLength: 40 },
        },
      },
    },
  ],
}

let documents = seedDocuments()
let templates = seedTemplates()

export function resetDocumentMockState() {
  documents = seedDocuments()
  templates = seedTemplates()
}

export function listDocuments(searchParams: URLSearchParams) {
  const parent = readParent(searchParams)
  const classification = searchParams.get('classification')
  const type = searchParams.get('type')
  const page = Number(searchParams.get('page') ?? '1')
  const pageSize = Number(searchParams.get('pageSize') ?? '20')
  const filtered = documents
    .filter((document) => document.parent.type === parent.type && document.parent.id === parent.id)
    .filter((document) => !classification || document.classification === classification)
    .filter((document) => !type || document.type === type)
    .sort((left, right) => right.auditTimestamps.updatedAtUtc.localeCompare(left.auditTimestamps.updatedAtUtc))
  const pageItems = filtered.slice((page - 1) * pageSize, page * pageSize)

  return {
    documents: pageItems.map((document) => {
      const latest = [...document.versions].sort((left, right) => right.n - left.n)[0]
      return {
        documentId: document.documentId,
        logicalName: document.logicalName,
        type: document.type,
        classification: document.classification,
        latestVersion: latest.n,
        status: latest.status,
        latestUpload: {
          atUtc: latest.uploadedAt,
          byUserId: latest.uploadedByUserId,
        },
        parent: document.parent,
      }
    }),
    pagination: {
      page,
      pageSize,
      total: filtered.length,
    },
  }
}

export async function uploadDocuments(formData: FormData) {
  const parent = {
    type: String(formData.get('parentType') ?? 'submission') as DocumentParentRefDto['type'],
    id: String(formData.get('parentId') ?? 'submission-1'),
  }
  const classification = String(formData.get('defaultClassification') ?? 'confidential') as DocumentClassification
  const metadataItems = formData.getAll('metadata').map((entry) => readMetadataItem(String(entry)))
  const type = String(formData.get('type') ?? '') || metadataItems[0]?.type || 'supplemental'
  const files = formData.getAll('files').filter((entry): entry is File => entry instanceof File)
  const accepted = files.map((file, index) => {
    const item = metadataItems[index]
    const documentType = item?.type || type
    const documentId = `doc_mock_${Date.now()}_${index}`
    const logicalName = file.name.replace(/\.[^.]+$/, '')
    documents.unshift(buildSidecar({
      documentId,
      logicalName,
      parent,
      classification,
      type: documentType,
      metadata: item?.metadata,
      status: 'quarantined',
      fileName: file.name,
      size: file.size,
    }))
    return { documentId, logicalName, status: 'quarantined' }
  })

  return {
    documents: accepted,
    rejected: [],
  }
}

export function getDocument(documentId: string): DocumentDetailDto | null {
  const sidecar = documents.find((document) => document.documentId === documentId)
    ?? templates.find((template) => template.documentId === documentId)
  if (!sidecar) return null

  return {
    sidecar,
    previewUrls: sidecar.versions.map((version) => version.status === 'available' ? `/documents/${sidecar.documentId}/versions/${version.n}/binary` : null),
  }
}

export function updateDocumentMetadata(documentId: string, patch: Partial<Pick<DocumentSidecarDto, 'classification' | 'type' | 'tags' | 'metadata'>>) {
  const index = documents.findIndex((document) => document.documentId === documentId)
  if (index < 0) return null

  documents[index] = {
    ...documents[index],
    classification: patch.classification ?? documents[index].classification,
    type: patch.type ?? documents[index].type,
    tags: patch.tags ?? documents[index].tags,
    metadataSchema: schemaRef(patch.type ?? documents[index].type),
    metadata: patch.metadata ?? documents[index].metadata,
    auditTimestamps: {
      ...documents[index].auditTimestamps,
      updatedAtUtc: NOW,
    },
    events: [
      ...documents[index].events,
      { kind: 'metadata_edited', at: NOW, byUserId: USER_ID },
    ],
  }

  return getDocument(documentId)
}

export function replaceDocument(documentId: string, file: File | null) {
  const index = documents.findIndex((document) => document.documentId === documentId)
  if (index < 0 || !file) return null

  const current = documents[index]
  const latest = [...current.versions].sort((left, right) => right.n - left.n)[0]
  const next = latest.n + 1
  documents[index] = {
    ...current,
    auditTimestamps: { ...current.auditTimestamps, updatedAtUtc: NOW },
    versions: [
      ...current.versions,
      {
        n: next,
        fileName: file.name,
        sizeBytes: file.size,
        sha256: `mock-${next}`,
        status: 'quarantined',
        uploadedAt: NOW,
        uploadedByUserId: USER_ID,
        supersedes: latest.n,
      },
    ],
  }

  return { documentId, version: next, status: 'quarantined' }
}

export function documentCompleteness(searchParams: URLSearchParams) {
  const parent = readParent(searchParams)
  const parentDocuments = documents.filter((document) => document.parent.type === parent.type && document.parent.id === parent.id)
  const latestStatuses = parentDocuments.map((document) => [...document.versions].sort((left, right) => right.n - left.n)[0]?.status)

  return {
    parent,
    totals: {
      available: latestStatuses.filter((status) => status === 'available').length,
      quarantined: latestStatuses.filter((status) => status === 'quarantined').length,
      failedPromote: latestStatuses.filter((status) => status === 'failed_promote').length,
    },
    byType: countBy(parentDocuments.map((document) => document.type)).map(([type, count]) => ({ type, count })),
    byClassification: (['public', 'confidential', 'restricted'] as DocumentClassification[]).map((classification) => ({
      classification,
      count: parentDocuments.filter((document) => document.classification === classification).length,
    })),
  }
}

export function listDocumentTemplates() {
  return {
    templates: templates.map(toTemplate),
    pagination: {
      page: 1,
      pageSize: 20,
      total: templates.length,
    },
  }
}

export async function uploadDocumentTemplate(formData: FormData) {
  const file = formData.get('file')
  if (!(file instanceof File)) return null

  const documentId = `doc_template_${Date.now()}`
  const sidecar = buildSidecar({
    documentId,
    logicalName: file.name.replace(/\.[^.]+$/, ''),
    parent: { type: 'submission', id: '00000000-0000-0000-0000-000000000000' },
    classification: String(formData.get('classification') ?? 'confidential') as DocumentClassification,
    type: 'template',
    status: 'quarantined',
    fileName: file.name,
    size: file.size,
    isTemplate: true,
  })
  templates.unshift(sidecar)
  return toTemplate(sidecar)
}

export function linkDocumentTemplate(templateId: string, searchParams: URLSearchParams) {
  const template = templates.find((item) => item.documentId === templateId)
  if (!template) return null

  const parent = readParent(searchParams)
  const documentId = `doc_from_template_${Date.now()}`
  documents.unshift(buildSidecar({
    documentId,
    logicalName: template.logicalName,
    parent,
    classification: template.classification,
    type: 'supplemental',
    status: 'quarantined',
    fileName: `${template.logicalName}.pdf`,
    size: template.versions[0]?.sizeBytes ?? 1024,
  }))

  return { documentId, logicalName: template.logicalName, status: 'quarantined' }
}

function readParent(searchParams: URLSearchParams): DocumentParentRefDto {
  return {
    type: String(searchParams.get('parent.type') ?? 'submission') as DocumentParentRefDto['type'],
    id: String(searchParams.get('parent.id') ?? 'submission-1'),
  }
}

function seedDocuments(): DocumentSidecarDto[] {
  return [
    buildSidecar({
      documentId: 'doc_mock_acord',
      logicalName: 'ACORD 125 intake',
      parent: { type: 'submission', id: 'submission-1' },
      classification: 'confidential',
      type: 'acord',
      metadata: {
        formNumber: '125',
        namedInsured: 'Blue Horizon Risk Partners',
        effectiveDate: '2026-05-01',
        carrier: 'Nebula Mutual',
      },
      status: 'available',
      fileName: 'acord-125.pdf',
      size: 348_000,
    }),
    buildSidecar({
      documentId: 'doc_mock_loss_run',
      logicalName: 'Loss runs Q1',
      parent: { type: 'account', id: 'account-1' },
      classification: 'restricted',
      type: 'loss-run',
      metadata: {
        valuationDate: '2026-03-31',
        periodStart: '2025-01-01',
        periodEnd: '2025-12-31',
        claimCount: 12,
      },
      status: 'quarantined',
      fileName: 'loss-runs.csv',
      size: 88_000,
    }),
    buildSidecar({
      documentId: 'doc_mock_policy',
      logicalName: 'Issued policy packet',
      parent: { type: 'policy', id: 'policy-1' },
      classification: 'confidential',
      type: 'supplemental',
      status: 'available',
      fileName: 'policy-packet.pdf',
      size: 704_000,
    }),
    buildSidecar({
      documentId: 'doc_mock_renewal',
      logicalName: 'Renewal questionnaire',
      parent: { type: 'renewal', id: 'renewal-1' },
      classification: 'public',
      type: 'supplemental',
      status: 'available',
      fileName: 'renewal-questionnaire.docx',
      size: 162_000,
    }),
  ]
}

function seedTemplates(): DocumentSidecarDto[] {
  return [
    buildSidecar({
      documentId: 'doc_template_acord',
      logicalName: 'ACORD supplemental template',
      parent: { type: 'submission', id: '00000000-0000-0000-0000-000000000000' },
      classification: 'public',
      type: 'template',
      status: 'available',
      fileName: 'acord-template.pdf',
      size: 220_000,
      isTemplate: true,
      useCount: 4,
    }),
  ]
}

function buildSidecar({
  documentId,
  logicalName,
  parent,
  classification,
  type,
  status,
  fileName,
  size,
  isTemplate = false,
  useCount = 0,
  metadata = {},
}: {
  documentId: string
  logicalName: string
  parent: DocumentParentRefDto
  classification: DocumentClassification
  type: string
  status: DocumentStatus
  fileName: string
  size: number
  isTemplate?: boolean
  useCount?: number
  metadata?: DocumentMetadata
}): DocumentSidecarDto {
  return {
    documentId,
    logicalName,
    parent,
    classification,
    type,
    tags: type === 'acord' ? ['acord'] : [],
    metadataSchema: schemaRef(type),
    metadata,
    uploaderId: USER_ID,
    auditTimestamps: {
      createdAtUtc: NOW,
      updatedAtUtc: NOW,
    },
    provenance: null,
    versions: [{
      n: 1,
      fileName,
      sizeBytes: size,
      sha256: `mock-${documentId}`,
      status,
      uploadedAt: NOW,
      uploadedByUserId: USER_ID,
      supersedes: null,
    }],
    useCount: isTemplate ? useCount : null,
    lastUsedAt: isTemplate && useCount > 0 ? NOW : null,
    events: [{ kind: 'uploaded', at: NOW, byUserId: USER_ID, version: 1 }],
  }
}

function readMetadataItem(value: string): { type?: string; metadata?: DocumentMetadata } {
  try {
    const parsed = JSON.parse(value) as { type?: string; metadata?: DocumentMetadata }
    return parsed ?? {}
  } catch {
    return {}
  }
}

function schemaRef(type: string) {
  const schema = documentMetadataSchemas.schemas.find((item) => item.id === type)
  return {
    id: schema?.id ?? type,
    version: schema?.version ?? 1,
    schemaHash: schema?.schemaHash ?? 'sha256:mock',
  }
}

function toTemplate(sidecar: DocumentSidecarDto): DocumentTemplateDto {
  return {
    templateId: sidecar.documentId,
    logicalName: sidecar.logicalName,
    type: 'template',
    classification: sidecar.classification,
    tags: sidecar.tags,
    useCount: sidecar.useCount ?? 0,
    lastUsedAt: sidecar.lastUsedAt,
    uploadedAtUtc: sidecar.versions[0]?.uploadedAt ?? NOW,
    uploadedByUserId: sidecar.versions[0]?.uploadedByUserId ?? USER_ID,
  }
}

function countBy(values: string[]): Array<[string, number]> {
  const counts = new Map<string, number>()
  for (const value of values) counts.set(value, (counts.get(value) ?? 0) + 1)
  return Array.from(counts.entries())
}
