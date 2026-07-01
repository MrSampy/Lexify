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
          fontSize: 16,
          fontWeight: 500,
          color: 'var(--fg-1)',
          marginBottom: 20,
          lineHeight: 1.5,
        }}
      >
        {question.questionText}
      </p>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginBottom: 20 }}>
        {options.map((option) => {
          const isSelected = selected.includes(option.optionText)
          return (
            <label
              key={option.id}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 12,
                padding: '11px 14px',
                background: isSelected ? 'var(--accent-ghost)' : 'var(--bg-3)',
                border: `1px solid ${isSelected ? 'var(--accent-line)' : 'var(--line-2)'}`,
                borderRadius: 'var(--r-md)',
                cursor: disabled ? 'default' : 'pointer',
                transition: 'border-color 0.12s, background 0.12s',
              }}
            >
              <input
                type="checkbox"
                checked={isSelected}
                onChange={() => !disabled && toggle(option.optionText)}
                disabled={disabled}
                style={{ accentColor: 'var(--accent-color)', width: 14, height: 14, flexShrink: 0 }}
              />
              <span style={{ fontSize: 14, color: 'var(--fg-1)' }}>{option.optionText}</span>
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
