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
      // Stop polling on every terminal status — 'failed' included, otherwise the poller runs forever
      return status === 'ready' || status === 'archived' || status === 'failed' ? false : 2000
    },
    select: (data) => data.status,
  })
}
