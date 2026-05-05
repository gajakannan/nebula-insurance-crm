import { useEffect, useState } from 'react'
import { Download, Save, Upload } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { DashboardLayout } from '@/components/layout/DashboardLayout'
import { Card, CardHeader, CardTitle } from '@/components/ui/Card'
import { ErrorFallback } from '@/components/ui/ErrorFallback'
import { Select } from '@/components/ui/Select'
import { Skeleton } from '@/components/ui/Skeleton'
import { TextInput } from '@/components/ui/TextInput'
import { useDocumentDetail, useDocumentMetadataSchemas, useDocumentPreviewUrl, useDownloadDocumentVersion, useReplaceDocument, useUpdateDocumentMetadata } from '../hooks'
import type { DocumentClassification, DocumentMetadata, DocumentVersionDto } from '../types'
import { DocumentMetadataFields } from './DocumentMetadataFields'
import { describeDocumentError } from './DocumentUploadDialog'

const CLASSIFICATION_OPTIONS = [
  { value: 'public', label: 'Public' },
  { value: 'confidential', label: 'Confidential' },
  { value: 'restricted', label: 'Restricted' },
]

export function DocumentDetailView() {
  const { documentId = '' } = useParams<{ documentId: string }>()
  const detailQuery = useDocumentDetail(documentId)
  const metadataSchemas = useDocumentMetadataSchemas()
  const detail = detailQuery.data
  const parent = detail?.sidecar.parent
  const updateMetadata = useUpdateDocumentMetadata(documentId, parent)
  const replaceDocument = useReplaceDocument(documentId, parent)
  const download = useDownloadDocumentVersion()
  const [classification, setClassification] = useState<DocumentClassification>('confidential')
  const [type, setType] = useState('')
  const [tags, setTags] = useState('')
  const [documentMetadata, setDocumentMetadata] = useState<DocumentMetadata>({})
  const [replacement, setReplacement] = useState<File | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!detail) return
    setClassification(detail.sidecar.classification)
    setType(detail.sidecar.type)
    setTags(detail.sidecar.tags.join(', '))
    setDocumentMetadata(detail.sidecar.metadata ?? {})
  }, [detail])

  async function saveMetadata() {
    try {
      setError('')
      await updateMetadata.mutateAsync({
        classification,
        type,
        tags: parseTags(tags),
        metadata: documentMetadata,
      })
    } catch (nextError) {
      setError(describeDocumentError(nextError))
    }
  }

  async function submitReplacement() {
    if (!replacement) return
    try {
      setError('')
      await replaceDocument.mutateAsync(replacement)
      setReplacement(null)
    } catch (nextError) {
      setError(describeDocumentError(nextError))
    }
  }

  async function downloadVersion(version: number | 'latest') {
    try {
      setError('')
      const blob = await download.mutateAsync({ documentId, versionRef: version })
      const url = URL.createObjectURL(blob)
      const anchor = document.createElement('a')
      anchor.href = url
      anchor.target = '_blank'
      anchor.rel = 'noopener noreferrer'
      anchor.click()
      window.setTimeout(() => URL.revokeObjectURL(url), 30_000)
    } catch (nextError) {
      setError(describeDocumentError(nextError))
    }
  }

  if (detailQuery.isLoading) {
    return (
      <DashboardLayout title="Document">
        <div className="space-y-4">
          <Skeleton className="h-16 w-full" />
          <Skeleton className="h-80 w-full" />
        </div>
      </DashboardLayout>
    )
  }

  if (detailQuery.error || !detail) {
    return (
      <DashboardLayout title="Document">
        <ErrorFallback message="Unable to load document." onRetry={() => detailQuery.refetch()} />
      </DashboardLayout>
    )
  }

  const latest = [...detail.sidecar.versions].sort((a, b) => b.n - a.n)[0]
  const previewVersion = [...detail.sidecar.versions]
    .filter((version) => version.status === 'available' && previewKindOf(version.fileName) !== null)
    .sort((a, b) => b.n - a.n)[0]
  const schema = metadataSchemas.data?.schemas.find((item) => item.id === type)?.schema

  return (
    <DashboardLayout title="Document">
      <div className="space-y-6">
        <Link to={parentLink(detail.sidecar.parent)} className="text-xs text-text-muted hover:text-text-secondary">
          Back to parent record
        </Link>

        <Card>
          <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
            <div>
              <div className="flex flex-wrap gap-2">
                <Pill>{detail.sidecar.classification}</Pill>
                <Pill>{latest?.status ?? 'unknown'}</Pill>
                <Pill>{detail.sidecar.type}</Pill>
              </div>
              <h2 className="mt-3 text-2xl font-semibold text-text-primary">{detail.sidecar.logicalName}</h2>
              <p className="mt-2 text-sm text-text-secondary">
                Version {latest?.n ?? 0} / {latest ? formatBytes(latest.sizeBytes) : 'No binary'} / {formatDate(detail.sidecar.auditTimestamps.updatedAtUtc)}
              </p>
            </div>
            <button
              type="button"
              onClick={() => downloadVersion('latest')}
              disabled={!latest || latest.status !== 'available'}
              className="inline-flex items-center justify-center gap-1.5 rounded-lg bg-nebula-violet px-3 py-1.5 text-sm font-medium text-white hover:bg-nebula-violet/90 disabled:opacity-60"
            >
              <Download size={15} />
              Download Latest
            </button>
          </div>
        </Card>

        {previewVersion && (
          <Card>
            <CardHeader>
              <CardTitle>Preview</CardTitle>
            </CardHeader>
            <DocumentPreview documentId={documentId} version={previewVersion} />
          </Card>
        )}

        <div className="grid gap-6 xl:grid-cols-[1fr_0.8fr]">
          <Card>
            <CardHeader>
              <CardTitle>Versions</CardTitle>
            </CardHeader>
            <div className="divide-y divide-surface-border overflow-hidden rounded-lg border border-surface-border">
              {detail.sidecar.versions.map((version) => (
                <div key={version.n} className="grid gap-2 bg-surface-card/40 px-4 py-3 sm:grid-cols-[1fr_auto]">
                  <div>
                    <p className="text-sm font-medium text-text-primary">{version.fileName}</p>
                    <p className="mt-1 text-xs text-text-muted">
                      v{version.n} / {formatBytes(version.sizeBytes)} / {formatDate(version.uploadedAt)}
                    </p>
                  </div>
                  <button
                    type="button"
                    onClick={() => downloadVersion(version.n)}
                    disabled={version.status !== 'available'}
                    className="inline-flex items-center justify-center gap-1.5 rounded-lg border border-surface-border px-3 py-1.5 text-sm text-text-secondary hover:bg-surface-card disabled:opacity-60"
                  >
                    <Download size={15} />
                    Open
                  </button>
                </div>
              ))}
            </div>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Metadata</CardTitle>
            </CardHeader>
            <div className="space-y-4">
              <Select
                label="Classification"
                value={classification}
                onChange={(event) => setClassification(event.target.value as DocumentClassification)}
                options={CLASSIFICATION_OPTIONS}
              />
              <TextInput
                label="Type"
                value={type}
                onChange={(event) => {
                  setType(event.target.value)
                  if (event.target.value !== detail.sidecar.type) setDocumentMetadata({})
                }}
              />
              <TextInput label="Tags" value={tags} onChange={(event) => setTags(event.target.value)} />
              <DocumentMetadataFields
                schema={schema}
                value={documentMetadata}
                onChange={setDocumentMetadata}
              />
              <button
                type="button"
                onClick={saveMetadata}
                disabled={updateMetadata.isPending}
                className="inline-flex items-center gap-1.5 rounded-lg bg-nebula-violet px-3 py-1.5 text-sm font-medium text-white hover:bg-nebula-violet/90 disabled:opacity-60"
              >
                <Save size={15} />
                Save Metadata
              </button>
            </div>
          </Card>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Replace Binary</CardTitle>
          </CardHeader>
          <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
            <div className="flex-1 space-y-1.5">
              <label htmlFor="replacement-file" className="block text-xs font-medium text-text-secondary">
                Replacement file
              </label>
              <input
                id="replacement-file"
                type="file"
                accept=".pdf,.png,.docx,.xlsx,.csv"
                onChange={(event) => setReplacement(event.target.files?.[0] ?? null)}
                className="block w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-secondary file:mr-3 file:rounded-md file:border-0 file:bg-nebula-violet/15 file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-nebula-violet"
              />
            </div>
            <button
              type="button"
              onClick={submitReplacement}
              disabled={!replacement || replaceDocument.isPending}
              className="inline-flex items-center justify-center gap-1.5 rounded-lg bg-nebula-violet px-3 py-2 text-sm font-medium text-white hover:bg-nebula-violet/90 disabled:opacity-60"
            >
              <Upload size={15} />
              Replace
            </button>
          </div>
          {error && <p className="mt-3 text-sm text-status-error">{error}</p>}
        </Card>
      </div>
    </DashboardLayout>
  )
}

type PreviewKind = 'pdf' | 'image'

function previewKindOf(fileName: string): PreviewKind | null {
  const extension = fileName.slice(fileName.lastIndexOf('.')).toLowerCase()
  if (extension === '.pdf') return 'pdf'
  if (extension === '.png') return 'image'
  return null
}

function DocumentPreview({ documentId, version }: { documentId: string; version: DocumentVersionDto }) {
  const kind = previewKindOf(version.fileName)
  const preview = useDocumentPreviewUrl(documentId, version.n)

  if (preview.isLoading) {
    return <Skeleton className="h-[600px] w-full" />
  }

  if (preview.error || !preview.url) {
    return (
      <p className="rounded-lg border border-status-error/30 bg-status-error/10 px-3 py-2 text-sm text-status-error">
        Unable to load preview.
      </p>
    )
  }

  if (kind === 'pdf') {
    return (
      <iframe
        src={preview.url}
        title={`Preview of ${version.fileName}`}
        className="h-[600px] w-full rounded-lg border border-surface-border bg-white"
      />
    )
  }

  return (
    <img
      src={preview.url}
      alt={`Preview of ${version.fileName}`}
      className="max-h-[600px] w-auto rounded-lg border border-surface-border"
    />
  )
}

function parentLink(parent: { type: string; id: string }) {
  if (parent.type === 'account') return `/accounts/${parent.id}`
  if (parent.type === 'policy') return `/policies/${parent.id}`
  return `/${parent.type}s/${parent.id}`
}

function parseTags(value: string) {
  return value.split(',').map((tag) => tag.trim()).filter(Boolean)
}

function Pill({ children }: { children: React.ReactNode }) {
  return (
    <span className="rounded-full border border-surface-border bg-surface-card px-2 py-0.5 text-xs text-text-secondary">
      {children}
    </span>
  )
}

function formatBytes(value: number) {
  if (value < 1024) return `${value} B`
  if (value < 1024 * 1024) return `${Math.round(value / 1024)} KB`
  return `${(value / 1024 / 1024).toFixed(1)} MB`
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(value))
}
