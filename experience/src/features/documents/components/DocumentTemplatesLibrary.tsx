import { useState } from 'react'
import { FilePlus2, Upload } from 'lucide-react'
import { DashboardLayout } from '@/components/layout/DashboardLayout'
import { Card, CardHeader, CardTitle } from '@/components/ui/Card'
import { Skeleton } from '@/components/ui/Skeleton'
import { useDocumentTemplates, useUploadDocumentTemplate } from '../hooks'
import type { DocumentClassification } from '../types'
import { describeDocumentError, TemplateUploadFields } from './DocumentUploadDialog'

export function DocumentTemplatesLibrary() {
  const templatesQuery = useDocumentTemplates()
  const upload = useUploadDocumentTemplate()
  const [file, setFile] = useState<File | null>(null)
  const [classification, setClassification] = useState<DocumentClassification>('confidential')
  const [tags, setTags] = useState('')
  const [error, setError] = useState('')

  async function submit() {
    if (!file) {
      setError('Select a template file.')
      return
    }

    try {
      setError('')
      await upload.mutateAsync({ file, classification, tags: parseTags(tags) })
      setFile(null)
      setTags('')
    } catch (nextError) {
      setError(describeDocumentError(nextError))
    }
  }

  return (
    <DashboardLayout title="Document Templates">
      <div className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>Upload Template</CardTitle>
          </CardHeader>
          <div className="space-y-4">
            <TemplateUploadFields
              file={file}
              classification={classification}
              tags={tags}
              onFile={setFile}
              onClassification={setClassification}
              onTags={setTags}
            />
            {error && <p className="text-sm text-status-error">{error}</p>}
            <button
              type="button"
              onClick={submit}
              disabled={upload.isPending}
              className="inline-flex items-center gap-1.5 rounded-lg bg-nebula-violet px-3 py-1.5 text-sm font-medium text-white hover:bg-nebula-violet/90 disabled:opacity-60"
            >
              <Upload size={15} />
              {upload.isPending ? 'Uploading' : 'Upload Template'}
            </button>
          </div>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Template Library</CardTitle>
          </CardHeader>
          {templatesQuery.isLoading ? (
            <div className="space-y-2">
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
            </div>
          ) : templatesQuery.data?.templates.length ? (
            <div className="divide-y divide-surface-border overflow-hidden rounded-lg border border-surface-border">
              {templatesQuery.data.templates.map((template) => (
                <div key={template.templateId} className="grid gap-2 bg-surface-card/40 px-4 py-3 sm:grid-cols-[1fr_auto]">
                  <div>
                    <p className="text-sm font-medium text-text-primary">{template.logicalName}</p>
                    <p className="mt-1 text-xs text-text-muted">
                      {template.classification} / used {template.useCount} times / {formatDate(template.uploadedAtUtc)}
                    </p>
                  </div>
                  <span className="inline-flex items-center gap-1.5 rounded-full border border-surface-border px-2 py-0.5 text-xs text-text-secondary">
                    <FilePlus2 size={13} />
                    {template.type}
                  </span>
                </div>
              ))}
            </div>
          ) : (
            <div className="rounded-lg border border-dashed border-surface-border px-4 py-6 text-center">
              <p className="text-sm text-text-secondary">No document templates yet.</p>
            </div>
          )}
        </Card>
      </div>
    </DashboardLayout>
  )
}

function parseTags(value: string) {
  return value.split(',').map((tag) => tag.trim()).filter(Boolean)
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(value))
}
