import { useEffect } from 'react'
import { motion } from 'motion/react'
import { Check, X } from 'lucide-react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ROUTES } from '@/shared/config'
import { fireConfetti } from '@/shared/lib'
import { Mascot, Spinner, fadeInUp, staggerContainer } from '@/shared/ui'
import { useAttemptResults } from '@/entities/test'
import { ScoreRing } from './ScoreRing'
import { StatsRow } from './StatsRow'

/** Matching-pairs answers travel as "term|trans;term|trans" — render them human-readable. */
function prettifyAnswer(questionType: string, answer: string): string {
  if (questionType !== 'matching_pairs') return answer
  return answer
    .split(';')
    .filter(Boolean)
    .map((pair) => pair.replace('|', ' → '))
    .join('; ')
}

const CONFETTI_DELAY_MS = 600

export function TestResultsPage() {
  const { t } = useTranslation()
  const { id: attemptId } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const { data, isLoading, isError } = useAttemptResults(attemptId ?? '')

  const scorePercent = data ? Math.round(data.score * 100) : 0

  // Confetti for top scores, timed to land as the ring count-up finishes.
  useEffect(() => {
    if (!data || scorePercent < 90) return
    const timer = setTimeout(fireConfetti, CONFETTI_DELAY_MS)
    return () => clearTimeout(timer)
  }, [data, scorePercent])

  if (isLoading) {
    return (
      <div className="flex justify-center py-20">
        <Spinner size="lg" />
      </div>
    )
  }

  if (isError || !data) {
    return (
      <div className="flex flex-col items-center gap-4 py-20">
        <p className="ds-sm m-0 text-[var(--fg-3)]">{t('testResults.notAvailable')}</p>
        <Link to={ROUTES.TESTS} className="font-bold text-[var(--accent-color)] no-underline">
          {t('testResults.backToTests')}
        </Link>
      </div>
    )
  }

  const wrongAnswers = data.answers.filter((a) => !a.isCorrect)

  const getMessage = () => {
    if (scorePercent >= 90) return t('testResults.excellent')
    if (scorePercent >= 60) return t('testResults.good')
    return t('testResults.keepPracticing')
  }

  return (
    <div className="mx-auto max-w-[720px]">
      {/* Score header */}
      <div className="mb-7 flex flex-col items-center text-center">
        <div className="mb-4 flex items-end gap-2">
          {scorePercent >= 90 ? (
            <Mascot pose="trophy" size={110} animate />
          ) : (
            scorePercent >= 60 && <Mascot pose="celebrate" size={110} animate />
          )}
          <ScoreRing
            percent={scorePercent}
            correct={data.correctAnswers}
            total={data.totalQuestions}
          />
        </div>

        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.5 }}
        >
          <h1 className="ds-h3 m-0 mb-1">{getMessage()}</h1>
          <p className="ds-body m-0 text-[var(--fg-3)]">
            {wrongAnswers.length > 0
              ? t('testResults.moved', { count: wrongAnswers.length })
              : t('testResults.allCorrect')}
          </p>
        </motion.div>
      </div>

      <StatsRow result={data} />

      {/* Answer breakdown */}
      {data.answers.length > 0 && (
        <motion.div
          variants={staggerContainer(0.05)}
          initial="hidden"
          animate="visible"
          className="mb-6 flex flex-col gap-2"
        >
          {data.answers.map((r) => (
            <motion.div
              key={r.questionId}
              variants={fadeInUp}
              className={`flex items-center gap-3.5 rounded-[var(--r-md)] border border-l-3 border-[var(--line-2)] bg-[var(--bg-2)] px-4.5 py-3 ${
                r.isCorrect ? 'border-l-[var(--success)]' : 'border-l-[var(--danger)]'
              }`}
            >
              <span className={r.isCorrect ? 'text-[var(--success)]' : 'text-[var(--danger)]'}>
                {r.isCorrect ? (
                  <Check size={16} strokeWidth={3} />
                ) : (
                  <X size={16} strokeWidth={3} />
                )}
              </span>
              <span className="min-w-0 flex-1 text-sm text-[var(--fg-1)]">{r.questionText}</span>
              {!r.isCorrect && (
                <div className="max-w-[45%] text-right text-[11px] font-semibold break-words">
                  <div className="text-[var(--danger)]">
                    {prettifyAnswer(r.questionType, r.givenAnswer) || '—'}
                  </div>
                  <div className="text-[var(--success)]">
                    → {prettifyAnswer(r.questionType, r.correctAnswer)}
                  </div>
                </div>
              )}
            </motion.div>
          ))}
        </motion.div>
      )}

      {/* Actions */}
      <div className="flex justify-center gap-3">
        <motion.button
          whileTap={{ scale: 0.97 }}
          className="lx-btn-secondary"
          onClick={() => navigate(ROUTES.TEST_RUNNER(data.testId))}
        >
          {t('testResults.tryAgain')}
        </motion.button>
        <motion.button
          whileTap={{ scale: 0.97 }}
          className="lx-btn-primary"
          onClick={() => navigate(ROUTES.TESTS)}
        >
          {t('testResults.backToTestsBtn')}
        </motion.button>
      </div>
    </div>
  )
}
