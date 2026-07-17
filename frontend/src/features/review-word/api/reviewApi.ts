import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'
import type { Word } from '@/entities/word'

export interface ReviewParams {
  /** Restrict the session to one block. */
  blockId?: string
  /** Cram mode: practise every word in scope regardless of its due date. */
  cram?: boolean
}

/** Review session queue with its new/review composition and the daily new-word budget. */
export interface ReviewQueue {
  words: Word[]
  newCount: number
  reviewCount: number
  newLimit: number
  newIntroducedToday: number
}

/** Post-review SM-2 state returned by the rate endpoint. */
export interface RateWordResult {
  intervalDays: number
  nextReviewAt: string
  easeFactor: number
  repetitions: number
  isLeech: boolean
}

export const reviewKeys = {
  all: ['review'] as const,
  due: (params: ReviewParams = {}) =>
    [...reviewKeys.all, 'due', params.blockId ?? null, params.cram ?? false] as const,
}

export const reviewApi = {
  getDueWords: (params: ReviewParams = {}) =>
    apiClient
      .get<ReviewQueue>('/api/review/due', {
        params: {
          blockId: params.blockId,
          mode: params.cram ? 'cram' : undefined,
        },
      })
      .then((r) => r.data),
  rateWord: (input: { wordId: string; quality: number }) =>
    apiClient.post<RateWordResult>('/api/review/rate', input).then((r) => r.data),
}

export function useDueWords(params: ReviewParams = {}) {
  return useQuery({
    queryKey: reviewKeys.due(params),
    queryFn: () => reviewApi.getDueWords(params),
  })
}

export function useRateWordMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: reviewApi.rateWord,
    onSuccess: () => {
      // Invalidate every due-list variant (all block/cram combinations) plus stats, which depend
      // on review activity (streak, heatmap, mastery).
      void queryClient.invalidateQueries({ queryKey: reviewKeys.all })
      void queryClient.invalidateQueries({ queryKey: ['user', 'stats'] })
    },
  })
}
