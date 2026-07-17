import { QueryClient, QueryClientProvider, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, type ReactNode } from 'react'
import { useAuthStore } from '@/entities/user'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 1000 * 60 * 2, retry: 1 },
  },
})

/**
 * Wipes the query cache whenever the authenticated identity changes (login as a
 * different user, logout, impersonation start/stop). Query keys like
 * ['user','profile'] or ['blocks'] are not user-scoped, so without this a newly
 * logged-in user would briefly see — and could act on — the previous user's
 * cached data. The initial null→user transition on boot clears nothing.
 */
function AuthCacheReset() {
  const client = useQueryClient()
  const userId = useAuthStore((s) => s.user?.id ?? null)
  const prev = useRef<string | null | undefined>(undefined)

  useEffect(() => {
    if (prev.current !== undefined && prev.current !== userId) {
      client.clear()
    }
    prev.current = userId
  }, [userId, client])

  return null
}

export function QueryProvider({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthCacheReset />
      {children}
    </QueryClientProvider>
  )
}
