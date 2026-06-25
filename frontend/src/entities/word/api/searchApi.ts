import { useQuery } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'

export interface SearchWordResult {
  wordId: string
  blockId: string
  blockTitle: string
  term: string
  translation: string
  wordType: string
  rank: number
}

export const searchKeys = {
  results: (q: string, lang?: number) => ['search', q, lang] as const,
}

export function useSearchWords(q: string, lang?: number) {
  return useQuery({
    queryKey: searchKeys.results(q, lang),
    queryFn: () =>
      apiClient.get<SearchWordResult[]>('/api/search', { params: { q, lang } }).then((r) => r.data),
    enabled: q.trim().length >= 2,
    staleTime: 30_000,
  })
}
