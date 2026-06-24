import { useQuery, useMutation } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'
import type { AnswerFeedback, AttemptResult, SubmitAnswerInput } from '../model/types'

export const attemptApi = {
  startAttempt: (testId: string) =>
    apiClient.post<{ attemptId: string }>(`/api/tests/${testId}/attempts`).then((r) => r.data),

  submitAnswer: (attemptId: string, input: SubmitAnswerInput) =>
    apiClient.post<AnswerFeedback>(`/api/attempts/${attemptId}/answer`, input).then((r) => r.data),

  finishAttempt: (attemptId: string) =>
    apiClient.post(`/api/attempts/${attemptId}/finish`).then((r) => r.data),

  getAttemptResults: (attemptId: string) =>
    apiClient.get<AttemptResult>(`/api/attempts/${attemptId}`).then((r) => r.data),
}

export function useStartAttemptMutation() {
  return useMutation({
    mutationFn: attemptApi.startAttempt,
  })
}

export function useSubmitAnswerMutation() {
  return useMutation({
    mutationFn: ({ attemptId, input }: { attemptId: string; input: SubmitAnswerInput }) =>
      attemptApi.submitAnswer(attemptId, input),
  })
}

export function useFinishAttemptMutation() {
  return useMutation({
    mutationFn: attemptApi.finishAttempt,
  })
}

export function useAttemptResults(attemptId: string) {
  return useQuery({
    queryKey: ['attempts', attemptId],
    queryFn: () => attemptApi.getAttemptResults(attemptId),
    enabled: !!attemptId,
  })
}
