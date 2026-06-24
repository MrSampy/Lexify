import { Checkbox } from '@/shared/ui'
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
    <div className="space-y-2">
      {ALL_TYPES.map((type) => (
        <label
          key={type}
          className="flex cursor-pointer items-center gap-3 rounded-md p-2 hover:bg-muted/50"
        >
          <Checkbox
            checked={questionTypes.includes(type)}
            onCheckedChange={() => toggleQuestionType(type)}
          />
          <span className="text-sm">{QUESTION_TYPE_LABELS[type]}</span>
        </label>
      ))}
    </div>
  )
}
