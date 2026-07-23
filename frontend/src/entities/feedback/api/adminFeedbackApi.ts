import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'
import type { PagedResult } from '@/shared/api'
import type { AdminFeedbackParams, FeedbackDetail, FeedbackListItem } from '../model/types'

// Admin triage of feedback lives here rather than in entities/admin: FSD forbids one entity from
// importing another, and these endpoints return the same Feedback shapes this entity already owns.
export const adminFeedbackKeys = {
  all: ['admin', 'feedback'] as const,
  list: (params: AdminFeedbackParams) => [...adminFeedbackKeys.all, 'list', params] as const,
  detail: (id: string) => [...adminFeedbackKeys.all, 'detail', id] as const,
}

export const adminFeedbackApi = {
  getList: (params: AdminFeedbackParams) =>
    apiClient
      .get<PagedResult<FeedbackListItem>>('/api/admin/feedback', { params })
      .then((r) => r.data),

  getDetail: (id: string) =>
    apiClient.get<FeedbackDetail>(`/api/admin/feedback/${id}`).then((r) => r.data),

  updateStatus: ({ id, status, adminNote }: UpdateFeedbackStatusInput) =>
    apiClient.put(`/api/admin/feedback/${id}/status`, { status, adminNote }).then((r) => r.data),

  // Fetched as a blob rather than linked directly — the endpoint needs the Authorization header.
  downloadAttachment: (feedbackId: string, attachmentId: string) =>
    apiClient
      .get<Blob>(`/api/admin/feedback/${feedbackId}/attachments/${attachmentId}`, {
        responseType: 'blob',
      })
      .then((r) => r.data),
}

export interface UpdateFeedbackStatusInput {
  id: string
  status: string
  adminNote?: string | null
}

export function useAdminFeedback(params: AdminFeedbackParams) {
  return useQuery({
    queryKey: adminFeedbackKeys.list(params),
    queryFn: () => adminFeedbackApi.getList(params),
    staleTime: 60_000,
  })
}

export function useAdminFeedbackDetail(id: string | null) {
  return useQuery({
    queryKey: adminFeedbackKeys.detail(id ?? ''),
    queryFn: () => adminFeedbackApi.getDetail(id!),
    enabled: !!id,
  })
}

export function useUpdateFeedbackStatusMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: adminFeedbackApi.updateStatus,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: adminFeedbackKeys.all })
    },
  })
}
