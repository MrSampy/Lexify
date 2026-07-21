import { useState } from 'react'
import { motion } from 'motion/react'
import { staggerContainer } from '@/shared/ui'
import type { QuestionRendererProps } from '../model/types'
import { OptionTile, type OptionTileState } from './OptionTile'

export function SingleChoiceQuestion({
  question,
  onSubmit,
  disabled,
  feedback,
}: QuestionRendererProps) {
  const [chosen, setChosen] = useState<string | null>(null)
  const options = [...question.options].sort((a, b) => a.sortOrder - b.sortOrder)

  const stateOf = (optionText: string): OptionTileState => {
    if (!feedback) return 'idle'
    const isCorrectOption = optionText.toLowerCase() === feedback.correctAnswer.toLowerCase()
    if (isCorrectOption) return 'correct'
    if (optionText === (chosen ?? feedback.givenAnswer)) return 'incorrect'
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
