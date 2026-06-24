import { useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { Button, Input, Spinner } from '@/shared/ui'
import { useGenerateTestMutation } from '@/entities/test'
import {
  useGenerateTestStore,
  useTestStatusPoller,
  BlockSelector,
  QuestionTypeSelector,
} from '@/features/generate-test'

export function TestCreatePage() {
  const navigate = useNavigate()

  const selectedBlockIds = useGenerateTestStore((s) => s.selectedBlockIds)
  const questionTypes = useGenerateTestStore((s) => s.questionTypes)
  const questionCount = useGenerateTestStore((s) => s.questionCount)
  const setQuestionCount = useGenerateTestStore((s) => s.setQuestionCount)
  const reset = useGenerateTestStore((s) => s.reset)

  const generateTest = useGenerateTestMutation()
  const generatingTestId = generateTest.data?.testId ?? null

  const { data: polledStatus } = useTestStatusPoller(generatingTestId)

  // Redirect to runner once test is ready
  useEffect(() => {
    if (polledStatus === 'ready' && generatingTestId) {
      reset()
      navigate(ROUTES.TEST_RUNNER(generatingTestId))
    }
  }, [polledStatus, generatingTestId, navigate, reset])

  const isGenerating = !!generatingTestId

  const canGenerate =
    selectedBlockIds.length > 0 &&
    questionTypes.length > 0 &&
    questionCount >= 5 &&
    questionCount <= 50

  const handleGenerate = async () => {
    if (!canGenerate) return
    await generateTest.mutateAsync({ blockIds: selectedBlockIds, questionTypes, questionCount })
  }

  if (isGenerating) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-4 bg-background">
        <Spinner size="lg" />
        <p className="text-lg font-medium">Generating your test…</p>
        <p className="text-sm text-muted-foreground">This may take a moment. Please wait.</p>
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
            ← Back to tests
          </Link>
          <h1 className="text-2xl font-bold">Create test</h1>
        </div>

        <div className="space-y-6">
          {/* Block selection */}
          <section className="rounded-lg border bg-card p-5 shadow-sm">
            <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-muted-foreground">
              Select blocks
            </h2>
            <BlockSelector />
            {selectedBlockIds.length > 0 && (
              <p className="mt-2 text-xs text-muted-foreground">
                {selectedBlockIds.length} block{selectedBlockIds.length !== 1 ? 's' : ''} selected
              </p>
            )}
          </section>

          {/* Question types */}
          <section className="rounded-lg border bg-card p-5 shadow-sm">
            <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-muted-foreground">
              Question types
            </h2>
            <QuestionTypeSelector />
          </section>

          {/* Question count */}
          <section className="rounded-lg border bg-card p-5 shadow-sm">
            <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-muted-foreground">
              Number of questions
            </h2>
            <div className="flex items-center gap-3">
              <Input
                type="number"
                min={5}
                max={50}
                value={questionCount}
                onChange={(e) => setQuestionCount(Number(e.target.value))}
                className="w-24"
              />
              <span className="text-sm text-muted-foreground">between 5 and 50</span>
            </div>
          </section>

          {/* Submit */}
          <div className="flex justify-end">
            <Button
              onClick={() => void handleGenerate()}
              disabled={!canGenerate || generateTest.isPending}
              size="lg"
            >
              Generate test
            </Button>
          </div>

          {generateTest.isError && (
            <p className="text-sm text-destructive">
              Failed to create test. Make sure selected blocks have at least 5 words.
            </p>
          )}
        </div>
      </div>
    </div>
  )
}
