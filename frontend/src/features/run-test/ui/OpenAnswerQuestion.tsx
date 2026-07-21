import { useState, useRef } from 'react'
import { motion } from 'motion/react'
import { useTranslation } from 'react-i18next'
import { levenshtein } from '@/shared/lib'
import { cn } from '@/lib/utils'
import type { QuestionRendererProps } from '../model/types'

export function OpenAnswerQuestion({
  question,
  onSubmit,
  disabled,
  feedback,
}: QuestionRendererProps) {
  const { t } = useTranslation()
  const [value, setValue] = useState('')
  const inputRef = useRef<HTMLInputElement>(null)

  const handleCheck = () => {
    const trimmed = value.trim()
    if (!trimmed) return
    onSubmit(trimmed)
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') handleCheck()
  }

  const firstOption = question.options[0]?.optionText ?? ''
  const distance = firstOption ? levenshtein(value.toLowerCase(), firstOption.toLowerCase()) : null
  const showCloseHint =
    !feedback && value.length > 2 && distance !== null && distance > 0 && distance <= 1

  return (
    <div>
      <p className="mb-6 text-xl leading-normal font-medium text-[var(--fg-1)]">
        {question.questionText}
      </p>
      <div className="flex gap-2.5">
        <input
          ref={inputRef}
          className={cn(
            'lx-input h-[50px] flex-1 text-[17px]',
            feedback && (feedback.isCorrect ? 'border-[var(--success)]' : 'border-[var(--danger)]'),
          )}
          value={value}
          onChange={(e) => setValue(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={t('runTest.typeAnswer')}
          disabled={disabled || !!feedback}
          autoFocus
        />
        {!feedback && (
          <motion.button
            whileTap={{ scale: 0.97 }}
            className="lx-btn-primary px-5"
            onClick={handleCheck}
            disabled={disabled || !value.trim()}
          >
            {t('runTest.check')}
          </motion.button>
        )}
      </div>
      {showCloseHint && (
        <p className="mt-2 text-[11px] font-semibold text-[var(--warning)]">
          {t('runTest.almostSpelling')}
        </p>
      )}
    </div>
  )
}
