import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { Spinner } from '@/shared/ui'
import { useGenerateTestMutation } from '@/entities/test'
import {
  useGenerateTestStore,
  useTestStatusPoller,
  BlockSelector,
  QuestionTypeSelector,
  EnglishLevelSelect,
} from '@/features/generate-test'

export function TestCreatePage() {
  const { t } = useTranslation()
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

  const generationFailed = polledStatus === 'failed'
  const isGenerating = !!generatingTestId && !generationFailed

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
          {t('testCreate.generating')}
        </div>
        <p className="ds-body" style={{ color: 'var(--fg-3)' }}>
          {t('testCreate.generatingDesc')}
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
        {t('testCreate.backToTests')}
      </Link>
      <h1 className="ds-h2" style={{ margin: '0 0 6px' }}>
        {t('testCreate.title')}
      </h1>
      <p className="ds-body" style={{ margin: '0 0 24px', color: 'var(--fg-3)' }}>
        {t('testCreate.subtitle')}
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
              {t('testCreate.blocks')}
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
              {t('testCreate.questionTypes')}
            </h2>
          </div>
          <QuestionTypeSelector />

          {/* Question count */}
          <div style={{ marginTop: 16 }}>
            <label className="lx-label" style={{ marginBottom: 6, display: 'block' }}>
              {t('testCreate.questionCount')}
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

          <EnglishLevelSelect />

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
                {t('testCreate.selected', { count: selectedBlockIds.length })}
              </span>
            </div>
          )}

          {generationFailed && (
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
              {t('testCreate.genFailed')}
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
              {t('testCreate.createFailed')}
            </div>
          )}

          <button
            className="lx-btn-primary"
            style={{ width: '100%', justifyContent: 'center', marginTop: 16 }}
            onClick={() => void handleGenerate()}
            disabled={!canGenerate || generateTest.isPending}
          >
            {t('testCreate.generate')}
          </button>
        </div>
      </div>
    </div>
  )
}
