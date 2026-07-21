import { useEffect, type ComponentType } from 'react'
import { motion } from 'motion/react'
import { useTranslation } from 'react-i18next'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { ROUTES } from '@/shared/config'
import { Spinner, questionSlide } from '@/shared/ui'
import {
  useTest,
  useStartAttemptMutation,
  useSubmitAnswerMutation,
  useFinishAttemptMutation,
  testKeys,
  testApi,
} from '@/entities/test'
import type { Question, QuestionType, Test } from '@/entities/test'
import {
  useTestRunnerStore,
  useKeyboardShortcuts,
  AnswerFeedback,
  RunnerHud,
  SingleChoiceQuestion,
  MultiSelectQuestion,
  OpenAnswerQuestion,
  FillInSentenceQuestion,
  MatchingPairsQuestion,
  ListenAndTypeQuestion,
  WordScrambleQuestion,
  SentenceBuilderQuestion,
  type QuestionRendererProps,
} from '@/features/run-test'

/**
 * Adding a question type = one renderer with QuestionRendererProps + one entry here. Types without
 * an entry (translate_to_native/foreign, definition_match) fall back to SingleChoiceQuestion.
 */
const RENDERERS: Partial<Record<QuestionType, ComponentType<QuestionRendererProps>>> = {
  multi_select_theme: MultiSelectQuestion,
  open_answer: OpenAnswerQuestion,
  fill_in_sentence: FillInSentenceQuestion,
  matching_pairs: MatchingPairsQuestion,
  listen_and_type: ListenAndTypeQuestion,
  word_scramble: WordScrambleQuestion,
  sentence_builder: SentenceBuilderQuestion,
}

function QuestionView(props: QuestionRendererProps & { question: Question }) {
  const Renderer = RENDERERS[props.question.questionType] ?? SingleChoiceQuestion
  return <Renderer key={props.question.id} {...props} />
}

/** Trailing run of consecutive correct answers over the questions answered so far. */
function computeStreak(
  questions: Question[],
  feedbacks: Record<string, { isCorrect: boolean }>,
): number {
  let streak = 0
  for (const question of questions) {
    const feedback = feedbacks[question.id]
    if (!feedback) break
    streak = feedback.isCorrect ? streak + 1 : 0
  }
  return streak
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
  const currentFeedback = currentQuestion ? (feedbacks[currentQuestion.id] ?? null) : null
  const correctCount = Object.values(feedbacks).filter((f) => f.isCorrect).length
  // Counted from feedbacks, not (index - correct): the index only advances on "Next", so right
  // after answering it still points at the current question and the difference goes negative.
  const incorrectCount = Object.values(feedbacks).filter((f) => !f.isCorrect).length
  const streak = computeStreak(questions, feedbacks)
  const isLastQuestion = currentQuestionIndex === questions.length - 1

  useKeyboardShortcuts(!!currentQuestion)

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
      <CenteredMessage>
        <p className="ds-body m-0 text-[var(--danger)]">{t('testRunner.startFailed')}</p>
        <button className="lx-btn-primary" onClick={handleRetryStart}>
          {t('testRunner.retry')}
        </button>
        <BackToTests label={t('testRunner.backToTests')} />
      </CenteredMessage>
    )
  }

  if (isLoading || startAttempt.isPending || (test?.status === 'ready' && questions.length === 0)) {
    return (
      <div className="flex justify-center py-20">
        <Spinner size="lg" />
      </div>
    )
  }

  if (isError || !test) {
    return (
      <CenteredMessage>
        <p className="ds-body m-0 text-[var(--fg-3)]">{t('testRunner.notFound')}</p>
        <BackToTests label={t('testRunner.backToTests')} />
      </CenteredMessage>
    )
  }

  if (test.status !== 'ready') {
    return (
      <CenteredMessage>
        <p className="ds-body m-0 text-[var(--fg-3)]">
          {t('testRunner.notReady')}{' '}
          <span className="font-bold text-[var(--warning)]">({test.status})</span>
        </p>
        <BackToTests label={t('testRunner.backToTests')} />
      </CenteredMessage>
    )
  }

  return (
    <div className="mx-auto max-w-[720px]">
      <div className="mb-4 flex items-center justify-between gap-3">
        <h1 className="ds-h3 m-0 min-w-0 truncate">{test.title}</h1>
        {questions.length > 0 && (
          <span className="shrink-0 text-sm font-bold text-[var(--accent-color)]">
            {t('testRunner.question', { n: currentQuestionIndex + 1, total: questions.length })}
          </span>
        )}
      </div>

      {questions.length > 0 && (
        <RunnerHud
          current={currentQuestionIndex}
          total={questions.length}
          correctCount={correctCount}
          incorrectCount={incorrectCount}
          streak={streak}
        />
      )}

      {currentQuestion && (
        <div className="overflow-hidden rounded-[var(--r-lg)] border border-[var(--line-2)] bg-[var(--bg-2)] shadow-[var(--shadow-2)]">
          <div className="flex items-center gap-2.5 border-b border-[var(--line-2)] bg-[var(--bg-3)] px-5 py-2.5">
            <span className="rounded-[var(--r-pill)] border border-[var(--accent-line)] bg-[var(--accent-ghost)] px-2.5 py-0.5 text-[10px] font-bold tracking-[0.06em] text-[var(--accent-dim)] uppercase [font-family:var(--font-body)]">
              {t(`testCreate.types.${currentQuestion.questionType}.label`)}
            </span>
          </div>

          <div className="p-[clamp(16px,5vw,32px)]">
            {/* Keyed remount (no AnimatePresence): the new question slides in, the old one is
                dropped instantly — exit animations proved unreliable with mode="wait" here. */}
            <motion.div
              key={currentQuestion.id}
              variants={questionSlide}
              initial="enter"
              animate="center"
            >
              <QuestionView
                question={currentQuestion}
                onSubmit={(answer) => void handleSubmitAnswer(answer)}
                disabled={submitAnswer.isPending}
                feedback={currentFeedback}
              />

              {currentFeedback && (
                <AnswerFeedback
                  isCorrect={currentFeedback.isCorrect}
                  correctAnswer={currentFeedback.correctAnswer}
                  givenAnswer={currentFeedback.givenAnswer}
                  questionType={currentQuestion.questionType}
                  isLast={isLastQuestion}
                  onNext={() => void handleNext()}
                />
              )}
            </motion.div>

            {finishAttempt.isPending && (
              <div className="mt-4 flex items-center gap-2">
                <Spinner size="sm" />
                <span className="text-[13px] text-[var(--fg-3)]">{t('testRunner.finishing')}</span>
              </div>
            )}
          </div>
        </div>
      )}

      <p className="m-0 mt-3 hidden text-center text-[11px] font-semibold text-[var(--fg-4)] md:block">
        {t('testRunner.shortcutsHint')}
      </p>

      <div className="mt-3.5 flex justify-center">
        <button
          className="cursor-pointer border-none bg-transparent px-3 py-1.5 text-[13px] font-semibold text-[var(--fg-4)] [font-family:var(--font-body)] transition-colors hover:text-[var(--danger)]"
          onClick={() => navigate(ROUTES.TESTS)}
        >
          {t('testRunner.quit')}
        </button>
      </div>
    </div>
  )
}

function CenteredMessage({ children }: { children: React.ReactNode }) {
  return <div className="flex flex-col items-center gap-4 py-20 text-center">{children}</div>
}

function BackToTests({ label }: { label: string }) {
  return (
    <Link to={ROUTES.TESTS} className="font-bold text-[var(--accent-color)] no-underline">
      {label}
    </Link>
  )
}
