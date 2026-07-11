import { useSyncExternalStore } from 'react'

// Matches Tailwind's `md` breakpoint: below 768px we consider the viewport "mobile"
// (sidebar becomes a drawer, wide toolbars collapse, etc.).
const QUERY = '(max-width: 767px)'

function subscribe(onChange: () => void) {
  const mql = window.matchMedia(QUERY)
  mql.addEventListener('change', onChange)
  return () => mql.removeEventListener('change', onChange)
}

function getSnapshot() {
  return window.matchMedia(QUERY).matches
}

/** True when the viewport is narrower than 768px (Tailwind `md`). Re-renders on resize across the breakpoint. */
export function useIsMobile(): boolean {
  return useSyncExternalStore(subscribe, getSnapshot, () => false)
}
