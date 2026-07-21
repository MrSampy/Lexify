import type { Question } from '@/entities/test'
import type { FeedbackEntry } from './store'

/**
 * Contract every question renderer implements. After the user answers, the renderer STAYS mounted
 * and receives `feedback` so it can highlight the chosen/correct options inline; the page shows the
 * FeedbackBar (AnswerFeedback) below it. Adding a question type = one component with these props +
 * one entry in the page's RENDERERS map.
 */
export interface QuestionRendererProps {
  question: Question
  onSubmit: (answer: string) => void
  disabled: boolean
  feedback?: FeedbackEntry | null
}
