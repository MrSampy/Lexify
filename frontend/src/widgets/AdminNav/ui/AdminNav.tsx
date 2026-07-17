import { NavLink } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  LayoutDashboard,
  Users,
  Bot,
  Settings,
  Globe,
  ScrollText,
  Home,
  type LucideIcon,
} from 'lucide-react'
import { ROUTES } from '@/shared/config'
import { useAuthStore } from '@/entities/user'

const NAV_ICON_SIZE = 18

const NAV_ITEMS: { labelKey: string; to: string; end?: boolean; icon: LucideIcon }[] = [
  { labelKey: 'adminNav.dashboard', to: ROUTES.ADMIN.DASHBOARD, end: true, icon: LayoutDashboard },
  { labelKey: 'adminNav.users', to: ROUTES.ADMIN.USERS, icon: Users },
  { labelKey: 'adminNav.aiMonitor', to: ROUTES.ADMIN.AI_MONITOR, icon: Bot },
  { labelKey: 'adminNav.settings', to: ROUTES.ADMIN.SETTINGS, icon: Settings },
  { labelKey: 'adminNav.languages', to: ROUTES.ADMIN.LANGUAGES, icon: Globe },
  { labelKey: 'adminNav.audit', to: ROUTES.ADMIN.AUDIT, icon: ScrollText },
]

interface AdminNavProps {
  /** When true, render only the nav content (no aside shell) — used inside the mobile drawer. */
  inDrawer?: boolean
}

export function AdminNav({ inDrawer = false }: AdminNavProps) {
  const { t } = useTranslation()
  const user = useAuthStore((s) => s.user)

  const content = (
    <>
      {/* Logo */}
      <div style={{ padding: '4px 8px 20px', display: 'flex', alignItems: 'center', gap: 10 }}>
        <div
          style={{
            width: 36,
            height: 36,
            background: 'var(--warning)',
            borderRadius: 'var(--r-sm)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: '0 4px 12px rgba(224,153,42,0.3)',
          }}
        >
          <span
            style={{
              color: '#fff',
              fontSize: 18,
              fontWeight: 800,
              fontFamily: 'var(--font-display)',
            }}
          >
            A
          </span>
        </div>
        <div>
          <div
            style={{
              fontFamily: 'var(--font-display)',
              fontSize: 18,
              fontWeight: 700,
              color: 'var(--fg-1)',
              lineHeight: 1.2,
            }}
          >
            Lexify
          </div>
          <div
            style={{
              fontSize: 10,
              fontWeight: 800,
              textTransform: 'uppercase',
              letterSpacing: '0.1em',
              color: 'var(--warning)',
            }}
          >
            Admin
          </div>
        </div>
      </div>

      <div
        style={{
          padding: '0 8px 8px',
          fontSize: 11,
          fontWeight: 800,
          textTransform: 'uppercase',
          letterSpacing: '0.10em',
          color: 'var(--fg-4)',
        }}
      >
        {t('nav.adminPanel')}
      </div>

      <nav style={{ display: 'flex', flexDirection: 'column', gap: 2, marginBottom: 8 }}>
        {NAV_ITEMS.map(({ labelKey, to, end, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            end={end}
            className={({ isActive }) => `lx-nav-item${isActive ? ' active' : ''}`}
          >
            <Icon size={NAV_ICON_SIZE} strokeWidth={2} style={{ flexShrink: 0 }} />
            <span>{t(labelKey)}</span>
          </NavLink>
        ))}
      </nav>

      <div style={{ flex: 1 }} />

      <div style={{ borderTop: '1.5px solid var(--line-2)', paddingTop: 12 }}>
        {user && (
          <div
            style={{
              padding: '0 8px 10px',
              fontSize: 12,
              color: 'var(--fg-4)',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
              fontWeight: 600,
            }}
          >
            {user.displayName ?? user.email}
          </div>
        )}
        <NavLink to={ROUTES.DASHBOARD} className="lx-nav-item">
          <Home size={NAV_ICON_SIZE} strokeWidth={2} style={{ flexShrink: 0 }} />
          <span>{t('adminNav.backToApp')}</span>
        </NavLink>
      </div>
    </>
  )

  if (inDrawer) return content

  return (
    <aside
      style={{
        width: 248,
        flexShrink: 0,
        position: 'sticky',
        top: 0,
        height: '100vh',
        overflowY: 'auto',
        background: 'var(--bg-1)',
        borderRight: '1.5px solid var(--line-2)',
        padding: '20px 12px',
        display: 'flex',
        flexDirection: 'column',
        boxShadow: '2px 0 12px rgba(20,80,60,0.05)',
      }}
    >
      {content}
    </aside>
  )
}
