import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'
import type { PagedResult } from '@/shared/api'
import type { FeedbackListItem, SubmitFeedbackInput, SubmitFeedbackResult } from '../model/types'

export const feedbackKeys = {
  all: ['feedback'] as const,
  mine: (page?: number) => [...feedbackKeys.all, 'mine', { page }] as const,
}

function toFormData(input: SubmitFeedbackInput): FormData {
  const form = new FormData()
  form.append('type', input.type)
  form.append('subject', input.subject)
  form.append('message', input.message)
  form.append('consent', String(input.consent))

  if (input.category) form.append('category', input.category)
  if (input.rating != null) form.append('rating', String(input.rating))
  if (input.contactEmail) form.append('contactEmail', input.contactEmail)

  // Repeated field name — how ASP.NET binds List<IFormFile>.
  input.attachments.forEach((file) => form.append('attachments', file))

  return form
}

export const feedbackApi = {
  // No explicit Content-Type: axios must set the multipart boundary itself.
  submit: (input: SubmitFeedbackInput) =>
    apiClient.post<SubmitFeedbackResult>('/api/feedback', toFormData(input)).then((r) => r.data),

  getMine: (page = 1, pageSize = 20) =>
    apiClient
      .get<PagedResult<FeedbackListItem>>('/api/feedback/mine', { params: { page, pageSize } })
      .then((r) => r.data),
}

export function useMyFeedback(page = 1) {
  return useQuery({
    queryKey: feedbackKeys.mine(page),
    queryFn: () => feedbackApi.getMine(page),
  })
}

export function useSubmitFeedbackMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: feedbackApi.submit,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: feedbackKeys.all })
    },
  })
}
