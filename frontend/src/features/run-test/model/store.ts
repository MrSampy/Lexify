import { create } from 'zustand'
import type { Question } from '@/entities/test'

interface FeedbackEntry {
  isCorrect: boolean
  correctAnswer: string
  givenAnswer: string
}

interface TestRunnerStore {
  testId: string | null
  attemptId: string | null
  questions: Question[]
  currentQuestionIndex: number
  feedbacks: Record<string, FeedbackEntry>
  isFinished: boolean
  questionStartedAt: number | null

  init: (testId: string, attemptId: string, questions: Question[]) => void
  recordFeedback: (questionId: string, entry: FeedbackEntry) => void
  nextQuestion: () => void
  markStarted: () => void
  finish: () => void
  reset: () => void
}

export const useTestRunnerStore = create<TestRunnerStore>((set) => ({
  testId: null,
  attemptId: null,
  questions: [],
  currentQuestionIndex: 0,
  feedbacks: {},
  isFinished: false,
  questionStartedAt: null,

  init: (testId, attemptId, questions) =>
    set({
      testId,
      attemptId,
      questions: [...questions].sort((a, b) => a.sortOrder - b.sortOrder),
      currentQuestionIndex: 0,
      feedbacks: {},
      isFinished: false,
      questionStartedAt: Date.now(),
    }),

  recordFeedback: (questionId, entry) =>
    set((s) => ({ feedbacks: { ...s.feedbacks, [questionId]: entry } })),

  nextQuestion: () =>
    set((s) => ({
      currentQuestionIndex: s.currentQuestionIndex + 1,
      questionStartedAt: Date.now(),
    })),

  markStarted: () => set({ questionStartedAt: Date.now() }),

  finish: () => set({ isFinished: true }),

  reset: () =>
    set({
      testId: null,
      attemptId: null,
      questions: [],
      currentQuestionIndex: 0,
      feedbacks: {},
      isFinished: false,
      questionStartedAt: null,
    }),
}))
