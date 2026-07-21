import { useRef, useState } from 'react'
import { motion, Reorder } from 'motion/react'
import { Delete } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { staggerContainer, fadeInUp } from '@/shared/ui'
import { cn } from '@/lib/utils'
import type { QuestionRendererProps } from '../model/types'

/**
 * Word-chip sentence builder: each option is one token of the original sentence in shuffled order.
 * Tap chips to add them, tap a placed chip to return it, and DRAG placed chips to reorder them.
 * The wire answer is the chips joined with spaces; the server grades word ORDER (case/whitespace/
 * final punctuation forgiven).
 */
export function SentenceBuilderQuestion({
  question,
  onSubmit,
  disabled,
  feedback,
}: QuestionRendererProps) {
  const { t } = useTranslation()
  const chips = [...question.options].sort((a, b) => a.sortOrder - b.sortOrder)
  const [usedIds, setUsedIds] = useState<string[]>([])
  // A drag ends with a click on the same chip; without this guard that click would remove the chip
  // the user just repositioned. Reset on pointer-down, set on drag start, checked on click.
  const draggedRef = useRef(false)

  const textOf = (id: string) => chips.find((c) => c.id === id)?.optionText ?? ''
  const assembled = usedIds.map(textOf).join(' ')
  const complete = usedIds.length === chips.length
  const locked = disabled || !!feedback

  return (
    <div>
      <p className="mb-1.5 text-xl leading-normal font-medium text-[var(--fg-1)]">
        {question.questionText}
      </p>
      <p className="mb-6 text-[13px] font-semibold text-[var(--fg-4)]">
        {t('runTest.builderHint')}
      </p>

      {/* Assembled sentence — drag to reorder */}
      <Reorder.Group
        axis="x"
        values={usedIds}
        onReorder={(v) => !locked && setUsedIds(v)}
        className={cn(
          'mb-5 flex min-h-14 list-none flex-wrap items-center gap-2 rounded-[var(--r-md)] border border-dashed px-3 py-2.5',
          feedback
            ? feedback.isCorrect
              ? 'border-[var(--success)] bg-[var(--success-ghost)]'
              : 'border-[var(--danger)] bg-[var(--danger-ghost)]'
            : 'border-[var(--line-3)] bg-[var(--bg-3)]',
        )}
      >
        {usedIds.length === 0 && (
          <span className="text-[13px] font-semibold text-[var(--fg-4)]">
            {t('runTest.builderEmpty')}
          </span>
        )}
        {usedIds.map((id) => (
          <Reorder.Item
            key={id}
            value={id}
            drag={locked ? false : 'x'}
            whileDrag={{ scale: 1.08, cursor: 'grabbing' }}
            onPointerDown={() => {
              draggedRef.current = false
            }}
            onDragStart={() => {
              draggedRef.current = true
            }}
            className={cn(
              'rounded-[var(--r-pill)] border border-[var(--accent-line)] bg-[var(--accent-ghost)] px-3.5 py-1.5 text-[15px] font-semibold text-[var(--fg-1)] select-none',
              locked ? '' : 'cursor-grab',
            )}
            onClick={() => {
              if (locked) return
              // Ignore the click that terminates a drag — only a genuine tap removes the chip.
              if (draggedRef.current) {
                draggedRef.current = false
                return
              }
              setUsedIds((prev) => prev.filter((x) => x !== id))
            }}
          >
            {textOf(id)}
          </Reorder.Item>
        ))}
      </Reorder.Group>

      {/* Chip pool */}
      <motion.div
        variants={staggerContainer(0.04)}
        initial="hidden"
        animate="visible"
        className="mb-6 flex flex-wrap gap-2"
      >
        {chips.map((chip) => {
          const used = usedIds.includes(chip.id)
          return (
            <motion.button
              key={chip.id}
              type="button"
              variants={fadeInUp}
              whileHover={used ? undefined : { y: -2 }}
              whileTap={used ? undefined : { scale: 0.94 }}
              onClick={() => setUsedIds((prev) => [...prev, chip.id])}
              disabled={locked || used}
              className={cn(
                'rounded-[var(--r-pill)] border px-3.5 py-1.5 text-[15px] font-semibold transition-colors',
                used
                  ? 'cursor-default border-[var(--line-1)] bg-[var(--bg-2)] text-[var(--fg-4)] opacity-35'
                  : 'cursor-pointer border-[var(--line-2)] bg-[var(--bg-3)] text-[var(--fg-1)] enabled:hover:border-[var(--accent-line)] enabled:hover:bg-[var(--accent-ghost)]',
              )}
            >
              {chip.optionText}
            </motion.button>
          )
        })}
      </motion.div>

      {!feedback && (
        <div className="flex items-center gap-2.5">
          <motion.button
            whileTap={{ scale: 0.97 }}
            className="lx-btn-primary px-6 py-2.5"
            onClick={() => complete && onSubmit(assembled)}
            disabled={disabled || !complete}
          >
            {t('runTest.check')}
          </motion.button>
          <button
            type="button"
            onClick={() => setUsedIds([])}
            disabled={disabled || usedIds.length === 0}
            className="flex cursor-pointer items-center gap-1.5 rounded-[var(--r-sm)] border-none bg-transparent px-3 py-2 text-[13px] font-semibold text-[var(--fg-4)] transition-colors enabled:hover:text-[var(--danger)] disabled:cursor-default disabled:opacity-40"
          >
            <Delete size={14} /> {t('runTest.clear')}
          </button>
        </div>
      )}
    </div>
  )
}
