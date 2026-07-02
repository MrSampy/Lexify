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
            className="rounded-[var(--r-md)] border border-[var(--line-2)] bg-[var(--bg-3)] px-4 py-3.5 text-center text-sm leading-[1.4] break-words text-[var(--fg-1)] transition-colors duration-100 [font-family:var(--font-body)] enabled:cursor-pointer enabled:hover:border-[var(--accent-line)] enabled:hover:bg-[var(--accent-ghost)] disabled:cursor-default"
          >
            {option.optionText}
          </button>
        ))}
      </div>
    </div>
  )
}
