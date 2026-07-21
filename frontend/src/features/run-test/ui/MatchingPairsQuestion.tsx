import { useState } from 'react'
import { motion } from 'motion/react'
import { useTranslation } from 'react-i18next'
import { staggerContainer } from '@/shared/ui'
import type { QuestionRendererProps } from '../model/types'
import { OptionTile, type OptionTileState } from './OptionTile'

/** Deterministic-enough client shuffle; runs once per mount via useState initializers. */
function shuffled<T>(items: T[]): T[] {
  const copy = [...items]
  for (let i = copy.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1))
    ;[copy[i], copy[j]] = [copy[j], copy[i]]
  }
  return copy
}

/**
 * Tap-to-pair matching: each option encodes a real "term|translation" pair; both columns are
 * shuffled independently. Tap a term, then a translation, to pair them (tapping a paired item
 * unpairs it). The wire answer joins pairs with ';' — order-insensitive on the server.
 */
export function MatchingPairsQuestion({
  question,
  onSubmit,
  disabled,
  feedback,
}: QuestionRendererProps) {
  const { t } = useTranslation()
  const pairsSource = question.options.map((o) => {
    const sep = o.optionText.indexOf('|')
    return { term: o.optionText.slice(0, sep), translation: o.optionText.slice(sep + 1) }
  })

  const [terms] = useState(() => shuffled(pairsSource.map((p) => p.term)))
  const [translations] = useState(() => shuffled(pairsSource.map((p) => p.translation)))
  const [selectedTerm, setSelectedTerm] = useState<string | null>(null)
  const [pairs, setPairs] = useState<Record<string, string>>({})

  const pairedTranslations = new Set(Object.values(pairs))
  const allPaired = Object.keys(pairs).length === pairsSource.length

  const correctSet = feedback
    ? new Set(question.options.map((o) => o.optionText.trim().toLowerCase()))
    : null

  const isPairCorrect = (term: string) =>
    correctSet?.has(`${term}|${pairs[term] ?? ''}`.trim().toLowerCase()) ?? false

  const handleTermClick = (term: string) => {
    if (pairs[term]) {
      setPairs((p) => Object.fromEntries(Object.entries(p).filter(([k]) => k !== term)))
      setSelectedTerm(null)
      return
    }
    setSelectedTerm((current) => (current === term ? null : term))
  }

  const handleTranslationClick = (translation: string) => {
    if (pairedTranslations.has(translation)) {
      setPairs((p) => Object.fromEntries(Object.entries(p).filter(([, v]) => v !== translation)))
      return
    }
    if (!selectedTerm) return
    setPairs((p) => ({ ...p, [selectedTerm]: translation }))
    setSelectedTerm(null)
  }

  const termState = (term: string): OptionTileState => {
    if (feedback) return isPairCorrect(term) ? 'correct' : 'incorrect'
    if (selectedTerm === term) return 'selected'
    if (pairs[term]) return 'dimmed'
    return 'idle'
  }

  const translationState = (translation: string): OptionTileState => {
    if (feedback) {
      const term = Object.keys(pairs).find((k) => pairs[k] === translation)
      return term && isPairCorrect(term) ? 'correct' : 'incorrect'
    }
    if (pairedTranslations.has(translation)) return 'dimmed'
    return 'idle'
  }

  /** 1-based badge shared by a paired term and its translation, so pairs are visually linked. */
  const pairNumber = (term: string) => Object.keys(pairs).indexOf(term)

  return (
    <div>
      <p className="mb-1.5 text-xl leading-normal font-medium text-[var(--fg-1)]">
        {t('runTest.matchTitle')}
      </p>
      <p className="mb-6 text-[13px] font-semibold text-[var(--fg-4)]">{t('runTest.matchHint')}</p>

      <motion.div
        variants={staggerContainer(0.05)}
        initial="hidden"
        animate="visible"
        className="grid grid-cols-2 gap-x-4 gap-y-2.5"
      >
        <div className="flex flex-col gap-2.5">
          {terms.map((term) => (
            <div key={term} className="relative">
              <OptionTile
                label={term}
                state={termState(term)}
                disabled={disabled || !!feedback}
                onClick={() => handleTermClick(term)}
                className="w-full py-3"
              />
              {pairs[term] && !feedback && <PairBadge n={pairNumber(term) + 1} />}
            </div>
          ))}
        </div>
        <div className="flex flex-col gap-2.5">
          {translations.map((translation) => {
            const term = Object.keys(pairs).find((k) => pairs[k] === translation)
            return (
              <div key={translation} className="relative">
                <OptionTile
                  label={translation}
                  state={translationState(translation)}
                  disabled={
                    disabled ||
                    !!feedback ||
                    (!selectedTerm && !pairedTranslations.has(translation))
                  }
                  onClick={() => handleTranslationClick(translation)}
                  className="w-full py-3"
                />
                {term && !feedback && <PairBadge n={pairNumber(term) + 1} />}
              </div>
            )
          })}
        </div>
      </motion.div>

      {!feedback && (
        <motion.button
          whileTap={{ scale: 0.97 }}
          className="lx-btn-primary mt-6 px-6 py-2.5"
          onClick={() =>
            onSubmit(
              Object.entries(pairs)
                .map(([term, translation]) => `${term}|${translation}`)
                .join(';'),
            )
          }
          disabled={disabled || !allPaired}
        >
          {t('runTest.check')}
        </motion.button>
      )}
    </div>
  )
}

function PairBadge({ n }: { n: number }) {
  return (
    <span className="absolute -top-1.5 -right-1.5 flex size-5 items-center justify-center rounded-full bg-[var(--accent-color)] text-[10px] font-bold text-white">
      {n}
    </span>
  )
}
