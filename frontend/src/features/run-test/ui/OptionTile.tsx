import { motion } from 'motion/react'
import { Check, X } from 'lucide-react'
import { fadeInUp, popIn, shake } from '@/shared/ui'
import { cn } from '@/lib/utils'

export type OptionTileState = 'idle' | 'selected' | 'correct' | 'incorrect' | 'dimmed'

interface OptionTileProps {
  label: string
  state: OptionTileState
  onClick: () => void
  disabled?: boolean
  /** 0-based option position; renders a "1"-"9" keycap and makes the tile keyboard-targetable. */
  index?: number
  className?: string
}

const STATE_CLASSES: Record<OptionTileState, string> = {
  idle: 'border-[var(--line-2)] bg-[var(--bg-3)] text-[var(--fg-1)] enabled:hover:border-[var(--accent-line)] enabled:hover:bg-[var(--accent-ghost)]',
  selected: 'border-[var(--accent-color)] bg-[var(--accent-ghost)] text-[var(--fg-1)]',
  correct: 'border-[var(--success)] bg-[var(--success-ghost)] text-[var(--fg-1)]',
  incorrect: 'border-[var(--danger)] bg-[var(--danger-ghost)] text-[var(--fg-1)]',
  dimmed: 'border-[var(--line-2)] bg-[var(--bg-3)] text-[var(--fg-1)] opacity-45',
}

/**
 * The shared answer-option primitive for every choice-based renderer: staggered entrance (parent
 * must be a staggerContainer), hover lift, press squish, and post-answer result states — pop-in
 * check on the correct option, shake + X on a wrong pick. `data-option-index` is the hook the
 * keyboard-shortcut handler clicks through.
 */
export function OptionTile({ label, state, onClick, disabled, index, className }: OptionTileProps) {
  const isResult = state === 'correct' || state === 'incorrect'

  return (
    <motion.button
      type="button"
      variants={fadeInUp}
      whileHover={disabled ? undefined : { y: -2 }}
      whileTap={disabled ? undefined : { scale: 0.97 }}
      animate={state === 'incorrect' ? shake : undefined}
      onClick={onClick}
      disabled={disabled}
      data-option-index={index}
      className={cn(
        'relative rounded-[var(--r-md)] border px-5 py-4 text-center text-base leading-[1.4] break-words transition-colors duration-100 [font-family:var(--font-body)] enabled:cursor-pointer disabled:cursor-default',
        STATE_CLASSES[state],
        className,
      )}
    >
      {index !== undefined && index < 9 && !isResult && (
        <span className="absolute top-1.5 left-1.5 hidden size-4.5 items-center justify-center rounded-[4px] border border-[var(--line-2)] bg-[var(--bg-2)] text-[10px] font-bold text-[var(--fg-4)] md:flex">
          {index + 1}
        </span>
      )}
      {isResult && (
        <motion.span
          variants={popIn}
          initial="hidden"
          animate="visible"
          className={cn(
            'absolute -top-2 -right-2 flex size-5 items-center justify-center rounded-full text-white',
            state === 'correct' ? 'bg-[var(--success)]' : 'bg-[var(--danger)]',
          )}
        >
          {state === 'correct' ? (
            <Check size={12} strokeWidth={3} />
          ) : (
            <X size={12} strokeWidth={3} />
          )}
        </motion.span>
      )}
      {label}
    </motion.button>
  )
}
