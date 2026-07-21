import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { useGenerateTestMutation } from '@/entities/test'
import {
  useGenerateTestStore,
  useTestStatusPoller,
  BlockSelector,
  QuestionTypeSelector,
  TestSummaryPanel,
  GeneratingState,
} from '@/features/generate-test'

export function TestCreatePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  const selectedBlockIds = useGenerateTestStore((s) => s.selectedBlockIds)
  const questionTypes = useGenerateTestStore((s) => s.questionTypes)
  const questionCount = useGenerateTestStore((s) => s.questionCount)
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
  // 'ready' also stops the spinner: navigation in the effect above is the normal exit, but if it
  // ever stalls the page must fall back to the form rather than spin forever.
  const isGenerating = !!generatingTestId && !generationFailed && polledStatus !== 'ready'

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
    return <GeneratingState />
  }

  return (
    <div>
      <Link
        to={ROUTES.TESTS}
        className="mb-4 inline-block text-sm font-bold text-[var(--accent-color)] no-underline"
      >
        {t('testCreate.backToTests')}
      </Link>
      <h1 className="ds-h2 m-0 mb-1.5">{t('testCreate.title')}</h1>
      <p className="ds-body m-0 mb-6 text-[var(--fg-3)]">{t('testCreate.subtitle')}</p>

      <div className="grid grid-cols-1 items-start gap-6 lg:grid-cols-[1fr_320px]">
        <div className="flex min-w-0 flex-col gap-7">
          <section>
            <SectionHeading label={t('testCreate.blocks')} />
            <BlockSelector />
          </section>

          <section>
            <SectionHeading label={t('testCreate.questionTypes')} />
            <QuestionTypeSelector />
          </section>
        </div>

        <TestSummaryPanel
          canGenerate={canGenerate}
          isPending={generateTest.isPending}
          generationFailed={generationFailed}
          createFailed={generateTest.isError}
          onGenerate={() => void handleGenerate()}
        />
      </div>
    </div>
  )
}

function SectionHeading({ label }: { label: string }) {
  return (
    <div className="lx-section-head mb-3.5">
      <h2 className="m-0 text-[13px] font-bold tracking-[0.06em] text-[var(--fg-2)] uppercase [font-family:var(--font-body)]">
        {label}
      </h2>
    </div>
  )
}
