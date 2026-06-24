export interface WordBlock {
  id: string
  userId: string
  languageId: number
  title: string
  description: string | null
  wordCount: number
  createdAt: string
  updatedAt: string
}

export interface BlockFilter {
  languageId?: number
  tag?: string
  page: number
  pageSize: number
}

export interface CreateBlockInput {
  languageId: number
  title: string
  description?: string
}

export interface UpdateBlockInput {
  title: string
  description?: string
}

// Word shape as returned inside block detail — kept here to avoid cross-entity import
export interface WordInBlock {
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
