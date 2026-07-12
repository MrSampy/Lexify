import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'
import type { PagedResult } from '@/shared/api'
import type { Word, CreateWordInput, UpdateWordInput } from '../model/types'

export const wordKeys = {
  all: (blockId: string) => ['words', blockId] as const,
  list: (blockId: string, params: Record<string, unknown>) =>
    [...wordKeys.all(blockId), 'list', params] as const,
}

const wordApi = {
  getWords: (blockId: string, search?: string, page = 1, pageSize = 50) =>
    apiClient
      .get<PagedResult<Word>>(`/api/blocks/${blockId}/words`, {
        params: { search, page, pageSize },
      })
      .then((r) => r.data),

  createWord: (blockId: string, input: CreateWordInput) =>
    apiClient.post<string>(`/api/blocks/${blockId}/words`, input).then((r) => r.data),

  updateWord: (blockId: string, wordId: string, input: UpdateWordInput) =>
    apiClient.patch(`/api/blocks/${blockId}/words/${wordId}`, input).then((r) => r.data),

  deleteWord: (blockId: string, wordId: string) =>
    apiClient.delete(`/api/blocks/${blockId}/words/${wordId}`).then((r) => r.data),

  importWords: (blockId: string, words: CreateWordInput[]) =>
    apiClient.post<number>(`/api/blocks/${blockId}/words/import`, { words }).then((r) => r.data),

  bulkDeleteWords: (blockId: string, wordIds: string[]) =>
    apiClient
      .post<number>(`/api/blocks/${blockId}/words/bulk-delete`, { wordIds })
      .then((r) => r.data),

  bulkMoveWords: (blockId: string, targetBlockId: string, wordIds: string[]) =>
    apiClient
      .post<number>(`/api/blocks/${blockId}/words/bulk-move`, { targetBlockId, wordIds })
      .then((r) => r.data),
}

export function useWords(blockId: string, search?: string, page = 1) {
  return useQuery({
    queryKey: wordKeys.list(blockId, { search, page }),
    queryFn: () => wordApi.getWords(blockId, search, page),
    enabled: !!blockId,
  })
}

// BlockDetailPage renders its word list from useBlock's ['blocks', 'detail', id] query,
// not from useWords, so word mutations must also invalidate the 'blocks' tree to refresh it.
export function useCreateWordMutation(blockId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: CreateWordInput) => wordApi.createWord(blockId, input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: wordKeys.all(blockId) })
      void queryClient.invalidateQueries({ queryKey: ['blocks'] })
    },
  })
}

export function useUpdateWordMutation(blockId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ wordId, input }: { wordId: string; input: UpdateWordInput }) =>
      wordApi.updateWord(blockId, wordId, input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: wordKeys.all(blockId) })
      void queryClient.invalidateQueries({ queryKey: ['blocks'] })
    },
  })
}

export function useDeleteWordMutation(blockId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (wordId: string) => wordApi.deleteWord(blockId, wordId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: wordKeys.all(blockId) })
      void queryClient.invalidateQueries({ queryKey: ['blocks'] })
    },
  })
}

export function useBulkDeleteWordsMutation(blockId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (wordIds: string[]) => wordApi.bulkDeleteWords(blockId, wordIds),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: wordKeys.all(blockId) })
      void queryClient.invalidateQueries({ queryKey: ['blocks'] })
    },
  })
}

export function useBulkMoveWordsMutation(blockId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ targetBlockId, wordIds }: { targetBlockId: string; wordIds: string[] }) =>
      wordApi.bulkMoveWords(blockId, targetBlockId, wordIds),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: wordKeys.all(blockId) })
      void queryClient.invalidateQueries({ queryKey: ['blocks'] })
    },
  })
}

export function useImportWordsMutation(blockId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (words: CreateWordInput[]) => wordApi.importWords(blockId, words),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: wordKeys.all(blockId) })
      void queryClient.invalidateQueries({ queryKey: ['blocks'] })
    },
  })
}
