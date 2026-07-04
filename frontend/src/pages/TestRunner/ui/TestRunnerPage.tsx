import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { ROUTES } from '@/shared/config'
import { Spinner } from '@/shared/ui'
import {
  useTest,
  useStartAttemptMutation,
  useSubmitAnswerMutation,
  useFinishAttemptMutation,
  testKeys,
  testApi,
} from '@/entities/test'
import type { Question, Test } from '@/entities/test'
import {
  useTestRunnerStore,
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
  const { t } = useTranslation()
  const { id: testId } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
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
      onSuccess: async ({ attemptId: aid }) => {
        // Fetch fresh test data so we always get the latest questions,
        // not the potentially stale closure value from when the effect ran
        const freshTest = await queryClient.fetchQuery<Test>({
          queryKey: testKeys.detail(test.id),
          queryFn: () => testApi.getTestById(test.id),
        })
        init(test.id, aid, freshTest.questions)
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
      <div style={{ display: 'flex', justifyContent: 'center', padding: '80px 0' }}>
        <Spinner size="lg" />
      </div>
    )
  }

  if (isError || !test) {
    return (
      <div
        style={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          gap: 16,
          padding: '80px 0',
          textAlign: 'center',
        }}
      >
        <p className="ds-body" style={{ color: 'var(--fg-3)' }}>
          {t('testRunner.notFound')}
        </p>
        <Link
          to={ROUTES.TESTS}
          style={{ color: 'var(--accent-color)', textDecoration: 'none', fontWeight: 700 }}
        >
          {t('testRunner.backToTests')}
        </Link>
      </div>
    )
  }

  if (test.status !== 'ready') {
    return (
      <div
        style={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          gap: 16,
          padding: '80px 0',
          textAlign: 'center',
        }}
      >
        <p className="ds-body" style={{ color: 'var(--fg-3)' }}>
          {t('testRunner.notReady')}{' '}
          <span style={{ color: 'var(--warning)', fontWeight: 700 }}>({test.status})</span>
        </p>
        <Link
          to={ROUTES.TESTS}
          style={{ color: 'var(--accent-color)', textDecoration: 'none', fontWeight: 700 }}
        >
          {t('testRunner.backToTests')}
        </Link>
      </div>
    )
  }

  const progress = questions.length > 0 ? (currentQuestionIndex / questions.length) * 100 : 0

  return (
    <div style={{ maxWidth: 640, margin: '0 auto' }}>
      {/* Header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          marginBottom: 16,
        }}
      >
        <h1 className="ds-h3" style={{ margin: 0 }}>
          {test.title}
        </h1>
        {questions.length > 0 && (
          <span style={{ color: 'var(--accent-color)', fontWeight: 700, fontSize: 14 }}>
            {currentQuestionIndex + 1} / {questions.length}
          </span>
        )}
      </div>

      {/* Progress bar */}
      {questions.length > 0 && (
        <div className="lx-progress-track" style={{ marginBottom: 24 }}>
          <div className="lx-progress-fill" style={{ width: `${progress}%` }} />
        </div>
      )}

      {/* Correct counter */}
      {questions.length > 0 && (
        <div style={{ display: 'flex', gap: 16, marginBottom: 20 }}>
          <span style={{ color: 'var(--success)', fontSize: 11, fontWeight: 700 }}>
            {t('testRunner.correct', { count: correctCount })}
          </span>
          <span style={{ color: 'var(--danger)', fontSize: 11, fontWeight: 700 }}>
            {t('testRunner.incorrect', { count: currentQuestionIndex - correctCount })}
          </span>
        </div>
      )}

      {/* Question card */}
      {currentQuestion && (
        <div
          style={{
            background: 'var(--bg-2)',
            border: '1px solid var(--line-2)',
            borderRadius: 'var(--r-lg)',
            overflow: 'hidden',
          }}
        >
          {/* Card header */}
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 10,
              padding: '10px 20px',
              background: 'var(--bg-3)',
              borderBottom: '1px solid var(--line-2)',
            }}
          >
            <span
              style={{
                fontFamily: 'var(--font-body)',
                fontWeight: 700,
                fontSize: 10,
                padding: '2px 10px',
                borderRadius: 'var(--r-pill)',
                background: 'var(--accent-ghost)',
                border: '1px solid var(--accent-line)',
                color: 'var(--accent-dim)',
                textTransform: 'uppercase',
                letterSpacing: '0.06em',
              }}
            >
              {currentQuestion.questionType.replaceAll('_', ' ')}
            </span>
          </div>

          <div style={{ padding: '24px 24px' }}>
            {!currentFeedback ? (
              <QuestionView
                question={currentQuestion}
                onSubmit={(answer) => void handleSubmitAnswer(answer)}
                disabled={submitAnswer.isPending}
              />
            ) : (
              <div>
                <p
                  style={{ fontSize: 16, fontWeight: 500, color: 'var(--fg-1)', marginBottom: 16 }}
                >
                  {currentQuestion.questionText}
                </p>
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
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 16 }}>
                <Spinner size="sm" />
                <span style={{ color: 'var(--fg-3)', fontSize: 13 }}>
                  {t('testRunner.finishing')}
                </span>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Quit */}
      <div style={{ marginTop: 14, display: 'flex', justifyContent: 'center' }}>
        <button
          style={{
            background: 'none',
            border: 'none',
            cursor: 'pointer',
            fontFamily: 'var(--font-body)',
            fontSize: 13,
            fontWeight: 600,
            color: 'var(--fg-4)',
            padding: '6px 12px',
          }}
          onClick={() => navigate(ROUTES.TESTS)}
        >
          {t('testRunner.quit')}
        </button>
      </div>
    </div>
  )
}
