import { motion } from 'motion/react'
import { CheckCircle2, XCircle } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { SPRING, popIn } from '@/shared/ui'
import { answerToLines, isMultilineAnswer } from '@/shared/lib'
import { cn } from '@/lib/utils'

interface AnswerFeedbackProps {
  isCorrect: boolean
  correctAnswer: string
  givenAnswer: string
  questionType: string
  isLast: boolean
  onNext: () => void
}

/**
 * Compact result bar shown under the (still-mounted) question renderer: slides up with a spring,
 * pops in the verdict icon, and carries the Next/Finish button. Short answers show inline as
 * "given → correct"; long/multi-part answers (matching pairs, sentence builder) stack into labeled
 * blocks with one line per pair so the raw wire string is never shown to the user.
 */
export function AnswerFeedback({
  isCorrect,
  correctAnswer,
  givenAnswer,
  questionType,
  isLast,
  onNext,
}: AnswerFeedbackProps) {
  const { t } = useTranslation()
  const multiline = isMultilineAnswer(questionType)

  return (
    <motion.div
      initial={{ opacity: 0, y: 24 }}
      animate={{ opacity: 1, y: 0 }}
      transition={SPRING}
      className={cn(
        'mt-6 rounded-[var(--r-md)] border border-l-3 px-4.5 py-3.5',
        isCorrect
          ? 'border-[var(--accent-line)] border-l-[var(--success)] bg-[var(--success-ghost)]'
          : 'border-[var(--danger)] border-l-[var(--danger)] bg-[var(--danger-ghost)]',
      )}
    >
      <div className="flex flex-wrap items-center gap-x-4 gap-y-3">
        <motion.span
          variants={popIn}
          initial="hidden"
          animate="visible"
          className={isCorrect ? 'text-[var(--success)]' : 'text-[var(--danger)]'}
        >
          {isCorrect ? <CheckCircle2 size={22} /> : <XCircle size={22} />}
        </motion.span>

        <p
          className={cn(
            'm-0 flex-1 text-sm font-semibold [font-family:var(--font-display)]',
            isCorrect ? 'text-[var(--success)]' : 'text-[var(--danger)]',
          )}
        >
          {isCorrect ? t('runTest.correctLabel') : t('runTest.incorrectLabel')}
        </p>

        <div className="flex items-center gap-2">
          <motion.button
            whileTap={{ scale: 0.96 }}
            className="lx-btn-primary px-5 py-2 text-[13px]"
            onClick={onNext}
            data-next-button
          >
            {isLast ? t('runTest.finish') : t('runTest.next')}
          </motion.button>
          <span className="hidden rounded-[4px] border border-[var(--line-2)] bg-[var(--bg-2)] px-1.5 py-0.5 text-[10px] font-bold text-[var(--fg-4)] md:inline">
            Enter ↵
          </span>
        </div>
      </div>

      {!isCorrect &&
        (multiline ? (
          <div className="mt-3 flex flex-col gap-2 border-t border-[rgba(255,92,108,0.25)] pt-3">
            <AnswerBlock
              label={t('runTest.yourAnswer')}
              lines={answerToLines(questionType, givenAnswer)}
              className="text-[var(--danger)]"
            />
            <AnswerBlock
              label={t('runTest.correctAnswer')}
              lines={answerToLines(questionType, correctAnswer)}
              className="text-[var(--success)]"
            />
          </div>
        ) : (
          <p className="m-0 mt-2 text-[12px] font-semibold text-[var(--fg-4)]">
            {t('runTest.yourAnswer')}:{' '}
            <span className="text-[var(--danger)]">{givenAnswer || '—'}</span>
            <span className="mx-1.5">→</span>
            <span className="text-[var(--success)]">{correctAnswer}</span>
          </p>
        ))}
    </motion.div>
  )
}

function AnswerBlock({
  label,
  lines,
  className,
}: {
  label: string
  lines: string[]
  className: string
}) {
  return (
    <div>
      <span className="block text-[10px] font-bold tracking-[0.05em] text-[var(--fg-4)] uppercase">
        {label}
      </span>
      <ul className="m-0 mt-1 list-none space-y-0.5 p-0">
        {lines.map((line, i) => (
          <li key={i} className={cn('text-[13px] font-semibold', className)}>
            {line}
          </li>
        ))}
      </ul>
    </div>
  )
}
