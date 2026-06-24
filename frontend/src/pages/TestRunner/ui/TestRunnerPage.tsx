import { useEffect, useRef } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { Button, Spinner } from '@/shared/ui'
import {
  useTest,
  useStartAttemptMutation,
  useSubmitAnswerMutation,
  useFinishAttemptMutation,
} from '@/entities/test'
import type { Question } from '@/entities/test'
import {
  useTestRunnerStore,
  TestProgressBar,
  AnswerFeedback,
  SingleChoiceQuestion,
  MultiSelectQuestion,
  OpenAnswerQuestion,
} from '@/features/run-test'

function QuestionView({
  question,
  onSubmit,
  disabled,
}: {
  question: Question
  onSubmit: (answer: string) => void
  disabled: boolean
}) {
  const type = question.questionType
  if (type === 'multi_select_theme') {
    return (
      <MultiSelectQuestion
        key={question.id}
        question={question}
        onSubmit={onSubmit}
        disabled={disabled}
      />
    )
  }
  if (type === 'open_answer') {
    return (
      <OpenAnswerQuestion
        key={question.id}
        question={question}
        onSubmit={onSubmit}
        disabled={disabled}
      />
    )
  }
  return (
    <SingleChoiceQuestion
      key={question.id}
      question={question}
      onSubmit={onSubmit}
      disabled={disabled}
    />
  )
}

export function TestRunnerPage() {
  const { id: testId } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const startedRef = useRef(false)

  const { data: test, isLoading, isError } = useTest(testId ?? '')

  const startAttempt = useStartAttemptMutation()
  const submitAnswer = useSubmitAnswerMutation()
  const finishAttempt = useFinishAttemptMutation()

  const {
    attemptId,
    questions,
    currentQuestionIndex,
    feedbacks,
    questionStartedAt,
    init,
    recordFeedback,
    nextQuestion,
    reset,
  } = useTestRunnerStore()

  // Start attempt once test is loaded and ready
  useEffect(() => {
    if (!test || test.status !== 'ready' || startedRef.current) return
    startedRef.current = true

    startAttempt.mutate(test.id, {
      onSuccess: ({ attemptId: aid }) => {
        init(test.id, aid, test.questions)
      },
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [test?.id, test?.status])

  // Cleanup on unmount
  useEffect(
    () => () => {
      reset()
    },
    [reset],
  )

  const currentQuestion = questions[currentQuestionIndex] ?? null
  const currentFeedback = currentQuestion ? feedbacks[currentQuestion.id] : null
  const correctCount = Object.values(feedbacks).filter((f) => f.isCorrect).length
  const isLastQuestion = currentQuestionIndex === questions.length - 1

  const handleSubmitAnswer = async (givenAnswer: string) => {
    if (!attemptId || !currentQuestion) return
    const timeSpentMs = questionStartedAt ? Date.now() - questionStartedAt : undefined

    const feedback = await submitAnswer.mutateAsync({
      attemptId,
      input: { questionId: currentQuestion.id, givenAnswer, timeSpentMs },
    })
    recordFeedback(currentQuestion.id, { ...feedback, givenAnswer })
  }

  const handleNext = async () => {
    if (!attemptId) return
    if (isLastQuestion) {
      await finishAttempt.mutateAsync(attemptId)
      navigate(ROUTES.TEST_RESULTS(attemptId))
    } else {
      nextQuestion()
    }
  }

  if (isLoading || startAttempt.isPending || (test?.status === 'ready' && questions.length === 0)) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Spinner size="lg" />
      </div>
    )
  }

  if (isError || !test) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-4">
        <p className="text-muted-foreground">Test not found.</p>
        <Link to={ROUTES.TESTS} className="text-sm text-primary hover:underline">
          Back to tests
        </Link>
      </div>
    )
  }

  if (test.status !== 'ready') {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-4">
        <p className="text-muted-foreground">This test is not ready yet (status: {test.status}).</p>
        <Link to={ROUTES.TESTS} className="text-sm text-primary hover:underline">
          Back to tests
        </Link>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="mx-auto max-w-2xl px-4 py-8">
        {/* Header */}
        <div className="mb-6">
          <Link
            to={ROUTES.TESTS}
            className="mb-2 inline-block text-sm text-muted-foreground hover:underline"
          >
            ← Tests
          </Link>
          <h1 className="text-xl font-bold">{test.title}</h1>
        </div>

        {/* Progress */}
        {questions.length > 0 && (
          <div className="mb-6">
            <TestProgressBar
              current={currentQuestionIndex + 1}
              total={questions.length}
              correctCount={correctCount}
            />
          </div>
        )}

        {/* Question card */}
        {currentQuestion && (
          <div className="rounded-lg border bg-card p-6 shadow-sm">
            <div className="mb-4">
              <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                {currentQuestion.questionType.replaceAll('_', ' ')}
              </span>
            </div>

            {!currentFeedback ? (
              <QuestionView
                question={currentQuestion}
                onSubmit={(answer) => void handleSubmitAnswer(answer)}
                disabled={submitAnswer.isPending}
              />
            ) : (
              <div className="space-y-4">
                <p className="text-base font-medium">{currentQuestion.questionText}</p>
                <AnswerFeedback
                  isCorrect={currentFeedback.isCorrect}
                  correctAnswer={currentFeedback.correctAnswer}
                  givenAnswer={currentFeedback.givenAnswer}
                  isLast={isLastQuestion}
                  onNext={() => void handleNext()}
                />
              </div>
            )}

            {finishAttempt.isPending && (
              <div className="mt-4 flex items-center gap-2">
                <Spinner size="sm" />
                <span className="text-sm text-muted-foreground">Finishing…</span>
              </div>
            )}
          </div>
        )}

        {/* Quit */}
        <div className="mt-4 flex justify-center">
          <Button
            variant="ghost"
            size="sm"
            className="text-muted-foreground"
            onClick={() => navigate(ROUTES.TESTS)}
          >
            Quit test
          </Button>
        </div>
      </div>
    </div>
  )
}
