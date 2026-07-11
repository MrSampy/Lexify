import { useState } from 'react'
import { useLocation, Outlet } from 'react-router-dom'
import { Menu } from 'lucide-react'
import { useIsMobile } from '@/shared/lib'
import { MobileDrawer } from '@/shared/ui'
import { AdminNav } from '@/widgets/AdminNav'

export function AdminLayout() {
  const location = useLocation()
  const isMobile = useIsMobile()
  const [drawerOpen, setDrawerOpen] = useState(false)

  // Close the drawer whenever navigation happens.
  // Render-time adjustment instead of an effect — see react.dev/learn/you-might-not-need-an-effect.
  const [prevPathname, setPrevPathname] = useState(location.pathname)
  if (prevPathname !== location.pathname) {
    setPrevPathname(location.pathname)
    setDrawerOpen(false)
  }

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
      {isMobile ? (
        <MobileDrawer open={drawerOpen} onClose={() => setDrawerOpen(false)}>
          <AdminNav inDrawer />
        </MobileDrawer>
      ) : (
        <AdminNav />
      )}

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
            padding: '10px clamp(12px, 3vw, 24px)',
            background: 'rgba(10, 14, 21, 0.85)',
            backdropFilter: 'blur(12px)',
            borderBottom: '1px solid var(--line-2)',
          }}
        >
          {isMobile && (
            <button
              onClick={() => setDrawerOpen(true)}
              aria-label="Open admin menu"
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                width: 40,
                height: 40,
                flexShrink: 0,
                border: '1px solid var(--line-2)',
                borderRadius: 'var(--r-sm)',
                background: 'transparent',
                color: 'var(--fg-2)',
                cursor: 'pointer',
              }}
            >
              <Menu style={{ width: 20, height: 20 }} />
            </button>
          )}
          <span
            className="ds-code"
            style={{
              color: 'var(--fg-3)',
              fontSize: 12,
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}
          >
            ~{location.pathname}
          </span>
          <div style={{ flex: 1 }} />
          <span
            style={{
              fontFamily: 'var(--font-body)',
              fontWeight: 800,
              fontSize: 10,
              textTransform: 'uppercase',
              letterSpacing: '0.1em',
              padding: '3px 10px',
              borderRadius: 'var(--r-pill)',
              background: 'var(--warning-ghost)',
              border: '1px solid rgba(224,153,42,0.3)',
              color: 'var(--warning)',
            }}
          >
            admin
          </span>
        </div>

        {/* Page content */}
        <div
          style={{
            flex: 1,
            padding: 'clamp(20px, 4vw, 32px) clamp(12px, 3vw, 24px) 64px',
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
