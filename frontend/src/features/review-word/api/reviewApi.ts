import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'
import type { Word } from '@/entities/word'

export const reviewKeys = {
  all: ['review'] as const,
  due: () => [...reviewKeys.all, 'due'] as const,
}

export const reviewApi = {
  getDueWords: () => apiClient.get<Word[]>('/api/review/due').then((r) => r.data),
  rateWord: (input: { wordId: string; quality: number }) =>
    apiClient.post('/api/review/rate', input).then((r) => r.data),
}

export function useDueWords() {
  return useQuery({
    queryKey: reviewKeys.due(),
    queryFn: reviewApi.getDueWords,
  })
}

export function useRateWordMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: reviewApi.rateWord,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: reviewKeys.due() })
    },
  })
}
