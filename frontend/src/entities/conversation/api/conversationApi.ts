import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'
import type { PagedResult } from '@/shared/api'
import type {
  ConversationDetail,
  ConversationListItem,
  ConversationSummary,
  StartConversationInput,
  StartConversationResult,
} from '../model/types'

export const conversationKeys = {
  all: ['conversations'] as const,
  list: (page?: number) => [...conversationKeys.all, 'list', { page }] as const,
  detail: (id: string) => [...conversationKeys.all, 'detail', id] as const,
}

export const conversationApi = {
  getConversations: (page = 1, pageSize = 20) =>
    apiClient
      .get<PagedResult<ConversationListItem>>('/api/conversations', { params: { page, pageSize } })
      .then((r) => r.data),

  getConversation: (id: string) =>
    apiClient.get<ConversationDetail>(`/api/conversations/${id}`).then((r) => r.data),

  start: (input: StartConversationInput) =>
    apiClient.post<StartConversationResult>('/api/conversations', input).then((r) => r.data),

  end: (id: string) =>
    apiClient.post<ConversationSummary>(`/api/conversations/${id}/end`).then((r) => r.data),
}

export function useConversations(page = 1) {
  return useQuery({
    queryKey: conversationKeys.list(page),
    queryFn: () => conversationApi.getConversations(page),
  })
}

export function useConversation(id: string) {
  return useQuery({
    queryKey: conversationKeys.detail(id),
    queryFn: () => conversationApi.getConversation(id),
    enabled: !!id,
  })
}

export function useStartConversationMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: conversationApi.start,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: conversationKeys.all })
    },
  })
}

export function useEndConversationMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: conversationApi.end,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: conversationKeys.all })
    },
  })
}
