import { useQuery } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'
import { testKeys } from '@/entities/test'
import type { Test } from '@/entities/test'

export function useTestStatusPoller(testId: string | null) {
  return useQuery({
    queryKey: testKeys.detail(testId ?? ''),
    queryFn: () => apiClient.get<Test>(`/api/tests/${testId}`).then((r) => r.data),
    enabled: !!testId,
    refetchInterval: (query) => {
      const status = query.state.data?.status
      return status === 'ready' || status === 'archived' ? false : 2000
    },
    select: (data) => data.status,
  })
}
