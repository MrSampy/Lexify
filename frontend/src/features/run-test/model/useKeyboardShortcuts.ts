import { useEffect } from 'react'

/**
 * Runner keyboard shortcuts: digits 1-9 press the matching answer tile (renderers expose them via
 * data-option-index on OptionTile), Enter presses the FeedbackBar's Next/Finish button
 * (data-next-button). Ignores keys while an input/textarea has focus so typing "1" into an open
 * answer never picks an option.
 */
export function useKeyboardShortcuts(enabled: boolean) {
  useEffect(() => {
    if (!enabled) return

    const handler = (e: KeyboardEvent) => {
      const target = e.target as HTMLElement | null
      if (
        target &&
        (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.isContentEditable)
      )
        return
      if (e.metaKey || e.ctrlKey || e.altKey) return

      if (e.key >= '1' && e.key <= '9') {
        const index = Number(e.key) - 1
        const tile = document.querySelector<HTMLButtonElement>(`[data-option-index="${index}"]`)
        if (tile && !tile.disabled) {
          e.preventDefault()
          tile.click()
        }
        return
      }

      if (e.key === 'Enter' || e.code === 'Enter' || e.code === 'NumpadEnter') {
        const next = document.querySelector<HTMLButtonElement>('[data-next-button]')
        if (next) {
          e.preventDefault()
          next.click()
        }
      }
    }

    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [enabled])
}
