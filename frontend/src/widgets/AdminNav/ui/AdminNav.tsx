import { NavLink } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { useAuthStore } from '@/entities/user'

const NAV_ITEMS = [
  { idx: '00', label: 'Dashboard', to: ROUTES.ADMIN.DASHBOARD, end: true },
  { idx: '01', label: 'Users', to: ROUTES.ADMIN.USERS },
  { idx: '02', label: 'AI Monitor', to: ROUTES.ADMIN.AI_MONITOR },
  { idx: '03', label: 'Settings', to: ROUTES.ADMIN.SETTINGS },
  { idx: '04', label: 'Languages', to: ROUTES.ADMIN.LANGUAGES },
  { idx: '05', label: 'Audit log', to: ROUTES.ADMIN.AUDIT },
]

export function AdminNav() {
  const user = useAuthStore((s) => s.user)

  return (
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
          <span style={{ color: 'var(--warning)' }}>&lt;</span>
          Lexify
          <span style={{ color: 'var(--warning)' }}>/&gt;</span>
        </span>
      </div>

      <div className="ds-code" style={{ padding: '0 8px 6px', fontSize: 11, color: 'var(--fg-4)' }}>
        // admin panel
      </div>

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
        {NAV_ITEMS.map(({ idx, label, to, end }) => (
          <NavLink
            key={to}
            to={to}
            end={end}
            className={({ isActive }) => `lx-nav-item${isActive ? ' active' : ''}`}
            style={{ color: 'var(--fg-2)' }}
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
            <span style={{ position: 'relative', zIndex: 1, flex: 1 }}>{label}</span>
          </NavLink>
        ))}
      </div>

      <div style={{ flex: 1 }} />

      {/* Back to app */}
      <div style={{ borderTop: '1px solid var(--line-1)', paddingTop: 14 }}>
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
        <NavLink to={ROUTES.DASHBOARD} className="lx-nav-item">
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
            ←
          </span>
          <span style={{ position: 'relative', zIndex: 1 }}>Back to app</span>
        </NavLink>
      </div>
    </aside>
  )
}
