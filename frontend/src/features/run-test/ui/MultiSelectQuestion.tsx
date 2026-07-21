import { useState } from 'react'
import { motion } from 'motion/react'
import { useTranslation } from 'react-i18next'
import { staggerContainer } from '@/shared/ui'
import type { QuestionRendererProps } from '../model/types'
import { OptionTile, type OptionTileState } from './OptionTile'

export function MultiSelectQuestion({
  question,
  onSubmit,
  disabled,
  feedback,
}: QuestionRendererProps) {
  const { t } = useTranslation()
  const [selected, setSelected] = useState<string[]>([])
  const options = [...question.options].sort((a, b) => a.sortOrder - b.sortOrder)

  const toggle = (text: string) =>
    setSelected((prev) => (prev.includes(text) ? prev.filter((t) => t !== text) : [...prev, text]))

  const handleCheck = () => {
    if (selected.length === 0) return
    onSubmit(selected.join(', '))
  }

  // The server's CorrectAnswer for multi-select is the comma-joined list of every correct option.
  const correctSet = feedback
    ? new Set(feedback.correctAnswer.split(',').map((s) => s.trim().toLowerCase()))
    : null

  const stateOf = (optionText: string): OptionTileState => {
    if (!correctSet) return selected.includes(optionText) ? 'selected' : 'idle'
    const isCorrectOption = correctSet.has(optionText.trim().toLowerCase())
    const wasPicked = selected.includes(optionText)
    if (isCorrectOption) return 'correct'
    if (wasPicked) return 'incorrect'
    return 'dimmed'
  }

  return (
    <div>
      <p className="mb-6 text-xl leading-normal font-medium text-[var(--fg-1)]">
        {question.questionText}
      </p>
      <motion.div
        variants={staggerContainer(0.06)}
        initial="hidden"
        animate="visible"
        className="mb-6 flex flex-col gap-2.5"
      >
        {options.map((option, i) => (
          <OptionTile
            key={option.id}
            label={option.optionText}
            index={i}
            state={stateOf(option.optionText)}
            disabled={disabled || !!feedback}
            onClick={() => toggle(option.optionText)}
            className="text-left"
          />
        ))}
      </motion.div>
      {!feedback && (
        <motion.button
          whileTap={{ scale: 0.97 }}
          className="lx-btn-primary px-6 py-2.5"
          onClick={handleCheck}
          disabled={disabled || selected.length === 0}
        >
          {t('runTest.check')}
        </motion.button>
      )}
    </div>
  )
}
