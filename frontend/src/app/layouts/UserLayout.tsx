import { Link, useNavigate } from 'react-router-dom'
import { Outlet } from 'react-router-dom'
import { LogOut } from 'lucide-react'
import { ROUTES } from '@/shared/config'
import { SearchBar } from '@/widgets/SearchBar'
import { useAuthStore } from '@/entities/user'
import { authApi } from '@/features/auth'
import { Button } from '@/shared/ui'

const RT_KEY = 'lexify_rt'

export function UserLayout() {
  const logout = useAuthStore((s) => s.logout)
  const navigate = useNavigate()

  async function handleLogout() {
    const token = sessionStorage.getItem(RT_KEY)
    if (token) {
      try {
        await authApi.logout(token)
      } catch {
        // ignore — still log out locally
      }
    }
    logout()
    navigate(ROUTES.LOGIN, { replace: true })
  }

  return (
    <div className="flex min-h-screen flex-col bg-background">
      <header className="sticky top-0 z-50 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="mx-auto flex max-w-6xl items-center gap-4 px-4 py-2">
          <Link
            to={ROUTES.DASHBOARD}
            className="text-sm font-semibold tracking-tight hover:text-primary"
          >
            Lexify
          </Link>
          <div className="flex-1" />
          <SearchBar />
          <Button
            variant="ghost"
            size="sm"
            onClick={handleLogout}
            className="gap-1.5 text-muted-foreground hover:text-foreground"
            title="Выйти"
          >
            <LogOut className="h-4 w-4" />
            <span className="hidden sm:inline">Выйти</span>
          </Button>
        </div>
      </header>
      <main className="flex-1">
        <Outlet />
      </main>
    </div>
  )
}
