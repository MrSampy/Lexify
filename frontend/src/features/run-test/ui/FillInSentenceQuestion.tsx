import { useTranslation } from 'react-i18next'
import type { Question } from '@/entities/test'

interface FillInSentenceQuestionProps {
  question: Question
  onSubmit: (answer: string) => void
  disabled: boolean
}

// The backend blanks the target word with three underscores (FillSentenceValidator.Blank).
const BLANK = '___'

/**
 * Cloze question: shows the sentence with the missing word rendered as a visible blank, then the
 * multiple-choice options. Distinct from a plain single-choice so the user reads the sentence as a
 * gap to fill rather than a bare prompt.
 */
export function FillInSentenceQuestion({
  question,
  onSubmit,
  disabled,
}: FillInSentenceQuestionProps) {
  const { t } = useTranslation()
  const options = [...question.options].sort((a, b) => a.sortOrder - b.sortOrder)

  const [before, ...rest] = question.questionText.split(BLANK)
  const after = rest.join(BLANK)
  const hasBlank = rest.length > 0

  return (
    <div>
      <p className="ds-eyebrow" style={{ color: 'var(--fg-4)', marginBottom: 8 }}>
        {t('runTest.fillHint')}
      </p>
      <p
        style={{
          fontSize: 20,
          fontWeight: 500,
          color: 'var(--fg-1)',
          marginBottom: 24,
          lineHeight: 1.6,
        }}
      >
        {hasBlank ? (
          <>
            {before}
            <span
              style={{
                display: 'inline-block',
                minWidth: 72,
                textAlign: 'center',
                borderBottom: '2px solid var(--accent-color)',
                color: 'var(--accent-color)',
                fontWeight: 700,
                margin: '0 4px',
              }}
            >
              ?
            </span>
            {after}
          </>
        ) : (
          question.questionText
        )}
      </p>
      <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
        {options.map((option) => (
          <button
            key={option.id}
            onClick={() => onSubmit(option.optionText)}
            disabled={disabled}
            className="rounded-[var(--r-md)] border border-[var(--line-2)] bg-[var(--bg-3)] px-5 py-5 text-center text-base leading-[1.4] break-words text-[var(--fg-1)] transition-colors duration-100 [font-family:var(--font-body)] enabled:cursor-pointer enabled:hover:border-[var(--accent-line)] enabled:hover:bg-[var(--accent-ghost)] disabled:cursor-default"
          >
            {option.optionText}
          </button>
        ))}
      </div>
    </div>
  )
}
