import { motion } from 'motion/react'
import { Minus, Plus } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { popIn } from '@/shared/ui'
import { useGenerateTestStore } from '../model/store'

const MIN = 5
const MAX = 50
const STEP = 5

export function QuestionCountStepper() {
  const { t } = useTranslation()
  const questionCount = useGenerateTestStore((s) => s.questionCount)
  const setQuestionCount = useGenerateTestStore((s) => s.setQuestionCount)

  const clamp = (value: number) => Math.min(MAX, Math.max(MIN, value))

  return (
    <div>
      <label className="lx-label mb-1.5 block">{t('testCreate.questionCount')}</label>
      <div className="flex items-center gap-3">
        <button
          type="button"
          aria-label={t('testCreate.fewerQuestions')}
          onClick={() => setQuestionCount(clamp(questionCount - STEP))}
          disabled={questionCount <= MIN}
          className="flex size-8 cursor-pointer items-center justify-center rounded-[var(--r-sm)] border border-[var(--line-2)] bg-[var(--bg-2)] text-[var(--fg-2)] transition-colors enabled:hover:border-[var(--accent-line)] enabled:hover:text-[var(--accent-color)] disabled:cursor-default disabled:opacity-40"
        >
          <Minus size={15} />
        </button>
        <motion.span
          key={questionCount}
          variants={popIn}
          initial="hidden"
          animate="visible"
          className="w-10 text-center text-xl font-bold text-[var(--fg-1)] [font-family:var(--font-display)]"
        >
          {questionCount}
        </motion.span>
        <button
          type="button"
          aria-label={t('testCreate.moreQuestions')}
          onClick={() => setQuestionCount(clamp(questionCount + STEP))}
          disabled={questionCount >= MAX}
          className="flex size-8 cursor-pointer items-center justify-center rounded-[var(--r-sm)] border border-[var(--line-2)] bg-[var(--bg-2)] text-[var(--fg-2)] transition-colors enabled:hover:border-[var(--accent-line)] enabled:hover:text-[var(--accent-color)] disabled:cursor-default disabled:opacity-40"
        >
          <Plus size={15} />
        </button>
        <span className="text-xs font-semibold text-[var(--fg-4)]">
          {MIN}–{MAX}
        </span>
      </div>
      <input
        type="range"
        min={MIN}
        max={MAX}
        step={STEP}
        value={questionCount}
        onChange={(e) => setQuestionCount(clamp(Number(e.target.value)))}
        aria-label={t('testCreate.questionCount')}
        className="mt-3 w-full accent-[var(--accent-color)]"
      />
    </div>
  )
}
