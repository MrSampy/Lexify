import type { ReactNode } from 'react'

interface MobileDrawerProps {
  open: boolean
  onClose: () => void
  children: ReactNode
}

/**
 * Off-canvas left drawer for mobile navigation: a sliding panel over a dimmed backdrop.
 * Purely presentational — the caller owns the open state and renders the panel content
 * (both layouts put their sidebar content inside). Uses theme variables so it follows
 * light/dark mode automatically.
 */
export function MobileDrawer({ open, onClose, children }: MobileDrawerProps) {
  return (
    <>
      {/* Backdrop */}
      <div
        onClick={onClose}
        style={{
          position: 'fixed',
          inset: 0,
          zIndex: 49,
          background: 'rgba(0, 0, 0, 0.4)',
          opacity: open ? 1 : 0,
          pointerEvents: open ? 'auto' : 'none',
          transition: 'opacity 0.2s ease',
        }}
      />
      {/* Sliding panel */}
      <div
        role="dialog"
        aria-modal="true"
        style={{
          position: 'fixed',
          top: 0,
          bottom: 0,
          left: 0,
          zIndex: 50,
          width: 'min(280px, 80vw)',
          background: 'var(--bg-1)',
          borderRight: '1.5px solid var(--line-2)',
          boxShadow: open ? 'var(--shadow-3)' : 'none',
          transform: open ? 'translateX(0)' : 'translateX(-100%)',
          transition: 'transform 0.22s ease',
          overflowY: 'auto',
          display: 'flex',
          flexDirection: 'column',
          padding: '20px 12px',
        }}
      >
        {children}
      </div>
    </>
  )
}
