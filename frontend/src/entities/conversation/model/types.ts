export type ConversationRole = 'user' | 'assistant'

export interface ChatMessage {
  id: string
  role: ConversationRole
  content: string
  createdAt: string
}

export interface ConversationTargetWord {
  wordId: string
  term: string
  translation: string
}

export interface StartConversationInput {
  blockId?: string
  scenario?: string
  nativeLanguage: string
}

export interface StartConversationResult {
  conversationId: string
  languageId: number
  targetLanguage: string
  title: string
  scenario: string | null
  targetWords: ConversationTargetWord[]
  openingMessage: string
  messageBudget: number
}

export interface ConversationDetail {
  id: string
  languageId: number
  title: string
  scenario: string | null
  status: string
  targetWords: ConversationTargetWord[]
  messages: ChatMessage[]
}

export interface ConversationListItem {
  id: string
  languageId: number
  title: string
  scenario: string | null
  status: string
  createdAt: string
  endedAt: string | null
  messageCount: number
}

export interface WordUsageResult {
  wordId: string
  term: string
  translation: string
  used: boolean
  usedCorrectly: boolean
  note: string | null
  intervalDays: number | null
  nextReviewAt: string | null
}

export interface ConversationScore {
  points: number
  stars: number
  wordsUsed: number
  totalWords: number
  messagesUsed: number
  messageBudget: number
}

export interface ConversationSummary {
  words: WordUsageResult[]
  score: ConversationScore
}
