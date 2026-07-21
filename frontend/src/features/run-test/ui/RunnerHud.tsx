import { motion } from 'motion/react'
import { Check, Flame, X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { SPRING, popIn } from '@/shared/ui'

interface RunnerHudProps {
  current: number
  total: number
  correctCount: number
  incorrectCount: number
  /** Trailing run of consecutive correct answers; the flame chip shows from 2 up. */
  streak: number
}

/** Progress bar + live score chips above the question card. */
export function RunnerHud({
  current,
  total,
  correctCount,
  incorrectCount,
  streak,
}: RunnerHudProps) {
  const { t } = useTranslation()
  const progress = total > 0 ? (current / total) * 100 : 0

  return (
    <div className="mb-6">
      <div className="lx-progress-track">
        <motion.div
          className="lx-progress-fill"
          initial={false}
          animate={{ width: `${progress}%` }}
          transition={SPRING}
        />
      </div>
      <div className="mt-3 flex items-center gap-2">
        <Chip className="border-[var(--accent-line)] bg-[var(--success-ghost)] text-[var(--success)]">
          <Check size={12} strokeWidth={3} /> {correctCount}
        </Chip>
        <Chip className="border-[rgba(255,92,108,0.3)] bg-[var(--danger-ghost)] text-[var(--danger)]">
          <X size={12} strokeWidth={3} /> {incorrectCount}
        </Chip>
        {streak >= 2 && (
          <motion.span
            key={streak}
            variants={popIn}
            initial="hidden"
            animate="visible"
            className="inline-flex items-center gap-1 rounded-[var(--r-pill)] border border-[var(--warning)] bg-[color-mix(in_srgb,var(--warning)_12%,transparent)] px-2.5 py-1 text-[11px] font-bold text-[var(--warning)]"
            title={t('testRunner.streak', { count: streak })}
          >
            <Flame size={12} /> {streak}
          </motion.span>
        )}
      </div>
    </div>
  )
}

function Chip({ children, className }: { children: React.ReactNode; className: string }) {
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-[var(--r-pill)] border px-2.5 py-1 text-[11px] font-bold ${className}`}
    >
      {children}
    </span>
  )
}
