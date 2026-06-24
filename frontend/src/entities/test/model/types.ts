export type QuestionType =
  | 'translate_to_native'
  | 'translate_to_foreign'
  | 'fill_in_sentence'
  | 'multi_select_theme'
  | 'open_answer'

export type TestStatus = 'generating' | 'ready' | 'archived'

export interface QuestionOption {
  id: string
  optionText: string
  sortOrder: number
}

export interface Question {
  id: string
  questionType: QuestionType
  questionText: string
  sortOrder: number
  options: QuestionOption[]
}

export interface Test {
  id: string
  title: string
  status: TestStatus
  questionCount: number | null
  createdAt: string
  questions: Question[]
}

export interface TestListItem {
  id: string
  title: string
  status: TestStatus
  questionCount: number | null
  createdAt: string
}

export interface AttemptAnswerDetail {
  questionId: string
  questionText: string
  questionType: string
  givenAnswer: string
  correctAnswer: string
  isCorrect: boolean
  timeSpentMs: number | null
}

export interface AttemptResult {
  attemptId: string
  testId: string
  startedAt: string
  finishedAt: string
  score: number
  totalQuestions: number
  correctAnswers: number
  answers: AttemptAnswerDetail[]
}

export interface GenerateTestInput {
  blockIds: string[]
  questionTypes: QuestionType[]
  questionCount: number
}

export interface SubmitAnswerInput {
  questionId: string
  givenAnswer: string
  timeSpentMs?: number
}

export interface AnswerFeedback {
  isCorrect: boolean
  correctAnswer: string
}
