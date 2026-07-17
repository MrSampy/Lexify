import { Volume2, Loader2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useSpeak } from '@/shared/lib'

interface SpeakButtonProps {
  text: string
  /** When set, server-side neural audio is used for this word (with browser TTS as fallback). */
  wordId?: string | null
  languageId?: number | null
  size?: number
  style?: React.CSSProperties
}

/**
 * Pronounces `text`. Uses server neural audio when available for the language, otherwise a browser
 * voice; renders nothing only when neither can speak. Shows a spinner while fetching server audio.
 */
export function SpeakButton({ text, wordId, languageId, size = 15, style }: SpeakButtonProps) {
  const { t } = useTranslation()
  const { speak, isLoading, supported } = useSpeak({ wordId, languageId })

  if (!supported) return null

  return (
    <button
      type="button"
      aria-label={t('common.speak')}
      title={t('common.speak')}
      disabled={isLoading}
      onClick={(e) => {
        e.stopPropagation()
        void speak(text)
      }}
      className="cursor-pointer border-none bg-transparent p-0 text-[var(--fg-4)] transition-colors duration-100 hover:text-[var(--accent-color)] disabled:cursor-default disabled:opacity-70"
      style={{ display: 'inline-flex', alignItems: 'center', ...style }}
    >
      {isLoading ? (
        <Loader2 className="animate-spin" style={{ width: size, height: size }} />
      ) : (
        <Volume2 style={{ width: size, height: size }} />
      )}
    </button>
  )
}
