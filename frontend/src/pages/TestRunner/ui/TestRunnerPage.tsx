import { useEffect } from 'react'
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
  FillInSentenceQuestion,
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
  if (type === 'fill_in_sentence') {
    return (
      <FillInSentenceQuestion
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

  // Start the attempt once the test is ready. Guarded by the mutation's own idle state rather than
  // a ref: under StrictMode (and any remount) a ref that survives the remount would block the fresh
  // mutation observer from ever firing, leaving the spinner below stuck forever even though the
  // test is ready. `isIdle` fires exactly once per live observer and re-fires only if the observer
  // is genuinely reset (a real remount), so no duplicate attempts under StrictMode's double-invoke.
  useEffect(() => {
    if (!test || test.status !== 'ready') return
    if (startAttempt.isIdle) startAttempt.mutate(test.id)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [test?.id, test?.status, startAttempt.isIdle])

  // Populate the runner store from the mutation RESULT, not from a mutate()-level onSuccess
  // callback (those are dropped if the component unmounts before the response lands — exactly what
  // happens on a StrictMode remount or a backgrounded tab). Idempotent: keyed on the attempt id, so
  // it runs once the attempt exists and the ready test's questions are available.
  useEffect(() => {
    const aid = startAttempt.data?.attemptId
    if (!aid || questions.length > 0) return
    if (!test || test.status !== 'ready') return

    if (test.questions?.length) {
      init(test.id, aid, test.questions)
      return
    }
    // A ready test should always carry its questions; if the cached copy came back empty, fetch a
    // fresh one rather than init-ing with an empty list (which would spin forever).
    void queryClient
      .fetchQuery<Test>({
        queryKey: testKeys.detail(test.id),
        queryFn: () => testApi.getTestById(test.id),
      })
      .then((fresh) => {
        if (fresh.questions?.length) init(test.id, aid, fresh.questions)
      })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [startAttempt.data?.attemptId, questions.length, test?.id, test?.status])

  const handleRetryStart = () => {
    if (!test) return
    startAttempt.reset()
  }

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
  // Counted from feedbacks, not (index - correct): the index only advances on "Next", so right
  // after answering it still points at the current question and the difference goes negative.
  const incorrectCount = Object.values(feedbacks).filter((f) => !f.isCorrect).length
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

  // Starting the attempt failed — without this branch the "ready but no questions yet" spinner
  // below would spin forever. Retry resets the mutation to idle, which re-fires the start effect.
  if (startAttempt.isError && questions.length === 0) {
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
        <p className="ds-body" style={{ color: 'var(--danger)' }}>
          {t('testRunner.startFailed')}
        </p>
        <button className="lx-btn-primary" onClick={handleRetryStart}>
          {t('testRunner.retry')}
        </button>
        <Link
          to={ROUTES.TESTS}
          style={{ color: 'var(--accent-color)', textDecoration: 'none', fontWeight: 700 }}
        >
          {t('testRunner.backToTests')}
        </Link>
      </div>
    )
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
    <div style={{ maxWidth: 940, margin: '0 auto' }}>
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
            {t('testRunner.incorrect', { count: incorrectCount })}
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

          <div style={{ padding: 'clamp(16px, 5vw, 32px)' }}>
            {!currentFeedback ? (
              <QuestionView
                question={currentQuestion}
                onSubmit={(answer) => void handleSubmitAnswer(answer)}
                disabled={submitAnswer.isPending}
              />
            ) : (
              <div>
                <p
                  style={{ fontSize: 20, fontWeight: 500, color: 'var(--fg-1)', marginBottom: 16 }}
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
