import { useLocation, useNavigate, NavLink, Outlet } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { useAuthStore } from '@/entities/user'
import { authApi } from '@/features/auth'

const RT_KEY = 'lexify_rt'

const APP_NAV = [
  { idx: '00', label: 'Dashboard', to: ROUTES.DASHBOARD },
  { idx: '01', label: 'Word blocks', to: ROUTES.BLOCKS },
  { idx: '02', label: 'Tests', to: ROUTES.TESTS },
  { idx: '03', label: 'Review', to: ROUTES.REVIEW },
  { idx: '04', label: 'Search', to: ROUTES.SEARCH },
]

export function UserLayout() {
  const logout = useAuthStore((s) => s.logout)
  const user = useAuthStore((s) => s.user)
  const navigate = useNavigate()
  const location = useLocation()

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

  const isAdmin = user?.role === 'admin'

  return (
    <div
      style={{
        position: 'relative',
        zIndex: 1,
        display: 'flex',
        alignItems: 'stretch',
        minHeight: '100vh',
      }}
    >
      {/* ── SIDEBAR ── */}
      <aside
        style={{
          width: 252,
          flexShrink: 0,
          position: 'sticky',
          top: 0,
          height: '100vh',
          overflowY: 'auto',
          background: 'var(--bg-1)',
          borderRight: '1px solid var(--line-1)',
          padding: '20px 14px',
          display: 'flex',
          flexDirection: 'column',
          gap: 6,
        }}
      >
        {/* Logo */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '6px 8px 18px' }}>
          <span
            style={{
              fontFamily: 'var(--font-mono)',
              fontSize: 18,
              fontWeight: 700,
              letterSpacing: '-0.02em',
              color: 'var(--fg-1)',
            }}
          >
            <span style={{ color: 'var(--accent-color)' }}>&lt;</span>
            Lexify
            <span style={{ color: 'var(--accent-color)' }}>/&gt;</span>
          </span>
        </div>

        {/* Section label */}
        <div
          className="ds-code"
          style={{ padding: '0 8px 6px', fontSize: 11, color: 'var(--fg-4)' }}
        >
          // navigation
        </div>

        {/* App nav */}
        <div style={{ marginBottom: 10 }}>
          <div
            style={{
              fontFamily: 'var(--font-mono)',
              fontSize: 11,
              letterSpacing: '0.12em',
              textTransform: 'uppercase',
              color: 'var(--fg-4)',
              padding: '8px 8px 6px',
            }}
          >
            ~/app
          </div>
          {APP_NAV.map(({ idx, label, to }) => (
            <NavLink
              key={to}
              to={to}
              end={to === ROUTES.DASHBOARD}
              className={({ isActive }) => `lx-nav-item${isActive ? ' active' : ''}`}
            >
              <span
                style={{
                  position: 'relative',
                  zIndex: 1,
                  fontFamily: 'var(--font-mono)',
                  fontSize: 11,
                  opacity: 0.6,
                  width: 18,
                  flexShrink: 0,
                }}
              >
                {idx}
              </span>
              <span style={{ position: 'relative', zIndex: 1, flex: 1, textAlign: 'left' }}>
                {label}
              </span>
            </NavLink>
          ))}
        </div>

        {/* Admin link */}
        {isAdmin && (
          <div style={{ marginBottom: 10 }}>
            <div
              style={{
                fontFamily: 'var(--font-mono)',
                fontSize: 11,
                letterSpacing: '0.12em',
                textTransform: 'uppercase',
                color: 'var(--fg-4)',
                padding: '8px 8px 6px',
              }}
            >
              ~/admin
            </div>
            <NavLink
              to={ROUTES.ADMIN.DASHBOARD}
              className={({ isActive }) => `lx-nav-item${isActive ? ' active' : ''}`}
              style={{ color: 'var(--warning)' }}
            >
              <span
                style={{
                  position: 'relative',
                  zIndex: 1,
                  fontFamily: 'var(--font-mono)',
                  fontSize: 11,
                  opacity: 0.6,
                  width: 18,
                  flexShrink: 0,
                }}
              >
                ⚙
              </span>
              <span style={{ position: 'relative', zIndex: 1 }}>Admin panel</span>
            </NavLink>
          </div>
        )}

        {/* Spacer */}
        <div style={{ flex: 1 }} />

        {/* User info + logout */}
        <div
          style={{
            borderTop: '1px solid var(--line-1)',
            paddingTop: 14,
            marginTop: 8,
          }}
        >
          {user && (
            <div
              style={{
                fontFamily: 'var(--font-mono)',
                fontSize: 11,
                color: 'var(--fg-4)',
                padding: '0 8px 10px',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              {user.email}
            </div>
          )}
          <button
            onClick={handleLogout}
            className="lx-nav-item"
            style={{ color: 'var(--danger)', width: '100%', textAlign: 'left' }}
          >
            <span
              style={{
                fontFamily: 'var(--font-mono)',
                fontSize: 11,
                opacity: 0.6,
                width: 18,
                flexShrink: 0,
              }}
            >
              ⏻
            </span>
            <span>Sign out</span>
          </button>
        </div>
      </aside>

      {/* ── MAIN CONTENT ── */}
      <main style={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
        {/* Chrome bar */}
        <div
          style={{
            position: 'sticky',
            top: 0,
            zIndex: 30,
            display: 'flex',
            alignItems: 'center',
            gap: 14,
            padding: '10px 24px',
            background: 'rgba(10, 14, 21, 0.85)',
            backdropFilter: 'blur(12px)',
            borderBottom: '1px solid var(--line-2)',
          }}
        >
          <span className="ds-code" style={{ color: 'var(--fg-3)', fontSize: 12 }}>
            ~{location.pathname}
          </span>
          <div style={{ flex: 1 }} />
          {user && (
            <span
              style={{
                fontFamily: 'var(--font-mono)',
                fontSize: 11,
                padding: '3px 10px',
                borderRadius: 'var(--r-sm)',
                background: 'var(--accent-ghost)',
                border: '1px solid var(--accent-line)',
                color: 'var(--accent-color)',
              }}
            >
              {user.displayName ?? user.email.split('@')[0]}
            </span>
          )}
        </div>

        {/* Page content */}
        <div style={{ flex: 1, padding: '32px 24px 64px', overflowX: 'hidden' }}>
          <div style={{ maxWidth: 1100, margin: '0 auto' }}>
            <Outlet />
          </div>
        </div>
      </main>
    </div>
  )
}
