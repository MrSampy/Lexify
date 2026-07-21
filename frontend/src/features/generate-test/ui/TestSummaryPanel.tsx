import { motion } from 'motion/react'
import { Sparkles } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useBlocks } from '@/entities/block'
import { useGenerateTestStore } from '../model/store'
import { EnglishLevelSelect } from './EnglishLevelSelect'
import { QuestionCountStepper } from './QuestionCountStepper'

interface TestSummaryPanelProps {
  canGenerate: boolean
  isPending: boolean
  generationFailed: boolean
  createFailed: boolean
  onGenerate: () => void
}

/**
 * Sticky right-hand summary on desktop, regular flow section on mobile. Shows the live selection
 * totals, the count stepper + CEFR level, error banners, and the Generate CTA with a reason hint
 * when it's disabled.
 */
export function TestSummaryPanel({
  canGenerate,
  isPending,
  generationFailed,
  createFailed,
  onGenerate,
}: TestSummaryPanelProps) {
  const { t } = useTranslation()
  const selectedBlockIds = useGenerateTestStore((s) => s.selectedBlockIds)
  const questionTypes = useGenerateTestStore((s) => s.questionTypes)
  const questionCount = useGenerateTestStore((s) => s.questionCount)
  const { data } = useBlocks({ page: 1, pageSize: 100 })

  const wordTotal = (data?.items ?? [])
    .filter((b) => selectedBlockIds.includes(b.id))
    .reduce((sum, b) => sum + b.wordCount, 0)

  const disabledHint = !canGenerate
    ? selectedBlockIds.length === 0
      ? t('testCreate.needBlocks')
      : questionTypes.length === 0
        ? t('testCreate.needTypes')
        : null
    : null

  return (
    <div className="rounded-[var(--r-lg)] border border-[var(--line-2)] bg-[var(--bg-2)] p-5 shadow-[var(--shadow-1)] lg:sticky lg:top-6">
      <h2 className="eyebrow m-0 mb-4">{t('testCreate.summary')}</h2>

      <div className="mb-4 flex flex-wrap gap-2">
        <SummaryChip label={t('testCreate.summaryBlocks', { count: selectedBlockIds.length })} />
        <SummaryChip label={t('testCreate.summaryWords', { count: wordTotal })} />
        <SummaryChip label={t('testCreate.summaryQuestions', { count: questionCount })} />
      </div>

      <div className="flex flex-col gap-4 border-t border-[var(--line-1)] pt-4">
        <QuestionCountStepper />
        <EnglishLevelSelect />
      </div>

      {generationFailed && <ErrorBanner text={t('testCreate.genFailed')} />}
      {createFailed && <ErrorBanner text={t('testCreate.createFailed')} />}

      <motion.button
        whileTap={{ scale: 0.97 }}
        className="lx-btn-primary mt-5 w-full justify-center gap-2"
        onClick={onGenerate}
        disabled={!canGenerate || isPending}
      >
        <Sparkles size={16} />
        {t('testCreate.generate')}
      </motion.button>
      {disabledHint && (
        <p className="m-0 mt-2 text-center text-xs font-semibold text-[var(--fg-4)]">
          {disabledHint}
        </p>
      )}
    </div>
  )
}

function SummaryChip({ label }: { label: string }) {
  return (
    <span className="rounded-[var(--r-pill)] border border-[var(--accent-line)] bg-[var(--accent-ghost)] px-3 py-1 text-xs font-bold text-[var(--fg-2)]">
      {label}
    </span>
  )
}

function ErrorBanner({ text }: { text: string }) {
  return (
    <div className="mt-3 rounded-[var(--r-md)] border border-[rgba(255,92,108,0.3)] bg-[var(--danger-ghost)] px-3.5 py-2.5 text-[13px] text-[var(--danger)]">
      {text}
    </div>
  )
}
