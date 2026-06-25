import { useQuery } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'

export interface UserStats {
  totalBlocks: number
  totalWords: number
  dueWordsCount: number
  wordsAnsweredThisWeek: number
  testsCompletedThisWeek: number
}

export function useUserStats() {
  return useQuery({
    queryKey: ['user', 'stats'],
    queryFn: () => apiClient.get<UserStats>('/api/stats').then((r) => r.data),
    staleTime: 60_000,
  })
}
