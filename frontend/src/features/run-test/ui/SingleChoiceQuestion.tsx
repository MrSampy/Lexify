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
          fontSize: 20,
          fontWeight: 500,
          color: 'var(--fg-1)',
          marginBottom: 24,
          lineHeight: 1.5,
        }}
      >
        {question.questionText}
      </p>
      {/* 1 column on phones, the original 2×2 on desktop */}
      <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
        {options.map((option) => (
          <button
            key={option.id}
            onClick={() => onSubmit(option.optionText)}
            disabled={disabled}
            className="rounded-[var(--r-md)] border border-[var(--line-2)] bg-[var(--bg-3)] px-5 py-5 text-center text-base leading-[1.4] break-words text-[var(--fg-1)] transition-colors duration-100 [font-family:var(--font-body)] enabled:cursor-pointer enabled:hover:border-[var(--accent-line)] enabled:hover:bg-[var(--accent-ghost)] disabled:cursor-default"
          >
            {option.optionText}
          </button>
        ))}
      </div>
    </div>
  )
}
