import { useState } from 'react'
import { useNavigate, useLocation, NavLink, Outlet } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useTheme } from 'next-themes'
import {
  Menu,
  Sun,
  Moon,
  Home,
  Library,
  FileText,
  RefreshCw,
  BarChart3,
  Search,
  User,
  Shield,
  LogOut,
  type LucideIcon,
} from 'lucide-react'
import { ROUTES } from '@/shared/config'
import { useIsMobile } from '@/shared/lib'
import { MobileDrawer } from '@/shared/ui'
import { useAuthStore, useProfile } from '@/entities/user'
import { authApi } from '@/features/auth'
import { SearchBar } from '@/widgets/SearchBar'

const NAV_ICON_SIZE = 18

const APP_NAV: { labelKey: string; to: string; icon: LucideIcon }[] = [
  { labelKey: 'nav.dashboard', to: ROUTES.DASHBOARD, icon: Home },
  { labelKey: 'nav.blocks', to: ROUTES.BLOCKS, icon: Library },
  { labelKey: 'nav.tests', to: ROUTES.TESTS, icon: FileText },
  { labelKey: 'nav.review', to: ROUTES.REVIEW, icon: RefreshCw },
  { labelKey: 'nav.stats', to: ROUTES.STATS, icon: BarChart3 },
  { labelKey: 'nav.search', to: ROUTES.SEARCH, icon: Search },
  { labelKey: 'nav.profile', to: ROUTES.PROFILE, icon: User },
]

const LANG_OPTIONS = [
  { code: 'en', label: 'EN' },
  { code: 'uk', label: 'УК' },
]

/** Compact light/dark switch for the top bar (Profile still has the full Light/Dark/System control). */
function ThemeToggle() {
  const { t } = useTranslation()
  const { resolvedTheme, setTheme } = useTheme()
  const isDark = resolvedTheme === 'dark'

  return (
    <button
      onClick={() => setTheme(isDark ? 'light' : 'dark')}
      aria-label={t('nav.toggleTheme')}
      title={t('nav.toggleTheme')}
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        width: 36,
        height: 36,
        flexShrink: 0,
        border: '1.5px solid var(--line-2)',
        borderRadius: 'var(--r-pill)',
        background: 'var(--bg-1)',
        color: 'var(--fg-2)',
        cursor: 'pointer',
      }}
    >
      {isDark ? (
        <Sun style={{ width: 17, height: 17 }} />
      ) : (
        <Moon style={{ width: 17, height: 17 }} />
      )}
    </button>
  )
}

/** Sidebar inner content — shared between the desktop sticky aside and the mobile drawer. */
function SidebarContent({
  isAdmin,
  displayName,
  onLogout,
}: {
  isAdmin: boolean
  displayName: string | undefined
  onLogout: () => void
}) {
  const { t } = useTranslation()
  const user = useAuthStore((s) => s.user)

  return (
    <>
      {/* Logo */}
      <div style={{ padding: '4px 8px 20px', display: 'flex', alignItems: 'center', gap: 10 }}>
        <div
          style={{
            width: 36,
            height: 36,
            background: 'var(--accent-color)',
            borderRadius: 'var(--r-sm)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: '0 4px 12px rgba(22,185,129,0.3)',
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
            L
          </span>
        </div>
        <span
          style={{
            fontFamily: 'var(--font-display)',
            fontSize: 20,
            fontWeight: 700,
            color: 'var(--fg-1)',
          }}
        >
          Lexify
        </span>
      </div>

      {/* Nav section label */}
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
        {t('nav.menu')}
      </div>

      {/* Nav items */}
      <nav style={{ display: 'flex', flexDirection: 'column', gap: 2, marginBottom: 8 }}>
        {APP_NAV.map(({ labelKey, to, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            end={to === ROUTES.DASHBOARD}
            className={({ isActive }) => `lx-nav-item${isActive ? ' active' : ''}`}
          >
            <Icon size={NAV_ICON_SIZE} strokeWidth={2} style={{ flexShrink: 0 }} />
            <span>{t(labelKey)}</span>
          </NavLink>
        ))}
      </nav>

      {/* Admin link */}
      {isAdmin && (
        <>
          <div
            style={{
              padding: '12px 8px 8px',
              fontSize: 11,
              fontWeight: 800,
              textTransform: 'uppercase',
              letterSpacing: '0.10em',
              color: 'var(--fg-4)',
            }}
          >
            {t('nav.admin')}
          </div>
          <NavLink
            to={ROUTES.ADMIN.DASHBOARD}
            className={({ isActive }) => `lx-nav-item${isActive ? ' active' : ''}`}
            style={({ isActive }) => ({ color: isActive ? 'var(--warning)' : 'var(--fg-3)' })}
          >
            <Shield size={NAV_ICON_SIZE} strokeWidth={2} style={{ flexShrink: 0 }} />
            <span>{t('nav.adminPanel')}</span>
          </NavLink>
        </>
      )}

      <div style={{ flex: 1 }} />

      {/* User info + logout */}
      <div style={{ borderTop: '1.5px solid var(--line-2)', paddingTop: 12, marginTop: 8 }}>
        {user && (
          <div
            style={{
              padding: '6px 8px 10px',
              fontSize: 12,
              color: 'var(--fg-4)',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
              fontWeight: 600,
            }}
          >
            {displayName}
          </div>
        )}
        <button
          onClick={onLogout}
          className="lx-nav-item"
          style={{ color: 'var(--danger)', width: '100%' }}
        >
          <LogOut size={NAV_ICON_SIZE} strokeWidth={2} style={{ flexShrink: 0 }} />
          <span>{t('nav.signOut')}</span>
        </button>
      </div>
    </>
  )
}

export function UserLayout() {
  const { i18n, t } = useTranslation()
  const logout = useAuthStore((s) => s.logout)
  const user = useAuthStore((s) => s.user)
  const impersonation = useAuthStore((s) => s.impersonation)
  const stopImpersonation = useAuthStore((s) => s.stopImpersonation)
  const { data: profile } = useProfile()
  const navigate = useNavigate()
  const location = useLocation()
  const isMobile = useIsMobile()
  const [drawerOpen, setDrawerOpen] = useState(false)
  const isAdmin = user?.role === 'admin' || user?.role === 'moderator'
  const displayName = profile?.displayName ?? user?.displayName ?? user?.email.split('@')[0]

  // Close the drawer whenever navigation happens (nav-item click, search, back/forward).
  // Render-time adjustment instead of an effect — see react.dev/learn/you-might-not-need-an-effect.
  const [prevPathname, setPrevPathname] = useState(location.pathname)
  if (prevPathname !== location.pathname) {
    setPrevPathname(location.pathname)
    setDrawerOpen(false)
  }

  async function handleLogout() {
    try {
      await authApi.logout()
    } catch {
      /* ignore */
    }
    logout()
    navigate(ROUTES.LOGIN, { replace: true })
  }

  const sidebarContent = (
    <SidebarContent
      isAdmin={isAdmin}
      displayName={displayName}
      onLogout={() => void handleLogout()}
    />
  )

  return (
    <div style={{ display: 'flex', alignItems: 'stretch', minHeight: '100vh' }}>
      {/* ── SIDEBAR: sticky aside on desktop, off-canvas drawer on mobile ── */}
      {isMobile ? (
        <MobileDrawer open={drawerOpen} onClose={() => setDrawerOpen(false)}>
          {sidebarContent}
        </MobileDrawer>
      ) : (
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
          {sidebarContent}
        </aside>
      )}

      {/* ── MAIN CONTENT ── */}
      <main
        style={{
          flex: 1,
          minWidth: 0,
          display: 'flex',
          flexDirection: 'column',
          background: 'var(--bg-0)',
        }}
      >
        {/* Impersonation banner — always visible while acting as another user */}
        {impersonation && (
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              flexWrap: 'wrap',
              gap: 12,
              padding: '8px 16px',
              background: 'var(--danger)',
              color: '#fff',
              fontSize: 13,
              fontWeight: 700,
            }}
          >
            <span>{t('nav.impersonating', { email: user?.email })}</span>
            <button
              onClick={() => {
                stopImpersonation()
                navigate(ROUTES.ADMIN.USERS)
              }}
              style={{
                border: '1.5px solid rgba(255,255,255,0.7)',
                borderRadius: 'var(--r-pill)',
                background: 'transparent',
                color: '#fff',
                fontSize: 12,
                fontWeight: 800,
                padding: '3px 12px',
                cursor: 'pointer',
              }}
            >
              {t('nav.stopImpersonating')}
            </button>
          </div>
        )}
        {/* Top bar */}
        <div
          style={{
            position: 'sticky',
            top: 0,
            zIndex: 30,
            display: 'flex',
            alignItems: 'center',
            gap: isMobile ? 8 : 14,
            padding: '10px clamp(12px, 3vw, 28px)',
            background: 'var(--header-bg)',
            backdropFilter: 'blur(12px)',
            borderBottom: '1.5px solid var(--line-2)',
          }}
        >
          {isMobile && (
            <button
              onClick={() => setDrawerOpen(true)}
              aria-label="Open menu"
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                width: 40,
                height: 40,
                flexShrink: 0,
                border: '1.5px solid var(--line-2)',
                borderRadius: 'var(--r-sm)',
                background: 'var(--bg-1)',
                color: 'var(--fg-2)',
                cursor: 'pointer',
              }}
            >
              <Menu style={{ width: 20, height: 20 }} />
            </button>
          )}
          <div style={{ flex: 1, minWidth: 0, display: 'flex', justifyContent: 'flex-end' }}>
            <SearchBar />
          </div>
          <ThemeToggle />
          {/* Language switcher */}
          <div
            style={{
              display: 'flex',
              flexShrink: 0,
              gap: 4,
              padding: 3,
              borderRadius: 'var(--r-pill)',
              border: '1.5px solid var(--line-2)',
              background: 'var(--bg-1)',
            }}
          >
            {LANG_OPTIONS.map(({ code, label }) => {
              const active = i18n.resolvedLanguage === code
              return (
                <button
                  key={code}
                  onClick={() => void i18n.changeLanguage(code)}
                  style={{
                    border: 'none',
                    cursor: 'pointer',
                    fontFamily: 'var(--font-body)',
                    fontSize: 11,
                    fontWeight: 800,
                    letterSpacing: '0.04em',
                    padding: '4px 10px',
                    borderRadius: 'var(--r-pill)',
                    background: active ? 'var(--accent-color)' : 'transparent',
                    color: active ? '#fff' : 'var(--fg-3)',
                    transition: 'all 0.12s',
                  }}
                >
                  {label}
                </button>
              )
            })}
          </div>
          {/* User pill — hidden on mobile (name + sign-out live in the drawer) */}
          {user && !isMobile && (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 8,
                padding: '5px 14px',
                borderRadius: 'var(--r-pill)',
                background: 'var(--accent-ghost)',
                border: '1.5px solid var(--accent-line)',
                color: 'var(--accent-dim)',
                fontSize: 13,
                fontWeight: 700,
              }}
            >
              <User size={15} strokeWidth={2.2} />
              {displayName}
            </div>
          )}
        </div>

        {/* Page content */}
        <div
          style={{
            flex: 1,
            padding: 'clamp(20px, 4vw, 32px) clamp(12px, 3vw, 28px) 64px',
            overflowX: 'hidden',
          }}
        >
          <div style={{ maxWidth: 1100, margin: '0 auto' }}>
            <Outlet />
          </div>
        </div>
      </main>
    </div>
  )
}
