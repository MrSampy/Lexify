import { useEffect, useRef, useState } from 'react'
import { motion } from 'motion/react'
import { Loader2, Volume2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useSpeak } from '@/shared/lib'
import { cn } from '@/lib/utils'
import type { QuestionRendererProps } from '../model/types'

/**
 * Listening drill: the term arrives in question.audioText (never shown), gets spoken via TTS on
 * mount and on the replay button, and the user types what they heard. Falls back to showing the
 * text when no TTS engine can speak — the question must stay answerable.
 */
export function ListenAndTypeQuestion({
  question,
  onSubmit,
  disabled,
  feedback,
}: QuestionRendererProps) {
  const { t } = useTranslation()
  const [value, setValue] = useState('')
  const { speak, isLoading, ready, supported } = useSpeak({
    wordId: question.wordId,
    languageId: question.languageId,
  })
  const audioText = question.audioText ?? ''
  const autoPlayed = useRef(false)

  useEffect(() => {
    // Wait for `ready`: auto-playing before TTS capabilities settle speaks via the browser first
    // and then again via Piper once caps arrive — two overlapping voices.
    if (!ready || !supported || !audioText || autoPlayed.current) return
    autoPlayed.current = true
    void speak(audioText)
  }, [ready, supported, audioText, speak])

  const handleCheck = () => {
    const trimmed = value.trim()
    if (!trimmed) return
    onSubmit(trimmed)
  }

  return (
    <div>
      <p className="mb-6 text-xl leading-normal font-medium text-[var(--fg-1)]">
        {question.questionText}
      </p>

      {supported && audioText ? (
        <div className="mb-6 flex justify-center">
          <motion.button
            type="button"
            whileHover={{ scale: 1.06 }}
            whileTap={{ scale: 0.94 }}
            onClick={() => void speak(audioText)}
            disabled={isLoading}
            aria-label={t('runTest.replay')}
            title={t('runTest.replay')}
            className="flex size-20 cursor-pointer items-center justify-center rounded-full border-2 border-[var(--accent-line)] bg-[var(--accent-ghost)] text-[var(--accent-color)] shadow-[var(--glow-accent)] transition-colors hover:border-[var(--accent-color)]"
          >
            {isLoading ? <Loader2 size={30} className="animate-spin" /> : <Volume2 size={30} />}
          </motion.button>
        </div>
      ) : (
        <p className="mb-6 text-center">
          <span className="text-2xl font-bold text-[var(--fg-1)]">{audioText}</span>
          <span className="mt-1 block text-[11px] font-semibold text-[var(--warning)]">
            {t('runTest.ttsUnavailable')}
          </span>
        </p>
      )}

      <div className="flex gap-2.5">
        <input
          className={cn(
            'lx-input h-[50px] flex-1 text-[17px]',
            feedback && (feedback.isCorrect ? 'border-[var(--success)]' : 'border-[var(--danger)]'),
          )}
          value={value}
          onChange={(e) => setValue(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && handleCheck()}
          placeholder={t('runTest.listenPlaceholder')}
          disabled={disabled || !!feedback}
          autoFocus
        />
        {!feedback && (
          <motion.button
            whileTap={{ scale: 0.97 }}
            className="lx-btn-primary px-5"
            onClick={handleCheck}
            disabled={disabled || !value.trim()}
          >
            {t('runTest.check')}
          </motion.button>
        )}
      </div>
    </div>
  )
}
