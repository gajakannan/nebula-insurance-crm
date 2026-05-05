import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/services/api'
import type {
  DocumentClassification,
  DocumentCompletenessSignalDto,
  DocumentDetailDto,
  DocumentMetadata,
  DocumentMetadataSchemaRegistryDto,
  DocumentMetadataUpdateRequestDto,
  DocumentParentRefDto,
  DocumentUploadResponseDto,
  PaginatedDocumentListDto,
  PaginatedDocumentTemplateListDto,
} from './types'

interface UseDocumentsOptions {
  parent: DocumentParentRefDto
  classification?: string
  type?: string
  page?: number
  pageSize?: number
  enabled?: boolean
}

export function useDocuments({
  parent,
  classification,
  type,
  page = 1,
  pageSize = 10,
  enabled = true,
}: UseDocumentsOptions) {
  const params = parentParams(parent)
  if (classification) params.set('classification', classification)
  if (type) params.set('type', type)
  params.set('page', String(page))
  params.set('pageSize', String(pageSize))

  return useQuery({
    queryKey: ['documents', 'parent', parent, { classification, type, page, pageSize }],
    queryFn: () => api.get<PaginatedDocumentListDto>(`/documents?${params.toString()}`),
    enabled: enabled && !!parent.id,
  })
}

export function useDocumentCompleteness(parent: DocumentParentRefDto, enabled = true) {
  const params = parentParams(parent)
  return useQuery({
    queryKey: ['documents', 'completeness', parent],
    queryFn: () => api.get<DocumentCompletenessSignalDto>(`/documents/completeness?${params.toString()}`),
    enabled: enabled && !!parent.id,
  })
}

export function useDocumentDetail(documentId: string) {
  return useQuery({
    queryKey: ['documents', 'detail', documentId],
    queryFn: () => api.get<DocumentDetailDto>(`/documents/${documentId}`),
    enabled: !!documentId,
  })
}

export function useDocumentMetadataSchemas() {
  return useQuery({
    queryKey: ['documents', 'metadata-schemas'],
    queryFn: () => api.get<DocumentMetadataSchemaRegistryDto>('/documents/metadata-schemas'),
  })
}

export function useUploadDocuments(parent: DocumentParentRefDto) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({
      files,
      classification,
      type,
      metadata,
    }: {
      files: File[]
      classification: DocumentClassification
      type?: string
      metadata?: DocumentMetadata
    }) => {
      const form = new FormData()
      for (const file of files) form.append('files', file)
      form.set('parentType', parent.type)
      form.set('parentId', parent.id)
      form.set('defaultClassification', classification)
      if (type) form.set('type', type)
      if (type || metadata) {
        const item = JSON.stringify({ type, metadata: metadata ?? {} })
        files.forEach(() => form.append('metadata', item))
      }
      return api.postMultipart<DocumentUploadResponseDto>('/documents', form)
    },
    onSuccess: () => invalidateParent(queryClient, parent),
  })
}

export function useReplaceDocument(documentId: string, parent?: DocumentParentRefDto) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (file: File) => {
      const form = new FormData()
      form.set('file', file)
      return api.putMultipart(`/documents/${documentId}/replace`, form)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documents', 'detail', documentId] })
      if (parent) invalidateParent(queryClient, parent)
    },
  })
}

export function useUpdateDocumentMetadata(documentId: string, parent?: DocumentParentRefDto) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (dto: DocumentMetadataUpdateRequestDto) => api.patch<DocumentDetailDto>(`/documents/${documentId}/metadata`, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documents', 'detail', documentId] })
      if (parent) invalidateParent(queryClient, parent)
    },
  })
}

export function useDownloadDocumentVersion() {
  return useMutation({
    mutationFn: ({ documentId, versionRef }: { documentId: string; versionRef: string | number }) =>
      api.downloadBlob(`/documents/${documentId}/versions/${versionRef}/binary`),
  })
}

interface DocumentPreviewState {
  url: string | null
  isLoading: boolean
  error: unknown
}

export function useDocumentPreviewUrl(documentId: string, version: number | null): DocumentPreviewState {
  const [state, setState] = useState<DocumentPreviewState>({ url: null, isLoading: false, error: null })

  useEffect(() => {
    if (!documentId || version == null) {
      setState({ url: null, isLoading: false, error: null })
      return
    }

    let cancelled = false
    let createdUrl: string | null = null
    setState({ url: null, isLoading: true, error: null })

    api.downloadBlob(`/documents/${documentId}/versions/${version}/binary`)
      .then((blob) => {
        if (cancelled) return
        createdUrl = URL.createObjectURL(blob)
        setState({ url: createdUrl, isLoading: false, error: null })
      })
      .catch((nextError) => {
        if (cancelled) return
        setState({ url: null, isLoading: false, error: nextError })
      })

    return () => {
      cancelled = true
      if (createdUrl) URL.revokeObjectURL(createdUrl)
    }
  }, [documentId, version])

  return state
}

export function useDocumentTemplates({ page = 1, pageSize = 20, type, classification }: {
  page?: number
  pageSize?: number
  type?: string
  classification?: string
} = {}) {
  const params = new URLSearchParams()
  params.set('page', String(page))
  params.set('pageSize', String(pageSize))
  if (type) params.set('type', type)
  if (classification) params.set('classification', classification)

  return useQuery({
    queryKey: ['document-templates', { page, pageSize, type, classification }],
    queryFn: () => api.get<PaginatedDocumentTemplateListDto>(`/document-templates?${params.toString()}`),
  })
}

export function useUploadDocumentTemplate() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ file, classification, tags }: { file: File; classification: DocumentClassification; tags: string[] }) => {
      const form = new FormData()
      form.set('file', file)
      form.set('classification', classification)
      form.set('tags', tags.join(','))
      return api.postMultipart('/document-templates', form)
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['document-templates'] }),
  })
}

export function useLinkDocumentTemplate(parent: DocumentParentRefDto) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (templateId: string) => {
      const params = parentParams(parent)
      return api.postMultipart(`/document-templates/${templateId}/link?${params.toString()}`, new FormData())
    },
    onSuccess: () => invalidateParent(queryClient, parent),
  })
}

function parentParams(parent: DocumentParentRefDto): URLSearchParams {
  const params = new URLSearchParams()
  params.set('parent.type', parent.type)
  params.set('parent.id', parent.id)
  return params
}

function invalidateParent(queryClient: ReturnType<typeof useQueryClient>, parent: DocumentParentRefDto) {
  queryClient.invalidateQueries({ queryKey: ['documents', 'parent', parent] })
  queryClient.invalidateQueries({ queryKey: ['documents', 'completeness', parent] })
}
