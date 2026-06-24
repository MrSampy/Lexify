import { NavLink } from 'react-router-dom'
import { BarChart2, Users, Bot, Settings, Globe, ClipboardList, ChevronLeft } from 'lucide-react'
import { ROUTES } from '@/shared/config'
import { useAuthStore } from '@/entities/user'

const NAV_ITEMS = [
  { to: ROUTES.ADMIN.DASHBOARD, icon: BarChart2, label: 'Dashboard' },
  { to: ROUTES.ADMIN.USERS, icon: Users, label: 'Users' },
  { to: ROUTES.ADMIN.AI_MONITOR, icon: Bot, label: 'AI Monitor' },
  { to: ROUTES.ADMIN.SETTINGS, icon: Settings, label: 'Settings' },
  { to: ROUTES.ADMIN.LANGUAGES, icon: Globe, label: 'Languages' },
  { to: ROUTES.ADMIN.AUDIT, icon: ClipboardList, label: 'Audit' },
]

export function AdminNav() {
  const user = useAuthStore((s) => s.user)

  return (
    <aside className="flex h-screen w-56 shrink-0 flex-col border-r bg-card">
      {/* Header */}
      <div className="border-b px-4 py-4">
        <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
          Admin Panel
        </p>
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto py-3">
        {NAV_ITEMS.map(({ to, icon: Icon, label }) => (
          <NavLink
            key={to}
            to={to}
            end={to === ROUTES.ADMIN.DASHBOARD}
            className={({ isActive }) =>
              `flex items-center gap-3 px-4 py-2.5 text-sm transition-colors ${
                isActive
                  ? 'bg-primary/10 font-medium text-primary'
                  : 'text-muted-foreground hover:bg-accent hover:text-foreground'
              }`
            }
          >
            <Icon size={16} />
            {label}
          </NavLink>
        ))}
      </nav>

      {/* Footer */}
      <div className="border-t px-4 py-3">
        {user && (
          <p className="mb-2 truncate text-xs text-muted-foreground" title={user.email}>
            {user.email}
          </p>
        )}
        <NavLink
          to={ROUTES.DASHBOARD}
          className="flex items-center gap-2 text-xs text-muted-foreground hover:text-foreground"
        >
          <ChevronLeft size={14} />
          Back to app
        </NavLink>
      </div>
    </aside>
  )
}
