import { create } from 'zustand'
import { persist, createJSONStorage } from 'zustand/middleware'
import type { EditableWord, ImportStep } from './types'

interface ImportWordsState {
  step: ImportStep
  blockId: string | null
  rawText: string
  targetLanguageId: number
  nativeLanguageId: number
  formattedWords: EditableWord[]
  suggestedTitle: string
  streamingText: string
  error: string | null
}

interface ImportWordsActions {
  setBlockId: (id: string) => void
  setRawText: (text: string) => void
  setTargetLanguageId: (id: number) => void
  setNativeLanguageId: (id: number) => void
  setSuggestedTitle: (title: string) => void
  startFormatting: () => void
  appendChunk: (chunk: string) => void
  setPreview: (words: EditableWord[], suggestedTitle: string) => void
  setError: (msg: string) => void
  setSaving: () => void
  setDone: () => void
  updateWord: (id: string, updates: Partial<EditableWord>) => void
  removeWord: (id: string) => void
  addWord: () => void
  restoreDraft: () => void
  resetToInput: () => void
  resetAll: () => void
}

const initialState: ImportWordsState = {
  step: 'input',
  blockId: null,
  rawText: '',
  targetLanguageId: 1,
  nativeLanguageId: 3,
  formattedWords: [],
  suggestedTitle: '',
  streamingText: '',
  error: null,
}

export const useImportWordsStore = create<ImportWordsState & ImportWordsActions>()(
  persist(
    (set) => ({
      ...initialState,

      setBlockId: (blockId) => set({ blockId }),
      setRawText: (rawText) => set({ rawText }),
      setTargetLanguageId: (targetLanguageId) => set({ targetLanguageId }),
      setNativeLanguageId: (nativeLanguageId) => set({ nativeLanguageId }),
      setSuggestedTitle: (suggestedTitle) => set({ suggestedTitle }),

      startFormatting: () =>
        set({ step: 'formatting', streamingText: '', formattedWords: [], error: null }),

      appendChunk: (chunk) => set((s) => ({ streamingText: s.streamingText + chunk })),

      setPreview: (formattedWords, suggestedTitle) =>
        set({ step: 'preview', formattedWords, suggestedTitle }),

      setError: (error) => set({ step: 'input', error }),

      setSaving: () => set({ step: 'saving' }),
      setDone: () => set({ step: 'done' }),

      updateWord: (id, updates) =>
        set((s) => ({
          formattedWords: s.formattedWords.map((w) => (w._id === id ? { ...w, ...updates } : w)),
        })),

      removeWord: (id) =>
        set((s) => ({ formattedWords: s.formattedWords.filter((w) => w._id !== id) })),

      addWord: () =>
        set((s) => ({
          formattedWords: [
            ...s.formattedWords,
            {
              _id: crypto.randomUUID(),
              term: '',
              translation: '',
              wordType: 'word',
              confidenceFlag: false,
            },
          ],
        })),

      restoreDraft: () => set({ step: 'preview', error: null }),
      resetToInput: () => set({ step: 'input', error: null, streamingText: '' }),
      resetAll: () => set(initialState),
    }),
    {
      name: 'lexify-import-words',
      storage: createJSONStorage(() => sessionStorage),
      partialize: (s) => ({
        // Reset 'formatting' on reload — SSE stream is gone
        step: s.step === 'formatting' ? 'input' : s.step,
        blockId: s.blockId,
        rawText: s.rawText,
        targetLanguageId: s.targetLanguageId,
        nativeLanguageId: s.nativeLanguageId,
        formattedWords: s.formattedWords,
        suggestedTitle: s.suggestedTitle,
      }),
    },
  ),
)
