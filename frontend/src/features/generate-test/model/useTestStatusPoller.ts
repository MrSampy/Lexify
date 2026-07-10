import { useQuery } from '@tanstack/react-query'
import { apiClient } from '@/shared/api'
import { testKeys } from '@/entities/test'
import type { Test } from '@/entities/test'

export function useTestStatusPoller(testId: string | null) {
  return useQuery({
    queryKey: testKeys.detail(testId ?? ''),
    queryFn: () => apiClient.get<Test>(`/api/tests/${testId}`).then((r) => r.data),
    enabled: !!testId,
    // Generation takes 30-60s, so users routinely switch away mid-wait. By default React Query
    // pauses refetchInterval while the tab is hidden AND skips the focus refetch because the
    // global staleTime (2 min) still considers the data fresh — the poller never fires again and
    // the "Generating..." spinner hangs until a manual reload. Poll in the background and treat
    // the status as always-stale instead.
    refetchIntervalInBackground: true,
    staleTime: 0,
    refetchInterval: (query) => {
      const status = query.state.data?.status
      // Stop polling on every terminal status — 'failed' included, otherwise the poller runs forever
      return status === 'ready' || status === 'archived' || status === 'failed' ? false : 2000
    },
    select: (data) => data.status,
  })
}
