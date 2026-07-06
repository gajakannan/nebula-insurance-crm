import { useMemo, useState } from 'react'
import { FileCheck2, ScanSearch } from 'lucide-react'
import { Select } from '@/components/ui/Select'
import { useDocumentTemplates, useIssueGeneratedDocument, usePreviewGeneratedDocument } from '../hooks'
import type {
  DocumentClassification,
  DocumentParentRefDto,
  GeneratedArtifactFamily,
  GeneratedDocumentPreviewResponseDto,
  GeneratedDocumentRequestDto,
} from '../types'
import { describeDocumentError } from './DocumentUploadDialog'

const FAMILY_OPTIONS: Array<{ value: GeneratedArtifactFamily; label: string }> = [
  { value: 'coi', label: 'COI' },
  { value: 'acord', label: 'ACORD' },
  { value: 'proposal', label: 'Proposal' },
]

const CLASSIFICATION_OPTIONS: Array<{ value: DocumentClassification; label: string }> = [
  { value: 'public', label: 'Public' },
  { value: 'confidential', label: 'Confidential' },
  { value: 'restricted', label: 'Restricted' },
]

export function GenerateDocumentPanel({ parent }: { parent: DocumentParentRefDto }) {
  const [family, setFamily] = useState<GeneratedArtifactFamily>('coi')
  const [classification, setClassification] = useState<DocumentClassification>('confidential')
  const [templateDocumentId, setTemplateDocumentId] = useState('')
  const [preview, setPreview] = useState<GeneratedDocumentPreviewResponseDto | null>(null)
  const [issuedId, setIssuedId] = useState<string | null>(null)
  const [error, setError] = useState('')
  const templatesQuery = useDocumentTemplates({ pageSize: 100, type: 'template' })
  const previewMutation = usePreviewGeneratedDocument()
  const issueMutation = useIssueGeneratedDocument(parent)
  const templates = useMemo(() => {
    return (templatesQuery.data?.templates ?? []).filter((template) =>
      template.tags.some((tag) => tag.toLowerCase() === family || tag.toLowerCase() === `outbound:${family}`),
    )
  }, [family, templatesQuery.data?.templates])
  const selectedTemplateId = templateDocumentId || templates[0]?.templateId || ''

  async function previewDocument() {
    try {
      setError('')
      setIssuedId(null)
      const result = await previewMutation.mutateAsync(toRequest(selectedTemplateId))
      setPreview(result)
    } catch (nextError) {
      setPreview(null)
      setError(describeDocumentError(nextError))
    }
  }

  async function issueDocument() {
    try {
      setError('')
      const result = await issueMutation.mutateAsync(toRequest(selectedTemplateId))
      setIssuedId(result.documentId)
      setPreview(null)
    } catch (nextError) {
      setError(describeDocumentError(nextError))
    }
  }

  function toRequest(nextTemplateId: string): GeneratedDocumentRequestDto {
    return {
      parent,
      artifactFamily: family,
      templateDocumentId: nextTemplateId,
      templateVersion: null,
      classification,
      regeneratedFromDocumentId: null,
      regenerationReason: null,
    }
  }

  return (
    <div className="rounded-lg border border-surface-border bg-surface-card/40 p-4">
      <div className="grid gap-3 md:grid-cols-[minmax(8rem,0.7fr)_minmax(9rem,0.7fr)_minmax(12rem,1.2fr)_auto] md:items-end">
        <Select
          label="Family"
          value={family}
          onChange={(event) => {
            setFamily(event.target.value as GeneratedArtifactFamily)
            setTemplateDocumentId('')
            setPreview(null)
            setIssuedId(null)
          }}
          options={FAMILY_OPTIONS}
        />
        <Select
          label="Classification"
          value={classification}
          onChange={(event) => setClassification(event.target.value as DocumentClassification)}
          options={CLASSIFICATION_OPTIONS}
        />
        <Select
          label="Template"
          value={selectedTemplateId}
          onChange={(event) => {
            setTemplateDocumentId(event.target.value)
            setPreview(null)
            setIssuedId(null)
          }}
          options={templates.map((template) => ({ value: template.templateId, label: template.logicalName }))}
        />
        <div className="flex gap-2">
          <button
            type="button"
            onClick={previewDocument}
            disabled={!selectedTemplateId || previewMutation.isPending}
            className="inline-flex items-center justify-center gap-1.5 rounded-lg border border-surface-border px-3 py-2 text-sm text-text-secondary hover:bg-surface-card disabled:opacity-60"
          >
            <ScanSearch size={15} />
            Preview
          </button>
          <button
            type="button"
            onClick={issueDocument}
            disabled={!selectedTemplateId || issueMutation.isPending || preview?.status === 'blocked'}
            className="inline-flex items-center justify-center gap-1.5 rounded-lg bg-nebula-violet px-3 py-2 text-sm font-medium text-white hover:bg-nebula-violet/90 disabled:opacity-60"
          >
            <FileCheck2 size={15} />
            Issue
          </button>
        </div>
      </div>
      {templatesQuery.isSuccess && templates.length === 0 && (
        <p className="mt-3 text-sm text-text-muted">No published {family.toUpperCase()} templates are available.</p>
      )}
      {preview && (
        <div className="mt-3 rounded-lg border border-surface-border px-3 py-2 text-sm text-text-secondary">
          <span className="font-medium text-text-primary">{preview.status}</span>
          <span> / template v{preview.templateVersion}</span>
          <span> / {preview.sourceSnapshotHash.slice(0, 18)}</span>
        </div>
      )}
      {issuedId && <p className="mt-3 text-sm text-status-success">Issued as {issuedId}.</p>}
      {error && <p className="mt-3 text-sm text-status-error">{error}</p>}
    </div>
  )
}
