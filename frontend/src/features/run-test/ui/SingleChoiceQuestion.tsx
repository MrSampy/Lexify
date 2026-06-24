import { Button } from '@/shared/ui'
import type { Question } from '@/entities/test'

interface SingleChoiceQuestionProps {
  question: Question
  onSubmit: (answer: string) => void
  disabled: boolean
}

export function SingleChoiceQuestion({ question, onSubmit, disabled }: SingleChoiceQuestionProps) {
  const options = [...question.options].sort((a, b) => a.sortOrder - b.sortOrder)

  return (
    <div>
      <p className="mb-4 text-base font-medium">{question.questionText}</p>
      <div className="grid grid-cols-2 gap-2">
        {options.map((option) => (
          <Button
            key={option.id}
            variant="outline"
            className="h-auto whitespace-normal py-3 text-sm"
            onClick={() => onSubmit(option.optionText)}
            disabled={disabled}
          >
            {option.optionText}
          </Button>
        ))}
      </div>
    </div>
  )
}
