export type WordType = 'word' | 'phrase' | 'idiom' | 'expression'

export interface Word {
  id: string
  blockId: string
  term: string
  translation: string
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
}

export interface CreateWordInput {
  term: string
  translation: string
  wordType: string
  notes?: string
  exampleSentence?: string
  sortOrder: number
}

export interface UpdateWordInput {
  translation: string
  notes?: string
  exampleSentence?: string
  confidenceFlag?: boolean
  confidenceNote?: string
}
