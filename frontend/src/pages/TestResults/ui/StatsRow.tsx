import { motion } from 'motion/react'
import { Clock3, Flame, Target } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import type { AttemptResult } from '@/entities/test'
import { fadeInUp, staggerContainer } from '@/shared/ui'

function formatDuration(ms: number): string {
  const totalSeconds = Math.max(0, Math.round(ms / 1000))
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${minutes}:${String(seconds).padStart(2, '0')}`
}

/** Time / accuracy / best-streak tiles under the score ring. */
export function StatsRow({ result }: { result: AttemptResult }) {
  const { t } = useTranslation()

  const summedMs = result.answers.reduce((sum, a) => sum + (a.timeSpentMs ?? 0), 0)
  const totalMs =
    summedMs > 0
      ? summedMs
      : new Date(result.finishedAt).getTime() - new Date(result.startedAt).getTime()

  let bestStreak = 0
  let run = 0
  for (const answer of result.answers) {
    run = answer.isCorrect ? run + 1 : 0
    bestStreak = Math.max(bestStreak, run)
  }

  const accuracy =
    result.totalQuestions > 0
      ? Math.round((result.correctAnswers / result.totalQuestions) * 100)
      : 0

  return (
    <motion.div
      variants={staggerContainer(0.08)}
      initial="hidden"
      animate="visible"
      className="mb-7 grid grid-cols-3 gap-3"
    >
      <StatTile
        icon={<Clock3 size={16} />}
        label={t('testResults.statTime')}
        value={formatDuration(totalMs)}
      />
      <StatTile
        icon={<Target size={16} />}
        label={t('testResults.statAccuracy')}
        value={`${accuracy}%`}
      />
      <StatTile
        icon={<Flame size={16} />}
        label={t('testResults.statStreak')}
        value={String(bestStreak)}
      />
    </motion.div>
  )
}

function StatTile({ icon, label, value }: { icon: React.ReactNode; label: string; value: string }) {
  return (
    <motion.div
      variants={fadeInUp}
      className="flex flex-col items-center gap-1 rounded-[var(--r-md)] border border-[var(--line-2)] bg-[var(--bg-2)] px-3 py-3.5 text-center"
    >
      <span className="flex items-center gap-1.5 text-[11px] font-bold tracking-[0.05em] text-[var(--fg-4)] uppercase">
        {icon} {label}
      </span>
      <span className="text-xl font-bold text-[var(--fg-1)] [font-family:var(--font-display)]">
        {value}
      </span>
    </motion.div>
  )
}
