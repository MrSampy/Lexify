import { useNavigate, NavLink, Outlet } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { useAuthStore } from '@/entities/user'
import { authApi } from '@/features/auth'
import { SearchBar } from '@/widgets/SearchBar'

const RT_KEY = 'lexify_rt'

const APP_NAV = [
  { label: 'Dashboard', to: ROUTES.DASHBOARD, emoji: '🏠' },
  { label: 'Word blocks', to: ROUTES.BLOCKS, emoji: '📚' },
  { label: 'Tests', to: ROUTES.TESTS, emoji: '📝' },
  { label: 'Review', to: ROUTES.REVIEW, emoji: '🔄' },
  { label: 'Search', to: ROUTES.SEARCH, emoji: '🔍' },
]

export function UserLayout() {
  const logout = useAuthStore((s) => s.logout)
  const user = useAuthStore((s) => s.user)
  const navigate = useNavigate()
  const isAdmin = user?.role === 'admin'

  async function handleLogout() {
    const token = sessionStorage.getItem(RT_KEY)
    if (token) {
      try {
        await authApi.logout(token)
      } catch {
        /* ignore */
      }
    }
    logout()
    navigate(ROUTES.LOGIN, { replace: true })
  }

  return (
    <div style={{ display: 'flex', alignItems: 'stretch', minHeight: '100vh' }}>
      {/* ── SIDEBAR ── */}
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
          Menu
        </div>

        {/* Nav items */}
        <nav style={{ display: 'flex', flexDirection: 'column', gap: 2, marginBottom: 8 }}>
          {APP_NAV.map(({ label, to, emoji }) => (
            <NavLink
              key={to}
              to={to}
              end={to === ROUTES.DASHBOARD}
              className={({ isActive }) => `lx-nav-item${isActive ? ' active' : ''}`}
            >
              <span style={{ fontSize: 16 }}>{emoji}</span>
              <span>{label}</span>
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
              Admin
            </div>
            <NavLink
              to={ROUTES.ADMIN.DASHBOARD}
              className={({ isActive }) => `lx-nav-item${isActive ? ' active' : ''}`}
              style={({ isActive }) => ({ color: isActive ? 'var(--warning)' : 'var(--fg-3)' })}
            >
              <span style={{ fontSize: 16 }}>⚙️</span>
              <span>Admin panel</span>
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
              {user.displayName ?? user.email.split('@')[0]}
            </div>
          )}
          <button
            onClick={handleLogout}
            className="lx-nav-item"
            style={{ color: 'var(--danger)', width: '100%' }}
          >
            <span style={{ fontSize: 16 }}>👋</span>
            <span>Sign out</span>
          </button>
        </div>
      </aside>

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
        {/* Top bar */}
        <div
          style={{
            position: 'sticky',
            top: 0,
            zIndex: 30,
            display: 'flex',
            alignItems: 'center',
            gap: 14,
            padding: '10px 28px',
            background: 'rgba(238,249,243,0.90)',
            backdropFilter: 'blur(12px)',
            borderBottom: '1.5px solid var(--line-2)',
          }}
        >
          <div style={{ flex: 1 }} />
          <SearchBar />
          {user && (
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
              <span>👤</span>
              {user.displayName ?? user.email.split('@')[0]}
            </div>
          )}
        </div>

        {/* Page content */}
        <div style={{ flex: 1, padding: '32px 28px 64px', overflowX: 'hidden' }}>
          <div style={{ maxWidth: 1100, margin: '0 auto' }}>
            <Outlet />
          </div>
        </div>
      </main>
    </div>
  )
}
