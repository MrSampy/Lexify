import { useRef, useState } from 'react'
import { motion, Reorder } from 'motion/react'
import { Delete } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { staggerContainer, fadeInUp } from '@/shared/ui'
import { cn } from '@/lib/utils'
import type { QuestionRendererProps } from '../model/types'

/**
 * Letter-tile anagram: each option is one letter of the scrambled term (keyed by option id — the
 * same letter can appear twice). Tap letters to build the answer, tap a built letter (or Backspace)
 * to return it. Grading is exact (case-insensitive) on the server.
 */
export function WordScrambleQuestion({
  question,
  onSubmit,
  disabled,
  feedback,
}: QuestionRendererProps) {
  const { t } = useTranslation()
  const letters = [...question.options].sort((a, b) => a.sortOrder - b.sortOrder)
  const [usedIds, setUsedIds] = useState<string[]>([])
  // A drag ends with a click on the same tile; without this guard that click would remove the
  // letter the user just repositioned. Reset on pointer-down, set on drag start, checked on click.
  const draggedRef = useRef(false)
  const locked = disabled || !!feedback

  const assembled = usedIds.map((id) => letters.find((l) => l.id === id)?.optionText ?? '').join('')
  const complete = usedIds.length === letters.length

  const pick = (id: string) => setUsedIds((prev) => [...prev, id])
  const unpick = (id: string) => setUsedIds((prev) => prev.filter((x) => x !== id))

  return (
    <div>
      <p className="mb-6 text-xl leading-normal font-medium text-[var(--fg-1)]">
        {question.questionText}
      </p>

      {/* Assembled answer slots — tap a tile to return it, drag to reorder */}
      <Reorder.Group
        axis="x"
        values={usedIds}
        onReorder={(v) => !locked && setUsedIds(v)}
        className={cn(
          'mb-5 flex min-h-13 list-none flex-wrap items-center justify-center gap-1.5 rounded-[var(--r-md)] border border-dashed px-3 py-2',
          feedback
            ? feedback.isCorrect
              ? 'border-[var(--success)] bg-[var(--success-ghost)]'
              : 'border-[var(--danger)] bg-[var(--danger-ghost)]'
            : 'border-[var(--line-3)] bg-[var(--bg-3)]',
        )}
        aria-label={t('runTest.yourAnswer')}
      >
        {usedIds.length === 0 && (
          <span className="text-[13px] font-semibold text-[var(--fg-4)]">
            {t('runTest.scrambleHint')}
          </span>
        )}
        {usedIds.map((id) => {
          const letter = letters.find((l) => l.id === id)
          return (
            <Reorder.Item
              key={id}
              value={id}
              drag={locked ? false : 'x'}
              whileDrag={{ scale: 1.12, cursor: 'grabbing' }}
              onPointerDown={() => {
                draggedRef.current = false
              }}
              onDragStart={() => {
                draggedRef.current = true
              }}
              className={cn(
                'flex size-10 items-center justify-center rounded-[var(--r-sm)] border border-[var(--accent-line)] bg-[var(--accent-ghost)] text-lg font-bold text-[var(--fg-1)] select-none [font-family:var(--font-display)]',
                locked ? '' : 'cursor-grab',
              )}
              onClick={() => {
                if (locked) return
                // Ignore the click that terminates a drag — only a genuine tap returns the letter.
                if (draggedRef.current) {
                  draggedRef.current = false
                  return
                }
                unpick(id)
              }}
            >
              {letter?.optionText}
            </Reorder.Item>
          )
        })}
      </Reorder.Group>

      {/* Letter pool */}
      <motion.div
        variants={staggerContainer(0.03)}
        initial="hidden"
        animate="visible"
        className="mb-6 flex flex-wrap justify-center gap-1.5"
      >
        {letters.map((letter) => {
          const used = usedIds.includes(letter.id)
          return (
            <motion.button
              key={letter.id}
              type="button"
              variants={fadeInUp}
              whileHover={used ? undefined : { y: -2 }}
              whileTap={used ? undefined : { scale: 0.9 }}
              onClick={() => pick(letter.id)}
              disabled={disabled || !!feedback || used}
              className={cn(
                'flex size-10 items-center justify-center rounded-[var(--r-sm)] border text-lg font-bold [font-family:var(--font-display)] transition-colors',
                used
                  ? 'cursor-default border-[var(--line-1)] bg-[var(--bg-2)] text-[var(--fg-4)] opacity-35'
                  : 'cursor-pointer border-[var(--line-2)] bg-[var(--bg-3)] text-[var(--fg-1)] enabled:hover:border-[var(--accent-line)] enabled:hover:bg-[var(--accent-ghost)]',
              )}
            >
              {letter.optionText}
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
