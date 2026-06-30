import { useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { Spinner } from '@/shared/ui'
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
      <div
        style={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          minHeight: '60vh',
          gap: 20,
        }}
      >
        <Spinner size="lg" />
        <div className="ds-h3" style={{ color: 'var(--accent-color)' }}>
          Generating your test…
        </div>
        <p className="ds-body" style={{ color: 'var(--fg-3)' }}>
          AI is building questions — this may take a moment
        </p>
      </div>
    )
  }

  return (
    <div>
      {/* Header */}
      <Link
        to={ROUTES.TESTS}
        style={{
          color: 'var(--accent-color)',
          textDecoration: 'none',
          display: 'inline-block',
          marginBottom: 16,
          fontSize: 14,
          fontWeight: 700,
        }}
      >
        ← Back to tests
      </Link>
      <h1 className="ds-h2" style={{ margin: '0 0 6px' }}>
        Create a test
      </h1>
      <p className="ds-body" style={{ margin: '0 0 24px', color: 'var(--fg-3)' }}>
        Pick blocks and question types — AI generates the quiz.
      </p>

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
          gap: 20,
          alignItems: 'start',
        }}
      >
        {/* Blocks column */}
        <div>
          <div className="lx-section-head" style={{ marginBottom: 14 }}>
            <h2
              style={{
                margin: 0,
                fontFamily: 'var(--font-body)',
                fontWeight: 700,
                fontSize: 13,
                textTransform: 'uppercase',
                letterSpacing: '0.06em',
                color: 'var(--fg-2)',
              }}
            >
              Blocks
            </h2>
          </div>
          <BlockSelector />
        </div>

        {/* Question types column */}
        <div>
          <div className="lx-section-head" style={{ marginBottom: 14 }}>
            <h2
              style={{
                margin: 0,
                fontFamily: 'var(--font-body)',
                fontWeight: 700,
                fontSize: 13,
                textTransform: 'uppercase',
                letterSpacing: '0.06em',
                color: 'var(--fg-2)',
              }}
            >
              Question types
            </h2>
          </div>
          <QuestionTypeSelector />

          {/* Question count */}
          <div style={{ marginTop: 16 }}>
            <label className="lx-label" style={{ marginBottom: 6, display: 'block' }}>
              Number of questions
            </label>
            <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
              <input
                type="number"
                min={5}
                max={50}
                value={questionCount}
                onChange={(e) => setQuestionCount(Number(e.target.value))}
                className="lx-input"
                style={{ width: 100 }}
              />
              <span className="ds-sm" style={{ color: 'var(--fg-4)' }}>
                5–50
              </span>
            </div>
          </div>

          {/* Info banner */}
          {selectedBlockIds.length > 0 && (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 10,
                padding: '12px 16px',
                background: 'var(--accent-ghost)',
                border: '1px solid var(--accent-line)',
                borderRadius: 'var(--r-md)',
                marginTop: 16,
              }}
            >
              <span style={{ color: 'var(--accent-color)', fontSize: 15 }}>ℹ️</span>
              <span className="ds-sm" style={{ color: 'var(--fg-2)' }}>
                {selectedBlockIds.length} block{selectedBlockIds.length !== 1 ? 's' : ''} selected.
              </span>
            </div>
          )}

          {generateTest.isError && (
            <div
              style={{
                padding: '10px 14px',
                background: 'var(--danger-ghost)',
                border: '1px solid rgba(255,92,108,0.3)',
                borderRadius: 'var(--r-md)',
                color: 'var(--danger)',
                fontSize: 13,
                fontFamily: 'var(--font-body)',
                marginTop: 12,
              }}
            >
              Failed to create test. Make sure selected blocks have at least 5 words.
            </div>
          )}

          <button
            className="lx-btn-primary"
            style={{ width: '100%', justifyContent: 'center', marginTop: 16 }}
            onClick={() => void handleGenerate()}
            disabled={!canGenerate || generateTest.isPending}
          >
            ⚡ Generate
          </button>
        </div>
      </div>
    </div>
  )
}
