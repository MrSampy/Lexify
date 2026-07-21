import { motion } from 'motion/react'
import {
  Check,
  Globe,
  Languages,
  Link2,
  ListChecks,
  PenLine,
  Puzzle,
  Shuffle,
  TextCursorInput,
  BookOpenText,
  Volume2,
  type LucideIcon,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import type { QuestionType } from '@/entities/test'
import { fadeInUp, popIn, staggerContainer } from '@/shared/ui'
import { cn } from '@/lib/utils'
import { useGenerateTestStore } from '../model/store'

/** One entry per question type — extend this array (plus i18n keys) when a new type ships. */
const TYPE_CONFIG: Array<{ type: QuestionType; icon: LucideIcon }> = [
  { type: 'translate_to_native', icon: Languages },
  { type: 'translate_to_foreign', icon: Globe },
  { type: 'fill_in_sentence', icon: TextCursorInput },
  { type: 'multi_select_theme', icon: ListChecks },
  { type: 'open_answer', icon: PenLine },
  { type: 'matching_pairs', icon: Link2 },
  { type: 'listen_and_type', icon: Volume2 },
  { type: 'word_scramble', icon: Shuffle },
  { type: 'sentence_builder', icon: Puzzle },
  { type: 'definition_match', icon: BookOpenText },
]

export function QuestionTypeSelector() {
  const { t } = useTranslation()
  const questionTypes = useGenerateTestStore((s) => s.questionTypes)
  const toggleQuestionType = useGenerateTestStore((s) => s.toggleQuestionType)

  return (
    <motion.div
      variants={staggerContainer(0.03)}
      initial="hidden"
      animate="visible"
      className="grid grid-cols-1 gap-2 sm:grid-cols-2"
    >
      {TYPE_CONFIG.map(({ type, icon: Icon }) => {
        const isSelected = questionTypes.includes(type)
        return (
          <motion.button
            key={type}
            type="button"
            variants={fadeInUp}
            whileHover={{ y: -2 }}
            whileTap={{ scale: 0.98 }}
            role="checkbox"
            aria-checked={isSelected}
            onClick={() => toggleQuestionType(type)}
            className={cn(
              'relative flex cursor-pointer items-center gap-3 rounded-[var(--r-md)] border p-3 pr-8 text-left transition-colors',
              isSelected
                ? 'border-[var(--accent-color)] bg-[var(--accent-ghost)]'
                : 'border-[var(--line-2)] bg-[var(--bg-2)] hover:border-[var(--accent-line)]',
            )}
          >
            <span
              className={cn(
                'flex size-9 shrink-0 items-center justify-center rounded-[var(--r-sm)]',
                isSelected
                  ? 'bg-[var(--accent-color)] text-white'
                  : 'bg-[var(--accent-ghost)] text-[var(--accent-color)]',
              )}
            >
              <Icon size={17} />
            </span>
            <span className="min-w-0">
              <span className="block truncate text-[13px] font-semibold text-[var(--fg-1)]">
                {t(`testCreate.types.${type}.label`)}
              </span>
              <span className="block truncate text-[11px] text-[var(--fg-3)]">
                {t(`testCreate.types.${type}.desc`)}
              </span>
            </span>
            {isSelected && (
              <motion.span
                variants={popIn}
                initial="hidden"
                animate="visible"
                className="absolute top-2 right-2 flex size-4 items-center justify-center rounded-full bg-[var(--accent-color)] text-white"
              >
                <Check size={10} strokeWidth={3} />
              </motion.span>
            )}
          </motion.button>
        )
      })}
    </motion.div>
  )
}
