import { useEffect, useState } from 'react'
import { useAuthStore } from '@/entities/user'
import { Spinner, Toaster } from '@/shared/ui'
import { QueryProvider } from './providers/QueryProvider'
import { ThemeProvider } from './providers/ThemeProvider'
import { AppRouter } from './router/routes'

function BootLoader({ children }: { children: React.ReactNode }) {
  const [ready, setReady] = useState(false)
  const refreshToken = useAuthStore((s) => s.refreshToken)

  useEffect(() => {
    const init = async () => {
      const rt = sessionStorage.getItem('lexify_rt')
      if (rt) await refreshToken()
      setReady(true)
    }
    void init()
  }, [refreshToken])

  if (!ready) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Spinner size="lg" />
      </div>
    )
  }

  return <>{children}</>
}

export function AppRoot() {
  return (
    <ThemeProvider>
      <QueryProvider>
        <BootLoader>
          <AppRouter />
          <Toaster />
        </BootLoader>
      </QueryProvider>
    </ThemeProvider>
  )
}
