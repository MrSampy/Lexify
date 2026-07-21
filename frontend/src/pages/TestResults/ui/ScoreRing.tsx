import { useEffect } from 'react'
import { animate, motion, useMotionValue, useTransform } from 'motion/react'
import { EASE } from '@/shared/ui'

interface ScoreRingProps {
  percent: number
  correct: number
  total: number
}

const SIZE = 168
const RADIUS = 66
const STROKE = 12
const CIRCUMFERENCE = 2 * Math.PI * RADIUS

/** SVG score ring: the arc sweeps in over ~1.2s while the center percentage counts up in sync. */
export function ScoreRing({ percent, correct, total }: ScoreRingProps) {
  const progress = useMotionValue(0)
  const dashOffset = useTransform(progress, (v) => CIRCUMFERENCE * (1 - v / 100))
  const label = useTransform(progress, (v) => `${Math.round(v)}%`)

  useEffect(() => {
    const controls = animate(progress, percent, { duration: 1.2, ease: EASE })
    // rAF-driven animations never tick in throttled/background tabs — guarantee the final value.
    const failsafe = setTimeout(() => progress.set(percent), 1600)
    return () => {
      controls.stop()
      clearTimeout(failsafe)
    }
  }, [percent, progress])

  const ringColor = percent >= 60 ? 'var(--accent-color)' : 'var(--warning)'

  return (
    <div
      className="relative"
      style={{
        width: SIZE,
        height: SIZE,
        filter: percent >= 90 ? 'drop-shadow(0 0 14px var(--accent-line))' : undefined,
      }}
      role="img"
      aria-label={`${percent}%`}
    >
      <svg width={SIZE} height={SIZE} viewBox={`0 0 ${SIZE} ${SIZE}`}>
        <circle
          cx={SIZE / 2}
          cy={SIZE / 2}
          r={RADIUS}
          fill="none"
          stroke="var(--bg-3)"
          strokeWidth={STROKE}
        />
        <motion.circle
          cx={SIZE / 2}
          cy={SIZE / 2}
          r={RADIUS}
          fill="none"
          stroke={ringColor}
          strokeWidth={STROKE}
          strokeLinecap="round"
          strokeDasharray={CIRCUMFERENCE}
          style={{ strokeDashoffset: dashOffset }}
          transform={`rotate(-90 ${SIZE / 2} ${SIZE / 2})`}
        />
      </svg>
      <div className="absolute inset-0 flex flex-col items-center justify-center">
        <motion.span className="text-[40px] font-bold text-[var(--fg-1)] [font-family:var(--font-display)]">
          {label}
        </motion.span>
        <span className="text-xs font-semibold text-[var(--fg-3)]">
          {correct} / {total}
        </span>
      </div>
    </div>
  )
}
