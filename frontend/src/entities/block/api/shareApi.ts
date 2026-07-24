import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'
import { blockKeys } from './blockApi'

/** The owner's view of a link. Null from the GET endpoint means sharing is off for this block. */
export interface BlockShare {
  token: string
  createdAt: string
  viewCount: number
  copyCount: number
}

export interface SharedWord {
  term: string
  translation: string
  alternativeTranslations: string[]
  synonyms: string[]
  wordType: string
  notes: string | null
  exampleSentence: string | null
}

export interface SharedBlock {
  title: string
  description: string | null
  languageId: number
  wordCount: number
  ownerDisplayName: string | null
  words: SharedWord[]
}

export const shareKeys = {
  ofBlock: (blockId: string) => [...blockKeys.detail(blockId), 'share'] as const,
  shared: (token: string) => ['shared-block', token] as const,
}

const shareApi = {
  getShare: (blockId: string) =>
    apiClient.get<BlockShare | null>(`/api/blocks/${blockId}/share`).then((r) => r.data),

  createShare: (blockId: string) =>
    apiClient.post<BlockShare>(`/api/blocks/${blockId}/share`).then((r) => r.data),

  revokeShare: (blockId: string) =>
    apiClient.delete(`/api/blocks/${blockId}/share`).then((r) => r.data),

  getShared: (token: string) =>
    apiClient.get<SharedBlock>(`/api/shared/${token}`).then((r) => r.data),

  copyShared: (token: string) =>
    apiClient.post<string>(`/api/shared/${token}/copy`).then((r) => r.data),
}

/** Only fetched while the share dialog is open — the block page itself has no use for it. */
export function useBlockShare(blockId: string, enabled = true) {
  return useQuery({
    queryKey: shareKeys.ofBlock(blockId),
    queryFn: () => shareApi.getShare(blockId),
    enabled: !!blockId && enabled,
  })
}

export function useCreateShareMutation(blockId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => shareApi.createShare(blockId),
    onSuccess: (share) => {
      queryClient.setQueryData(shareKeys.ofBlock(blockId), share)
    },
  })
}

export function useRevokeShareMutation(blockId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => shareApi.revokeShare(blockId),
    onSuccess: () => {
      queryClient.setQueryData(shareKeys.ofBlock(blockId), null)
    },
  })
}

export function useSharedBlock(token: string) {
  return useQuery({
    queryKey: shareKeys.shared(token),
    queryFn: () => shareApi.getShared(token),
    enabled: !!token,
    // A dead link stays dead — retrying a 404 just delays the message.
    retry: false,
  })
}

export function useCopySharedBlockMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (token: string) => shareApi.copyShared(token),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: blockKeys.all })
    },
  })
}
