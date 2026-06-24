import { Button } from '@/shared/ui'

interface AnswerFeedbackProps {
  isCorrect: boolean
  correctAnswer: string
  givenAnswer: string
  isLast: boolean
  onNext: () => void
}

export function AnswerFeedback({
  isCorrect,
  correctAnswer,
  givenAnswer,
  isLast,
  onNext,
}: AnswerFeedbackProps) {
  return (
    <div
      className={`rounded-lg border p-4 ${
        isCorrect
          ? 'border-green-200 bg-green-50 dark:bg-green-950/20'
          : 'border-red-200 bg-red-50 dark:bg-red-950/20'
      }`}
    >
      <p className={`mb-2 font-semibold ${isCorrect ? 'text-green-700' : 'text-red-700'}`}>
        {isCorrect ? 'Correct!' : 'Incorrect'}
      </p>
      {!isCorrect && (
        <div className="mb-3 space-y-1 text-sm">
          <p className="text-muted-foreground">
            Your answer: <span className="text-red-600">{givenAnswer || '—'}</span>
          </p>
          <p className="text-muted-foreground">
            Correct: <span className="font-medium text-green-700">{correctAnswer}</span>
          </p>
        </div>
      )}
      <Button size="sm" onClick={onNext}>
        {isLast ? 'Finish' : 'Next'}
      </Button>
    </div>
  )
}
