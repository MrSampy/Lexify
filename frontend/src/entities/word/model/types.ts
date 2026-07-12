export type WordType = 'word' | 'phrase' | 'idiom' | 'expression'

export interface Word {
  id: string
  blockId: string
  term: string
  translation: string
  alternativeTranslations?: string[] | null
  synonyms?: string[] | null
  wordType: string
  notes: string | null
  exampleSentence: string | null
  confidenceFlag: boolean
  confidenceNote: string | null
  sortOrder: number
  createdAt: string
  easeFactor: number
  intervalDays: number
  repetitions: number
  nextReviewAt: string
  /** Present only on review-session payloads — used for TTS voice selection. */
  languageId?: number | null
}

export interface CreateWordInput {
  term: string
  translation: string
  alternativeTranslations?: string[]
  synonyms?: string[]
  wordType: string
  notes?: string
  exampleSentence?: string
  confidenceFlag?: boolean
  confidenceNote?: string | null
  sortOrder: number
}

export interface UpdateWordInput {
  translation: string
  alternativeTranslations?: string[]
  synonyms?: string[]
  notes?: string
  exampleSentence?: string
  confidenceFlag?: boolean
  confidenceNote?: string
}
