import { useState } from 'react'
import { motion } from 'motion/react'
import { useTranslation } from 'react-i18next'
import { staggerContainer } from '@/shared/ui'
import { cn } from '@/lib/utils'
import type { QuestionRendererProps } from '../model/types'
import { OptionTile, type OptionTileState } from './OptionTile'

// The backend blanks the target word with three underscores (FillSentenceValidator.Blank).
const BLANK = '___'

/**
 * Cloze question: shows the sentence with the missing word rendered as a visible blank, then the
 * multiple-choice options. Once feedback arrives the blank is filled with the correct word so the
 * user rereads the complete sentence.
 */
export function FillInSentenceQuestion({
  question,
  onSubmit,
  disabled,
  feedback,
}: QuestionRendererProps) {
  const { t } = useTranslation()
  const [chosen, setChosen] = useState<string | null>(null)
  const options = [...question.options].sort((a, b) => a.sortOrder - b.sortOrder)

  const [before, ...rest] = question.questionText.split(BLANK)
  const after = rest.join(BLANK)
  const hasBlank = rest.length > 0

  const stateOf = (optionText: string): OptionTileState => {
    if (!feedback) return 'idle'
    if (optionText.toLowerCase() === feedback.correctAnswer.toLowerCase()) return 'correct'
    if (optionText === (chosen ?? feedback.givenAnswer)) return 'incorrect'
    return 'dimmed'
  }

  return (
    <div>
      <p className="ds-eyebrow mb-2 text-[var(--fg-4)]">{t('runTest.fillHint')}</p>
      <p className="mb-6 text-xl leading-[1.6] font-medium text-[var(--fg-1)]">
        {hasBlank ? (
          <>
            {before}
            <span
              className={cn(
                'mx-1 inline-block min-w-[84px] border-b-2 text-center font-bold align-baseline',
                feedback
                  ? feedback.isCorrect
                    ? 'border-[var(--success)] text-[var(--success)]'
                    : 'border-[var(--danger)] text-[var(--success)]'
                  : 'border-[var(--accent-color)] text-[var(--accent-color)]',
              )}
            >
              {/* Empty underlined gap before answering (classic cloze blank), filled with the
                  correct word once feedback arrives. */}
              {feedback ? feedback.correctAnswer : ' '}
            </span>
            {after}
          </>
        ) : (
          question.questionText
        )}
      </p>
      <motion.div
        variants={staggerContainer(0.06)}
        initial="hidden"
        animate="visible"
        className="grid grid-cols-1 gap-3 md:grid-cols-2"
      >
        {options.map((option, i) => (
          <OptionTile
            key={option.id}
            label={option.optionText}
            index={i}
            state={stateOf(option.optionText)}
            disabled={disabled || !!feedback}
            onClick={() => {
              setChosen(option.optionText)
              onSubmit(option.optionText)
            }}
          />
        ))}
      </motion.div>
    </div>
  )
}
