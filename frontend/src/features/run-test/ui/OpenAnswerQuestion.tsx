import { useState, useRef } from 'react'
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

  const firstOption = question.options[0]?.optionText ?? ''
  const distance = firstOption ? levenshtein(value.toLowerCase(), firstOption.toLowerCase()) : null
  const showCloseHint = value.length > 2 && distance !== null && distance > 0 && distance <= 1

  return (
    <div>
      <p
        style={{
          fontSize: 20,
          fontWeight: 500,
          color: 'var(--fg-1)',
          marginBottom: 24,
          lineHeight: 1.5,
        }}
      >
        {question.questionText}
      </p>
      <div style={{ display: 'flex', gap: 10 }}>
        <input
          ref={inputRef}
          className="lx-input"
          value={value}
          onChange={(e) => setValue(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="type your answer…"
          disabled={disabled}
          autoFocus
          style={{ flex: 1, height: 50, fontSize: 17 }}
        />
        <button
          className="lx-btn-primary"
          onClick={handleCheck}
          disabled={disabled || !value.trim()}
          style={{ padding: '0 22px' }}
        >
          Check
        </button>
      </div>
      {showCloseHint && (
        <p style={{ marginTop: 8, color: 'var(--warning)', fontSize: 11, fontWeight: 600 }}>
          Almost — double-check your spelling.
        </p>
      )}
    </div>
  )
}
