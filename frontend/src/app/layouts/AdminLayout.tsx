import { useLocation, Outlet } from 'react-router-dom'
import { AdminNav } from '@/widgets/AdminNav'

export function AdminLayout() {
  const location = useLocation()

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
      <AdminNav />

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
          <span
            style={{
              fontFamily: 'var(--font-mono)',
              fontSize: 11,
              padding: '3px 10px',
              borderRadius: 'var(--r-sm)',
              background: 'var(--warning-ghost)',
              border: '1px solid rgba(245,181,61,0.3)',
              color: 'var(--warning)',
            }}
          >
            admin
          </span>
        </div>

        {/* Page content */}
        <div style={{ flex: 1, padding: '32px 24px 64px' }}>
          <div style={{ maxWidth: 1100, margin: '0 auto' }}>
            <Outlet />
          </div>
        </div>
      </main>
    </div>
  )
}
