export interface FormatWordItem {
  term: string
  translation: string
  alternativeTranslations?: string[] | null
  synonyms?: string[] | null
  wordType: string
  notes?: string | null
  exampleSentence?: string | null
  confidenceFlag: boolean
  confidenceNote?: string | null
}

export interface FormatWordsResult {
  words: FormatWordItem[]
  suggestedTitle?: string | null
}

export interface EditableWord extends FormatWordItem {
  _id: string
}

export type ImportStep = 'input' | 'formatting' | 'preview' | 'saving' | 'done'
