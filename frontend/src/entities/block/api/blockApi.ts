import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'
import type { PagedResult } from '@/shared/api'
import type {
  WordBlock,
  BlockFilter,
  CreateBlockInput,
  UpdateBlockInput,
  WordInBlock,
} from '../model/types'

export const blockKeys = {
  all: ['blocks'] as const,
  list: (f: BlockFilter) => [...blockKeys.all, 'list', f] as const,
  detail: (id: string) => [...blockKeys.all, 'detail', id] as const,
}

export interface BlockDetail {
  block: WordBlock
  words: PagedResult<WordInBlock>
  /** Confidence-flagged words in the whole block, not just the current page. */
  flaggedCount: number
}

const blockApi = {
  getBlocks: (filter: BlockFilter) =>
    apiClient.get<PagedResult<WordBlock>>('/api/blocks', { params: filter }).then((r) => r.data),

  getBlock: (id: string, wordsPage = 1, wordsPageSize = 50) =>
    apiClient
      .get<BlockDetail>(`/api/blocks/${id}`, {
        params: { wordsPage, wordsPageSize },
      })
      .then((r) => r.data),

  createBlock: (input: CreateBlockInput) =>
    apiClient.post<string>('/api/blocks', input).then((r) => r.data),

  updateBlock: (id: string, input: UpdateBlockInput) =>
    apiClient.patch(`/api/blocks/${id}`, input).then((r) => r.data),

  deleteBlock: (id: string) => apiClient.delete(`/api/blocks/${id}`).then((r) => r.data),

  exportBlock: (id: string) =>
    apiClient
      .get(`/api/blocks/${id}/export`, { params: { format: 'csv' }, responseType: 'blob' })
      .then((r) => ({ blob: r.data as Blob, filename: `block-${id}.csv` })),
}

export function useBlocks(filter: BlockFilter) {
  return useQuery({
    queryKey: blockKeys.list(filter),
    queryFn: () => blockApi.getBlocks(filter),
  })
}

export function useBlock(id: string, wordsPage = 1) {
  return useQuery({
    queryKey: [...blockKeys.detail(id), wordsPage],
    queryFn: () => blockApi.getBlock(id, wordsPage),
    enabled: !!id,
  })
}

export function useCreateBlockMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: CreateBlockInput) => blockApi.createBlock(input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: blockKeys.all })
    },
  })
}

export function useUpdateBlockMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: UpdateBlockInput }) =>
      blockApi.updateBlock(id, input),
    onSuccess: (_data, { id }) => {
      void queryClient.invalidateQueries({ queryKey: blockKeys.all })
      void queryClient.invalidateQueries({ queryKey: blockKeys.detail(id) })
    },
  })
}

export function useDeleteBlockMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => blockApi.deleteBlock(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: blockKeys.all })
    },
  })
}

export function useExportBlock() {
  return useMutation({
    mutationFn: (id: string) => blockApi.exportBlock(id),
    onSuccess: ({ blob, filename }) => {
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = filename
      a.click()
      URL.revokeObjectURL(url)
    },
  })
}
