import { useState } from 'react'
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
      <div style={{ display: 'flex', flexDirection: 'column', gap: 10, marginBottom: 24 }}>
        {options.map((option) => {
          const isSelected = selected.includes(option.optionText)
          return (
            <label
              key={option.id}
              className={`flex items-center gap-3 rounded-[var(--r-md)] border px-4 py-3.5 transition-colors duration-100 ${
                disabled ? 'cursor-default' : 'cursor-pointer'
              } ${
                isSelected
                  ? 'border-[var(--accent-line)] bg-[var(--accent-ghost)]'
                  : 'border-[var(--line-2)] bg-[var(--bg-3)]'
              }`}
            >
              <input
                type="checkbox"
                checked={isSelected}
                onChange={() => !disabled && toggle(option.optionText)}
                disabled={disabled}
                className="h-4 w-4 shrink-0 accent-[var(--accent-color)]"
              />
              <span className="text-base text-[var(--fg-1)]">{option.optionText}</span>
            </label>
          )
        })}
      </div>
      <button
        className="lx-btn-primary"
        onClick={handleCheck}
        disabled={disabled || selected.length === 0}
        style={{ padding: '10px 24px' }}
      >
        Check
      </button>
    </div>
  )
}
