import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'
import { blockKeys } from './blockApi'

export const tagKeys = {
  userTags: ['tags', 'user'] as const,
}

const tagApiClient = {
  getUserTags: () => apiClient.get<string[]>('/api/tags').then((r) => r.data),

  addTag: (blockId: string, tagName: string) =>
    apiClient.post(`/api/blocks/${blockId}/tags`, { tagName }).then((r) => r.data),

  removeTag: (blockId: string, tagName: string) =>
    apiClient
      .delete(`/api/blocks/${blockId}/tags/${encodeURIComponent(tagName)}`)
      .then((r) => r.data),
}

export function useUserTags() {
  return useQuery({
    queryKey: tagKeys.userTags,
    queryFn: tagApiClient.getUserTags,
    staleTime: 60_000,
  })
}

export function useAddTagMutation(blockId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (tagName: string) => tagApiClient.addTag(blockId, tagName),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: blockKeys.detail(blockId) })
      void queryClient.invalidateQueries({ queryKey: blockKeys.all })
      void queryClient.invalidateQueries({ queryKey: tagKeys.userTags })
    },
  })
}

export function useRemoveTagMutation(blockId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (tagName: string) => tagApiClient.removeTag(blockId, tagName),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: blockKeys.detail(blockId) })
      void queryClient.invalidateQueries({ queryKey: blockKeys.all })
      void queryClient.invalidateQueries({ queryKey: tagKeys.userTags })
    },
  })
}
