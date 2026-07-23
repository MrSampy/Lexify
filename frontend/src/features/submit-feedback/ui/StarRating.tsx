import { useState } from 'react'
import { Star } from 'lucide-react'

interface StarRatingProps {
  value: number | undefined
  onChange: (value: number) => void
  label: string
}

const STARS = [1, 2, 3, 4, 5]

/**
 * 1–5 star picker. A radiogroup rather than five buttons so arrow keys move between the options and
 * screen readers announce it as a single choice — `shared/ui` has no equivalent control.
 */
export function StarRating({ value, onChange, label }: StarRatingProps) {
  const [hovered, setHovered] = useState<number | null>(null)
  const shown = hovered ?? value ?? 0

  const move = (delta: number) => {
    const next = Math.min(5, Math.max(1, (value ?? 0) + delta))
    onChange(next)
  }

  return (
    <div
      role="radiogroup"
      aria-label={label}
      onKeyDown={(e) => {
        if (e.key === 'ArrowRight' || e.key === 'ArrowUp') {
          e.preventDefault()
          move(1)
        } else if (e.key === 'ArrowLeft' || e.key === 'ArrowDown') {
          e.preventDefault()
          move(-1)
        }
      }}
      onMouseLeave={() => setHovered(null)}
      style={{ display: 'flex', gap: 6 }}
    >
      {STARS.map((star) => {
        const filled = star <= shown
        return (
          <button
            key={star}
            type="button"
            role="radio"
            aria-checked={value === star}
            aria-label={`${star}`}
            // Only the selected star (or the first, when empty) is tabbable — arrow keys do the rest.
            tabIndex={value === star || (value == null && star === 1) ? 0 : -1}
            onClick={() => onChange(star)}
            onMouseEnter={() => setHovered(star)}
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              width: 40,
              height: 40,
              border: '1.5px solid var(--line-2)',
              borderRadius: 'var(--r-sm)',
              background: filled ? 'var(--warning)' : 'var(--bg-1)',
              color: filled ? '#fff' : 'var(--fg-3)',
              cursor: 'pointer',
              transition: 'background 120ms ease, color 120ms ease',
            }}
          >
            <Star style={{ width: 20, height: 20 }} fill={filled ? 'currentColor' : 'none'} />
          </button>
        )
      })}
    </div>
  )
}
