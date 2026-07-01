import type { QuestionType } from '@/entities/test'
import { useGenerateTestStore } from '../model/store'

const QUESTION_TYPE_LABELS: Record<QuestionType, string> = {
  translate_to_native: 'Translate to native',
  translate_to_foreign: 'Translate to foreign',
  fill_in_sentence: 'Fill in the sentence',
  multi_select_theme: 'Multi-select by theme',
  open_answer: 'Open answer',
}

const ALL_TYPES: QuestionType[] = [
  'translate_to_native',
  'translate_to_foreign',
  'fill_in_sentence',
  'multi_select_theme',
  'open_answer',
]

export function QuestionTypeSelector() {
  const questionTypes = useGenerateTestStore((s) => s.questionTypes)
  const toggleQuestionType = useGenerateTestStore((s) => s.toggleQuestionType)

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
      {ALL_TYPES.map((type) => {
        const isSelected = questionTypes.includes(type)
        return (
          <label
            key={type}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 12,
              padding: '10px 14px',
              background: isSelected ? 'var(--accent-ghost)' : 'var(--bg-3)',
              border: `1px solid ${isSelected ? 'var(--accent-line)' : 'var(--line-2)'}`,
              borderRadius: 'var(--r-md)',
              cursor: 'pointer',
              transition: 'border-color 0.12s, background 0.12s',
            }}
          >
            <input
              type="checkbox"
              checked={isSelected}
              onChange={() => toggleQuestionType(type)}
              style={{ accentColor: 'var(--accent-color)', width: 14, height: 14, flexShrink: 0 }}
            />
            <span style={{ fontSize: 13, color: 'var(--fg-1)' }}>{QUESTION_TYPE_LABELS[type]}</span>
          </label>
        )
      })}
    </div>
  )
}
