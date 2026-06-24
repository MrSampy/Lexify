import { create } from 'zustand'
import type { QuestionType } from '@/entities/test'

interface GenerateTestStore {
  selectedBlockIds: string[]
  questionTypes: QuestionType[]
  questionCount: number
  toggleBlock: (id: string) => void
  toggleQuestionType: (type: QuestionType) => void
  setQuestionCount: (count: number) => void
  reset: () => void
}

const ALL_QUESTION_TYPES: QuestionType[] = [
  'translate_to_native',
  'translate_to_foreign',
  'fill_in_sentence',
  'multi_select_theme',
  'open_answer',
]

export const useGenerateTestStore = create<GenerateTestStore>((set) => ({
  selectedBlockIds: [],
  questionTypes: ALL_QUESTION_TYPES,
  questionCount: 10,

  toggleBlock: (id) =>
    set((s) => ({
      selectedBlockIds: s.selectedBlockIds.includes(id)
        ? s.selectedBlockIds.filter((b) => b !== id)
        : [...s.selectedBlockIds, id],
    })),

  toggleQuestionType: (type) =>
    set((s) => ({
      questionTypes: s.questionTypes.includes(type)
        ? s.questionTypes.filter((t) => t !== type)
        : [...s.questionTypes, type],
    })),

  setQuestionCount: (count) => set({ questionCount: count }),

  reset: () => set({ selectedBlockIds: [], questionTypes: ALL_QUESTION_TYPES, questionCount: 10 }),
}))
