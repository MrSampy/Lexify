import { useState, useRef } from 'react'
import { Button, Input } from '@/shared/ui'
import type { Question } from '@/entities/test'
import { levenshtein } from '@/shared/lib'

interface OpenAnswerQuestionProps {
  question: Question
  onSubmit: (answer: string) => void
  disabled: boolean
}

export function OpenAnswerQuestion({ question, onSubmit, disabled }: OpenAnswerQuestionProps) {
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

  // Client-side hint: check if close enough (distance ≤ 1 character) to show suggestion
  const firstOption = question.options[0]?.optionText ?? ''
  const distance = firstOption ? levenshtein(value.toLowerCase(), firstOption.toLowerCase()) : null
  const showCloseHint = value.length > 2 && distance !== null && distance > 0 && distance <= 1

  return (
    <div>
      <p className="mb-4 text-base font-medium">{question.questionText}</p>
      <div className="flex gap-2">
        <Input
          ref={inputRef}
          value={value}
          onChange={(e) => setValue(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Type your answer…"
          disabled={disabled}
          className="flex-1"
          autoFocus
        />
        <Button onClick={handleCheck} disabled={disabled || !value.trim()}>
          Check
        </Button>
      </div>
      {showCloseHint && (
        <p className="mt-1.5 text-xs text-amber-600">Almost there — double-check your spelling.</p>
      )}
    </div>
  )
}
