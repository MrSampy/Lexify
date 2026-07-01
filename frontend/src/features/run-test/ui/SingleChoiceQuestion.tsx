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
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
        {options.map((option) => (
          <button
            key={option.id}
            onClick={() => onSubmit(option.optionText)}
            disabled={disabled}
            style={{
              padding: '14px 16px',
              background: 'var(--bg-3)',
              border: '1px solid var(--line-2)',
              borderRadius: 'var(--r-md)',
              cursor: disabled ? 'default' : 'pointer',
              fontFamily: 'var(--font-body)',
              fontSize: 14,
              color: 'var(--fg-1)',
              textAlign: 'center',
              lineHeight: 1.4,
              transition: 'border-color 0.12s, background 0.12s',
              wordBreak: 'break-word',
            }}
            onMouseEnter={(e) => {
              if (!disabled) {
                const el = e.currentTarget
                el.style.borderColor = 'var(--accent-line)'
                el.style.background = 'var(--accent-ghost)'
              }
            }}
            onMouseLeave={(e) => {
              if (!disabled) {
                const el = e.currentTarget
                el.style.borderColor = 'var(--line-2)'
                el.style.background = 'var(--bg-3)'
              }
            }}
          >
            {option.optionText}
          </button>
        ))}
      </div>
    </div>
  )
}
