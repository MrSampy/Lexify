import { useState } from 'react'
import { Button, Checkbox } from '@/shared/ui'
import type { Question } from '@/entities/test'

interface MultiSelectQuestionProps {
  question: Question
  onSubmit: (answer: string) => void
  disabled: boolean
}

export function MultiSelectQuestion({ question, onSubmit, disabled }: MultiSelectQuestionProps) {
  const [selected, setSelected] = useState<string[]>([])
  const options = [...question.options].sort((a, b) => a.sortOrder - b.sortOrder)

  const toggle = (text: string) =>
    setSelected((prev) => (prev.includes(text) ? prev.filter((t) => t !== text) : [...prev, text]))

  const handleCheck = () => {
    if (selected.length === 0) return
    onSubmit(selected.join(', '))
  }

  return (
    <div>
      <p className="mb-4 break-words text-base font-medium">{question.questionText}</p>
      <div className="mb-4 space-y-2">
        {options.map((option) => (
          <label
            key={option.id}
            className="flex cursor-pointer items-center gap-3 rounded-md p-2 hover:bg-muted/50"
          >
            <Checkbox
              checked={selected.includes(option.optionText)}
              onCheckedChange={() => toggle(option.optionText)}
              disabled={disabled}
            />
            <span className="text-sm">{option.optionText}</span>
          </label>
        ))}
      </div>
      <Button onClick={handleCheck} disabled={disabled || selected.length === 0}>
        Check
      </Button>
    </div>
  )
}
