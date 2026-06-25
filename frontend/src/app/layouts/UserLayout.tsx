import { Link } from 'react-router-dom'
import { Outlet } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { SearchBar } from '@/widgets/SearchBar'

export function UserLayout() {
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
        </div>
      </header>
      <main className="flex-1">
        <Outlet />
      </main>
    </div>
  )
}
